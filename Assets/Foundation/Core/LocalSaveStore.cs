using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Idlebound.Foundation
{
    public sealed class SaveCompatibilityException : Exception
    { public SaveCompatibilityException(string message) : base(message) { } }
    public static class SaveCodec
    {
        public const int Version = 1;
        public const int MaximumBytes = 4 * 1024 * 1024;
        [DataContract] private sealed class Envelope
        {
            [DataMember] public int Version;
            [DataMember] public string Payload, Sha256;
        }
        public static string Hash(string text)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant(); }
        public static string Json<T>(T value)
        {
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        private static T Read<T>(string text)
        {
            if (text == null || Encoding.UTF8.GetByteCount(text) > MaximumBytes) throw new SerializationException("Save size limit exceeded.");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }
        public static string Encode(SaveDocument document, int version = Version)
        {
            string payload = Json(document);
            string result = Json(new Envelope { Version = version, Payload = payload, Sha256 = Hash(payload) });
            if (Encoding.UTF8.GetByteCount(result) > MaximumBytes) throw new SerializationException("Save size limit exceeded.");
            return result;
        }
        public static SaveDocument Decode(string text, int version = Version)
        {
            var envelope = Read<Envelope>(text);
            if (envelope == null) throw new SerializationException("Missing save envelope.");
            if (envelope.Version > version) throw new SaveCompatibilityException("Unsupported local save version " + envelope.Version + ". Files were preserved.");
            if (envelope.Version != version) throw new SerializationException("Missing or invalid save version.");
            if (envelope.Payload == null || envelope.Sha256 != Hash(envelope.Payload)) throw new SerializationException("Local save checksum mismatch.");
            return Read<SaveDocument>(envelope.Payload);
        }
        public static T Copy<T>(T value) => Read<T>(Json(value));
    }
    public interface ILocalSaveStore
    {
        SaveDocument Load(Action<SaveDocument> validate);
        void Write(SaveDocument document);
    }
    // File operations are deliberately separate from the simulation and UTC policy.
    public sealed class AtomicLocalSaveStore : ILocalSaveStore
    {
        public string Path { get; }
        public string Backup => Path + ".bak";
        public string Temporary => Path + ".tmp";
        public string Diagnostic { get; private set; }
        private bool invalidPrimary;
        private readonly Action beforeReplace;
        private readonly int version;
        public AtomicLocalSaveStore(string path, Action beforeReplace = null, int version = SaveCodec.Version)
        { Path = System.IO.Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path))); this.beforeReplace = beforeReplace; this.version=version; }
        private SaveDocument ReadFile(string file, Action<SaveDocument> validate)
        {
            if (new FileInfo(file).Length > SaveCodec.MaximumBytes) throw new SerializationException("Oversized save.");
            var document = SaveCodec.Decode(File.ReadAllText(file,Encoding.UTF8),version); validate(document); return document;
        }
        public SaveDocument Load(Action<SaveDocument> validate)
        {
            invalidPrimary = false; Diagnostic = null;
            Exception primaryError = null;
            if (File.Exists(Path))
            {
                try { return ReadFile(Path,validate); }
                catch (SaveCompatibilityException) { throw; }
                catch (Exception error) when (error is SerializationException || error is ArgumentException || error is FormatException || error is OverflowException)
                { primaryError = error; invalidPrimary = true; }
            }
            if (File.Exists(Backup))
            {
                var recovered = ReadFile(Backup,validate);
                Diagnostic = "Recovered last-known-good backup. Primary: " + (primaryError?.Message ?? "missing");
                return recovered;
            }
            if (primaryError != null) throw new SerializationException("Local save is invalid; no valid backup. Original preserved.",primaryError);
            if (File.Exists(Temporary) || File.Exists(Backup + ".tmp") || Directory.Exists(System.IO.Path.GetDirectoryName(Path)) &&
                Directory.GetFiles(System.IO.Path.GetDirectoryName(Path),System.IO.Path.GetFileName(Path) + "*.corrupt-*").Length > 0)
                throw new SerializationException("Interrupted/orphaned save found; recovery is required, not a new game.");
            return null;
        }
        private static void DurableWrite(string file, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using (var stream = new FileStream(file,FileMode.Create,FileAccess.Write,FileShare.None))
            { stream.Write(bytes,0,bytes.Length); stream.Flush(true); }
        }
        public void Write(SaveDocument document)
        {
            string text = SaveCodec.Encode(document,version); SaveCodec.Decode(text,version);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            // Never downgrade a future-version file, including a future-version backup.
            foreach (var existing in new[] { Path, Backup })
                if (File.Exists(existing))
                {
                    try { SaveCodec.Decode(File.ReadAllText(existing,Encoding.UTF8),version); }
                    catch (SaveCompatibilityException) { throw; }
                    catch (SerializationException)
                    {
                        if (existing == Path) invalidPrimary = true;
                        else File.Copy(existing,existing + ".corrupt-" + Guid.NewGuid().ToString("N"));
                    }
                }
            DurableWrite(Temporary,text);
            beforeReplace?.Invoke(); // Failure injection after flush, before the commit point.
            if (invalidPrimary && File.Exists(Path)) File.Move(Path,Path + ".corrupt-" + Guid.NewGuid().ToString("N"));
            if (File.Exists(Path)) File.Replace(Temporary,Path,Backup);
            else
            {
                File.Move(Temporary,Path);
                if (!File.Exists(Backup))
                { DurableWrite(Backup + ".tmp",text); File.Move(Backup + ".tmp",Backup); }
            }
            invalidPrimary = false;
        }
    }
}
