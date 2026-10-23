using System.Collections.Generic;
using EnhancementTower.Combat;
using EnhancementTower.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EnhancementTower.Presentation
{
    public sealed partial class CombatPrototype
    {
        [SerializeField] private bool enableJourneyPrototype;
        private PrototypeJourney journey;
        private Canvas journeyCanvas;
        private CanvasScaler journeyScaler;
        private RectTransform journeyRoot, journeyPage;
        private GameObject journeyEvents;
        private Font journeyFont;
        private bool journeyDirty;
        private Vector2 journeySize;
        private int selectedItem;
        private float forgeUntil, nextHudUpdate;
        private string forgeNotice = "Choose equipment, enhance it, then challenge the tower.";
        private string journeyResult = "Your first ascent starts at the gate.";
        private Color noticeColor = new Color(.55f, .82f, .77f);
        private Text journeyWallet, campStats, campTitle, campPower, campChance, campNotice;
        private Button campEquip, campEnhance;
        private Button[] campSelectors;
        private readonly List<Button> journeyButtons = new List<Button>();
        private Text journeyHeroText, journeyStatusText, journeyGuardText;
        private Image journeyHealthFill;
        private Button journeyGuard;
        private static readonly Color Ink = new Color(.07f, .10f, .12f), Card = new Color(.12f, .17f, .19f),
            Muted = new Color(.63f, .72f, .74f), Accent = new Color(.30f, .73f, .64f), Gold = new Color(.95f, .72f, .35f);
        private float PageWidth => journeyRoot.rect.width;
        private float PageHeight => journeyRoot.rect.height;
        private void InitializeJourney()
        {
            if (equipment == null) throw new System.InvalidOperationException("Journey requires equipment mode.");
            journey = new PrototypeJourney(); workshopOpen = true;
            journeyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Journey UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false); journeyCanvas = go.GetComponent<Canvas>();
            journeyCanvas.renderMode = RenderMode.ScreenSpaceOverlay; journeyCanvas.sortingOrder = 30000;
            var scaler = journeyScaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(400, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            journeyRoot = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            journeyRoot.SetParent(go.transform, false); journeyRoot.offsetMin = journeyRoot.offsetMax = Vector2.zero;
            // Created once for this standalone prototype scene; do not change any existing EventSystem.
            if (EventSystem.current == null)
            {
                journeyEvents = new GameObject("Journey Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                journeyEvents.transform.SetParent(transform, false);
            }
            journeyDirty = true;
        }
        private void UpdateJourney()
        {
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) return;
            // Fit the usable area, including notches, rather than only the full screen width.
            journeyScaler.referenceResolution = new Vector2(400 * Screen.width / safe.width, 720 * Screen.height / safe.height);
            Vector2 min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            Vector2 max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            if (journeyRoot.anchorMin != min || journeyRoot.anchorMax != max)
            { journeyRoot.anchorMin = min; journeyRoot.anchorMax = max; journeyDirty = true; }
            if ((journeySize - journeyRoot.rect.size).sqrMagnitude > .25f) { journeySize = journeyRoot.rect.size; journeyDirty = true; }
            if (forgeUntil > 0 && Time.unscaledTime >= forgeUntil) { forgeUntil = 0; RefreshJourneyCamp(); }
            if (journeyDirty && journeyRoot.rect.width > 0) BuildJourneyPage();
            workshopOpen = journey.Screen != JourneyScreen.Combat;
            if (journey.Screen != JourneyScreen.Combat || Time.unscaledTime < nextHudUpdate) return;
            nextHudUpdate = Time.unscaledTime + .1f;
            var hero = simulation.Hero;
            journeyHeroText.text = $"HP {hero.Health} / {hero.MaxHealth}     ATK {hero.Damage}  DEF {hero.Defense}";
            journeyHealthFill.fillAmount = hero.Health / (float)hero.MaxHealth;
            var config = journey.Stage(journey.Current);
            float remaining = Mathf.Max(0, config.Seconds - simulation.Ticks / 60f);
            journeyStatusText.text = bossMode ? $"{simulation.BossPhase}  |  {remaining:F0}s left\nBoss HP {simulation.Monster.Health}/{simulation.Monster.MaxHealth}" :
                $"Defeated {stage.Kills}/{config.Total}  |  {remaining:F0}s left\nEnemies {simulation.LivingMonsters}  /  Arrivals remaining {stage.Pending}";
            if (journeyGuard != null)
            {
                journeyGuard.interactable = simulation.GuardSecondsRemaining <= 0;
                journeyGuardText.text = simulation.GuardActive ? "GUARD ACTIVE" : journeyGuard.interactable ? "FOCUS GUARD" : $"GUARD  {simulation.GuardSecondsRemaining:F1}s";
            }
        }
        private void SettleJourney()
        {
            if (!journey.Active) return;
            var config = journey.Stage(journey.Current);
            bool clear = stage != null ? stage.Result == StageResult.Clear : simulation.Hero.Alive && simulation.LivingMonsters == 0;
            bool timeout = stage != null ? stage.Result == StageResult.Timeout : simulation.Ticks >= config.Seconds * 60;
            bool defeat = !simulation.Hero.Alive;
            if (!clear && !timeout && !defeat) return;
            if (!journey.Complete(clear)) return;
            bool rewarded = clear && equipment.RewardMaterialRun(equipmentRun, config.Gold, config.Stones);
            long damage = 0; foreach (var enemy in simulation.Monsters) damage += enemy.DamageTaken;
            string verdict = clear ? "CLEAR" : defeat ? "DEFEAT" : "TIME UP";
            journeyResult = $"{verdict}  /  {config.Name}\n\nTime  {simulation.Ticks / 60f:F1}s\nDefeated  {simulation.Monsters.Count - simulation.LivingMonsters}/{config.Total}\nDamage dealt  {damage}\nHP remaining  {simulation.Hero.Health}/{simulation.Hero.MaxHealth}\nRun stats  ATK {runStats.Attack} / DEF {runStats.Defense}\n\n" +
                (rewarded ? $"REWARDS   +{config.Gold} Gold / +{config.Stones} Stones" : "No clear rewards. Equipment and materials are retained.");
            forgeNotice = clear ? "Clear rewards received. Strengthen your gear or climb higher." : "Try enhancing your weapon or armor, or gather materials in Supply Run.";
            workshopOpen = true; journeyDirty = true;
        }
        private void OpenJourney(JourneyScreen screen) { journey.Navigate(screen); journeyDirty = true; }
        private void EnterJourney(int index)
        {
            journey.Begin(index); materialRun = wallTest = trapTest = false;
            bossMode = journey.Stage(index).Roster == JourneyRoster.Boss; stageMode = !bossMode;
            workshopOpen = false; Restart(); journeyDirty = true;
        }
        private RectTransform UiRect(string name, Transform parent, Rect rect)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(rect.x, -rect.y); rt.sizeDelta = rect.size; return rt;
        }
        private Image Panel(string name, Transform parent, Rect rect, Color color)
        {
            var image = UiRect(name, parent, rect).gameObject.AddComponent<Image>(); image.color = color; return image;
        }
        private Text Words(Transform parent, Rect rect, string value, int size = 15, Color? color = null)
        {
            var text = UiRect("Text", parent, rect).gameObject.AddComponent<Text>(); text.font = journeyFont; text.fontSize = size;
            text.text = value; text.color = color ?? Color.white; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        private Button Action(Transform parent, Rect rect, string value, System.Action click, bool enabled = true, bool primary = false)
        {
            var image = Panel(value, parent, rect, primary ? Accent : new Color(.22f, .29f, .31f));
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled && forgeUntil <= 0;
            var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(.65f, .85f, .8f); colors.disabledColor = new Color(.40f, .43f, .44f); button.colors = colors;
            var label = Words(image.transform, new Rect(8, 0, rect.width - 16, rect.height), value, 15, primary ? Ink : Color.white);
            label.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(() => { if (forgeUntil <= 0) click(); }); journeyButtons.Add(button); return button;
        }
        private void BuildJourneyPage()
        {
            journeyDirty = false; nextHudUpdate = 0;
            if (journeyPage != null) { journeyPage.gameObject.SetActive(false); Destroy(journeyPage.gameObject); }
            journeyPage = UiRect("Screen " + journey.Screen, journeyRoot, new Rect(0, 0, PageWidth, PageHeight));
            // Screen changes are opaque. Data refreshes keep their existing input targets alive.
            journeyButtons.Clear(); campSelectors = null;
            journeyHeroText = journeyStatusText = journeyGuardText = null; journeyGuard = null;
            float w = PageWidth, h = PageHeight;
            bool combat = journey.Screen == JourneyScreen.Combat;
            if (!combat) Panel("Background", journeyPage, new Rect(0, 0, w, h), Ink);
            Panel("Header", journeyPage, new Rect(0, 0, w, 94), Ink);
            Words(journeyPage, new Rect(20, 14, w - 40, 20), "ENHANCEMENT TOWER", 12, Accent);
            Words(journeyPage, new Rect(20, 38, w - 40, 30), combat ? journey.Stage(journey.Current).Name : journey.Screen == JourneyScreen.Camp ? "CAMP / FORGE" : journey.Screen == JourneyScreen.Stages ? "CHOOSE YOUR CHALLENGE" : "ENCOUNTER RESULT", 22);
            journeyWallet = Words(journeyPage, new Rect(20, 72, w - 40, 20), $"GOLD {equipment.Gold}    STONES {equipment.Stones}    ROUTE {journey.Unlocked}/4", 12, Muted);
            if (journey.Screen == JourneyScreen.Camp) BuildCamp(w, h);
            else if (journey.Screen == JourneyScreen.Stages) BuildStageSelection(w, h);
            else if (journey.Screen == JourneyScreen.Result) BuildJourneyResult(w, h);
            else BuildBattleHud(w, h);
        }
        private void BuildCamp(float w, float h)
        {
            Panel("Stats", journeyPage, new Rect(16, 104, w - 32, 66), Card);
            campStats = Words(journeyPage, new Rect(28, 114, w - 56, 22), "", 14, Accent);
            Words(journeyPage, new Rect(28, 140, w - 56, 22), journey.Active ? $"Paused run keeps ATK {runStats.Attack} / DEF {runStats.Defense}." : "Equipment applies when you start a new encounter.", 12, Muted);
            campSelectors = new Button[equipment.Count];
            for (int i = 0; i < equipment.Count; i++)
            {
                int index = i; var entry = equipment.Item(i); float cell = (w - 44) / 2;
                var selector = Action(journeyPage, new Rect(16 + i % 2 * (cell + 12), 182 + i / 2 * 52, cell, 44),
                    $"{entry.Spec.Name}\n+{entry.Level} {(equipment.IsEquipped(i) ? "[EQUIPPED]" : "")}",
                    () => { selectedItem = index; RefreshJourneyCamp(); }, primary: selectedItem == i);
                selector.GetComponentInChildren<Text>().fontSize = 12; campSelectors[i] = selector;
            }
            var item = equipment.Item(selectedItem);
            float top = 292;
            Panel("Selected equipment", journeyPage, new Rect(16, top, w - 32, 156), Card);
            campTitle = Words(journeyPage, new Rect(28, top + 10, w - 56, 26), $"{item.Spec.Name}  +{item.Level}", 19, Gold);
            campPower = Words(journeyPage, new Rect(28, top + 40, w - 56, 22), item.Spec.Slot == EquipmentSlot.Weapon ? $"ATK +{item.Power}   /   per level +{item.Spec.PowerPerLevel}" : $"DEF +{item.Power} / HP +{item.Health}   |   per level +{item.Spec.PowerPerLevel} / +{item.Spec.HealthPerLevel}", 13, Muted);
            campChance = Words(journeyPage, new Rect(28, top + 66, w - 56, 22), $"SUCCESS {equipment.Policy.Chance(item):P0}    COST {equipment.Policy.GoldCost(item)}G / {equipment.Policy.StoneCost}S", 14);
            campEquip = Action(journeyPage, new Rect(28, top + 98, 92, 44), equipment.IsEquipped(selectedItem) ? "UNEQUIP" : "EQUIP", () => {
                var chosen = equipment.Item(selectedItem);
                if (equipment.IsEquipped(selectedItem)) equipment.Unequip(chosen.Spec.Slot); else equipment.Equip(selectedItem);
                forgeNotice = "Loadout updated. Start a new encounter to apply it."; noticeColor = Accent; RefreshJourneyCamp();
            });
            campEnhance = Action(journeyPage, new Rect(132, top + 98, w - 160, 44), forgeUntil > 0 ? "FORGING..." : equipment.CanEnhance(selectedItem) ? "ENHANCE" : "NEED MATERIALS", EnhanceJourneyItem, equipment.CanEnhance(selectedItem), true);
            Panel("Forge result", journeyPage, new Rect(16, 460, w - 32, 94), Card);
            campNotice = Words(journeyPage, new Rect(28, 470, w - 56, 74), forgeNotice, 14, noticeColor);
            if (journey.Active) Action(journeyPage, new Rect(16, h - 144, w - 32, 42), "RESUME PAUSED ENCOUNTER", () => OpenJourney(JourneyScreen.Combat));
            Action(journeyPage, new Rect(16, h - 94, w - 32, 50), "CHOOSE STAGE  >", () => OpenJourney(JourneyScreen.Stages), primary: true);
            Words(journeyPage, new Rect(20, h - 32, w - 40, 22), "No item destruction. Progress lasts for this Play session.", 11, Muted);
            RefreshJourneyCamp();
        }
        private void RefreshJourneyCamp()
        {
            if (journey.Screen != JourneyScreen.Camp || campSelectors == null) return;
            bool ready = forgeUntil <= 0;
            foreach (var button in journeyButtons) button.interactable = ready;
            journeyWallet.text = $"GOLD {equipment.Gold}    STONES {equipment.Stones}    ROUTE {journey.Unlocked}/4";
            var stats = equipment.Stats(heroHealth, heroDamage, damageDefinition != null ? damageDefinition.heroDefense : 1);
            campStats.text = $"NEXT RUN   ATK {stats.Attack}  /  DEF {stats.Defense}  /  HP {stats.Health}";
            for (int i = 0; i < campSelectors.Length; i++)
            {
                var entry = equipment.Item(i); var label = campSelectors[i].GetComponentInChildren<Text>();
                label.text = $"{entry.Spec.Name}\n+{entry.Level} {(equipment.IsEquipped(i) ? "[EQUIPPED]" : "")}";
                campSelectors[i].GetComponent<Image>().color = i == selectedItem ? Accent : new Color(.22f, .29f, .31f);
                label.color = i == selectedItem ? Ink : Color.white;
            }
            var item = equipment.Item(selectedItem);
            campTitle.text = $"{item.Spec.Name}  +{item.Level}";
            campPower.text = item.Spec.Slot == EquipmentSlot.Weapon ? $"ATK +{item.Power}   /   per level +{item.Spec.PowerPerLevel}" : $"DEF +{item.Power} / HP +{item.Health}   |   per level +{item.Spec.PowerPerLevel} / +{item.Spec.HealthPerLevel}";
            campChance.text = $"SUCCESS {equipment.Policy.Chance(item):P0}    COST {equipment.Policy.GoldCost(item)}G / {equipment.Policy.StoneCost}S";
            campEquip.GetComponentInChildren<Text>().text = equipment.IsEquipped(selectedItem) ? "UNEQUIP" : "EQUIP";
            campEnhance.interactable = ready && equipment.CanEnhance(selectedItem);
            campEnhance.GetComponentInChildren<Text>().text = !ready ? "FORGING..." : equipment.CanEnhance(selectedItem) ? "ENHANCE" : "NEED MATERIALS";
            campNotice.text = forgeNotice; campNotice.color = noticeColor;
        }
        private void EnhanceJourneyItem()
        {
            if (forgeUntil > 0) return;
            var item = equipment.Item(selectedItem); int level = item.Level, gold = equipment.Gold, stones = equipment.Stones;
            long power = item.Power, health = item.Health;
            var outcome = equipment.Enhance(selectedItem);
            noticeColor = outcome == EnhancementOutcome.Success || outcome == EnhancementOutcome.Blessed ? Gold : Muted;
            forgeNotice = $"{outcome.ToString().ToUpperInvariant()}   +{level}  >  +{item.Level}\n" +
                (item.Spec.Slot == EquipmentSlot.Weapon ? $"ATK {power} > {item.Power}" : $"DEF {power} > {item.Power} / HP {health} > {item.Health}") +
                $"\nSpent {gold - equipment.Gold}G / {stones - equipment.Stones}S   |   Next chance {equipment.Policy.Chance(item):P0}";
            forgeUntil = Time.unscaledTime + .65f; RefreshJourneyCamp();
        }
        private void BuildStageSelection(float w, float h)
        {
            Words(journeyPage, new Rect(20, 102, w - 40, 36), journey.Active ? "Starting a stage replaces the paused fight with full HP." : "Clear a stage to unlock the next. Boss entry is manual.", 13, Muted);
            for (int i = 0; i < PrototypeJourney.TowerCount; i++)
            {
                int index = i; var config = journey.Stage(i); float y = 148 + i * 90;
                Panel("Stage card", journeyPage, new Rect(16, y, w - 32, 80), Card);
                Words(journeyPage, new Rect(28, y + 8, w - 142, 24), config.Name, 17, journey.CanEnter(i) ? Color.white : Muted);
                Words(journeyPage, new Rect(28, y + 34, w - 142, 38), $"{config.Description}\n{config.Total} foes / {config.Seconds:F0}s / {config.Gold}G + {config.Stones}S", 11, Muted);
                Action(journeyPage, new Rect(w - 106, y + 18, 78, 44), journey.CanEnter(i) ? "ENTER" : "LOCKED", () => EnterJourney(index), journey.CanEnter(i), journey.CanEnter(i));
            }
            Action(journeyPage, new Rect(16, 518, w - 32, 52), "SUPPLY RUN   /   Earn 40G + 3 Stones", () => EnterJourney(PrototypeJourney.SupplyIndex));
            Words(journeyPage, new Rect(20, 578, w - 40, 40), "Supply Run is a short repeatable encounter.\nFailing a stage keeps your equipment and materials.", 12, Muted);
            Action(journeyPage, new Rect(16, h - 74, w - 32, 50), "<  BACK TO CAMP", () => OpenJourney(JourneyScreen.Camp));
        }
        private void BuildJourneyResult(float w, float h)
        {
            Panel("Result card", journeyPage, new Rect(16, 110, w - 32, 320), Card);
            Words(journeyPage, new Rect(30, 126, w - 60, 288), journeyResult, 17, journey.Cleared ? Gold : Color.white);
            Words(journeyPage, new Rect(20, 444, w - 40, 58), forgeNotice, 15, Muted);
            bool next = journey.Cleared && journey.Current < PrototypeJourney.TowerCount - 1;
            if (next) Action(journeyPage, new Rect(16, h - 200, w - 32, 48), journey.Current == 2 ? "ENTER BOSS GATE  >" : "NEXT STAGE  >", () => EnterJourney(journey.Current + 1), primary: true);
            Action(journeyPage, new Rect(16, h - 144, (w - 44) / 2, 48), "RETRY", () => EnterJourney(journey.Current));
            Action(journeyPage, new Rect(28 + (w - 44) / 2, h - 144, (w - 44) / 2, 48), "STAGES", () => OpenJourney(JourneyScreen.Stages));
            Action(journeyPage, new Rect(16, h - 88, w - 32, 52), "RETURN TO FORGE", () => OpenJourney(JourneyScreen.Camp), primary: !next);
        }
        private void BuildBattleHud(float w, float h)
        {
            Panel("Battle stats", journeyPage, new Rect(0, 94, w, 92), Ink);
            journeyHeroText = Words(journeyPage, new Rect(20, 98, w - 40, 22), "", 14);
            Panel("Health track", journeyPage, new Rect(20, 124, w - 40, 7), Card);
            journeyHealthFill = Panel("Health", journeyPage, new Rect(20, 124, w - 40, 7), Accent);
            journeyHealthFill.type = Image.Type.Filled; journeyHealthFill.fillMethod = Image.FillMethod.Horizontal;
            journeyStatusText = Words(journeyPage, new Rect(20, 138, w - 40, 44), "", 13, Muted);
            Action(journeyPage, new Rect(16, h - 70, 112, 48), "CAMP / PAUSE", () => OpenJourney(JourneyScreen.Camp));
            if (bossMode)
            {
                journeyGuard = Action(journeyPage, new Rect(140, h - 70, w - 156, 48), "FOCUS GUARD", () => simulation.RequestBossGuard(), primary: true);
                journeyGuardText = journeyGuard.GetComponentInChildren<Text>();
                Words(journeyPage, new Rect(16, h - 104, w - 32, 28), "Read the warning. Guard the impact, not the windup.", 12, Gold);
            }
            else Words(journeyPage, new Rect(140, h - 62, w - 156, 38), "Automatic movement and attacks.\nClear the field before time runs out.", 12, Muted);
        }
        private void DisposeJourney()
        {
            if (journeyCanvas != null) Destroy(journeyCanvas.gameObject);
            if (journeyEvents != null) Destroy(journeyEvents);
        }
    }
}
