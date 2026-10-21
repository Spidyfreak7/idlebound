using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Idlebound.Foundation.Development.MobileUiKit;

namespace Idlebound.Foundation.Development
{
    public sealed class IdleboundMobileUI : MonoBehaviour
    {
        [SerializeField] private Sprite adventurerSprite;
        [SerializeField] private Sprite enemySprite;
        private GameSessionHost host;
        private GameUiModel Model => host.Model;
        private GatheringSession Session => host.Runtime.Session;
        private RectTransform safeRoot, body, characterTabs, modalLayer, activityHeader, titleBar;
        private ActivityHeaderFeedback headerFeedback;
        private ScrollRect scroll;
        private Text heading, notice;
        private CanvasScaler canvasScaler;
        private readonly List<Action> bindings = new List<Action>();
        private readonly List<Action> modalBindings = new List<Action>();
        private readonly List<Action> bankBindings = new List<Action>();
        private readonly List<GridLayoutGroup> grids = new List<GridLayoutGroup>();
        private readonly Dictionary<MainScreen, Button> mainTabs = new Dictionary<MainScreen, Button>();
        private string signature;
        private Action refreshBank;
        private float nextRefresh;
        private Rect lastSafe;
        private Vector2 lastScreen;
        private int generation;
        private string localNotice = "", shownFeedback = "";
        private float feedbackUntil;
        private bool shopBuy = true;
        private RectTransform equipmentPanel, equipmentHero;
        private readonly Dictionary<EquipmentSlot, RectTransform> equipmentButtons = new Dictionary<EquipmentSlot, RectTransform>();
        private float equipmentWidth = -1;
        private string systemPanel;
        private void Awake()
        {
            host = GameSessionHost.Ensure(gameObject);
            BuildCanvas();
            if (host.Runtime != null) Rebuild(true);
            else { activityHeader.gameObject.SetActive(false); characterTabs.gameObject.SetActive(false); }
        }
        private void BuildCanvas()
        {
            var canvasRoot = Node("Idlebound Player Canvas", transform);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>(); canvasScaler = scaler; scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 854); scaler.matchWidthOrHeight = 0;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>(); Surface(canvasRoot, Background);
            safeRoot = Column(canvasRoot, "Safe Area", 12); Fill(safeRoot);
            titleBar = Row(safeRoot,32); titleBar.name = "Dashboard title strip";
            Label(titleBar,"IDLEBOUND",22,Gold);
            heading = Label(titleBar,"",18,Muted);
            activityHeader = Column(safeRoot,"Persistent activity header",6); Surface(activityHeader,Panel);
            activityHeader.GetComponent<VerticalLayoutGroup>().spacing = 4;
            notice = Label(safeRoot, "", 18, Gold); notice.name = "Transient feedback"; notice.gameObject.SetActive(false);
            characterTabs = Row(safeRoot, 58);
            var scrollRoot = Node("Screen content", safeRoot); Size(scrollRoot, 0, 1);
            scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = Node("Viewport", scrollRoot); Fill(viewport); Surface(viewport, Background).raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            body = Column(viewport, "Content"); body.anchorMin = new Vector2(0,1); body.anchorMax = Vector2.one; body.pivot = new Vector2(.5f,1); body.offsetMin = body.offsetMax = Vector2.zero;
            var fitter = body.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = body;
            var nav = Row(safeRoot, 72);
            foreach (MainScreen screen in Enum.GetValues(typeof(MainScreen)))
            {
                var tab = screen;
                var b = Button(nav, screen == MainScreen.CollectionLog ? "Collection\nLog" : screen.ToString(), () => NavigateMain(tab));
                b.GetComponentInChildren<Text>().fontSize = 19; mainTabs.Add(screen,b);
            }
            modalLayer = Node("Modal layer", canvasRoot); Fill(modalLayer); modalLayer.gameObject.SetActive(false);
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Idlebound EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            ApplySafeArea();
        }
        private void NavigateMain(MainScreen screen)
        {
            bool returning = Model.Screen == MainScreen.Bank && screen == MainScreen.Roster;
            Model.Navigate(screen,scroll.verticalNormalizedPosition);
            CloseModal(); scroll.StopMovement(); Rebuild(true);
            if (returning && Model.CharacterOpen) scroll.verticalNormalizedPosition = Model.CharacterScrollPosition;
        }
        private void BackToCharacter()
        {
            bool restored = Model.ReturnFromBank();
            CloseModal(); scroll.StopMovement(); Rebuild(true);
            if (restored) scroll.verticalNormalizedPosition = Model.CharacterScrollPosition;
        }
        private void ApplySafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            canvasScaler.matchWidthOrHeight = Screen.width > Screen.height ? 1 : 0;
            var safe = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            modalLayer.anchorMin = safeRoot.anchorMin; modalLayer.anchorMax = safeRoot.anchorMax;
            modalLayer.offsetMin = modalLayer.offsetMax = Vector2.zero;
            lastSafe = safe; lastScreen = new Vector2(Screen.width,Screen.height);
        }
        private string Structure() => host.Generation + "|" + Session.ActiveCharacterId + "|" + string.Join(";", Session.Inventory.Keys.OrderBy(k => k)) + "|" +
            string.Join(";", Session.Assignments.OrderBy(p => p.Key).Select(p => p.Key + p.Value)) + "|" + string.Join(";", Session.ProductionJobs.Keys.OrderBy(k => k)) + "|" +
            string.Join(";", Session.Loadouts.SelectMany(p => p.Value.Slots.Select(q => p.Key + q.Value.InstanceId))) + "|" +
            string.Join(";", Session.Characters.SelectMany(c => c.Skills.Select(s => s.Level.ToString())));
        private void Update()
        {
            if (host == null || body == null) return;
            if (host.IsLoading || host.PersistenceError != null || host.Runtime == null)
            {
                string state = host.PersistenceError != null ? "error" : "loading";
                if (systemPanel != state)
                {
                    Modal(state == "error" ? "Local save needs attention" : "Preparing your adventurers",content =>
                    {
                        Label(content,host.PersistenceError ?? "Restoring your game and offline progress…",21);
                        if (state == "error") Button(content,"Retry",host.RetryPersistence);
                    },false); systemPanel = state;
                }
                return;
            }
            if (systemPanel == "loading" || systemPanel == "error") { CloseModal(); systemPanel = null; Rebuild(true); }
            if (lastSafe != Screen.safeArea || lastScreen != new Vector2(Screen.width,Screen.height)) ApplySafeArea();
            ApplyEquipmentLayout();
            foreach (var grid in grids)
                if (grid != null) grid.cellSize = new Vector2(Mathf.Max(1, (((RectTransform)grid.transform).rect.width - grid.spacing.x * (grid.constraintCount - 1)) / grid.constraintCount),
                    grid.gameObject.name == "Skills" ? 92 : grid.gameObject.name == "Bank items" ? 184 : 118);
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .1f;
            if (headerFeedback == null || !ReferenceEquals(headerFeedback.Session,Session)) headerFeedback = new ActivityHeaderFeedback(Session);
            headerFeedback.Observe(Model.InspectedId);
            string next = Structure();
            if (next != signature)
            {
                if (generation != host.Generation) { CloseModal(); systemPanel = null; Rebuild(true); }
                else if (Model.Screen == MainScreen.Bank && !Model.CharacterOpen) { refreshBank?.Invoke(); signature = next; }
                else Rebuild(false);
            }
            var pending = host.PendingReport;
            if (pending != null && systemPanel != pending.Id) ShowWelcome(pending);
            foreach (var action in bindings) action();
            foreach (var action in bankBindings) action();
            foreach (var action in modalBindings) action();
            if (shownFeedback != Model.Feedback) { shownFeedback = Model.Feedback; feedbackUntil = Time.unscaledTime + 4; }
            notice.text = host.ClockError ?? (localNotice.Length > 0 ? localNotice : Time.unscaledTime < feedbackUntil ? shownFeedback : "");
            notice.gameObject.SetActive(!string.IsNullOrEmpty(notice.text));
        }
        private void Bind(Text label, Func<string> value) { bindings.Add(() => { if (label != null) label.text = value(); }); label.text = value(); }
        private void BindProgress(Image bar, Func<double> value) { bindings.Add(() => { if (bar != null) SetProgress(bar,value()); }); SetProgress(bar,value()); }
        private void Rebuild(bool resetScroll)
        {
            float position = resetScroll ? 1 : scroll.verticalNormalizedPosition;
            equipmentPanel = equipmentHero = null; equipmentButtons.Clear(); equipmentWidth = -1;
            Clear(body); Clear(characterTabs); Clear(activityHeader); bindings.Clear(); bankBindings.Clear(); grids.Clear(); refreshBank = null;
            characterTabs.gameObject.SetActive(Model.CharacterOpen);
            activityHeader.gameObject.SetActive(Model.CharacterOpen);
            titleBar.gameObject.SetActive(!Model.CharacterOpen);
            heading.text = Model.CharacterOpen ? Model.CharacterTab.ToString() : Model.Screen == MainScreen.CollectionLog ? "Collection Log" : Model.Screen.ToString();
            foreach (var tab in mainTabs) tab.Value.GetComponent<Image>().color = tab.Key == Model.Screen ? new Color32(96,78,46,255) : Raised;
            if (Model.CharacterOpen)
            {
                BuildActivityHeader();
                foreach (CharacterScreen tab in Enum.GetValues(typeof(CharacterScreen)))
                { var t = tab; Button(characterTabs, tab.ToString(), () => { Model.ShowCharacterTab(t); Rebuild(true); }, Model.CharacterTab == tab, 58); }
                if (Model.CharacterTab == CharacterScreen.Overview) Overview();
                else if (Model.CharacterTab == CharacterScreen.Activities) Activities();
                else Equipment();
            }
            else if (Model.Screen == MainScreen.Roster) Roster();
            else if (Model.Screen == MainScreen.Bank) Bank();
            else if (Model.Screen == MainScreen.Shop) Shop();
            else Collection();
            generation = host.Generation; signature = Structure();
            Canvas.ForceUpdateCanvases(); ApplyEquipmentLayout(); scroll.verticalNormalizedPosition = position;
        }
        private static string Status(CharacterView c) => c.Status == WorkStatus.NeedsAttention
            ? c.Hp > 0 && new[] { "smithing", "crafting", "cooking" }.Contains(c.SkillId) ? "Out of materials" : "Needs Attention"
            : c.Status.ToString();
        private string Role(string id) => Session.IsActive(id) ? "ACTIVE • full online XP" : "BACKGROUND • " + Session.BackgroundEfficiency(id, Model.Character(id).SkillId ?? "mining").ToString("P0") + " XP";
        private void Summary(Transform parent, string id)
        {
            var row = Row(parent, 70); Icon(row, "hero", 60);
            var column = Column(row); Bind(Label(column,"",22,Gold), () => Model.Character(id).Name + " • " + Status(Model.Character(id)));
            Bind(Label(column,"",18,Muted), () => Role(id));
            Bind(Label(parent,"",21), () => Model.Character(id).Assignment + " • HP " + Session.Vitals[id].Hp + "/" + Session.Vitals[id].MaxHp);
            BindProgress(Progress(parent), () => Model.Character(id).Progress);
        }
        private void Roster()
        {
            var active = Session.ActiveCharacterId;
            Label(body, "YOUR ACTIVE ADVENTURER", 20, Gold, 32);
            var top = Card(body); Summary(top,active);
            var current = Model.Character(active).SkillId;
            if (current != null) Bind(Label(top,"",19,Muted), () => SkillText(Model.Skills(active).First(s => s.Id == Model.Character(active).SkillId)));
            Button(top, "Watch & manage", () => { Model.Inspect(active); Rebuild(true); });
            Label(body, "ROSTER", 20, Gold, 32);
            foreach (var c in Session.Characters)
            {
                var id = c.Id; var card = Card(body); Summary(card,id);
                Button(card,"Inspect " + c.DisplayName, () => { Model.Inspect(id); Rebuild(true); });
            }
            Label(body,"3 available development characters / 9 capacity. Remaining slot unlocks and membership are not implemented.",19,Muted);
        }
        private static string SkillText(SkillView s) => s.Name + "  Lv " + s.Level + " • " + s.Xp.ToString("0.##") + " XP" +
            (s.NextXp.HasValue ? " • " + (s.NextXp.Value - s.Xp).ToString("0.##") + " to next level" : " • 10M XP cap");
        private void BuildActivityHeader()
        {
            string id = Model.InspectedId;
            if (headerFeedback == null || !ReferenceEquals(headerFeedback.Session,Session)) headerFeedback = new ActivityHeaderFeedback(Session);
            headerFeedback.Observe(id);
            var row = Row(activityHeader,48);
            var group = row.GetComponent<HorizontalLayoutGroup>(); group.childForceExpandHeight = false; group.childForceExpandWidth = false; group.childAlignment = TextAnchor.MiddleLeft;
            var identity = Label(row,"",19,Gold,48); Size(identity,48).flexibleWidth = 1;
            Bind(identity,() => { var c = Model.Character(id); return c.Name + (c.Active ? " • ACTIVE" : " • BACKGROUND") + "\n" + Status(c) + " • HP " + c.Hp + "/" + c.MaxHp; });
            var toggle = Button(row,Model.SceneCollapsed ? "v" : "^",() => { Model.SceneCollapsed = !Model.SceneCollapsed; Rebuild(false); },false,48);
            toggle.name = "Activity header toggle"; var size = Size(toggle,48); size.minWidth = size.preferredWidth = 48; size.flexibleWidth = 0;
            toggle.GetComponent<Image>().color = Panel; toggle.GetComponentInChildren<Text>().color = Muted;
            Bind(Label(activityHeader,"",18),() =>
            {
                var character = Model.Character(id);
                return (character.SkillId == null ? "" : Session.Skills.First(s => s.Id == character.SkillId).DisplayName + " • ") + character.Assignment +
                    (Session.Encounters.TryGetValue(id,out var fight) ? " • Enemy HP " + fight.EnemyHp : "");
            });
            BindProgress(Progress(activityHeader),() => Model.Character(id).Progress);
            var latest = Label(activityHeader,"",16,Muted,24); latest.name = "Latest activity feedback";
            Bind(latest,() => headerFeedback.Latest.Length > 0 ? "Latest: " + headerFeedback.Latest : "No new activity rewards observed");
            if (!Model.SceneCollapsed)
            {
                var stage = Node("Activity scene",activityHeader); Size(stage,180);
                stage.gameObject.AddComponent<ActivityStage>().Initialize(host,id,adventurerSprite,enemySprite,false);
            }
        }
        private void Overview()
        {
            string id = Model.InspectedId;
            if (!Session.IsActive(id)) Button(body,"Make Active", () => Act(() => Model.MakeActive()));
            var current = Model.Character(id).SkillId;
            if (current != null)
            {
                Bind(Label(body,"",20,Gold), () => SkillText(Model.Skills(id).First(s => s.Id == Model.Character(id).SkillId)));
                BindProgress(Progress(body), () => (double)Model.Skills(id).First(s => s.Id == Model.Character(id).SkillId).Progress);
            }
            Label(body,"SKILLS — tap to choose an activity",20,Gold,32);
            var grid = Grid(body,"Skills");
            foreach (var skill in Model.Skills(id))
            {
                var key = skill.Id;
                var b = Button(grid,"", () => { Model.ShowSkill(key); Rebuild(true); });
                var t = b.GetComponentInChildren<Text>(); t.fontSize = 18;
                Bind(t, () => { var s = Model.Skills(id).First(k => k.Id == key); return s.Name + "\nLv " + s.Level + " • " + s.Progress.ToString("P0") + "\n" + s.Xp.ToString("0.#") + " XP"; });
            }
        }
        private RectTransform Grid(Transform parent, string name)
        {
            var r = Node(name,parent); var g = r.gameObject.AddComponent<GridLayoutGroup>(); g.spacing = new Vector2(8,8);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = name == "Bank items" ? 2 : 3; g.cellSize = new Vector2(name == "Bank items" ? 216 : 136,name == "Skills" ? 92 : name == "Bank items" ? 184 : 118);
            grids.Add(g); return r;
        }
        private void Activities()
        {
            string id = Model.InspectedId;
            Button(body,"Skill: " + Session.Skills.First(s => s.Id == Model.SkillFilter).DisplayName + " — change", () =>
                Modal("Choose a skill", content =>
                {
                    foreach (var skill in Session.Skills)
                    { var key = skill.Id; Button(content,skill.DisplayName,() => { Model.ShowSkill(key); CloseModal(); Rebuild(true); }); }
                }));
            foreach (var item in Model.Activities())
            {
                var a = item; var card = Card(body); card.name = "Activity card: " + a.Id;
                card.GetComponent<VerticalLayoutGroup>().spacing = 6;
                var row = Row(card,56); var group = row.GetComponent<HorizontalLayoutGroup>(); group.childForceExpandWidth = false;
                Icon(row,a.Id,40);
                var name = Label(row,a.Name,21,Gold); Size(name,56).flexibleWidth = 1;
                var start = Button(row,a.Locked ? "Locked" : a.Occupant != null ? "Swap…" : "Start", () =>
                {
                    if (a.Occupant == null) Act(() => Model.Start(a.Id));
                    else Confirm("Swap Assignment", "Displace " + Model.Character(a.Occupant).Name + " from " + a.Name + "? They become unassigned. Unfinished production inputs are refunded; progress restarts.", () => Act(() => Model.Start(a.Id,true,a.Occupant)));
                },false,56);
                start.name = "Activity start: " + a.Id;
                var startSize = Size(start,56); startSize.minWidth = startSize.preferredWidth = 100; startSize.flexibleWidth = 0;
                start.GetComponentInChildren<Text>().fontSize = 20;
                start.interactable = a.CanStart;
                bindings.Add(() => { if (start != null) start.interactable = Model.Activities().First(x => x.Id == a.Id).CanStart; });
                string requirement = "Lv " + a.Level;
                if (a.Locked) requirement = a.Kind == "Combat"
                    ? Session.Vitals[id].Hp <= 0 ? "No HP remaining" : "Equip a " + Session.Skills.First(k => k.Id == a.Skill).DisplayName + " weapon"
                    : "Requires " + Session.Skills.First(k => k.Id == a.Skill).DisplayName + " Lv " + a.Level;
                string xp = a.Xp.HasValue ? a.Xp + " XP" : a.Kind == "Combat" ? Session.Combat.Rules.XpPerDamage((CombatStyle)Enum.Parse(typeof(CombatStyle),a.Skill,true)) + " XP / damage" : "XP unavailable";
                Label(card,requirement + " · " + xp + " · " + GameUiModel.Seconds(a.Duration) + "s" + (a.Kind == "Combat" ? " / attack" : ""),18,a.Locked ? Bad : Muted);
                if (a.Inputs.Length > 0) Bind(Label(card,"",18), () => "Needs " + string.Join(" · ",a.Inputs.Select(stock =>
                    stock.Required + " " + stock.Name + " (bank " + Session.Quantity(stock.Id) + ")")));
                if (a.Occupant != null) Label(card,"Assigned to " + Model.Character(a.Occupant).Name,18,Muted);
                if (a.Assigned)
                {
                    BindProgress(Progress(card), () => Model.Character(id).Progress);
                    if (a.Kind == "Production") Bind(Label(card,"",18,Gold), () => Session.ProductionJobs.ContainsKey(id)
                        ? "Running · " + Model.Character(id).Progress.ToString("P0") : "Out of materials");
                    if (a.Kind == "Combat") CombatDetails(card,id);
                    if (a.Kind != "Production" || Session.ProductionJobs.ContainsKey(id))
                    {
                        var controls = Row(card,48);
                        var cancel = Button(controls,a.Kind == "Production" ? "Cancel job" : "Stop activity",() => Act(() => Model.Stop()),false,48);
                        var size = Size(cancel,48); size.minWidth = size.preferredWidth = 156; size.flexibleWidth = 0;
                        controls.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                        cancel.GetComponent<Image>().color = Panel; cancel.GetComponentInChildren<Text>().color = Muted;
                    }
                }
            }
        }
        private void CombatDetails(Transform parent,string id)
        {
            Bind(Label(parent,"",18,Muted), () => Session.Encounters.TryGetValue(id,out var fight)
                ? "HP " + Session.Vitals[id].Hp + "/" + Session.Vitals[id].MaxHp + " · Enemy " + fight.EnemyHp + " HP"
                : "Combat stopped");
        }
        private void Act(Func<bool> action)
        {
            if (host.IsLoading || host.PersistenceError != null) return;
            try { action(); localNotice = ""; }
            catch (Exception error) { localNotice = error.Message; }
            Rebuild(false);
        }
        private void Equipment()
        {
            string id = Model.InspectedId;
            var profile = Label(body,"",18); Bind(profile, () => { var p = Session.Profile(id); return p.Style + " • Damage " + p.Damage + " • Defense " + p.Defense + " • " + GameUiModel.Seconds(p.IntervalTicks) + "s / attack"; });
            equipmentPanel = Node("Ten-slot loadout",body); Size(equipmentPanel,EquipmentLayout.Height); Surface(equipmentPanel,Panel);
            equipmentHero = Node("Character",equipmentPanel);
            var image = equipmentHero.gameObject.AddComponent<Image>(); image.sprite = adventurerSprite != null ? adventurerSprite : PixelArtwork.Icon("hero"); image.preserveAspect = true; image.raycastTarget = false;
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var selectedSlot = slot;
                bool occupied = Session.Loadouts[id].Slots.TryGetValue(slot,out var item);
                var button = Button(equipmentPanel,"",() => SlotDetails(selectedSlot),occupied);
                button.name = "Slot " + slot;
                button.GetComponentInChildren<Text>().gameObject.SetActive(false);
                var rect = (RectTransform)button.transform;
                equipmentButtons.Add(slot,rect);
                var icon = Icon(button.transform,occupied ? item.Definition.Id : slot.ToString());
                Fill(icon.rectTransform,10);
                icon.color = occupied ? Ink : new Color(Muted.r,Muted.g,Muted.b,.55f);
            }
            Label(body,"Tap a slot to browse gear and compare changed stats.",19,Muted);
        }
        private void ApplyEquipmentLayout()
        {
            if (equipmentPanel == null || equipmentPanel.rect.width < 200 || Mathf.Approximately(equipmentWidth,equipmentPanel.rect.width)) return;
            equipmentWidth = equipmentPanel.rect.width;
            var geometry = new EquipmentLayout(equipmentWidth);
            PlaceEquipment(equipmentHero,geometry.CharacterBounds);
            foreach (var pair in equipmentButtons) PlaceEquipment(pair.Value,geometry.Slots[pair.Key]);
        }
        private static void PlaceEquipment(RectTransform rect,Rect bounds)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = bounds.center; rect.sizeDelta = bounds.size;
        }
        private static string Requirements(EquipmentDefinition gear) => "Requires " + string.Join(" / ",gear.Requirements.Select(r => r.Key + " " + r.Value));
        private void SlotDetails(EquipmentSlot slot)
        {
            Modal(slot.ToString(), content =>
            {
                string id = Model.InspectedId;
                if (Session.Loadouts[id].Slots.TryGetValue(slot,out var current))
                {
                    Label(content,"Equipped: " + PlayerNames.Display(current.Definition.Name),22,Gold);
                    Label(content,Requirements(current.Definition),20,Muted); Label(content,"Instance " + current.InstanceId,17,Muted);
                    Button(content,"Inspect equipped item",() => ItemDetails(current.Definition.Id));
                    Button(content,"Unequip to bank",() => { CloseModal(); Act(() => Model.Unequip(slot)); });
                }
                else Label(content,"Empty " + slot + " slot",22,Muted);
                Label(content,"Compatible replacements • shared bank",20,Gold);
                var stock = Model.CompatibleStock(slot);
                if (stock.Length == 0) Label(content,"No compatible equipment in the shared bank. Gather and produce items, then return here.",21);
                foreach (var item in stock)
                { var key = item.Id; Button(content,PlayerNames.Display(item.Name) + " × " + Session.Quantity(key),() => ItemDetails(key)); }
            });
        }
        private void ItemDetails(string id)
        {
            Modal(PlayerNames.Display(Session.BankItems[id]), content =>
            {
                Icon(content,id,80); var quantity = Label(content,"Shared bank: " + Session.Quantity(id),23,Gold);
                modalBindings.Add(() => { if (quantity != null) quantity.text = "Shared bank: " + Session.Quantity(id); });
                var gear = Session.Equipment.Definitions.FirstOrDefault(g => g.Id == id);
                if (gear == null) { Label(content,"Materials, products and loot are shared by all characters. Selling and item use are not implemented.",21); return; }
                Label(content,"Equip for " + Model.Character(Model.InspectedId).Name + " • " + gear.Slot,21);
                bool equipped = Session.Loadouts[Model.InspectedId].Slots.TryGetValue(gear.Slot,out var worn) && worn.Definition.Id == id;
                Label(content,equipped ? "Currently equipped — bank quantity counts spare copies only" : "Bank replacement for this slot",20,Gold);
                Label(content,Requirements(gear),20,Muted);
                Label(content,"DEVELOPMENT stats: damage +" + gear.DamageBonus + ", defense +" + gear.DefenseBonus +
                    (gear.Style.HasValue ? ", " + gear.Style + " / " + GameUiModel.Seconds(gear.AttackIntervalTicks) + "s" : "") + (gear.TwoHanded ? ". Two-handed: returns offhand to bank." : ""),20,Muted);
                foreach (var change in Model.Compare(id)) Label(content,change.Name + "  " + change.Before + " → " + change.After,22,change.Improvement ? Good : Bad);
                var current = Session.Profile(Model.InspectedId);
                if (gear.Style.HasValue && gear.Style.Value != current.Style) Label(content,"Style  " + current.Style + " → " + gear.Style,21,Gold);
                Button(content,"Equip / replace",() => { CloseModal(); Act(() => Model.Equip(id)); }).interactable = Session.Quantity(id) > 0;
            });
        }
        private void Bank()
        {
            if (Model.CanReturnToCharacter)
            {
                var backRow = Row(body,48); backRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                var back = Button(backRow,"Back to Character",BackToCharacter,false,48);
                var size = Size(back,48); size.minWidth = size.preferredWidth = 210; size.flexibleWidth = 0;
            }
            Label(body,"SHARED BANK",22,Gold,36);
            Button(body,"Equipment target: " + Model.Character(Model.InspectedId).Name + " — change", () => Modal("Choose equipment target", content =>
            {
                foreach (var c in Session.Characters)
                { string id = c.Id; Button(content,c.DisplayName,() => { Model.SetBankEquipmentTarget(id); CloseModal(); Rebuild(false); }); }
            }));
            var search = SearchBox(body,Model.Search);
            var filters = Row(body,56);
            foreach (BankFilter filter in Enum.GetValues(typeof(BankFilter)))
            { var f = filter; var label = filter == BankFilter.Resources ? "Raw" : filter == BankFilter.Equipment ? "Gear" : filter == BankFilter.Products ? "Made" : filter.ToString();
                Button(filters,label, () => { Model.Filter = f; Rebuild(false); },Model.Filter == f,56).GetComponentInChildren<Text>().fontSize = 17; }
            var items = Grid(body,"Bank items");
            refreshBank = () =>
            {
                Clear(items); bankBindings.Clear();
                foreach (var item in Model.Bank())
                {
                    var key = item.Id;
                    var tile = Button(items,"",() => ItemDetails(key)); tile.name = "Bank item: " + key;
                    var icon = Icon(tile.transform,key,56); icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(.5f,.76f); icon.rectTransform.anchoredPosition = Vector2.zero;
                    var text = tile.GetComponentInChildren<Text>(); text.name = "Item name"; text.fontSize = 20; text.alignment = TextAnchor.MiddleCenter;
                    text.text = item.Name;
                    text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = new Vector2(1,.48f);
                    text.rectTransform.offsetMin = new Vector2(8,8); text.rectTransform.offsetMax = new Vector2(-8,0);
                    text.resizeTextForBestFit = true; text.resizeTextMinSize = 18; text.resizeTextMaxSize = 20;
                    var quantity = Label(tile.transform,"× " + Session.Quantity(key),21,Gold); quantity.name = "Quantity"; quantity.alignment = TextAnchor.MiddleRight;
                    quantity.rectTransform.anchorMin = new Vector2(.05f,.49f); quantity.rectTransform.anchorMax = new Vector2(.95f,.65f);
                    quantity.rectTransform.offsetMin = quantity.rectTransform.offsetMax = Vector2.zero;
                    quantity.resizeTextForBestFit = true; quantity.resizeTextMinSize = 12; quantity.resizeTextMaxSize = 21;
                    bankBindings.Add(() => { if (quantity != null) quantity.text = "× " + Session.Quantity(key); });
                }
            };
            refreshBank(); search.onValueChanged.AddListener(value => { Model.Search = value; refreshBank?.Invoke(); });
            var empty = Label(body,"",20,Muted); Bind(empty,() => Model.Bank().Length == 0 ? "No matching items. Complete an activity to stock the bank." : "Tap an item for its name, details and equipment actions.");
            Label(body,"Search and filters organize the display only. Custom tabs/reordering and saved organization are deferred. No selling or fictitious currency.",18,Muted);
        }
        private InputField SearchBox(Transform parent,string value)
        {
            var r = Node("Search",parent); Size(r,64); var background = Surface(r,Raised); background.raycastTarget = true;
            var input = r.gameObject.AddComponent<InputField>(); input.targetGraphic = background;
            var text = Label(r,"",23); Fill(text.rectTransform,12); text.supportRichText = false;
            var placeholder = Label(r,"Search shared bank…",23,Muted); Fill(placeholder.rectTransform,12);
            input.textComponent = text; input.placeholder = placeholder; input.lineType = InputField.LineType.SingleLine; input.text = value;
            return input;
        }
        private void Shop()
        {
            Label(body,"THE TRADING POST",27,Gold,50);
            var tabs = Row(body); Button(tabs,"Buy",() => { shopBuy = true; Rebuild(false); },shopBuy); Button(tabs,"Sell",() => { shopBuy = false; Rebuild(false); },!shopBuy);
            var card = Card(body); Icon(card,"wood",90);
            Label(card,(shopBuy ? "Buying" : "Selling") + " is not yet implemented.",25,Gold);
            Label(card,"No purchasable stock, prices, currency or transactions exist. Your shared bank remains available from the Bank tab.",22);
        }
        private void Collection()
        {
            Label(body,"COLLECTION LOG",27,Gold,50);
            Label(body,"Boss collections are not yet implemented. No earned uniques or boss kill counts are recorded by the current session.",22);
            foreach (var collection in BossCollectionShell.All)
            {
                var card = Card(body); Button(card,collection.Boss + "  +",() =>
                    Modal(collection.Boss,content =>
                    {
                        Label(content,"Unavailable — boss encounters and ownership tracking are future work.",21,Muted);
                        foreach (var item in collection.Items)
                        {
                            var row = Row(content,76); var silhouette = Icon(row,item.IconKey,50); silhouette.color = new Color32(8,12,16,255);
                            Label(row,item.Name + "\nNot tracked",19,Muted);
                        }
                    }));
            }
        }
        private void Confirm(string title,string message,Action accept)
        { Modal(title,content => { Label(content,message,22); Button(content,"Confirm Swap Assignment",() => { CloseModal(); accept(); }); }); }
        private void Modal(string title,Action<RectTransform> build,bool dismissible = true)
        {
            modalBindings.Clear(); Clear(modalLayer); modalLayer.gameObject.SetActive(true);
            var shade = SurfaceOnce(modalLayer,new Color(0,0,0,.88f)); shade.raycastTarget = true;
            var panel = Column(modalLayer,"Details",18); panel.anchorMin = new Vector2(.06f,.08f); panel.anchorMax = new Vector2(.94f,.92f); panel.offsetMin = panel.offsetMax = Vector2.zero; Surface(panel,Panel);
            Label(panel,title,26,Gold,70); if (dismissible) Button(panel,"Close",CloseModal,false,58);
            var area = Node("Details scroll",panel); Size(area,0,1); var sr = area.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false;
            var viewport = Node("Viewport",area); Fill(viewport); viewport.gameObject.AddComponent<RectMask2D>(); Surface(viewport,Panel).raycastTarget = true;
            var content = Column(viewport); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1); content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport; sr.content = content; build(content);
        }
        private static string AwayTime(long ticks)
        {
            var time = TimeSpan.FromTicks(ticks);
            return time.TotalHours >= 1 ? ((long)time.TotalHours) + "h " + time.Minutes + "m" : ((long)time.TotalMinutes) + "m " + time.Seconds + "s";
        }
        private void ShowWelcome(OfflineReport report)
        {
            systemPanel = report.Id;
            Modal("WELCOME BACK!",content =>
            {
                Label(content,(report.Intervals > 1 ? "Away across " + report.Intervals + " visits: " : "Away: ") + AwayTime(report.AwayTicks) + "\nOffline progress: " + AwayTime(report.EligibleTicks),22,Gold);
                if (report.Capped) Label(content,"Offline limit reached · 4h maximum per absence",18,Muted);
                foreach (var c in report.Characters)
                {
                    var card = Card(content);
                    string character = Session.Characters.First(x => x.Id == c.Character).DisplayName;
                    string activity = c.Activity == null ? "Unassigned" : PlayerNames.Display(Session.Activities.First(a => a.Id == c.Activity).DisplayName);
                    Label(card,character + " — " + activity,21,Gold);
                    foreach (var item in c.Items) Label(card,"+" + item.Quantity + " " + PlayerNames.Display(Session.BankItems[item.Id]),20);
                    foreach (var xp in c.Xp) Label(card,"+" + xp.Quantity.ToString("0.##") + " " + Session.Skills.First(k => k.Id == xp.Skill).DisplayName + " XP",19);
                    if (c.Xp.Count == 0) Label(card,"0 XP",18,Muted);
                    if (c.Items.Count == 0 && c.Xp.Count == 0) Label(card,"No rewards earned",18,Muted);
                    if (c.StopReason != null) Label(card,c.StopReason == "Defeated" ? "Combat stopped: defeated" : c.StopReason,19,Muted);
                }
                Button(content,"Continue",() => { if (host.AcknowledgeWelcome(report.Id)) { CloseModal(); systemPanel = null; } });
            },false);
        }
        private static Image SurfaceOnce(RectTransform rect,Color color)
        { var image = rect.GetComponent<Image>(); if (image == null) image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
        private void CloseModal() { systemPanel = null; modalBindings.Clear(); modalLayer.gameObject.SetActive(false); Clear(modalLayer); }
    }
}
