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
        private System.IDisposable isolation;
        private Button Button(string text) => root.GetComponentsInChildren<Button>(true).First(b =>
            b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>() != null && b.GetComponentInChildren<Text>().text == text);
        private Button ActivityStart(string id) => root.GetComponentsInChildren<Button>().Single(b => b.name == "Activity start: " + id);
        [UnitySetUp]
        public IEnumerator Open()
        {
            Assert.That(GameSessionHost.Instance,Is.Null,"Run PlayMode tests in the Test Runner's isolated test scene.");
            isolation = GameSessionHost.IsolateForTests();
            root = new GameObject("Mobile UI test"); root.AddComponent<IdleboundMobileUI>(); yield return null;
        }
        [UnityTearDown]
        public IEnumerator Close() { Object.Destroy(root); yield return null; isolation?.Dispose(); }
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
            Assert.That(root.GetComponentsInChildren<ActivityStage>().Length,Is.EqualTo(1),"Activities defaults to the pinned live scene.");
            Button("Equipment").onClick.Invoke(); yield return null;
            Assert.That(root.GetComponentsInChildren<ActivityStage>().Length,Is.EqualTo(1),"Equipment keeps the pinned live scene above its static loadout.");
        }
        private static Rect BoundsIn(RectTransform child,RectTransform parent)
        {
            var corners = new Vector3[4]; child.GetWorldCorners(corners);
            var min = parent.InverseTransformPoint(corners[0]); var max = parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        [UnityTest]
        public IEnumerator CompactEquipmentSlotsStayClearAndAllTenOpenDetails()
        {
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            Button("Equipment").onClick.Invoke(); yield return null;
            var canvas = root.GetComponentInChildren<CanvasScaler>();
            foreach (float referenceWidth in new[] { 304f,384f,480f })
            {
                canvas.referenceResolution = new Vector2(referenceWidth,854); canvas.matchWidthOrHeight = 0;
                yield return null; Canvas.ForceUpdateCanvases(); yield return null;
                var panel = root.GetComponentsInChildren<RectTransform>().First(r => r.name == "Ten-slot loadout");
                var character = root.GetComponentsInChildren<RectTransform>().First(r => r.name == "Character");
                var slots = panel.GetComponentsInChildren<Button>(); Assert.That(slots.Length,Is.EqualTo(10));
                var heroBounds = BoundsIn(character,panel);
                foreach (var slot in slots)
                {
                    var rect = BoundsIn((RectTransform)slot.transform,panel);
                    Assert.That(rect.width,Is.EqualTo(rect.height).Within(.01));
                    Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(panel.rect.xMin - .1f)); Assert.That(rect.xMax,Is.LessThanOrEqualTo(panel.rect.xMax + .1f));
                    Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(panel.rect.yMin - .1f)); Assert.That(rect.yMax,Is.LessThanOrEqualTo(panel.rect.yMax + .1f));
                    Assert.That(rect.Overlaps(heroBounds),Is.False,slot.name + " covers the character.");
                    Assert.That(slot.GetComponentsInChildren<Text>().Length,Is.Zero,"Slot labels belong in details, not over the portrait.");
                    foreach (var other in slots.Where(b => b != slot)) Assert.That(rect.Overlaps(BoundsIn((RectTransform)other.transform,panel)),Is.False);
                }
            }
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                root.GetComponentsInChildren<Button>().Single(b => b.name == "Slot " + slot).onClick.Invoke(); yield return null;
                Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text == slot.ToString()),Is.True);
                Button("Close").onClick.Invoke(); yield return null;
            }
            Assert.That(root.GetComponentsInChildren<ActivityStage>().Length,Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator CharacterPagesStartWithTheLiveHeaderWithoutTitleBannerOrGlobalStop()
        {
            var host = GameSessionHost.Instance;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            var session = host.Runtime.Session; long time = session.LastSettledTicks;
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            foreach (var tab in new[] { "Overview","Equipment","Activities" })
            {
                Button(tab).onClick.Invoke(); yield return null;
                var safe = root.GetComponentsInChildren<RectTransform>().Single(r => r.name == "Safe Area");
                var first = safe.Cast<Transform>().First(t => t.gameObject.activeInHierarchy);
                Assert.That(first.name,Is.EqualTo("Persistent activity header"));
                Assert.That(first.GetComponentInChildren<ActivityStage>(),Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text == "IDLEBOUND" || t.text.Contains("progress is not saved")),Is.False);
                Assert.That(root.GetComponentsInChildren<Button>().Any(b => b.GetComponentInChildren<Text>()?.text == "Stop / cancel current assignment"),Is.False);
            }
            Assert.That(host.Runtime.Session,Is.SameAs(session)); Assert.That(session.LastSettledTicks,Is.EqualTo(time));
            Assert.That(session.Inventory,Is.Empty); Assert.That(session.Assignments,Is.Empty);
        }
        [UnityTest]
        public IEnumerator ProductionCancellationBelongsToItsDirectlyActionableActivityCard()
        {
            var host = GameSessionHost.Instance;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            host.Model.ShowSkill("mining"); host.Model.Start("mining-1"); host.Step(10);
            Button("Activities").onClick.Invoke(); yield return null;
            Button("Skill: Mining — change").onClick.Invoke(); yield return null;
            Button("Smithing").onClick.Invoke(); yield return null;
            ActivityStart("bronze-dagger").onClick.Invoke(); yield return null;
            var session = host.Runtime.Session; long time = session.LastSettledTicks;
            var cancel = Button("Cancel job");
            Assert.That(cancel.transform.parent.parent.GetComponentsInChildren<Button>().Any(b => b.name == "Activity start: bronze-dagger"),Is.True);
            Assert.That(((RectTransform)cancel.transform).rect.height,Is.LessThanOrEqualTo(48.1f));
            cancel.onClick.Invoke(); yield return null;
            Assert.That(session.ProductionJobs,Is.Empty); Assert.That(session.Quantity("metal-1"),Is.EqualTo(2));
            Assert.That(session.Quantity("bronze-dagger-item"),Is.Zero); Assert.That(session.LastSettledTicks,Is.EqualTo(time));
        }
        [UnityTest]
        public IEnumerator ActivityCardsExposeStartRequirementsAndLocksWithoutExpansion()
        {
            var host = GameSessionHost.Instance;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            var session = host.Runtime.Session;
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            Button("Activities").onClick.Invoke(); yield return null;
            Assert.That(session.Assignments,Is.Empty,"Opening Activities must not start work.");
            Assert.That(root.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Activity start: ")),Is.EqualTo(10));
            var locked = ActivityStart("mining-10"); Assert.That(locked.interactable,Is.False);
            Assert.That(locked.GetComponentInChildren<Text>().text,Is.EqualTo("Locked"));
            locked.onClick.Invoke(); yield return null; Assert.That(session.Assignments,Is.Empty);
            Assert.That(ActivityStart("mining-1").interactable,Is.True);
            ActivityStart("mining-1").onClick.Invoke(); yield return null;
            Assert.That(session.Assignments["character-1"],Is.EqualTo("mining-1"));
            Assert.That(session.Inventory,Is.Empty); host.Step(5);
            Button("Skill: Mining — change").onClick.Invoke(); yield return null;
            Button("Smithing").onClick.Invoke(); yield return null;
            var start = ActivityStart("bronze-dagger"); Assert.That(start.interactable,Is.False,"One Bronze cannot fund a two-Bronze recipe.");
            var card = start.transform.parent.parent;
            Assert.That(card.GetComponentsInChildren<Text>().Any(t => t.text.Contains("Needs 2 Bronze") && t.text.Contains("bank 1")),Is.True);
            Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text.Contains("Starter balance") || t.text.Contains("one job per Start") || t.text.Contains("Development recipe")),Is.False);
            host.Step(5); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(ActivityStart("bronze-dagger").interactable,Is.True);
            ActivityStart("bronze-dagger").onClick.Invoke(); yield return null;
            Assert.That(session.Quantity("metal-1"),Is.Zero); Assert.That(session.ProductionJobs.ContainsKey("character-1"),Is.True);
            Assert.That(Button("Cancel job"),Is.Not.Null);
            host.Step(15); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(session.Quantity("bronze-dagger-item"),Is.EqualTo(1));
            Button("Bank").onClick.Invoke(); yield return null;
            Button("Back to Character").onClick.Invoke(); yield return null;
            Assert.That(host.Model.SkillFilter,Is.EqualTo("smithing")); Assert.That(host.Model.CharacterTab,Is.EqualTo(CharacterScreen.Activities));
            Assert.That(session.Quantity("bronze-dagger-item"),Is.EqualTo(1)); Assert.That(session.ProductionJobs,Is.Empty);
        }
        [UnityTest]
        public IEnumerator BankGearNamesAndItemDetailsHideDevelopmentPrefixes()
        {
            var host = GameSessionHost.Instance;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            var session = host.Runtime.Session; session.GrantDevelopmentGearKitAt(session.LastSettledTicks);
            Button("Bank").onClick.Invoke(); yield return null;
            var item = host.Model.Bank().First(i => session.BankItems[i.Id].StartsWith("[DEV]"));
            var tile = root.GetComponentsInChildren<Button>().Single(b => b.name == "Bank item: " + item.Id);
            Assert.That(tile.GetComponentsInChildren<Text>().Single(t => t.name == "Item name").text,Is.EqualTo(PlayerNames.Display(session.BankItems[item.Id])));
            tile.onClick.Invoke(); yield return null;
            Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text.Contains("[DEV]")),Is.False);
            Assert.That(session.BankItems[item.Id],Does.StartWith("[DEV]"));
            Assert.That(host.Runtime.Session,Is.SameAs(session)); Assert.That(session.Quantity(item.Id),Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator BankTilesShowCatalogNamesAndRefreshQuantitiesWithoutResettingSession()
        {
            var host = GameSessionHost.Instance; var session = host.Runtime.Session;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            var scaler = root.GetComponentInChildren<CanvasScaler>(); scaler.referenceResolution = new Vector2(480,854); scaler.matchWidthOrHeight = 0;
            host.Model.Inspect("character-1"); host.Model.ShowSkill("mining"); Assert.That(host.Model.Start("mining-1"),Is.True);
            host.Step(5); Button("Bank").onClick.Invoke(); yield return null;
            var tile = root.GetComponentsInChildren<Button>().Single(b => b.name == "Bank item: metal-1");
            var texts = tile.GetComponentsInChildren<Text>();
            Assert.That(texts.Single(t => t.name == "Item name").text,Is.EqualTo(PlayerNames.Display(session.BankItems["metal-1"])));
            Assert.That(texts.Single(t => t.name == "Quantity").text,Is.EqualTo("× 1"));
            host.Step(5); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(texts.Single(t => t.name == "Quantity").text,Is.EqualTo("× 2"));
            tile.onClick.Invoke(); yield return null;
            Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text == PlayerNames.Display(session.BankItems["metal-1"])),Is.True);
            Button("Close").onClick.Invoke(); yield return null;
            session.GrantDevelopmentGearKitAt(session.LastSettledTicks); yield return new WaitForSecondsRealtime(.15f);
            var longest = host.Model.Bank().OrderByDescending(i => i.Name.Length).First();
            var longTile = root.GetComponentsInChildren<Button>().Single(b => b.name == "Bank item: " + longest.Id);
            var name = longTile.GetComponentsInChildren<Text>().Single(t => t.name == "Item name"); Canvas.ForceUpdateCanvases();
            Assert.That(name.text,Is.EqualTo(longest.Name)); Assert.That(name.horizontalOverflow,Is.EqualTo(HorizontalWrapMode.Wrap));
            Assert.That(name.preferredHeight,Is.LessThanOrEqualTo(name.rectTransform.rect.height + 1),"Catalog name should fit without clipping.");
            Assert.That(host.Runtime.Session,Is.SameAs(session)); Assert.That(session.Quantity("metal-1"),Is.EqualTo(2));
            Assert.That(session.Assignments["character-1"],Is.EqualTo("mining-1"));
        }
        [UnityTest]
        public IEnumerator PersistentHeaderDefaultsAndToggleRemainSeparateFromGameplayActions()
        {
            var host = GameSessionHost.Instance;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            var session = host.Runtime.Session;
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            var header = root.GetComponentsInChildren<RectTransform>().Single(r => r.name == "Persistent activity header");
            Assert.That(header.GetComponentInParent<ScrollRect>(),Is.Null,"The header must stay above scrolling content.");
            Assert.That(header.GetComponentInChildren<ActivityStage>(),Is.Not.Null,"Overview defaults expanded.");
            Assert.That(root.GetComponentsInChildren<Button>().Any(b => b.GetComponentInChildren<Text>() != null &&
                new[] { "Open activity scene","Expand activity scene","Collapse activity scene" }.Contains(b.GetComponentInChildren<Text>().text)),Is.False);
            Button("Activities").onClick.Invoke(); yield return null;
            Assert.That(header.GetComponentsInChildren<ActivityStage>().Length,Is.EqualTo(1)); Assert.That(host.Model.SceneCollapsed,Is.False);
            var toggle = header.GetComponentsInChildren<Button>().Single(b => b.name == "Activity header toggle");
            Assert.That(((RectTransform)toggle.transform).rect.width,Is.LessThanOrEqualTo(48.1f));
            long time = session.LastSettledTicks; toggle.onClick.Invoke(); yield return null;
            Assert.That(header.GetComponentInChildren<ActivityStage>(),Is.Null);
            Assert.That(session.Assignments,Is.Empty); Assert.That(session.Inventory,Is.Empty); Assert.That(session.LastSettledTicks,Is.EqualTo(time));
            header.GetComponentsInChildren<Button>().Single(b => b.name == "Activity header toggle").onClick.Invoke(); yield return null;
            host.Model.ShowSkill("mining"); Assert.That(host.Model.Start("mining-1"),Is.True); host.Step(5);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(header.GetComponentsInChildren<Text>().Any(t => t.text.Contains("Mining")),Is.True);
            Button("Equipment").onClick.Invoke(); yield return null;
            Assert.That(host.Model.SceneCollapsed,Is.False); Assert.That(header.GetComponentsInChildren<ActivityStage>().Length,Is.EqualTo(1));
            Assert.That(header.GetComponentsInChildren<Text>().Any(t => t.text.Contains("Character 1") && t.text.Contains("ACTIVE")),Is.True);
            Assert.That(session.Assignments["character-1"],Is.EqualTo("mining-1")); Assert.That(session.Quantity("metal-1"),Is.EqualTo(1));
            Button("Overview").onClick.Invoke(); yield return null;
            Assert.That(host.Model.SceneCollapsed,Is.False); Assert.That(header.GetComponentInChildren<ActivityStage>(),Is.Not.Null);
            Button("Bank").onClick.Invoke(); yield return null; Assert.That(header.gameObject.activeInHierarchy,Is.False);
            Assert.That(host.Runtime.Session,Is.SameAs(session));
        }
        [UnityTest]
        public IEnumerator HeaderToggleDoesNotStartCancelOrDuplicateAProductionJob()
        {
            var host = GameSessionHost.Instance;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            host.Model.ShowSkill("mining"); host.Model.Start("mining-1"); host.Step(10);
            Button("Activities").onClick.Invoke(); yield return null;
            Button("Skill: Mining — change").onClick.Invoke(); yield return null;
            Button("Smithing").onClick.Invoke(); yield return null;
            var session = host.Runtime.Session;
            for (int i = 0; i < 2; i++)
            { root.GetComponentsInChildren<Button>().Single(b => b.name == "Activity header toggle").onClick.Invoke(); yield return null; }
            Assert.That(session.ProductionJobs,Is.Empty); Assert.That(session.Quantity("metal-1"),Is.EqualTo(2));
            ActivityStart("bronze-dagger").onClick.Invoke(); yield return null;
            Assert.That(session.ProductionJobs.ContainsKey("character-1"),Is.True); Assert.That(session.Quantity("metal-1"),Is.Zero);
            for (int i = 0; i < 2; i++)
            { root.GetComponentsInChildren<Button>().Single(b => b.name == "Activity header toggle").onClick.Invoke(); yield return null; }
            Assert.That(session.ProductionJobs.ContainsKey("character-1"),Is.True); Assert.That(session.Quantity("metal-1"),Is.Zero);
            host.Step(15); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(session.Quantity("bronze-dagger-item"),Is.EqualTo(1)); Assert.That(session.ProductionJobs,Is.Empty);
            root.GetComponentsInChildren<Button>().Single(b => b.name == "Activity header toggle").onClick.Invoke(); yield return null;
            Assert.That(host.Runtime.Session,Is.SameAs(session)); Assert.That(session.Quantity("bronze-dagger-item"),Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator BankBackAndBottomRosterRestoreEachCharacterTabAndActivityScroll()
        {
            var host = GameSessionHost.Instance;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            var session = host.Runtime.Session;
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            foreach (var tab in new[] { CharacterScreen.Overview,CharacterScreen.Equipment,CharacterScreen.Activities })
            {
                Button(tab.ToString()).onClick.Invoke(); yield return null;
                var page = root.GetComponentsInChildren<ScrollRect>().Single(s => s.gameObject.name == "Screen content");
                Canvas.ForceUpdateCanvases(); page.verticalNormalizedPosition = .35f;
                float position = page.verticalNormalizedPosition;
                bool scrollable = page.content.rect.height > page.viewport.rect.height + 1;
                Button("Bank").onClick.Invoke(); yield return null;
                Assert.That(host.Model.CharacterOpen,Is.False); Assert.That(host.Model.CanReturnToCharacter,Is.True);
                Button("Back to Character").onClick.Invoke(); yield return null;
                Assert.That(host.Model.CharacterOpen,Is.True); Assert.That(host.Model.CharacterTab,Is.EqualTo(tab));
                Assert.That(host.Model.InspectedId,Is.EqualTo("character-1"));
                if (scrollable) Assert.That(page.verticalNormalizedPosition,Is.EqualTo(position).Within(.03));
                Button("Bank").onClick.Invoke(); yield return null;
                Button("Roster").onClick.Invoke(); yield return null;
                Assert.That(host.Model.CharacterOpen,Is.True); Assert.That(host.Model.CharacterTab,Is.EqualTo(tab));
            }
            Assert.That(host.Runtime.Session,Is.SameAs(session)); Assert.That(session.Assignments,Is.Empty); Assert.That(session.Inventory,Is.Empty);
        }
        [UnityTest]
        public IEnumerator DashboardBankHasNoCharacterBackAndReturnsToRosterDashboard()
        {
            Button("Bank").onClick.Invoke(); yield return null;
            Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text == "Back to Character"),Is.False);
            Button("Roster").onClick.Invoke(); yield return null;
            Assert.That(GameSessionHost.Instance.Model.CharacterOpen,Is.False);
            Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text == "YOUR ACTIVE ADVENTURER"),Is.True);
        }
    }
}
