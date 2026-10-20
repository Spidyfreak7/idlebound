using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Idlebound.Foundation.Development;

namespace Idlebound.Foundation.Tests
{
    public sealed class MobileNavigationTests
    {
        private GameObject root;
        private Button Button(string text) => root.GetComponentsInChildren<Button>(true).First(b =>
            b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>() != null && b.GetComponentInChildren<Text>().text == text);
        [UnitySetUp]
        public IEnumerator Open()
        {
            Assert.That(GameSessionHost.Instance,Is.Null,"Run PlayMode tests in the Test Runner's isolated test scene.");
            root = new GameObject("Mobile UI test"); root.AddComponent<IdleboundMobileUI>(); yield return null;
        }
        [UnityTearDown]
        public IEnumerator Close() { Object.Destroy(root); yield return null; }
        [UnityTest]
        public IEnumerator CanvasAndSafeAreaInitializeWithOneClockHost()
        {
            Assert.That(root.GetComponentsInChildren<Canvas>().Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<GameSessionHost>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            var safe = root.GetComponentsInChildren<RectTransform>().First(t => t.name == "Safe Area");
            Assert.That(safe.anchorMin.x,Is.EqualTo(Screen.safeArea.xMin / Screen.width).Within(.001));
            Assert.That(safe.anchorMax.y,Is.EqualTo(Screen.safeArea.yMax / Screen.height).Within(.001));
            Assert.That(root.GetComponentsInChildren<ScrollRect>().Any(s => s.viewport != null && s.content != null && !s.horizontal),Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator MainNavigationButtonsReuseTheAuthoritativeSession()
        {
            var host = GameSessionHost.Instance; var session = host.Runtime.Session;
            Button("Bank").onClick.Invoke(); yield return null; Assert.That(host.Model.Screen,Is.EqualTo(MainScreen.Bank));
            Button("Shop").onClick.Invoke(); yield return null; Assert.That(host.Model.Screen,Is.EqualTo(MainScreen.Shop));
            Button("Collection\nLog").onClick.Invoke(); yield return null; Assert.That(host.Model.Screen,Is.EqualTo(MainScreen.CollectionLog));
            Button("Roster").onClick.Invoke(); yield return null; Assert.That(host.Runtime.Session,Is.SameAs(session));
            Assert.That(session.ActiveCharacterId,Is.EqualTo("character-1"));
        }
        [UnityTest]
        public IEnumerator InspectMakeActiveAndCharacterTabsUseRealCommands()
        {
            var host = GameSessionHost.Instance;
            Button("Inspect Character 2").onClick.Invoke(); yield return null;
            Assert.That(host.Model.InspectedId,Is.EqualTo("character-2")); Assert.That(host.Runtime.Session.ActiveCharacterId,Is.EqualTo("character-1"));
            Button("Make Active").onClick.Invoke(); yield return null; Assert.That(host.Runtime.Session.ActiveCharacterId,Is.EqualTo("character-2"));
            Button("Activities").onClick.Invoke(); yield return null;
            Assert.That(root.GetComponentsInChildren<ActivityStage>().Length,Is.Zero,"Activities must not keep the large animation stage.");
            Button("Equipment").onClick.Invoke(); yield return null;
            Assert.That(root.GetComponentsInChildren<ActivityStage>().Length,Is.Zero,"Equipment must use a static central loadout.");
        }
    }
}
