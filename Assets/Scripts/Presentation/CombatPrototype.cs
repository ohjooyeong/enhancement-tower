using EnhancementTower.Combat;
using UnityEngine;

namespace EnhancementTower.Presentation
{
    public sealed class CombatPrototype : MonoBehaviour
    {
        [SerializeField, Min(1)] private int heroHealth = 240, monsterHealth = 60;
        [SerializeField, Min(1)] private int heroDamage = 12, monsterDamage = 6;
        [SerializeField] private Vector2 heroStart = new Vector2(-0.7f, -1.8f), monsterStart = new Vector2(0.8f, 1.8f);
        [SerializeField, Range(1, 12)] private int monsterCount = 5;
        [SerializeField, Range(0, 1)] private float cleaveMultiplier = 0.35f;
        [SerializeField, Range(0, 4)] private int maxSecondaryHits = 3;
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
        private bool wallTest;
        private CombatSimulation simulation;
        private Sprite square;
        private Texture2D texture;
        private ActorView heroView;
        private ActorView[] monsterViews;
        private Camera viewCamera;
        private sealed class ActorView
        {
            public Transform Root, Facing, Slash;
            public SpriteRenderer Body, Blade, Direction, HealthBack, HealthFill, TargetMarker;
            public Color Color;
        }
        private void Start()
        {
            texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.white); texture.Apply();
            square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            var cameraObject = new GameObject("Fixed Camera"); cameraObject.transform.SetParent(transform);
            viewCamera = cameraObject.AddComponent<Camera>();
            viewCamera.orthographic = true; viewCamera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0, 0.6f, -10), Quaternion.identity);
            MakeSprite("Arena", transform, new Vector3(0, 0.5f, 0), new Vector2(4.56f, 7.56f), new Color(0.15f, 0.19f, 0.21f), -1000);
            heroView = MakeActor("Hero", new Color(0.25f, 0.7f, 0.95f));
            Restart();
        }
        private void Restart()
        {
            if (monsterViews != null)
                for (int i = 0; i < monsterViews.Length; i++) Destroy(monsterViews[i].Root.gameObject);
            int count = wallTest ? 3 : Mathf.Clamp(monsterCount, 1, 12);
            Vector2 playerStart = wallTest ? new Vector2(-2, -2) : heroStart;
            monsterViews = new ActorView[count];
            var monsters = new Fighter[count];
            for (int i = 0; i < count; i++)
            {
                // Authored scattered entry points, not movement lanes.
                Vector2 start = wallTest ? (i == 0 ? new Vector2(-2, -1.35f) :
                    i == 1 ? new Vector2(-1.4f, -1.9f) : new Vector2(-1.4f, -1.35f)) : monsterStart + EntryOffsets[i];
                start.x = Mathf.Clamp(start.x, -2, 2); start.y = Mathf.Clamp(start.y, -3, 4);
                monsters[i] = new Fighter(new Point(start.x, start.y), monsterHealth, monsterDamage, 1.4f, 0.85f, 0.3f, 0.7f);
                monsterViews[i] = MakeActor($"Monster {i + 1}", new Color(0.95f, 0.4f, 0.3f));
            }
            simulation = new CombatSimulation(
                new Fighter(new Point(playerStart.x, playerStart.y), heroHealth, heroDamage, 1.9f, 0.85f),
                monsters, cleaveMultiplier, maxSecondaryHits,
                new RepositionSettings(enableReposition, pressureThreshold, repositionDistance, repositionCooldown, retreatDistance));
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
            Draw(heroView, simulation.Hero, false);
            for (int i = 0; i < monsterViews.Length; i++)
                Draw(monsterViews[i], simulation.Monsters[i], simulation.Hero.Target == simulation.Monsters[i]);
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
            view.Body.color = fighter.HitFlash > 0 ? Color.white : fighter.Alive ? view.Color : Color.gray;
            view.Body.transform.localScale = fighter.Alive ? new Vector3(0.4f, 0.6f, 1) : new Vector3(0.5f, 0.15f, 1);
            view.Blade.enabled = fighter.Alive && fighter.SlashTime > 0;
            Vector2 attack = new Vector2(fighter.AttackAim.X, fighter.AttackAim.Y);
            view.Slash.localPosition = (Vector3)(attack * 0.5f) + new Vector3(0, 0.3f, 0);
            view.Slash.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(attack.y, attack.x) * Mathf.Rad2Deg - 55 + (1 - fighter.SlashTime / 0.16f) * 110);
            view.Blade.sortingOrder = depth + 2;
        }
        private void OnGUI()
        {
            if (simulation == null) return;
            GUI.Label(new Rect(12, 12, 400, 30), $"Hero {simulation.Hero.Health}/{heroHealth}  {simulation.Hero.State}");
            GUI.Label(new Rect(12, 40, 400, 30), $"Enemies {simulation.LivingMonsters}/{simulation.Monsters.Count} | Last slash hits: {simulation.Hero.LastHitCount}");
            if (!simulation.Hero.Alive || simulation.LivingMonsters == 0)
                GUI.Label(new Rect(12, 216, 400, 30), simulation.Hero.Alive ? "CLEAR" : "DEFEAT");
            GUI.Label(new Rect(12, 68, 500, 30), $"Pressure {simulation.Pressure} | Repositions {simulation.Repositions} | CD {simulation.RepositionCooldown:F1}s");
            GUI.Label(new Rect(12, 94, 500, 30), $"Rear limit {simulation.RetreatFloor:F2} | Hero Y {simulation.Hero.Position.Y:F2}");
            float targetDistance = simulation.Hero.Target == null ? 0 :
                (simulation.Hero.Target.Position - simulation.Hero.Position).Length;
            GUI.Label(new Rect(12, 248, 500, 30), $"Hero attacks {simulation.Hero.Attacks} | Target distance {targetDistance:F3}");
            if (GUI.Button(new Rect(12, 128, 100, 38), "Restart")) Restart();
            if (GUI.Button(new Rect(118, 128, 70, 38), "5 foes")) { wallTest = false; monsterCount = 5; Restart(); }
            if (GUI.Button(new Rect(194, 128, 70, 38), "8 foes")) { wallTest = false; monsterCount = 8; Restart(); }
            if (GUI.Button(new Rect(270, 128, 70, 38), "12 foes")) { wallTest = false; monsterCount = 12; Restart(); }
            if (GUI.Button(new Rect(270, 176, 90, 30), "Wall test")) { wallTest = true; Restart(); }
            bool reposition = GUI.Toggle(new Rect(12, 176, 260, 30), enableReposition, "Pressure reposition");
            if (reposition != enableReposition) { enableReposition = reposition; Restart(); }
        }
        private void OnDestroy() { if (square != null) Destroy(square); if (texture != null) Destroy(texture); }
    }
}
