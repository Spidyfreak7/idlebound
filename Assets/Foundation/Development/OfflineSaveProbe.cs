#if UNITY_EDITOR
using System;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    // Editor-only, opt-in fixture. Never rewrites the real save or changes the device clock.
    [InitializeOnLoad]
    public static class OfflineSaveProbe
    {
        private const string PathKey = "Idlebound.OfflineProbe.Path", ClockKey = "Idlebound.OfflineProbe.Utc";
        private static IDisposable isolation;
        private sealed class Clock : IUtcClock
        {
            private readonly long start, utc;
            internal Clock(long value) { utc = value; start = DateTime.UtcNow.Ticks; }
            public long UtcTicks => checked(utc + Math.Max(0,DateTime.UtcNow.Ticks - start));
        }
        static OfflineSaveProbe()
        {
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.EnteredEditMode) { isolation?.Dispose(); isolation = null; } };
        }
        [MenuItem("Idlebound/Local save/Reveal real save folder")]
        private static void RevealSave()
        {
            string path = Path.Combine(Application.persistentDataPath,"Idlebound");
            if (Directory.Exists(path)) EditorUtility.RevealInFinder(path);
            else Debug.Log("The local save folder will be created on first launch: " + path);
        }
        [MenuItem("Idlebound/Offline probe/30 minutes (isolated save copy)")]
        private static void ThirtyMinutes() => Prepare(30 * TimeSpan.TicksPerMinute);
        [MenuItem("Idlebound/Offline probe/10 hours (isolated save copy)")]
        private static void TenHours() => Prepare(10 * TimeSpan.TicksPerHour);
        private static void Prepare(long away)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play mode before starting an offline probe."); return; }
            string source = Path.Combine(Application.persistentDataPath,"Idlebound","player-v1.json");
            try
            {
                if (!File.Exists(source)) throw new InvalidOperationException("Create and close a normal local player session first.");
                var copy = SaveCodec.Decode(File.ReadAllText(source));
                long now = DateTime.UtcNow.Ticks; copy.UtcTicks = now; copy.Pending = null;
                string directory = Path.Combine(Application.temporaryCachePath,"IdleboundOfflineProbe-" + Guid.NewGuid().ToString("N"));
                new AtomicLocalSaveStore(Path.Combine(directory,"player-v1.json")).Write(copy);
                SessionState.SetString(PathKey,directory); SessionState.SetString(ClockKey,checked(now + away).ToString(CultureInfo.InvariantCulture));
                Debug.Log("Idlebound offline probe uses isolated copy: " + directory + ". Original player save is untouched.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { Debug.LogException(error); }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeginProbe()
        {
            string directory = SessionState.GetString(PathKey,""); if (directory.Length == 0) return;
            string utc = SessionState.GetString(ClockKey,""); SessionState.EraseString(PathKey); SessionState.EraseString(ClockKey);
            isolation = GameSessionHost.IsolateForTests(directory,new Clock(long.Parse(utc,CultureInfo.InvariantCulture)));
        }
    }
}
#endif
