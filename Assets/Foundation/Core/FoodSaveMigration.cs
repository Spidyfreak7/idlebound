using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Idlebound.Foundation
{
    // Reads v3 first. Older stores are validation-only and never written during this migration.
    public sealed class FoodSaveMigration : ILocalSaveStore
    {
        private readonly AtomicLocalSaveStore current;
        private readonly MigratingLocalSaveStore previous;
        private readonly Func<GatheringSession> oldFactory,newFactory;
        private readonly string oldCatalog,newCatalog,directory;
        public string Diagnostic=>current.Diagnostic ?? previous.Diagnostic;
        public FoodSaveMigration(string directory,Func<GatheringSession> v1Factory,Func<GatheringSession> v2Factory,Func<GatheringSession> v3Factory,
            string v1Catalog,string v2Catalog,string v3Catalog,IEnumerable<string> v1Identities=null,Action beforeCommit=null)
        {
            this.directory=directory;oldFactory=v2Factory;newFactory=v3Factory;oldCatalog=v2Catalog;newCatalog=v3Catalog;
            current=new AtomicLocalSaveStore(Path.Combine(directory,"player-v3.json"),beforeCommit,3);
            previous=new MigratingLocalSaveStore(directory,v1Factory,v1Catalog,v2Catalog,compatibleLegacyCatalogs:v1Identities);
        }
        public SaveDocument Load(Action<SaveDocument> validate)
        {
            Action<SaveDocument> strict=d=>
            {
                if(d?.State?.Characters==null || d.State.Characters.Any(c=>c?.Food==null))throw new ArgumentException("Missing v3 food settings.");
                validate(d);
            };
            if(!File.Exists(current.Path) && !File.Exists(current.Backup) && File.Exists(current.Temporary))
            {
                if(new FileInfo(current.Temporary).Length>SaveCodec.MaximumBytes)throw new SaveCompatibilityException("Oversized v3 temporary preserved.");
                var temp=SaveCodec.Decode(File.ReadAllText(current.Temporary),3);strict(temp);File.Move(current.Temporary,current.Path);
            }
            var latest=current.Load(strict);if(latest!=null)return latest;
            var old=previous.Load(d=>
            {
                new LocalGame(previous,oldFactory,oldCatalog).Validate(d);
                if(d.State.Characters.Any(c=>c.Food!=null) || d.Pending?.Characters.Any(c=>c.Consumed!=null)==true)
                    throw new SaveCompatibilityException("Older file contains newer food state; preserved for review.");
            });
            if(old==null)return null;
            var converted=SaveCodec.Copy(old);converted.Catalog=newCatalog;
            converted.State=GatheringSession.Restore(converted.State,newFactory()).Capture();strict(converted);
            foreach(var name in new[]{"player-v1.json","player-v1.json.bak","player-v2.json","player-v2.json.bak"})
            {
                var source=Path.Combine(directory,name);if(!File.Exists(source))continue;var archive=source+".pre-v3";
                if(!File.Exists(archive))File.Copy(source,archive,false);
                else if(!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(archive)))throw new SaveCompatibilityException("Conflicting archived save. Files preserved.");
            }
            return converted;
        }
        public void Write(SaveDocument document)=>current.Write(document);
    }
}
