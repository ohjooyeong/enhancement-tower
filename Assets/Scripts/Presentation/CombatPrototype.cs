using EnhancementTower.Combat;
using EnhancementTower.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EnhancementTower.Presentation
{
    public sealed class CombatPrototype : MonoBehaviour
    {
        [SerializeField, Min(1)] private int heroHealth = 240, monsterHealth = 60;
        [SerializeField, Min(1)] private int heroDamage = 12, monsterDamage = 6;
        [SerializeField] private Vector2 heroStart = new Vector2(-0.7f, -1.8f), monsterStart = new Vector2(0.8f, 1.8f);
        [SerializeField, Range(1, 12)] private int monsterCount = 5;
        [SerializeField] private ComboDefinition comboDefinition;
        [SerializeField] private CombatDamageDefinition damageDefinition;
        [SerializeField] private CombatTacticsDefinition tacticsDefinition;
        [SerializeField] private FastMonsterDefinition fastMonsterDefinition;
        [SerializeField] private bool includeFastMonsters = true;
        [SerializeField] private StageDefinition stageDefinition;
        private StageEncounter stage;
        private bool stageMode;
        private PrototypeMonsterKind[] monsterKinds;
        private int normalCount, fastCount, heavyCount;
        [SerializeField] private bool enableReposition = true;
        [SerializeField, Min(1)] private int pressureThreshold = 3;
        [SerializeField, Range(0.1f, 0.8f)] private float repositionDistance = 0.5f;
        [SerializeField, Min(1)] private float repositionCooldown = 3f;
        [SerializeField, Min(0)] private float retreatDistance = 0.8f;
        private static readonly Vector2[] EntryOffsets =
        {
            new Vector2(0, 0), new Vector2(-1.25f, -0.25f), new Vector2(0.7f, 0.85f),
            new Vector2(-1.65f, 1.2f), new Vector2(-0.25f, 1.65f), new Vector2(-2.35f, 0.25f),
            new Vector2(0.95f, 1.75f), new Vector2(-0.65f, 0.55f), new Vector2(-2.4f, 1.9f),
            new Vector2(0.3f, 0.4f), new Vector2(-1.15f, 2.05f), new Vector2(-1.9f, -0.1f)
        };
        [SerializeField, Range(0, 0.6f)] private float knockbackDistance = 0.3f;
        [SerializeField] private bool randomizeSeedEachRun = true;
        [SerializeField] private int fixedSeed = 12345;
        private int seedCounter;
        [SerializeField] private bool showHitEffects = true, showDamageNumbers = true, playCombatSounds = true;
        [SerializeField] private bool showAttackArea;
        [SerializeField] private bool showSwordMotion = true;
        private LineRenderer attackArea;
        [SerializeField, Range(0, 1)] private float combatVolume = 0.18f;
        private PrototypeCombatFeedback feedback;
        private bool wallTest, trapTest, bossMode;
        [SerializeField, Min(1)] private int bossHealth = 600;
        [SerializeField, Min(0.1f)] private float bossTelegraph = 1.1f;
        [SerializeField, Min(0.1f)] private float bossChargeSpeed = 5, bossChargeDistance = 3.2f;
        private SpriteRenderer chargeWarning;
        private LineRenderer sweepWarning;
        private CombatSimulation simulation;
        private Sprite square;
        private Texture2D texture;
        private ActorView heroView;
        private ActorView[] monsterViews;
        private Camera viewCamera;
        [SerializeField] private bool enableViewComparison;
        [SerializeField] private int comparisonRendererIndex = -1;
        [SerializeField] private Material comparisonSpriteMaterial, comparisonStoneMaterial;
        private bool quarterView = true;
        private QuarterViewStage quarterStage;
        private SpriteRenderer flatArena;
        private int actorViewSequence;
        private bool QuarterView => quarterStage != null && quarterView;
        private Vector3 DisplayPoint(Point p, float height = 0) => QuarterView ? QuarterViewStage.Ground(p) + viewCamera.transform.up * height : new Vector3(p.X, p.Y + height, 0);
        private Vector2 DisplayVector(Point p) => QuarterView ? (Vector2)QuarterViewStage.BillboardDelta(p) : new Vector2(p.X, p.Y);
        private sealed class ActorView
        {
            public Transform Root, Facing, Slash, Windup;
            public QuarterViewStage.Actor QuarterActor;
            public SortingGroup Group;
            public int StableOrder;
            public SpriteRenderer Foot;
            public SpriteRenderer WindupRenderer;
            public PrototypeCombatFeedback.ActorFeedback Feedback;
            public SpriteRenderer Body, Blade, Direction, HealthBack, HealthFill, TargetMarker;
            public Color Color;
            public float DeathTime;
            public Vector2 BodySize = new Vector2(0.4f, 0.6f);
        }
        private void Start()
        {
            texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.white); texture.Apply();
            square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            var cameraObject = new GameObject("Fixed Camera"); cameraObject.transform.SetParent(transform);
            viewCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            feedback = new PrototypeCombatFeedback(transform, square);
            viewCamera.orthographic = true; viewCamera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0, 0.6f, -10), Quaternion.identity);
            flatArena = MakeSprite("Arena", transform, new Vector3(0, 0.5f, 0), new Vector2(4.56f, 7.56f), new Color(0.15f, 0.19f, 0.21f), -1000);
            if (enableViewComparison && comparisonRendererIndex >= 0)
                quarterStage = new QuarterViewStage(transform, comparisonSpriteMaterial, comparisonStoneMaterial);
            feedback.ProjectPoint = DisplayPoint;
            ConfigureView();
            heroView = MakeActor("Hero", new Color(0.25f, 0.7f, 0.95f));
            var guide = new GameObject("Attack area guide"); guide.transform.SetParent(heroView.Root, false);
            attackArea = guide.AddComponent<LineRenderer>(); attackArea.useWorldSpace = false;
            attackArea.sharedMaterial = heroView.Body.sharedMaterial; attackArea.positionCount = 19;
            attackArea.startWidth = attackArea.endWidth = 0.018f; attackArea.enabled = false;
            Restart();
        }
        private void ConfigureView()
        {
            bool hybrid = QuarterView;
            flatArena.enabled = !hybrid;
            if (quarterStage != null) quarterStage.Root.gameObject.SetActive(hybrid);
            var data = viewCamera.GetUniversalAdditionalCameraData();
            data.SetRenderer(hybrid ? comparisonRendererIndex : -1);
            Quaternion rotation = hybrid ? quarterStage.Rotation : Quaternion.identity;
            Vector3 target = hybrid ? new Vector3(0, .3f, .5f) : new Vector3(0, .6f, 0);
            viewCamera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * (hybrid ? 12 : 10), rotation);
        }
        private void Restart()
        {
            if (chargeWarning == null)
                chargeWarning = MakeSprite("Charge warning", transform, Vector3.zero, Vector2.one, new Color(1, 0.3f, 0.1f, 0.3f), -500);
            chargeWarning.enabled = false;
            if (sweepWarning == null)
            {
                var guide = new GameObject("Boss sweep area"); guide.transform.SetParent(transform, false);
                sweepWarning = guide.AddComponent<LineRenderer>(); sweepWarning.useWorldSpace = false;
                sweepWarning.sharedMaterial = chargeWarning.sharedMaterial;
                sweepWarning.positionCount = 22; sweepWarning.startWidth = sweepWarning.endWidth = 0.035f;
                sweepWarning.startColor = sweepWarning.endColor = new Color(1, 0.45f, 0.15f, 0.8f);
                sweepWarning.sortingOrder = -490;
            }
            sweepWarning.enabled = false;
            feedback.ResetRun(); feedback.Reset(heroView.Feedback);
            if (monsterViews != null)
                for (int i = 0; i < monsterViews.Length; i++)
                    if (monsterViews[i] != null) Destroy(monsterViews[i].Root.gameObject);
            actorViewSequence = 1;
            stage = null;
            bool finite = stageMode && !bossMode && !wallTest && !trapTest;
            StageSettings stageSettings = finite ? (stageDefinition != null ? stageDefinition.Snapshot() : new StageSettings()) : null;
            int count = bossMode ? 1 : finite ? stageSettings.Total : trapTest ? 8 : wallTest ? 3 : Mathf.Clamp(monsterCount, 1, 12);
            Vector2 playerStart = trapTest ? new Vector2(0, -1.5f) : wallTest ? new Vector2(-2, -2) : heroStart;
            monsterViews = new ActorView[count]; monsterKinds = new PrototypeMonsterKind[count];
            normalCount = fastCount = heavyCount = 0;
            // Capture run definitions once; later arrivals cannot read edited assets mid-run.
            FastMonsterSettings fast = fastMonsterDefinition != null ? fastMonsterDefinition.Snapshot() : new FastMonsterSettings();
            DamageProfile heroRules = damageDefinition != null ? damageDefinition.Snapshot(true) : new DamageProfile(0.85f, 1.15f, 0.15f);
            DamageProfile enemyRules = damageDefinition != null ? damageDefinition.Snapshot(false) : new DamageProfile(0.85f, 1.15f, 0.05f);
            bool mixed = tacticsDefinition != null && tacticsDefinition.mixedThreats, includeFast = includeFastMonsters;
            int baseHealth = monsterHealth, baseDamage = monsterDamage;
            int normalDefense = damageDefinition != null ? damageDefinition.monsterDefense : 0;
            int heavyDefense = damageDefinition != null ? damageDefinition.strongMonsterDefense : 3;
            float heavyThreat = tacticsDefinition != null ? tacticsDefinition.strongEnemyThreat : 1.8f;
            bool trap = trapTest;
            Fighter CreateEnemy(int index, Point position)
            {
                if (bossMode)
                {
                    monsterKinds[index] = PrototypeMonsterKind.Heavy; heavyCount++;
                    return new Fighter(position, bossHealth, Mathf.Max(1, Mathf.RoundToInt(baseDamage * 1.6f)), 1.55f, 0.85f, 0.32f, 0.48f,
                        defense: heavyDefense, damageProfile: enemyRules);
                }
                PrototypeMonsterKind kind = monsterKinds[index] = FastMonsterSettings.KindAt(index, mixed, includeFast);
                bool strong = kind == PrototypeMonsterKind.Heavy;
                if (kind == PrototypeMonsterKind.Fast)
                { fastCount++; return fast.Create(position, baseHealth, baseDamage, normalDefense, enemyRules, trap ? 120 : 1); }
                if (strong) heavyCount++; else normalCount++;
                return new Fighter(position, trap ? Mathf.Max(120, baseHealth) : baseHealth,
                    strong ? Mathf.Max(1, Mathf.RoundToInt(baseDamage * 1.5f)) : baseDamage,
                    strong ? 1.05f : 1.4f, 0.85f, strong ? 0.5f : 0.3f, strong ? 0.9f : 0.7f,
                    defense: strong ? heavyDefense : normalDefense, threatWeight: strong ? heavyThreat : 1, damageProfile: enemyRules);
            }
            var monsters = finite ? new Fighter[0] : new Fighter[count];
            for (int i = 0; i < monsters.Length; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector2 start = trapTest ? playerStart + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.65f : wallTest ? (i == 0 ? new Vector2(-2, -1.35f) :
                    i == 1 ? new Vector2(-1.4f, -1.9f) : new Vector2(-1.4f, -1.35f)) : monsterStart + EntryOffsets[i];
                start.x = Mathf.Clamp(start.x, -2, 2); start.y = Mathf.Clamp(start.y, -3, 4);
                monsters[i] = CreateEnemy(i, new Point(start.x, start.y));
            }
            int seed = randomizeSeedEachRun ? unchecked((int)System.DateTime.UtcNow.Ticks ^ (++seedCounter * 16777619)) : fixedSeed;
            simulation = new CombatSimulation(
                new Fighter(new Point(playerStart.x, playerStart.y), heroHealth, heroDamage, 1.9f, 0.85f,
                    defense: damageDefinition != null ? damageDefinition.heroDefense : 1, damageProfile: heroRules),
                monsters, comboDefinition != null ? comboDefinition.Snapshot() : AttackStep.DefaultCombo(),
                new RepositionSettings(enableReposition, pressureThreshold, repositionDistance, repositionCooldown, retreatDistance), trapTest ? 0 : knockbackDistance,
                tacticsDefinition != null ? tacticsDefinition.Snapshot() : new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f),
                seed, finite ? count : 0);
            if (bossMode) simulation.EnableChargeBoss(monsters[0], bossTelegraph, bossChargeSpeed, bossChargeDistance);
            if (finite) stage = new StageEncounter(simulation, stageSettings, CreateEnemy);
            SyncMonsterViews();
        }
        private void SyncMonsterViews()
        {
            for (int i = 0; i < simulation.Monsters.Count; i++)
            {
                if (monsterViews[i] != null) continue;
                PrototypeMonsterKind kind = monsterKinds[i]; bool strong = kind == PrototypeMonsterKind.Heavy;
                Color color = kind == PrototypeMonsterKind.Fast ? new Color(1, 0.85f, 0.2f) :
                    strong ? new Color(0.7f, 0.35f, 0.85f) : new Color(0.95f, 0.4f, 0.3f);
                ActorView view = monsterViews[i] = MakeActor($"Monster {i + 1} ({kind})", color);
                if (kind == PrototypeMonsterKind.Fast)
                { view.BodySize = new Vector2(0.3f, 0.42f); view.Body.transform.localRotation = Quaternion.Euler(0, 0, 45); }
                else if (strong) view.BodySize = new Vector2(0.5f, 0.7f);
            }
        }
        private SpriteRenderer MakeSprite(string label, Transform parent, Vector3 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(label); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = square; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }
        private ActorView MakeActor(string label, Color color)
        {
            var root = new GameObject(label).transform; root.SetParent(transform, false);
            var result = new ActorView { Root = root, Color = color, StableOrder = actorViewSequence++ };
            result.Foot = MakeSprite("Foot", root, Vector3.zero, new Vector2(0.56f, 0.18f), Color.black, 0);
            result.Body = MakeSprite("Body", root, new Vector3(0, 0.3f, 0), new Vector2(0.4f, 0.6f), color, 1);
            result.Direction = MakeSprite("Four Direction", root, Vector3.zero, new Vector2(0.13f, 0.13f), Color.white, 2);
            result.Facing = result.Direction.transform;
            result.HealthBack = MakeSprite("Health Background", root, new Vector3(0, QuarterView ? 1.6f : 0.76f, 0), new Vector2(0.55f, 0.06f), Color.black, 5);
            result.HealthFill = MakeSprite("Health", root, new Vector3(0, QuarterView ? 1.6f : 0.76f, 0), new Vector2(0.55f, 0.04f), Color.green, 6);
            result.TargetMarker = MakeSprite("Current Target", root, new Vector3(0, -0.13f, 0), new Vector2(0.38f, 0.04f), Color.yellow, 4);
            result.Feedback = feedback.CreateActor(root);
            result.WindupRenderer = MakeSprite("Attack preparation", root, new Vector3(0, 0.3f, 0), new Vector2(0.3f, 0.04f), Color.yellow, 3);
            result.Windup = result.WindupRenderer.transform;
            result.Blade = MakeSprite("Diagonal Slash", root, Vector3.zero, new Vector2(0.85f, 0.07f), Color.yellow, 3);
            result.Slash = result.Blade.transform;
            if (quarterStage != null)
            {
                result.Group = root.gameObject.AddComponent<SortingGroup>();
                result.QuarterActor = quarterStage.CreateActor(root, color, bossMode && label != "Hero", label.Contains("Fast"), label == "Hero");
            }
            return result;
        }
        private void Update()
        {
            if (simulation == null) return;
            if (stage != null) stage.Advance(Time.deltaTime); else simulation.Advance(Time.deltaTime);
            SyncMonsterViews();
            // Preserve the entire field on narrow portrait displays, with a fixed orientation.
            chargeWarning.enabled = bossMode && simulation.Hero.Alive && simulation.Monster.Alive &&
                (simulation.BossPhase == ChargePhase.Telegraph || simulation.BossPhase == ChargePhase.Charging);
            if (chargeWarning.enabled)
            {
                Point a = simulation.ChargeOrigin, b = simulation.ChargeEnd, d = b - a;
                chargeWarning.transform.localPosition = QuarterView ? QuarterViewStage.Ground((a + b) * .5f, .04f) : new Vector3((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f, 0);
                chargeWarning.transform.localRotation = QuarterView ? Quaternion.LookRotation(Vector3.down, new Vector3(-d.Y, 0, d.X)) : Quaternion.Euler(0, 0, Mathf.Atan2(d.Y, d.X) * Mathf.Rad2Deg);
                chargeWarning.transform.localScale = new Vector3(d.Length, CombatSimulation.FootRadius * 4, 1);
            }
            sweepWarning.enabled = bossMode && simulation.Hero.Alive && simulation.Monster.Alive && simulation.BossPhase == ChargePhase.Sweep;
            if (sweepWarning.enabled)
            {
                Fighter boss = simulation.ChargeBoss;
                float heading = Mathf.Atan2(boss.AttackAim.Y, boss.AttackAim.X);
                sweepWarning.useWorldSpace = QuarterView;
                sweepWarning.transform.localPosition = QuarterView ? Vector3.zero : new Vector3(boss.Position.X, boss.Position.Y, 0);
                sweepWarning.SetPosition(0, QuarterView ? QuarterViewStage.Ground(boss.Position, .045f) : Vector3.zero);
                for (int i = 0; i < 20; i++)
                {
                    float angle = heading + Mathf.Deg2Rad * Mathf.Lerp(-CombatSimulation.BossSweepArc * 0.5f, CombatSimulation.BossSweepArc * 0.5f, i / 19f);
                    Point offset = new Point(Mathf.Cos(angle), Mathf.Sin(angle)) * simulation.BossSweepRange;
                    sweepWarning.SetPosition(i + 1, QuarterView ? QuarterViewStage.Ground(boss.Position + offset, .045f) : new Vector3(offset.X, offset.Y, 0));
                }
                sweepWarning.SetPosition(21, QuarterView ? QuarterViewStage.Ground(boss.Position, .045f) : Vector3.zero);
            }
            viewCamera.orthographicSize = Mathf.Max(QuarterView ? 3.9f : 4.7f, (QuarterView ? 3.15f : 2.65f) / viewCamera.aspect);
            feedback.Effects = showHitEffects; feedback.Numbers = showDamageNumbers;
            feedback.Sound = playCombatSounds; feedback.Volume = combatVolume;
            feedback.BeginFrame(simulation.Hero);
            feedback.UpdateActor(heroView.Feedback, simulation.Hero, true, Time.unscaledDeltaTime);
            Draw(heroView, simulation.Hero, false);
            if (!QuarterView && simulation.GuardActive) heroView.Body.color = new Color(0.4f, 1, 0.85f);
            DrawAttackArea();
            for (int i = 0; i < simulation.Monsters.Count; i++)
            {
                if (!simulation.Monsters[i].Alive) monsterViews[i].DeathTime += Time.unscaledDeltaTime;
                monsterViews[i].Root.gameObject.SetActive(monsterViews[i].DeathTime < 1);
                feedback.UpdateActor(monsterViews[i].Feedback, simulation.Monsters[i], false, Time.unscaledDeltaTime);
                Draw(monsterViews[i], simulation.Monsters[i], simulation.Hero.Target == simulation.Monsters[i]);
            }
        }
        private void DrawAttackArea()
        {
            Fighter actor = simulation.Hero; AttackStep step = actor.CurrentAttack;
            attackArea.enabled = showAttackArea && actor.Alive && actor.State == CombatState.Attack && actor.PhaseTime < step.Windup + step.Active;
            if (!attackArea.enabled) return;
            float angle = Mathf.Atan2(actor.AttackAim.Y, actor.AttackAim.X);
            float half = step.Arc * Mathf.Deg2Rad * 0.5f;
            float range = actor.Range * step.RangeScale;
            Color color = PrototypeCombatFeedback.AttackColor(step.Kind); color.a = 0.35f;
            attackArea.startColor = attackArea.endColor = color;
            attackArea.sortingOrder = 101 - Mathf.RoundToInt(actor.Position.Y * 20);
            attackArea.useWorldSpace = QuarterView;
            attackArea.SetPosition(0, QuarterView ? QuarterViewStage.Ground(actor.Position, .05f) : Vector3.zero);
            for (int i = 0; i <= 16; i++)
            {
                float direction = angle - half + 2 * half * i / 16;
                Point offset = new Point(Mathf.Cos(direction) * range, Mathf.Sin(direction) * range);
                attackArea.SetPosition(i + 1, QuarterView ? QuarterViewStage.Ground(actor.Position + offset, .05f) : new Vector3(offset.X, offset.Y, 0));
            }
            attackArea.SetPosition(18, QuarterView ? QuarterViewStage.Ground(actor.Position, .05f) : Vector3.zero);
        }
        private void Draw(ActorView view, Fighter fighter, bool targeted)
        {
            view.Root.localPosition = QuarterView ? QuarterViewStage.Ground(fighter.Position) : new Vector3(fighter.Position.X, fighter.Position.Y, 0);
            view.Root.localRotation = QuarterView ? quarterStage.Rotation : Quaternion.identity;
            if (view.Group != null)
            {
                view.Group.enabled = QuarterView;
                view.Group.sortingOrder = 10000 - Mathf.RoundToInt(fighter.Position.Y * 32) * 128 + 127 - view.StableOrder;
                view.QuarterActor.Root.gameObject.SetActive(QuarterView);
                if (QuarterView) view.QuarterActor.Draw(fighter, fighter == simulation.Hero && simulation.GuardActive, showSwordMotion, showHitEffects);
            }
            view.Body.enabled = !QuarterView; view.Foot.enabled = !QuarterView;
            int depth = 100 - Mathf.RoundToInt(fighter.Position.Y * 20);
            view.Body.sortingOrder = depth;
            var aim = new Vector2(fighter.Aim.X, fighter.Aim.Y);
            Vector2 cardinal = Mathf.Abs(aim.x) > Mathf.Abs(aim.y) ? new Vector2(Mathf.Sign(aim.x), 0) : new Vector2(0, Mathf.Sign(aim.y));
            view.Facing.localPosition = (Vector3)(cardinal * 0.25f) + new Vector3(0, 0.3f, 0);
            view.Direction.sortingOrder = depth + 1;
            view.Direction.enabled = fighter.Alive && !QuarterView;
            view.TargetMarker.enabled = targeted && fighter.Alive;
            view.TargetMarker.sortingOrder = depth + 1;
            view.HealthBack.enabled = view.HealthFill.enabled = fighter.Alive;
            view.HealthBack.sortingOrder = depth + 3; view.HealthFill.sortingOrder = depth + 4;
            float barHeight = QuarterView ? view.QuarterActor.Height + .12f : .76f;
            view.HealthBack.transform.localPosition = new Vector3(0, barHeight, 0);
            float health = (float)fighter.Health / fighter.MaxHealth;
            view.HealthFill.transform.localScale = new Vector3(0.55f * health, 0.04f, 1);
            view.HealthFill.transform.localPosition = new Vector3(-0.275f * (1 - health), barHeight, 0);
            view.Body.color = fighter.HitFlash > 0 ? (fighter.LastHitKind == AttackKind.Thrust ? new Color(0.6f, 1, 1) : Color.white) : fighter.Alive ? view.Color : Color.gray;
            Vector2 recoil = fighter.Alive ? new Vector2(fighter.HitDirection.X, fighter.HitDirection.Y) * fighter.HitFlash * 0.5f : Vector2.zero;
            view.Body.transform.localPosition = new Vector3(recoil.x, 0.3f + recoil.y, 0);
            view.Body.transform.localScale = fighter.Alive ? new Vector3(view.BodySize.x, view.BodySize.y, 1) : new Vector3(0.5f, 0.15f, 1);
            bool heldSword = QuarterView && fighter == simulation.Hero && showSwordMotion;
            view.Blade.enabled = !heldSword && fighter.Alive && fighter.SlashTime > 0;
            Vector2 attack = DisplayVector(fighter.AttackAim);
            AttackStep step = fighter.CurrentAttack;
            float duration = Mathf.Max(0.01f, step.Active);
            float progress = Mathf.Clamp01(1 - fighter.SlashTime / duration);
            bool thrust = step.Kind == AttackKind.Thrust;
            bool preparing = fighter.Alive && fighter.State == CombatState.Attack && fighter.PhaseTime < step.Windup;
            view.WindupRenderer.enabled = preparing && !heldSword;
            float preparation = step.Windup > 0 ? Mathf.Clamp01(fighter.PhaseTime / step.Windup) : 0;
            float groundAngle = Mathf.Atan2(fighter.AttackAim.Y, fighter.AttackAim.X) * Mathf.Rad2Deg;
            Vector2 ProjectHeading(float degrees) => DisplayVector(new Point(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad)));
            Vector2 preparationDirection = ProjectHeading(groundAngle + (thrust ? 0 : step.Kind == AttackKind.ReverseSlash ? 65 : -55));
            float windupAngle = Mathf.Atan2(preparationDirection.y, preparationDirection.x) * Mathf.Rad2Deg;
            view.Windup.localRotation = Quaternion.Euler(0, 0, windupAngle);
            view.Windup.localPosition = (Vector3)(attack * Mathf.Lerp(0.15f, 0.3f, preparation)) + new Vector3(0, QuarterView ? .65f : .3f, 0);
            view.Windup.localScale = new Vector3(Mathf.Lerp(0.15f, step.Kind == AttackKind.Breakthrough ? 0.65f : 0.35f, preparation), step.Kind == AttackKind.Breakthrough ? 0.1f : 0.045f, 1);
            Color prepareColor = PrototypeCombatFeedback.AttackColor(step.Kind);
            view.WindupRenderer.color = new Color(prepareColor.r, prepareColor.g, prepareColor.b, Mathf.Lerp(0.2f, 0.85f, preparation));
            view.WindupRenderer.sortingOrder = depth + 2;
            float sweep = step.Kind == AttackKind.ReverseSlash ? 65 - progress * 130 : -55 + progress * 110;
            view.Slash.localPosition = (Vector3)(attack * (thrust ? 0.35f + Mathf.Sin(progress * Mathf.PI) * 0.4f : 0.5f)) + new Vector3(0, QuarterView ? .65f : .3f, 0);
            Vector2 bladeDirection = ProjectHeading(groundAngle + (thrust ? 0 : sweep));
            view.Slash.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(bladeDirection.y, bladeDirection.x) * Mathf.Rad2Deg);
            view.Slash.localScale = new Vector3((thrust ? .75f : .85f) * bladeDirection.magnitude, thrust ? .11f : .07f, 1);
            view.Blade.color = step.Kind == AttackKind.Breakthrough ? new Color(1, 0.35f, 0.15f) : thrust ? new Color(0.5f, 1, 1) : step.Kind == AttackKind.ReverseSlash ? new Color(1, 0.6f, 0.2f) : Color.yellow;
            view.Blade.sortingOrder = depth + 2;
        }
        private void OnGUI()
        {
            if (simulation == null) return;
            if (enableViewComparison) { DrawComparisonHUD(); return; }
            feedback.DrawNumber(heroView.Feedback, viewCamera);
            for (int i = 0; i < simulation.Monsters.Count; i++) feedback.DrawNumber(monsterViews[i].Feedback, viewCamera);
            showHitEffects = GUI.Toggle(new Rect(12, 430, 110, 30), showHitEffects, "Hit sparks");
            showDamageNumbers = GUI.Toggle(new Rect(130, 430, 100, 30), showDamageNumbers, "Damage");
            playCombatSounds = GUI.Toggle(new Rect(245, 430, 110, 30), playCombatSounds, "Sound");
            showAttackArea = GUI.Toggle(new Rect(12, 462, 160, 30), showAttackArea, "Attack area");
            bool fastEnemies = GUI.Toggle(new Rect(12, 494, 180, 30), includeFastMonsters, "Include fast foes");
            if (fastEnemies != includeFastMonsters) { includeFastMonsters = fastEnemies; Restart(); }
            GUI.Label(new Rect(12, 524, 400, 30), $"Roster: Normal {normalCount} | Fast {fastCount} | Heavy {heavyCount}");
            GUI.Label(new Rect(12, 12, 400, 30), $"Hero {simulation.Hero.Health}/{heroHealth}  {simulation.Hero.State}");
            GUI.Label(new Rect(12, 40, 400, 30), $"Enemies {simulation.LivingMonsters}/{simulation.Monsters.Count} | Swing hits: {simulation.Hero.LastHitCount}");
            if (GUI.Button(new Rect(160, 558, 140, 32), "Charge boss"))
            { bossMode = true; stageMode = trapTest = wallTest = false; Restart(); }
            if (bossMode && simulation.Hero.Alive && simulation.Monster.Alive)
            {
                float width = Mathf.Min(280, Screen.width - 24);
                bool ready = simulation.GuardSecondsRemaining <= 0;
                GUI.enabled = ready;
                string label = simulation.GuardActive ? "FOCUS GUARD — ACTIVE" : ready ? "FOCUS GUARD" : $"GUARD {simulation.GuardSecondsRemaining:F1}s";
                if (GUI.Button(new Rect(Screen.width - width - 12, Screen.height - 110, width, 64), label))
                    simulation.RequestBossGuard();
                GUI.enabled = true;
                GUI.Label(new Rect(12, Screen.height - 40, Screen.width - 24, 28), "Time your guard. Boss attacks are not auto-dodged.");
            }
            if (bossMode) GUI.Label(new Rect(12, 594, 500, 30), $"Boss: {simulation.BossPhase} | Charge {simulation.Charges} / Sweep {simulation.BossSweeps} {(simulation.BossEnraged ? "ENRAGED" : "")}");
            if (GUI.Button(new Rect(12, 558, 140, 32), "Stage run"))
            { bossMode = false; stageMode = true; trapTest = wallTest = false; Restart(); }
            if (stage != null)
            {
                GUI.Label(new Rect(12, 594, 500, 30), $"Kills {stage.Kills}/{stage.Settings.Total} | Pending {stage.Pending} | Time {stage.SecondsRemaining:F1}s");
                GUI.Label(new Rect(12, 624, 500, 30), $"Stage: {stage.Result} | Alive {simulation.LivingMonsters}/{stage.Settings.MaxAlive}");
                if (stage.Result != StageResult.Running)
                    GUI.Label(new Rect(12, 216, 400, 30), stage.Result == StageResult.Clear ? "CLEAR" : stage.Result == StageResult.Timeout ? "TIME UP" : "DEFEAT");
            }
            if (stage == null && (!simulation.Hero.Alive || simulation.LivingMonsters == 0))
                GUI.Label(new Rect(12, 216, 400, 30), simulation.Hero.Alive ? "CLEAR" : "DEFEAT");
            GUI.Label(new Rect(12, 68, 500, 30), $"Pressure {simulation.Pressure} | Repositions {simulation.Repositions} | CD {simulation.RepositionCooldown:F1}s");
            GUI.Label(new Rect(12, 94, 500, 30), $"Rear limit {simulation.RetreatFloor:F2} | Hero Y {simulation.Hero.Position.Y:F2}");
            GUI.Label(new Rect(12, 278, 500, 30), $"Attack: {simulation.Hero.CurrentAttack.Kind} | Combo slot: {simulation.Hero.ComboIndex + 1}");
            GUI.Label(new Rect(12, 308, 500, 30), $"Blocked {simulation.BlockedSeconds:F1}s | Breakthroughs {simulation.Breakthroughs} | CD {simulation.BreakthroughCooldown:F1}s");
            GUI.Label(new Rect(12, 388, 500, 30), $"Seed {simulation.Seed} | Move choices {simulation.MovementChoices} | Target choices {simulation.TargetChoices}");
            bool randomSeed = GUI.Toggle(new Rect(12, 350, 250, 30), randomizeSeedEachRun, "New seed each restart");
            if (randomSeed != randomizeSeedEachRun) { randomizeSeedEachRun = randomSeed; Restart(); }
            float targetDistance = simulation.Hero.Target == null ? 0 :
                (simulation.Hero.Target.Position - simulation.Hero.Position).Length;
            GUI.Label(new Rect(12, 248, 500, 30), $"Hero attacks {simulation.Hero.Attacks} | Target distance {targetDistance:F3}");
            if (GUI.Button(new Rect(12, 128, 100, 38), "Restart")) Restart();
            if (GUI.Button(new Rect(118, 128, 70, 38), "5 foes")) { bossMode = false; stageMode = false; trapTest = false; wallTest = false; monsterCount = 5; Restart(); }
            if (GUI.Button(new Rect(194, 128, 70, 38), "8 foes")) { bossMode = false; stageMode = false; trapTest = false; wallTest = false; monsterCount = 8; Restart(); }
            if (GUI.Button(new Rect(270, 128, 70, 38), "12 foes")) { bossMode = false; stageMode = false; trapTest = false; wallTest = false; monsterCount = 12; Restart(); }
            if (GUI.Button(new Rect(270, 176, 90, 30), "Wall test")) { bossMode = false; stageMode = false; trapTest = false; wallTest = true; Restart(); }
            if (GUI.Button(new Rect(270, 350, 90, 30), "Trap test")) { bossMode = false; stageMode = false; wallTest = false; trapTest = true; Restart(); }
            bool reposition = GUI.Toggle(new Rect(12, 176, 260, 30), enableReposition, "Pressure reposition");
            if (reposition != enableReposition) { enableReposition = reposition; Restart(); }
        }
        private void DrawComparisonHUD()
        {
            feedback.DrawNumber(heroView.Feedback, viewCamera);
            for (int i = 0; i < simulation.Monsters.Count; i++) feedback.DrawNumber(monsterViews[i].Feedback, viewCamera);
            Rect safe = Screen.safeArea;
            float scale = Mathf.Max(.5f, Mathf.Min(safe.width / 380, safe.height / 640));
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0), Quaternion.identity, Vector3.one * scale);
            float width = safe.width / scale, height = safe.height / scale;
            GUI.Box(new Rect(8, 8, width - 16, 112), "");
            GUI.Label(new Rect(16, 12, width - 32, 24), $"VIEW STUDY | {(QuarterView ? "2D sprites + 3D" : "Original 2D")}");
            GUI.Label(new Rect(16, 36, width - 32, 24), $"Hero {simulation.Hero.Health}/{heroHealth} | Foes {simulation.LivingMonsters}");
            GUI.Label(new Rect(16, 60, width - 32, 24), stage != null ? $"{stage.Result} | {stage.Kills}/{stage.Settings.Total} | {stage.SecondsRemaining:F1}s" : bossMode ? $"Boss {simulation.BossPhase} | Guarded {simulation.GuardedHits}" : $"Seed {simulation.Seed} | {simulation.Hero.State}");
            if (GUI.Button(new Rect(16, 88, 166, 26), "Switch view (same run)")) { quarterView = !quarterView; ConfigureView(); }
            if (GUI.Button(new Rect(width - 100, 88, 84, 26), "Restart")) Restart();
            if (!simulation.Hero.Alive || simulation.LivingMonsters == 0 && stage == null)
                GUI.Label(new Rect(16, 130, width - 32, 28), simulation.Hero.Alive ? "CLEAR" : "DEFEAT");
            if (GUI.Button(new Rect(12, height - 160, 76, 32), "1 foe")) { bossMode = stageMode = trapTest = wallTest = false; monsterCount = 1; Restart(); }
            if (GUI.Button(new Rect(92, height - 160, 76, 32), "5 foes")) { bossMode = stageMode = trapTest = wallTest = false; monsterCount = 5; Restart(); }
            if (GUI.Button(new Rect(172, height - 160, 76, 32), "8 foes")) { bossMode = stageMode = trapTest = wallTest = false; monsterCount = 8; Restart(); }
            if (GUI.Button(new Rect(252, height - 160, 76, 32), "12 foes")) { bossMode = stageMode = trapTest = wallTest = false; monsterCount = 12; Restart(); }
            if (GUI.Button(new Rect(128, height - 120, 112, 32), "Stage run")) { bossMode = trapTest = wallTest = false; stageMode = true; Restart(); }
            if (GUI.Button(new Rect(12, height - 120, 112, 32), "Charge boss")) { bossMode = true; stageMode = trapTest = wallTest = false; Restart(); }
            showSwordMotion = GUI.Toggle(new Rect(16, height - 198, 150, 28), showSwordMotion, "Sword motion");
            showAttackArea = GUI.Toggle(new Rect(180, height - 198, 150, 28), showAttackArea, "Attack area");
            if (bossMode && simulation.Hero.Alive && simulation.Monster.Alive)
            {
                GUI.enabled = simulation.GuardSecondsRemaining <= 0;
                if (GUI.Button(new Rect(12, height - 76, width - 24, 60), simulation.GuardActive ? "FOCUS GUARD ACTIVE" : GUI.enabled ? "FOCUS GUARD" : $"GUARD {simulation.GuardSecondsRemaining:F1}s")) simulation.RequestBossGuard();
                GUI.enabled = true;
            }
            GUI.matrix = previous;
        }
        private void OnDestroy() { quarterStage?.Dispose(); feedback?.Dispose(); if (square != null) Destroy(square); if (texture != null) Destroy(texture); }
    }
}
