using EnhancementTower.Combat;
using EnhancementTower.Data;
using UnityEngine;

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
        private LineRenderer attackArea;
        [SerializeField, Range(0, 1)] private float combatVolume = 0.18f;
        private PrototypeCombatFeedback feedback;
        private bool wallTest, trapTest;
        private CombatSimulation simulation;
        private Sprite square;
        private Texture2D texture;
        private ActorView heroView;
        private ActorView[] monsterViews;
        private Camera viewCamera;
        private sealed class ActorView
        {
            public Transform Root, Facing, Slash, Windup;
            public SpriteRenderer WindupRenderer;
            public PrototypeCombatFeedback.ActorFeedback Feedback;
            public SpriteRenderer Body, Blade, Direction, HealthBack, HealthFill, TargetMarker;
            public Color Color;
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
            MakeSprite("Arena", transform, new Vector3(0, 0.5f, 0), new Vector2(4.56f, 7.56f), new Color(0.15f, 0.19f, 0.21f), -1000);
            heroView = MakeActor("Hero", new Color(0.25f, 0.7f, 0.95f));
            var guide = new GameObject("Attack area guide"); guide.transform.SetParent(heroView.Root, false);
            attackArea = guide.AddComponent<LineRenderer>(); attackArea.useWorldSpace = false;
            attackArea.sharedMaterial = heroView.Body.sharedMaterial; attackArea.positionCount = 19;
            attackArea.startWidth = attackArea.endWidth = 0.018f; attackArea.enabled = false;
            Restart();
        }
        private void Restart()
        {
            feedback.ResetRun(); feedback.Reset(heroView.Feedback);
            if (monsterViews != null)
                for (int i = 0; i < monsterViews.Length; i++) Destroy(monsterViews[i].Root.gameObject);
            int count = trapTest ? 8 : wallTest ? 3 : Mathf.Clamp(monsterCount, 1, 12);
            Vector2 playerStart = trapTest ? new Vector2(0, -1.5f) : wallTest ? new Vector2(-2, -2) : heroStart;
            monsterViews = new ActorView[count];
            var monsters = new Fighter[count];
            DamageProfile heroDamageRules = damageDefinition != null ? damageDefinition.Snapshot(true) : new DamageProfile(0.85f, 1.15f, 0.15f);
            DamageProfile monsterDamageRules = damageDefinition != null ? damageDefinition.Snapshot(false) : new DamageProfile(0.85f, 1.15f, 0.05f);
            for (int i = 0; i < count; i++)
            {
                // Authored scattered entry points, not movement lanes.
                float angle = i * Mathf.PI * 0.25f;
                Vector2 start = trapTest ? playerStart + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.65f : wallTest ? (i == 0 ? new Vector2(-2, -1.35f) :
                    i == 1 ? new Vector2(-1.4f, -1.9f) : new Vector2(-1.4f, -1.35f)) : monsterStart + EntryOffsets[i];
                start.x = Mathf.Clamp(start.x, -2, 2); start.y = Mathf.Clamp(start.y, -3, 4);
                bool strong = tacticsDefinition != null && tacticsDefinition.mixedThreats && i % 4 == 3;
                float threat = strong ? tacticsDefinition.strongEnemyThreat : 1;
                monsters[i] = new Fighter(new Point(start.x, start.y), trapTest ? Mathf.Max(120, monsterHealth) : monsterHealth,
                    strong ? Mathf.Max(1, Mathf.RoundToInt(monsterDamage * 1.5f)) : monsterDamage,
                    strong ? 1.05f : 1.4f, 0.85f, strong ? 0.5f : 0.3f, strong ? 0.9f : 0.7f, defense: damageDefinition != null ? (strong ? damageDefinition.strongMonsterDefense : damageDefinition.monsterDefense) : (strong ? 3 : 0),
                    threatWeight: threat, damageProfile: monsterDamageRules);
                monsterViews[i] = MakeActor($"Monster {i + 1}", strong ? new Color(0.7f, 0.35f, 0.85f) : new Color(0.95f, 0.4f, 0.3f));
            }
            int seed = randomizeSeedEachRun ? unchecked((int)System.DateTime.UtcNow.Ticks ^ (++seedCounter * 16777619)) : fixedSeed;
            simulation = new CombatSimulation(
                new Fighter(new Point(playerStart.x, playerStart.y), heroHealth, heroDamage, 1.9f, 0.85f,
                    defense: damageDefinition != null ? damageDefinition.heroDefense : 1, damageProfile: heroDamageRules),
                monsters, comboDefinition != null ? comboDefinition.Snapshot() : AttackStep.DefaultCombo(),
                new RepositionSettings(enableReposition, pressureThreshold, repositionDistance, repositionCooldown, retreatDistance), trapTest ? 0 : knockbackDistance,
                tacticsDefinition != null ? tacticsDefinition.Snapshot() : new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f), seed);
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
            var result = new ActorView { Root = root, Color = color };
            MakeSprite("Foot", root, Vector3.zero, new Vector2(0.56f, 0.18f), Color.black, 0);
            result.Body = MakeSprite("Body", root, new Vector3(0, 0.3f, 0), new Vector2(0.4f, 0.6f), color, 1);
            result.Direction = MakeSprite("Four Direction", root, Vector3.zero, new Vector2(0.13f, 0.13f), Color.white, 2);
            result.Facing = result.Direction.transform;
            result.HealthBack = MakeSprite("Health Background", root, new Vector3(0, 0.76f, 0), new Vector2(0.55f, 0.06f), Color.black, 5);
            result.HealthFill = MakeSprite("Health", root, new Vector3(0, 0.76f, 0), new Vector2(0.55f, 0.04f), Color.green, 6);
            result.TargetMarker = MakeSprite("Current Target", root, new Vector3(0, -0.13f, 0), new Vector2(0.38f, 0.04f), Color.yellow, 4);
            result.Feedback = feedback.CreateActor(root);
            result.WindupRenderer = MakeSprite("Attack preparation", root, new Vector3(0, 0.3f, 0), new Vector2(0.3f, 0.04f), Color.yellow, 3);
            result.Windup = result.WindupRenderer.transform;
            result.Blade = MakeSprite("Diagonal Slash", root, Vector3.zero, new Vector2(0.85f, 0.07f), Color.yellow, 3);
            result.Slash = result.Blade.transform;
            return result;
        }
        private void Update()
        {
            if (simulation == null) return;
            simulation.Advance(Time.deltaTime);
            // Preserve the entire field on narrow portrait displays, with a fixed orientation.
            viewCamera.orthographicSize = Mathf.Max(4.7f, 2.65f / viewCamera.aspect);
            feedback.Effects = showHitEffects; feedback.Numbers = showDamageNumbers;
            feedback.Sound = playCombatSounds; feedback.Volume = combatVolume;
            feedback.BeginFrame(simulation.Hero);
            feedback.UpdateActor(heroView.Feedback, simulation.Hero, true, Time.unscaledDeltaTime);
            Draw(heroView, simulation.Hero, false);
            DrawAttackArea();
            for (int i = 0; i < monsterViews.Length; i++)
            {
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
            attackArea.SetPosition(0, Vector3.zero);
            for (int i = 0; i <= 16; i++)
            {
                float direction = angle - half + 2 * half * i / 16;
                attackArea.SetPosition(i + 1, new Vector3(Mathf.Cos(direction) * range, Mathf.Sin(direction) * range, 0));
            }
            attackArea.SetPosition(18, Vector3.zero);
        }
        private static void Draw(ActorView view, Fighter fighter, bool targeted)
        {
            view.Root.localPosition = new Vector3(fighter.Position.X, fighter.Position.Y, 0);
            int depth = 100 - Mathf.RoundToInt(fighter.Position.Y * 20);
            view.Body.sortingOrder = depth;
            var aim = new Vector2(fighter.Aim.X, fighter.Aim.Y);
            Vector2 cardinal = Mathf.Abs(aim.x) > Mathf.Abs(aim.y) ? new Vector2(Mathf.Sign(aim.x), 0) : new Vector2(0, Mathf.Sign(aim.y));
            view.Facing.localPosition = (Vector3)(cardinal * 0.25f) + new Vector3(0, 0.3f, 0);
            view.Direction.sortingOrder = depth + 1;
            view.Direction.enabled = fighter.Alive;
            view.TargetMarker.enabled = targeted && fighter.Alive;
            view.TargetMarker.sortingOrder = depth + 1;
            view.HealthBack.enabled = view.HealthFill.enabled = fighter.Alive;
            view.HealthBack.sortingOrder = depth + 3; view.HealthFill.sortingOrder = depth + 4;
            float health = (float)fighter.Health / fighter.MaxHealth;
            view.HealthFill.transform.localScale = new Vector3(0.55f * health, 0.04f, 1);
            view.HealthFill.transform.localPosition = new Vector3(-0.275f * (1 - health), 0.76f, 0);
            view.Body.color = fighter.HitFlash > 0 ? (fighter.LastHitKind == AttackKind.Thrust ? new Color(0.6f, 1, 1) : Color.white) : fighter.Alive ? view.Color : Color.gray;
            Vector2 recoil = fighter.Alive ? new Vector2(fighter.HitDirection.X, fighter.HitDirection.Y) * fighter.HitFlash * 0.5f : Vector2.zero;
            view.Body.transform.localPosition = new Vector3(recoil.x, 0.3f + recoil.y, 0);
            view.Body.transform.localScale = fighter.Alive ? new Vector3(0.4f, 0.6f, 1) : new Vector3(0.5f, 0.15f, 1);
            view.Blade.enabled = fighter.Alive && fighter.SlashTime > 0;
            Vector2 attack = new Vector2(fighter.AttackAim.X, fighter.AttackAim.Y);
            AttackStep step = fighter.CurrentAttack;
            float duration = Mathf.Max(0.01f, step.Active);
            float progress = Mathf.Clamp01(1 - fighter.SlashTime / duration);
            float angle = Mathf.Atan2(attack.y, attack.x) * Mathf.Rad2Deg;
            bool thrust = step.Kind == AttackKind.Thrust;
            bool preparing = fighter.Alive && fighter.State == CombatState.Attack && fighter.PhaseTime < step.Windup;
            view.WindupRenderer.enabled = preparing;
            float preparation = step.Windup > 0 ? Mathf.Clamp01(fighter.PhaseTime / step.Windup) : 0;
            float windupAngle = thrust ? angle : angle + (step.Kind == AttackKind.ReverseSlash ? 65 : -55);
            view.Windup.localRotation = Quaternion.Euler(0, 0, windupAngle);
            view.Windup.localPosition = (Vector3)(attack * Mathf.Lerp(0.15f, 0.3f, preparation)) + new Vector3(0, 0.3f, 0);
            view.Windup.localScale = new Vector3(Mathf.Lerp(0.15f, step.Kind == AttackKind.Breakthrough ? 0.65f : 0.35f, preparation), step.Kind == AttackKind.Breakthrough ? 0.1f : 0.045f, 1);
            Color prepareColor = PrototypeCombatFeedback.AttackColor(step.Kind);
            view.WindupRenderer.color = new Color(prepareColor.r, prepareColor.g, prepareColor.b, Mathf.Lerp(0.2f, 0.85f, preparation));
            view.WindupRenderer.sortingOrder = depth + 2;
            float sweep = step.Kind == AttackKind.ReverseSlash ? 65 - progress * 130 : -55 + progress * 110;
            view.Slash.localPosition = (Vector3)(attack * (thrust ? 0.35f + Mathf.Sin(progress * Mathf.PI) * 0.4f : 0.5f)) + new Vector3(0, 0.3f, 0);
            view.Slash.localRotation = Quaternion.Euler(0, 0, angle + (thrust ? 0 : sweep));
            view.Slash.localScale = new Vector3(thrust ? 0.75f : 0.85f, thrust ? 0.11f : 0.07f, 1);
            view.Blade.color = step.Kind == AttackKind.Breakthrough ? new Color(1, 0.35f, 0.15f) : thrust ? new Color(0.5f, 1, 1) : step.Kind == AttackKind.ReverseSlash ? new Color(1, 0.6f, 0.2f) : Color.yellow;
            view.Blade.sortingOrder = depth + 2;
        }
        private void OnGUI()
        {
            if (simulation == null) return;
            feedback.DrawNumber(heroView.Feedback, viewCamera);
            for (int i = 0; i < monsterViews.Length; i++) feedback.DrawNumber(monsterViews[i].Feedback, viewCamera);
            showHitEffects = GUI.Toggle(new Rect(12, 430, 110, 30), showHitEffects, "Hit sparks");
            showDamageNumbers = GUI.Toggle(new Rect(130, 430, 100, 30), showDamageNumbers, "Damage");
            playCombatSounds = GUI.Toggle(new Rect(245, 430, 110, 30), playCombatSounds, "Sound");
            showAttackArea = GUI.Toggle(new Rect(12, 462, 160, 30), showAttackArea, "Attack area");
            GUI.Label(new Rect(12, 12, 400, 30), $"Hero {simulation.Hero.Health}/{heroHealth}  {simulation.Hero.State}");
            GUI.Label(new Rect(12, 40, 400, 30), $"Enemies {simulation.LivingMonsters}/{simulation.Monsters.Count} | Swing hits: {simulation.Hero.LastHitCount}");
            if (!simulation.Hero.Alive || simulation.LivingMonsters == 0)
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
            if (GUI.Button(new Rect(118, 128, 70, 38), "5 foes")) { trapTest = false; wallTest = false; monsterCount = 5; Restart(); }
            if (GUI.Button(new Rect(194, 128, 70, 38), "8 foes")) { trapTest = false; wallTest = false; monsterCount = 8; Restart(); }
            if (GUI.Button(new Rect(270, 128, 70, 38), "12 foes")) { trapTest = false; wallTest = false; monsterCount = 12; Restart(); }
            if (GUI.Button(new Rect(270, 176, 90, 30), "Wall test")) { trapTest = false; wallTest = true; Restart(); }
            if (GUI.Button(new Rect(270, 350, 90, 30), "Trap test")) { wallTest = false; trapTest = true; Restart(); }
            bool reposition = GUI.Toggle(new Rect(12, 176, 260, 30), enableReposition, "Pressure reposition");
            if (reposition != enableReposition) { enableReposition = reposition; Restart(); }
        }
        private void OnDestroy() { feedback?.Dispose(); if (square != null) Destroy(square); if (texture != null) Destroy(texture); }
    }
}
