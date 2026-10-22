using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Idlebound.Foundation
{
    // V2 evidence always takes precedence; invalid newer saves can never replay obsolete v1 progress.
    public sealed class MigratingLocalSaveStore : ILocalSaveStore
    {
        private readonly AtomicLocalSaveStore current, legacy;
        private readonly Func<GatheringSession> legacyFactory;
        private readonly string currentCatalog;
        private readonly HashSet<string> compatibleLegacyCatalogs;
        public string Diagnostic => current.Diagnostic ?? legacy.Diagnostic;
        public MigratingLocalSaveStore(string directory,Func<GatheringSession> legacyFactory,string legacyCatalog,string currentCatalog,Action beforeCommit=null,IEnumerable<string> compatibleLegacyCatalogs=null)
        {
            current=new AtomicLocalSaveStore(Path.Combine(directory,"player-v2.json"),beforeCommit,2);
            legacy=new AtomicLocalSaveStore(Path.Combine(directory,"player-v1.json"));
            this.compatibleLegacyCatalogs=new HashSet<string>(compatibleLegacyCatalogs ?? new[]{legacyCatalog});
            this.legacyFactory=legacyFactory;this.currentCatalog=currentCatalog;
        }
        public SaveDocument Load(Action<SaveDocument> validate)
        {
            if(!File.Exists(current.Path) && !File.Exists(current.Backup) && File.Exists(current.Temporary))
            {
                // Recover a fully flushed first migration transaction; never replay v1 instead.
                if(new FileInfo(current.Temporary).Length>SaveCodec.MaximumBytes)throw new SaveCompatibilityException("Oversized pending migration; preserved.");
                var pending=SaveCodec.Decode(File.ReadAllText(current.Temporary),2);validate(pending);
                File.Move(current.Temporary,current.Path);
            }
            var newer=current.Load(validate); if(newer!=null)return newer;
            var old=legacy.Load(d=>
            {
                if(d==null || !compatibleLegacyCatalogs.Contains(d.Catalog))throw new SaveCompatibilityException("Unrecognized legacy catalog identity. Files preserved.");
                new LocalGame(legacy,legacyFactory,d.Catalog).Validate(d);
                if(d.State.Equipment.Any(e=>e.Enchantment!=null) || d.State.Characters.Any(c=>c.Enchant!=null || c.Attacks!=0 || c.Regen!=0))
                    throw new SaveCompatibilityException("Legacy save contains newer state. Preserved; migration blocked.");
            });
            if(old==null)return null;
            var converted=SaveCodec.Copy(old);converted.Catalog=currentCatalog;validate(converted);
            // Keep exact original bytes, including the backup, before any new-format commit.
            foreach(var source in new[]{legacy.Path,legacy.Backup})
                if(File.Exists(source))
                {
                    var archive=source+".pre-v2";
                    if(!File.Exists(archive))File.Copy(source,archive,false);
                    else if(!File.ReadAllBytes(archive).SequenceEqual(File.ReadAllBytes(source)))
                        throw new SaveCompatibilityException("Legacy archive differs from current v1 bytes. Preserved; inspect migration files.");
                }
            return converted;
        }
        public void Write(SaveDocument document) => current.Write(document);
    }
}
