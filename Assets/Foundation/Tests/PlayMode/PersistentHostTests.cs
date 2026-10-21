using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Idlebound.Foundation.Development;
using Object = UnityEngine.Object;

namespace Idlebound.Foundation.Tests
{
    public sealed class PersistentHostTests
    {
        private sealed class Clock : IUtcClock
        { public long Now = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc).Ticks; public long UtcTicks => Now; }
        private string directory;
        private GameObject root;
        private IDisposable isolation;
        private Clock clock;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(GameSessionHost.Instance,Is.Null);
            directory = Path.Combine(Path.GetTempPath(),"Idlebound-host-" + Guid.NewGuid().ToString("N")); clock = new Clock();
            yield return Open();
        }
        private IEnumerator Open()
        {
            isolation = GameSessionHost.IsolateForTests(directory,clock);
            root = new GameObject("Persistent host fixture"); root.AddComponent<IdleboundMobileUI>();
            for (int frame = 0; frame < 1200 && GameSessionHost.Instance.IsLoading; frame++) yield return null;
            Assert.That(GameSessionHost.Instance.IsLoading,Is.False); Assert.That(GameSessionHost.Instance.PersistenceError,Is.Null);
            GameSessionHost.Instance.SetRunning(false); yield return null;
        }
        private IEnumerator Close()
        { Object.Destroy(root); yield return null; isolation?.Dispose(); isolation = null; }
        [UnityTearDown] public IEnumerator Cleanup()
        { yield return Close(); if (Directory.Exists(directory)) Directory.Delete(directory,true); }
        private Button Button(string label) => root.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>()?.text == label);
        [UnityTest] public IEnumerator ColdHostRestoresBankAndNavigationCannotReplayOfflineTime()
        {
            var host = GameSessionHost.Instance; host.Model.Inspect("character-1"); host.Model.ShowSkill("mining"); host.Model.Start("mining-1");
            clock.Now += 5 * TimeSpan.TicksPerSecond; yield return null; host.Step(5); host.SaveCurrent();
            yield return Close(); yield return Open(); host = GameSessionHost.Instance;
            Assert.That(host.Runtime.Session.Quantity("metal-1"),Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<GameSessionHost>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            Button("Activities").onClick.Invoke(); yield return null;
            var session = host.Runtime.Session; long ticks = session.LastSettledTicks;
            Button("Bank").onClick.Invoke(); yield return null; Button("Back to Character").onClick.Invoke(); yield return null;
            Assert.That(host.Runtime.Session,Is.SameAs(session)); Assert.That(session.Quantity("metal-1"),Is.EqualTo(1)); Assert.That(session.LastSettledTicks,Is.EqualTo(ticks));
        }
        [UnityTest] public IEnumerator WelcomeContinueIsDurableAndOnlyOnePopupReturns()
        {
            var host = GameSessionHost.Instance; host.Model.Inspect("character-1"); host.Model.ShowSkill("mining"); host.Model.Start("mining-1"); host.SaveCurrent();
            yield return Close(); clock.Now += 30 * TimeSpan.TicksPerMinute; yield return Open(); host = GameSessionHost.Instance;
            Assert.That(host.Runtime.Session.Quantity("metal-1"),Is.EqualTo(360)); Assert.That(host.PendingReport,Is.Not.Null);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text == "WELCOME BACK!"),Is.True);
            Button("Continue").onClick.Invoke(); yield return null; Assert.That(host.PendingReport,Is.Null);
            yield return Close(); yield return Open();
            Assert.That(GameSessionHost.Instance.PendingReport,Is.Null); Assert.That(GameSessionHost.Instance.Runtime.Session.Quantity("metal-1"),Is.EqualTo(360));
        }
    }
}
