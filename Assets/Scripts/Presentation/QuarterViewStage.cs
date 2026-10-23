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
        private readonly Sprite shadow;

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
        public Actor CreateActor(Transform parent, Color color, bool boss, bool fast)
        {
            if (!costumes.TryGetValue(color, out Sprite[] frames))
            {
                frames = new Sprite[8];
                for (int direction = 0; direction < 4; direction++)
                    for (int step = 0; step < 2; step++) frames[direction * 2 + step] = MakePerson(color, direction, step);
                costumes.Add(color, frames);
            }
            return new Actor(parent, this, frames, boss ? 1.25f : fast ? .82f : 1);
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
            private readonly SpriteRenderer body, shadowRenderer;
            private readonly Sprite[] frames;
            private readonly float scale;
            public Actor(Transform parent, QuarterViewStage stage, Sprite[] frames, float scale)
            {
                this.frames = frames; this.scale = scale;
                Root = new GameObject("Quarter-view actor").transform; Root.SetParent(parent, false);
                var visual = new GameObject("Billboard visual").transform; visual.SetParent(Root, false); visual.localRotation = Quaternion.identity;
                var go = new GameObject("Four-head pixel silhouette"); go.transform.SetParent(visual, false);
                body = go.AddComponent<SpriteRenderer>(); body.sharedMaterial = stage.SpriteMaterial; body.sprite = frames[0];
                body.transform.localScale = Vector3.one * scale;
                go = new GameObject("Ground shadow"); go.transform.SetParent(Root, false);
                go.transform.localPosition = Quaternion.Inverse(stage.Rotation) * (Vector3.up * .025f); go.transform.localRotation = Quaternion.Inverse(stage.Rotation) * Quaternion.Euler(90, 0, 0);
                go.transform.localScale = new Vector3(.62f, .38f, 1) * scale;
                shadowRenderer = go.AddComponent<SpriteRenderer>(); shadowRenderer.sharedMaterial = stage.SpriteMaterial; shadowRenderer.sprite = stage.shadow; shadowRenderer.sortingOrder = -10;
            }
            public void Draw(Fighter fighter, bool guard)
            {
                Point aim = fighter.State == CombatState.Attack ? fighter.AttackAim : fighter.Aim;
                int direction = Mathf.Abs(aim.X) > Mathf.Abs(aim.Y) ? (aim.X < 0 ? 2 : 3) : aim.Y > 0 ? 1 : 0;
                int step = fighter.Movement.Length > .05f ? ((int)(fighter.Position.X * 6 + fighter.Position.Y * 6) & 1) : 0;
                body.sprite = frames[direction * 2 + step];
                body.color = guard ? new Color(.35f, 1, .8f) : fighter.HitFlash > 0 ? new Color(1, .55f, .5f) : fighter.Alive ? Color.white : Color.gray;
                body.transform.localScale = new Vector3(scale, fighter.Alive ? scale : scale * .22f, 1);
                shadowRenderer.enabled = fighter.Alive;
            }
        }
    }
}
