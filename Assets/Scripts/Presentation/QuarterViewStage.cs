using System;
using System.Collections.Generic;
using EnhancementTower.Combat;
using UnityEngine;

namespace EnhancementTower.Presentation
{
    // A disposable presentation experiment. Never writes to combat models or owns a combat clock.
    public sealed class QuarterViewStage : IDisposable
    {
        public const float Pitch = 40;
        public readonly Transform Root;
        public readonly Quaternion Rotation = Quaternion.Euler(Pitch, 0, 0);
        public readonly Material SpriteMaterial;
        private readonly Material stone;
        private readonly Mesh box;
        private readonly List<Texture2D> textures = new List<Texture2D>();
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly Dictionary<Color, Sprite[]> costumes = new Dictionary<Color, Sprite[]>();
        private readonly Sprite shadow, sword, solid;

        public static Vector3 Ground(Point p, float height = 0) => new Vector3(p.X, height, p.Y);
        public static Vector3 BillboardDelta(Point p) => new Vector3(p.X, Mathf.Sin(Pitch * Mathf.Deg2Rad) * p.Y, 0);

        public QuarterViewStage(Transform parent, Material spriteTemplate, Material stoneTemplate)
        {
            Root = new GameObject("Quarter view environment (visual only)").transform;
            Root.SetParent(parent, false);
            SpriteMaterial = new Material(spriteTemplate);
            stone = new Material(stoneTemplate);
            var palette = new Texture2D(6, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            palette.SetPixels(new[] { Color.white * 1.15f, Color.white * .45f, Color.white * .65f, Color.white * .9f, Color.white * .75f, Color.white * .55f });
            palette.Apply(); textures.Add(palette); stone.SetTexture("_BaseMap", palette);
            box = MakeBox();
            shadow = MakeShadow();
            solid = MakeSprite("Pose pixel", 1, 1, new[] { Color.white }, new Vector2(.5f, .5f), 1);
            var blade = new Color[12 * 48];
            void Paint(int x, int y, int w, int h, Color color)
            { for (int iy = y; iy < y + h; iy++) for (int ix = x; ix < x + w; ix++) blade[iy * 12 + ix] = color; }
            Paint(5, 1, 3, 10, new Color(.18f, .12f, .08f));
            Paint(2, 10, 9, 3, new Color(.75f, .56f, .24f));
            Paint(4, 13, 5, 29, new Color(.25f, .37f, .45f));
            Paint(5, 13, 3, 30, new Color(.72f, .85f, .9f));
            Paint(5, 14, 1, 28, Color.white); Paint(6, 43, 1, 4, new Color(.9f, .97f, 1));
            sword = MakeSprite("Held longsword placeholder", 12, 48, blade, new Vector2(.5f, 7f / 48), 50);
            for (int x = 0; x < 6; x++)
                for (int z = 0; z < 10; z++)
                    Box("Floor tile", new Vector3(-2.5f + x, -0.14f, -4 + z), new Vector3(.98f, .25f, .98f),
                        (x + z) % 2 == 0 ? new Color(.22f, .27f, .29f) : new Color(.25f, .3f, .32f));
            Box("Rear wall", new Vector3(0, .55f, 5.1f), new Vector3(6, 1.3f, .3f), new Color(.33f, .36f, .4f));
            Box("Raised rear landing (not walkable)", new Vector3(0, .12f, 4.7f), new Vector3(3, .3f, .5f), new Color(.4f, .43f, .46f));
            foreach (float x in new[] { -2.7f, 2.7f })
                foreach (float z in new[] { -2.3f, 2.8f })
                {
                    Box("Pillar plinth", new Vector3(x, .08f, z), new Vector3(.58f, .18f, .58f), new Color(.39f, .42f, .45f));
                    Box("Pillar", new Vector3(x, .67f, z), new Vector3(.32f, 1.2f, .32f), new Color(.46f, .49f, .53f));
                    Box("Pillar cap", new Vector3(x, 1.31f, z), new Vector3(.52f, .15f, .52f), new Color(.5f, .53f, .56f));
                }
        }
        private void Box(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(Root, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = box;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = stone;
            var properties = new MaterialPropertyBlock(); properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
            // No collider: these props must not introduce a second movement/physics system.
        }
        private static Mesh MakeBox()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector3[] normals = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            for (int face = 0; face < normals.Length; face++)
            {
                Vector3 n = normals[face], u = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.Cross(Vector3.up, n);
                Vector3 v = Vector3.Cross(n, u);
                int first = vertices.Count;
                vertices.Add((n - u - v) * .5f); vertices.Add((n + u - v) * .5f);
                vertices.Add((n + u + v) * .5f); vertices.Add((n - u + v) * .5f);
                for (int j = 0; j < 4; j++) uv.Add(new Vector2((face + .5f) / 6, .5f));
                triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
                triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
            }
            // Vertex shading is baked into a tiny palette texture through UVs; no scene lighting dependency.
            var mesh = new Mesh { name = "View-only shaded box" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            return mesh;
        }
        public Actor CreateActor(Transform parent, Color color, bool boss, bool fast, bool hero = false)
        {
            if (!costumes.TryGetValue(color, out Sprite[] frames))
            {
                frames = new Sprite[8];
                for (int direction = 0; direction < 4; direction++)
                    for (int step = 0; step < 2; step++) frames[direction * 2 + step] = MakePerson(color, direction, step);
                costumes.Add(color, frames);
            }
            return new Actor(parent, this, frames, boss ? 1.25f : fast ? .82f : 1, hero, color);
        }
        private Sprite MakePerson(Color costume, int direction, int step)
        {
            const int width = 24, height = 48;
            var pixels = new Color[width * height];
            Color outline = new Color(.08f, .1f, .14f), skin = new Color(.86f, .69f, .5f);
            void Rect(int x, int y, int w, int h, Color c)
            { for (int iy = y; iy < y + h; iy++) for (int ix = x; ix < x + w; ix++) pixels[iy * width + ix] = c; }
            // Four head lengths, feet at the pivot. Intentionally simple authored-code placeholders.
            Rect(6, 35, 12, 12, outline); Rect(7, 36, 10, 9, skin);
            Rect(6, 43, 12, 4, costume * .55f); Rect(7, 45, 10, 2, costume * .8f);
            Rect(5, 19, 14, 16, outline); Rect(6, 20, 12, 14, costume);
            Rect(7, 30, 10, 3, costume * 1.25f); Rect(5, 18, 14, 3, new Color(.38f, .27f, .18f));
            Rect(3, 23, 3, 10, outline); Rect(18, 23, 3, 10, outline);
            Rect(3, 21, 3, 4, skin); Rect(18, 21, 3, 4, skin);
            int left = step == 0 ? 0 : 2, right = step == 0 ? 2 : 0;
            Rect(6, 3 + left, 5, 15, outline); Rect(13, 3 + right, 5, 15, outline);
            Rect(7, 6 + left, 3, 11, costume * .6f); Rect(14, 6 + right, 3, 11, costume * .6f);
            Rect(5, left, 6, 5, outline); Rect(13, right, 6, 5, outline);
            if (direction == 0) { Rect(9, 39, 2, 2, outline); Rect(14, 39, 2, 2, outline); }
            else if (direction == 1) Rect(7, 35, 10, 8, costume * .5f);
            else { Rect(direction == 2 ? 7 : 15, 39, 2, 2, outline); Rect(direction == 2 ? 14 : 7, 36, 3, 7, costume * .5f); }
            return MakeSprite("Four-head silhouette", width, height, pixels, new Vector2(.5f, 0), 40);
        }
        private Sprite MakeShadow()
        {
            var pixels = new Color[24 * 24];
            for (int y = 0; y < 24; y++) for (int x = 0; x < 24; x++)
                if (new Vector2((x - 11.5f) / 11.5f, (y - 11.5f) / 11.5f).sqrMagnitude <= 1)
                    pixels[y * 24 + x] = new Color(.015f, .02f, .025f, .5f);
            return MakeSprite("Foot shadow", 24, 24, pixels, new Vector2(.5f, .5f), 24);
        }
        private Sprite MakeSprite(string name, int width, int height, Color[] pixels, Vector2 pivot, float ppu)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels); texture.Apply(); textures.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot, ppu); sprites.Add(sprite); return sprite;
        }
        public void Dispose()
        {
            foreach (Sprite sprite in sprites) UnityEngine.Object.Destroy(sprite);
            foreach (Texture2D texture in textures) UnityEngine.Object.Destroy(texture);
            UnityEngine.Object.Destroy(SpriteMaterial); UnityEngine.Object.Destroy(stone); UnityEngine.Object.Destroy(box);
            if (Root != null) UnityEngine.Object.Destroy(Root.gameObject);
        }
        public sealed class Actor
        {
            public readonly Transform Root;
            public float Height => scale * 1.2f;
            private readonly SpriteRenderer body, shadowRenderer, torso, weapon;
            private readonly SpriteRenderer[] arms, hands;
            private readonly Transform visual, upperBody;
            private readonly LineRenderer trail;
            private readonly Sprite[] frames, lowerFrames, upperFrames;
            private readonly float scale;
            private readonly bool hero;
            public Actor(Transform parent, QuarterViewStage stage, Sprite[] frames, float scale, bool hero, Color costume)
            {
                this.frames = frames; this.scale = scale; this.hero = hero;
                Root = new GameObject("Quarter-view actor").transform; Root.SetParent(parent, false);
                visual = new GameObject("Billboard visual").transform; visual.SetParent(Root, false);
                visual.localScale = Vector3.one * scale;
                SpriteRenderer Part(string name, Transform owner, Sprite sprite, Color color, int order)
                {
                    var go = new GameObject(name); go.transform.SetParent(owner, false);
                    var renderer = go.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = stage.SpriteMaterial;
                    renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; return renderer;
                }
                body = Part("Four-head pixel silhouette", visual, frames[0], Color.white, 0);
                shadowRenderer = Part("Ground shadow", Root, stage.shadow, Color.white, -10);
                shadowRenderer.transform.localPosition = Quaternion.Inverse(stage.Rotation) * (Vector3.up * .025f);
                shadowRenderer.transform.localRotation = Quaternion.Inverse(stage.Rotation) * Quaternion.Euler(90, 0, 0);
                shadowRenderer.transform.localScale = new Vector3(.62f, .38f, 1) * scale;
                if (!hero) return;
                lowerFrames = new Sprite[frames.Length]; upperFrames = new Sprite[frames.Length];
                for (int i = 0; i < frames.Length; i++)
                {
                    // Remove painted arms from the upper layer; the articulated arms now own those pixels.
                    Texture2D source = frames[i].texture;
                    var pixels = source.GetPixels();
                    for (int y = 18; y < 35; y++) for (int x = 0; x < 24; x++)
                        if (x < 6 || x >= 18) pixels[y * 24 + x] = Color.clear;
                    var texture = new Texture2D(24, 48, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    texture.SetPixels(pixels); texture.Apply(); stage.textures.Add(texture);
                    lowerFrames[i] = Sprite.Create(texture, new Rect(0, 0, 24, 18), new Vector2(.5f, 0), 40);
                    upperFrames[i] = Sprite.Create(texture, new Rect(0, 18, 24, 30), new Vector2(.5f, 4f / 30), 40);
                    stage.sprites.Add(lowerFrames[i]); stage.sprites.Add(upperFrames[i]);
                }
                upperBody = new GameObject("Torso pivot").transform; upperBody.SetParent(visual, false);
                torso = Part("Torso and head", upperBody, upperFrames[0], Color.white, 5);
                weapon = Part("Held sword", visual, stage.sword, Color.white, 12);
                arms = new SpriteRenderer[4]; hands = new SpriteRenderer[2];
                for (int i = 0; i < 4; i++) arms[i] = Part("Arm segment " + i, visual, stage.solid, costume * .8f, 9);
                for (int i = 0; i < 2; i++) hands[i] = Part("Sword grip hand " + i, visual, stage.solid, new Color(.86f, .69f, .5f), 13);
                var arc = new GameObject("Sword sweep trail"); arc.transform.SetParent(visual, false);
                trail = arc.AddComponent<LineRenderer>(); trail.sharedMaterial = stage.SpriteMaterial;
                trail.useWorldSpace = false; trail.positionCount = 10; trail.startWidth = .02f; trail.endWidth = .075f;
                trail.startColor = new Color(.65f, .88f, 1, 0); trail.endColor = new Color(.9f, .98f, 1, .65f);
                trail.sortingOrder = 11; trail.enabled = false;
            }
            public void Draw(Fighter fighter, bool guard, bool swordMotion = true, bool effects = true, bool majorAttack = false)
            {
                Point aim = fighter.State == CombatState.Attack ? fighter.AttackAim : fighter.Aim;
                int direction = Mathf.Abs(aim.X) > Mathf.Abs(aim.Y) ? (aim.X < 0 ? 2 : 3) : aim.Y > 0 ? 1 : 0;
                int step = fighter.Movement.Length > .05f ? ((int)(fighter.Position.X * 6 + fighter.Position.Y * 6) & 1) : 0;
                int frame = direction * 2 + step;
                bool posed = hero && swordMotion;
                body.sprite = posed ? lowerFrames[frame] : frames[frame];
                Color tint = guard ? new Color(.35f, 1, .8f) : fighter.HitFlash > 0 ? new Color(1, .55f, .5f) :
                    majorAttack && fighter.Alive ? new Color(1f, .45f, .2f) : fighter.Alive ? Color.white : Color.gray;
                body.color = tint;
                visual.localScale = new Vector3(scale, fighter.Alive ? scale : scale * .22f, 1);
                shadowRenderer.enabled = fighter.Alive;
                if (!hero) return;
                upperBody.gameObject.SetActive(posed); weapon.enabled = posed && fighter.Alive;
                for (int i = 0; i < arms.Length; i++) arms[i].enabled = posed && fighter.Alive;
                for (int i = 0; i < hands.Length; i++) hands[i].enabled = posed && fighter.Alive;
                trail.enabled = false;
                if (!posed) return;
                torso.sprite = upperFrames[frame]; torso.color = tint;
                DrawSwordPose(fighter, aim, effects, guard);
            }
            private void DrawSwordPose(Fighter fighter, Point aim, bool effects, bool guard)
            {
                AttackStep attack = fighter.CurrentAttack;
                bool attacking = fighter.Alive && fighter.State == CombatState.Attack;
                bool thrust = attack.Kind == AttackKind.Thrust;
                float time = fighter.PhaseTime;
                float prepare = attacking ? Mathf.Clamp01(time / Mathf.Max(.001f, attack.Windup)) : 0;
                float active = attacking ? Mathf.Clamp01((time - attack.Windup) / Mathf.Max(.001f, attack.Active)) : 0;
                float recover = attacking ? Mathf.Clamp01((time - attack.Windup - attack.Active) / Mathf.Max(.001f, attack.Recovery)) : 0;
                bool windup = attacking && time < attack.Windup;
                bool hitting = attacking && time >= attack.Windup && time < attack.Windup + attack.Active;
                float sign = attack.Kind == AttackKind.ReverseSlash ? -1 : 1;
                float idle = -25;
                float sweep = guard ? 52 : !attacking ? idle : thrust ? 0 : windup ? Mathf.Lerp(idle, -85 * sign, Smooth(prepare)) : hitting ? Mathf.Lerp(-85 * sign, 85 * sign, Smooth(active)) : Mathf.Lerp(85 * sign, idle, Smooth(recover));
                float twist = guard ? -8 : !attacking ? 0 : windup ? -13 * sign * Smooth(prepare) : hitting ? Mathf.Lerp(-13, 14, Smooth(active)) * sign : 14 * sign * (1 - Smooth(recover));
                float lean = guard ? -.04f : !attacking ? 0 : windup ? -.035f * Smooth(prepare) : .045f * (1 - Smooth(recover));
                Vector3 forward = BillboardDelta(aim.Normalized);
                upperBody.localPosition = new Vector3(forward.x * lean, .55f + forward.y * lean, 0);
                upperBody.localRotation = Quaternion.Euler(0, 0, twist);
                float heading = Mathf.Atan2(aim.Y, aim.X) * Mathf.Rad2Deg;
                Vector3 BladeDirection(float offset)
                {
                    float angle = (heading + offset) * Mathf.Deg2Rad;
                    return BillboardDelta(new Point(Mathf.Cos(angle), Mathf.Sin(angle)));
                }
                Vector3 blade = BladeDirection(sweep);
                float reach = guard ? .2f : thrust && attacking ? windup ? .12f : hitting ? Mathf.Lerp(.15f, .5f, Smooth(active)) : Mathf.Lerp(.5f, .26f, Smooth(recover)) : .27f;
                Vector3 chest = new Vector3(0, .74f, 0);
                Vector3 grip = chest + blade * reach + Vector3.up * (windup ? .09f * prepare : 0);
                weapon.transform.localPosition = grip;
                weapon.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(blade.y, blade.x) * Mathf.Rad2Deg - 90);
                weapon.transform.localScale = new Vector3(1, blade.magnitude, 1);
                bool behind = blade.y > .2f;
                weapon.sortingOrder = behind ? 3 : 12;
                for (int hand = 0; hand < 2; hand++)
                {
                    Vector3 shoulder = upperBody.localPosition + upperBody.localRotation * new Vector3(hand == 0 ? -.14f : .14f, .23f, 0);
                    Vector3 hold = grip - blade.normalized * (.035f + hand * .065f);
                    Vector3 elbow = (shoulder + hold) * .5f + new Vector3(hand == 0 ? -.035f : .035f, -.06f, 0);
                    Segment(arms[hand * 2], shoulder, elbow, .075f);
                    Segment(arms[hand * 2 + 1], elbow, hold, .065f);
                    arms[hand * 2].sortingOrder = arms[hand * 2 + 1].sortingOrder = behind ? 4 : 10;
                    hands[hand].transform.localPosition = hold;
                    hands[hand].transform.localScale = new Vector3(.08f, .09f, 1);
                    hands[hand].sortingOrder = behind ? 4 : 13;
                }
                trail.enabled = effects && hitting && !thrust && !guard;
                if (trail.enabled)
                {
                    for (int i = 0; i < trail.positionCount; i++)
                    {
                        float previous = Mathf.Lerp(Mathf.Max(0, active - .3f), active, i / 9f);
                        float angle = Mathf.Lerp(-85 * sign, 85 * sign, Smooth(previous));
                        trail.SetPosition(i, chest + BladeDirection(angle) * 1.03f);
                    }
                }
            }
            private static float Smooth(float value) => value * value * (3 - 2 * value);
            private static void Segment(SpriteRenderer part, Vector3 from, Vector3 to, float width)
            {
                Vector3 delta = to - from;
                part.transform.localPosition = (from + to) * .5f;
                part.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                part.transform.localScale = new Vector3(delta.magnitude, width, 1);
            }
        }
    }
}
