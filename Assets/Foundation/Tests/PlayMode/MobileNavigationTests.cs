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
        public IEnumerator AllProductionCardsContinueAcrossBankAndStopOnlyWhenMaterialsRunOut()
        {
            var host = GameSessionHost.Instance;
            host.Runtime.SetPresence(host,Time.realtimeSinceStartupAsDouble,false,true); host.SetRunning(false);
            Button("Inspect Character 1").onClick.Invoke(); yield return null;
            foreach (var entry in new[] {
                "mining|smithing|mining-1|bronze-dagger|metal-1|bronze-dagger-item|2|15",
                "woodcutting|crafting|woodcutting-1|wood-shortbow|wood-1|wood-shortbow-item|2|15",
                "fishing|cooking|fishing-1|cooked-shrimp|fish-1|cooked-shrimp-item|1|5" })
            {
                var data = entry.Split('|'); int cost = int.Parse(data[6]), seconds = int.Parse(data[7]);
                host.Model.ShowSkill(data[0]); Assert.That(host.Model.Start(data[2]),Is.True); host.Step(3 * cost * 5);
                host.Model.ShowSkill(data[1]); Button("Activities").onClick.Invoke(); yield return null;
                var start = ActivityStart(data[3]); Assert.That(start.GetComponentInChildren<Text>().text,Is.EqualTo("Start"));
                start.onClick.Invoke(); yield return null; host.Step(2 * seconds + seconds / 2);
                Button("Bank").onClick.Invoke(); yield return null; Button("Back to Character").onClick.Invoke(); yield return null;
                Assert.That(host.Runtime.Session.Quantity(data[5]),Is.EqualTo(2)); Assert.That(host.Runtime.Session.ProductionJobs.Count,Is.EqualTo(1));
                Button("Cancel job").onClick.Invoke(); yield return null; Assert.That(host.Runtime.Session.Quantity(data[4]),Is.EqualTo(cost));
                ActivityStart(data[3]).onClick.Invoke(); yield return null; host.Step(seconds); yield return new WaitForSecondsRealtime(.15f);
                Assert.That(host.Runtime.Session.Quantity(data[5]),Is.EqualTo(3)); Assert.That(host.Runtime.Session.Quantity(data[4]),Is.Zero);
                Assert.That(host.Runtime.Session.ProductionJobs,Is.Empty);
                Assert.That(root.GetComponentsInChildren<Text>().Any(t => t.text == "Out of materials"),Is.True);
                Assert.That(ActivityStart(data[3]).interactable,Is.False);
            }
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

        [UnityTest] public IEnumerator MagicOffers48LockedEnchantmentsAndTierFilterWithoutChangingSession()
        {
            var host=GameSessionHost.Instance;host.SetRunning(false);var session=host.Runtime.Session;
            Button("Inspect Character 1").onClick.Invoke();yield return null;host.Model.ShowSkill("magic");Button("Activities").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Transform>().Any(t=>t.name=="Enchant activity"),Is.True);
            Button("Choose enchantment").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Button>().Count(b=>b.GetComponentInChildren<Text>()?.text=="Locked" && b.transform.IsChildOf(root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Details"))),Is.EqualTo(48));
            Button("Tier: All — next").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Button>().Count(b=>b.GetComponentInChildren<Text>()?.text=="Locked" && b.transform.IsChildOf(root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Details"))),Is.EqualTo(12));
            Button("Close").onClick.Invoke();yield return null;Button("Bank").onClick.Invoke();yield return null;Button("Back to Character").onClick.Invoke();yield return null;
            Assert.That(host.Runtime.Session,Is.SameAs(session));Assert.That(session.ActiveCharacterId,Is.EqualTo("character-1"));Assert.That(session.Assignments,Is.Empty);
        }
        [UnityTest] public IEnumerator PermanentEnchantConfirmationProgressBankReturnAndItemDetailsUseRealInstance()
        {
            var host=GameSessionHost.Instance;host.SetRunning(false);var old=host.Runtime.Session;var d=old.Capture();
            d.Bank=new[]{new ItemAmount{Id="metal-1",Quantity=40},new ItemAmount{Id="wood-1",Quantity=20},new ItemAmount{Id="bronze-dagger-item",Quantity=1}};
            var magic=d.Characters[0].Skills.Single(k=>k.Id=="magic");magic.Level=15;magic.Xp=old.Curve.Thresholds[15];
            host.Runtime.AdoptRestored(host,GatheringSession.Restore(d,old),Time.realtimeSinceStartupAsDouble,false);
            Button("Inspect Character 1").onClick.Invoke();yield return null;host.Model.ShowSkill("magic");Button("Activities").onClick.Invoke();yield return null;
            Button("Choose enchantment").onClick.Invoke();yield return null;Button("Choose bank equipment").onClick.Invoke();yield return null;
            var target=root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>()?.text.EndsWith("new instance from bank")==true);target.onClick.Invoke();yield return null;
            Assert.That(host.Runtime.Session.EnchantJobs,Is.Empty,"Choosing a target must not start without permanent confirmation.");
            Button("Confirm permanent enchantment").onClick.Invoke();yield return null;
            var session=host.Runtime.Session;string instance=session.EnchantJobs["character-1"].InstanceId;host.Step(15);yield return null;
            Button("Bank").onClick.Invoke();yield return null;Button("Back to Character").onClick.Invoke();yield return null;
            Assert.That(session.PartialTicks("character-1"),Is.EqualTo(15*System.TimeSpan.TicksPerSecond));
            Button("Cancel enchant");host.Step(15);yield return new WaitForSecondsRealtime(.15f);
            Assert.That(session.EnchantJobs,Is.Empty);Assert.That(session.BankEquipmentInstances[instance].PermanentEnchantmentId,Is.EqualTo("miningspeed-1"));
            Button("Bank").onClick.Invoke();yield return null;
            root.GetComponentsInChildren<Button>().Single(b=>b.name=="Bank item: bronze-dagger-item").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Mining Speed I") && t.text.Contains("permanent")),Is.True);
            Assert.That(session.ActiveCharacterId,Is.EqualTo("character-1"));Assert.That(session.Quantity("bronze-dagger-item"),Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator EquipmentShowsLiveSetBackgroundEfficiencyAndOfflineAllowance()
        {
            var host=GameSessionHost.Instance;host.SetRunning(false);var session=host.Runtime.Session;session.GrantDevelopmentGearKitAt(session.LastSettledTicks);
            foreach(string id in new[]{"dev-practice-hood","dev-practice-body","dev-practice-legs","dev-practice-boots"})
            {
                var key=session.MaterializeBankEquipmentAt(session.LastSettledTicks,id);var d=session.Capture();d.Equipment.Single(e=>e.Instance==key).Enchantment="backgroundxp-4";
                session=GatheringSession.Restore(d,session);Assert.That(session.EquipAt(session.LastSettledTicks,"character-1",id,session.Equipment.Definitions.Single(e=>e.Id==id).Slot,out _,key),Is.True);
            }
            host.Runtime.AdoptRestored(host,session,Time.realtimeSinceStartupAsDouble,false);
            Button("Inspect Character 1").onClick.Invoke();yield return null;Button("Equipment").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Matching four-piece set: active") && t.text.Contains("50.0") && t.text.Contains("4h")),Is.True);
            Assert.That(session.HasMatchingSet("character-1"),Is.True);
        }

        private void InstallFoodFixture(decimal hp=20)
        {
            var host=GameSessionHost.Instance;host.SetRunning(false);var template=host.Runtime.Session;var d=template.Capture();
            foreach(var c in d.Characters)c.Hp=hp;
            d.Bank=new[]{new ItemAmount{Id="cooked-shrimp-item",Quantity=3},new ItemAmount{Id="cooked-sardine-item",Quantity=1}};
            host.Runtime.AdoptRestored(host,GatheringSession.Restore(d,template),Time.realtimeSinceStartupAsDouble,false);
        }

        [UnityTest] public IEnumerator OverviewHasNineSkillsAndBankOwnsIndependentFoodSettingsAndActions()
        {
            InstallFoodFixture();var host=GameSessionHost.Instance;Button("Inspect Character 1").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Transform>().Any(t=>t.name=="Food controls"),Is.False);
            var skills=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Skills");Assert.That(skills.GetComponentsInChildren<Button>().Length,Is.EqualTo(9));Assert.That(skills.GetComponentsInChildren<Button>().All(b=>b.interactable && b.gameObject.activeInHierarchy),Is.True);
            Assert.That(root.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>()?.text=="Eat"),Is.False);
            Button("Bank").onClick.Invoke();yield return null;Button("Food settings").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.text=="Auto-eating is always active in combat."),Is.True);
            Assert.That(root.GetComponentsInChildren<Button>().Any(b=>new[]{"Auto-eat ON","Auto-eat OFF"}.Contains(b.GetComponentInChildren<Text>()?.text)),Is.False);
            Button("Change HP threshold").onClick.Invoke();yield return null;Button("80% HP").onClick.Invoke();yield return null;
            Button("Cooked shrimp").onClick.Invoke();yield return null;Button("Auto").onClick.Invoke();yield return null;
            Assert.That(host.Runtime.Session.FoodFor("character-1").Preferred,Is.EqualTo("auto"));Button("Close").onClick.Invoke();yield return null;
            root.GetComponentsInChildren<Button>().Single(b=>b.name=="Bank item: cooked-shrimp-item").onClick.Invoke();yield return null;
            Button("Eat").onClick.Invoke();yield return null;Assert.That(host.Runtime.Session.Vitals["character-1"].Hp,Is.EqualTo(28));Assert.That(host.Runtime.Session.Quantity("cooked-shrimp-item"),Is.EqualTo(2));Assert.That(Button("Eat").interactable,Is.False);
            Button("Set as preferred food").onClick.Invoke();yield return new WaitForSecondsRealtime(.15f);Assert.That(Button("Already preferred").interactable,Is.False);
            Button("Close").onClick.Invoke();yield return null;
            Button("Equipment & food: Character 1 — change").onClick.Invoke();yield return null;Button("Character 2").onClick.Invoke();yield return null;Button("Food settings").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.text=="Food settings · Character 2"),Is.True);
            Assert.That(host.Runtime.Session.FoodFor("character-2").Threshold,Is.EqualTo(50));Button("Close").onClick.Invoke();yield return null;
            long ticks=host.Runtime.Session.LastSettledTicks;Button("Back to Character").onClick.Invoke();yield return null;
            Assert.That(host.Model.InspectedId,Is.EqualTo("character-1"));Assert.That(host.Model.CharacterTab,Is.EqualTo(CharacterScreen.Overview));Assert.That(host.Runtime.Session.LastSettledTicks,Is.EqualTo(ticks));Assert.That(host.Runtime.Session.ActiveCharacterId,Is.EqualTo("character-1"));
        }
        [UnityTest] public IEnumerator CombatSummaryAndBankHealingDetailsRefreshAfterRealConsumption()
        {
            InstallFoodFixture(26);var host=GameSessionHost.Instance;host.Model.Inspect("character-1");host.Model.ConfigureFood(true,50,"cooked-shrimp-item");host.Model.ShowSkill("melee");host.Model.Start("dev-chicken");
            Button("Inspect Character 1").onClick.Invoke();yield return null;Button("Activities").onClick.Invoke();yield return null;
            host.Step(3);yield return new WaitForSecondsRealtime(.15f);
            Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Auto-eat always active") && t.text.Contains("50%") && t.text.Contains("×2")),Is.True);
            Assert.That(host.Runtime.Session.Vitals["character-1"].Hp,Is.EqualTo(33));
            Button("Bank").onClick.Invoke();yield return null;root.GetComponentsInChildren<Button>().Single(b=>b.name=="Bank item: cooked-shrimp-item").onClick.Invoke();yield return null;
            Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Restores 8 HP")),Is.True);
        }
    }
}
