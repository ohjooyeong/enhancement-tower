using EnhancementTower.Combat;
using UnityEngine;

namespace EnhancementTower.Presentation
{
    // All state here is cosmetic. No clock, actor position or damage is written back to the simulation.
    public sealed class PrototypeCombatFeedback
    {
        public sealed class ActorFeedback
        {
            internal long ObservedDamage;
            internal int ObservedHits, ObservedCriticalHits;
            internal bool Critical;
            internal float SparkTime, NumberTime, MergeTime;
            internal int DisplayDamage;
            internal string Number = "";
            internal Vector3 NumberOrigin;
            internal Color Color;
            internal Transform Spark;
            internal SpriteRenderer[] Rays;
        }
        private readonly Transform root;
        private readonly Sprite square;
        private readonly AudioSource swingSource;
        private readonly AudioSource[] impactSources;
        private readonly AudioClip[] swings;
        private readonly AudioClip hit, heavyHit, death;
        private int observedAttacks, nextImpact;
        private float nextImpactTime;
        private GUIStyle numberStyle;
        public int PresentedHits { get; private set; }
        public bool Effects = true, Numbers = true, Sound = true;
        public float Volume = 0.18f;
        public System.Func<Point, float, Vector3> ProjectPoint;

        public PrototypeCombatFeedback(Transform parent, Sprite sprite)
        {
            root = parent; square = sprite;
            swingSource = MakeSource("Swing audio");
            impactSources = new[] { MakeSource("Impact audio 1"), MakeSource("Impact audio 2") };
            swings = new[] { Tone("Slash", 0.11f, 420, 180, 0.85f), Tone("Reverse slash", 0.12f, 360, 140, 0.8f),
                Tone("Thrust", 0.08f, 900, 500, 0.25f), Tone("Monster strike", 0.08f, 200, 100, 0.5f),
                Tone("Breakthrough", 0.18f, 240, 80, 0.6f) };
            hit = Tone("Hit", 0.065f, 620, 260, 0.6f);
            heavyHit = Tone("Heavy hit", 0.1f, 360, 110, 0.35f);
            death = Tone("Death", 0.16f, 180, 60, 0.3f);
        }
        private AudioSource MakeSource(string label)
        {
            var go = new GameObject(label); go.transform.SetParent(root, false);
            var source = go.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 0; source.loop = false;
            return source;
        }
        // Original, short synthetic placeholders. No external audio assets or simulation randomness.
        private static AudioClip Tone(string label, float seconds, float start, float end, float noiseMix)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(seconds * rate)];
            uint noise = 314159u; float phase = 0, filtered = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float progress = (float)i / samples.Length;
                noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                filtered = Mathf.Lerp(filtered, noise / 2147483648f - 1, 0.18f);
                phase += Mathf.Lerp(start, end, progress) * (Mathf.PI * 2 / rate);
                float envelope = Mathf.Min(1, progress * 20) * (1 - progress) * (1 - progress) * Mathf.Exp(-progress * 2);
                samples[i] = (Mathf.Sin(phase) * (1 - noiseMix) + filtered * noiseMix) * envelope * 0.5f;
            }
            var clip = AudioClip.Create(label, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        public ActorFeedback CreateActor(Transform parent)
        {
            var state = new ActorFeedback();
            state.Spark = new GameObject("Hit spark").transform; state.Spark.SetParent(parent, false);
            state.Spark.localPosition = new Vector3(0, 0.35f, 0);
            state.Rays = new SpriteRenderer[3];
            for (int i = 0; i < state.Rays.Length; i++)
            {
                var ray = new GameObject("Spark ray"); ray.transform.SetParent(state.Spark, false);
                ray.transform.localScale = new Vector3(0.25f, 0.035f, 1);
                ray.transform.localRotation = Quaternion.Euler(0, 0, i * 60);
                state.Rays[i] = ray.AddComponent<SpriteRenderer>(); state.Rays[i].sprite = square;
                state.Rays[i].enabled = false;
            }
            return state;
        }
        public void Reset(ActorFeedback state)
        {
            state.ObservedDamage = 0; state.ObservedHits = state.ObservedCriticalHits = 0; state.Critical = false;
            state.SparkTime = state.NumberTime = state.MergeTime = 0;
            state.DisplayDamage = 0; state.Number = "";
            for (int i = 0; i < state.Rays.Length; i++) state.Rays[i].enabled = false;
        }
        public void ResetRun()
        {
            observedAttacks = 0; PresentedHits = 0; nextImpactTime = 0;
            swingSource.Stop(); for (int i = 0; i < impactSources.Length; i++) impactSources[i].Stop();
        }
        public void BeginFrame(Fighter hero)
        {
            if (!Sound)
            { swingSource.Stop(); for (int i = 0; i < impactSources.Length; i++) impactSources[i].Stop(); }
            if (hero.Attacks == observedAttacks) return;
            observedAttacks = hero.Attacks;
            if (Sound)
            {
                swingSource.clip = swings[(int)hero.CurrentAttack.Kind];
                swingSource.volume = Mathf.Clamp01(Volume); swingSource.Play();
            }
        }
        public void UpdateActor(ActorFeedback state, Fighter actor, bool hero, float delta)
        {
            state.SparkTime = Mathf.Max(0, state.SparkTime - delta);
            state.NumberTime = Mathf.Max(0, state.NumberTime - delta);
            state.MergeTime = Mathf.Max(0, state.MergeTime - delta);
            if (actor.HitsTaken != state.ObservedHits)
            {
                long amount = actor.DamageTaken - state.ObservedDamage;
                state.DisplayDamage = state.MergeTime > 0 ? state.DisplayDamage + (int)amount : (int)amount;
                state.Critical = (state.MergeTime > 0 && state.Critical) || actor.CriticalHitsTaken > state.ObservedCriticalHits;
                state.ObservedCriticalHits = actor.CriticalHitsTaken;
                state.Number = (state.Critical ? "CRIT " : "") + "-" + state.DisplayDamage + (actor.Alive ? "" : " DOWN");
                state.ObservedHits = actor.HitsTaken; state.ObservedDamage = actor.DamageTaken;
                state.MergeTime = 0.12f; state.NumberTime = 0.65f; state.SparkTime = 0.12f;
                state.NumberOrigin = new Vector3(actor.Position.X, actor.Position.Y + 0.85f, 0);
                state.Color = state.Critical ? new Color(1, 0.35f, 0.85f) : hero ? new Color(1, 0.45f, 0.4f) : AttackColor(actor.LastHitKind);
                PresentedHits++;
                if (Sound && Time.unscaledTime >= nextImpactTime)
                {
                    var source = impactSources[nextImpact++ % impactSources.Length];
                    source.clip = !actor.Alive ? death : actor.LastHitKind == AttackKind.Thrust || actor.LastHitKind == AttackKind.Breakthrough ? heavyHit : hit;
                    source.volume = Mathf.Clamp01(Volume) * (hero ? 1 : 0.8f); source.Play();
                    nextImpactTime = Time.unscaledTime + 0.04f;
                }
            }
            float progress = 1 - state.SparkTime / 0.12f;
            state.Spark.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.3f, progress);
            int depth = 105 - Mathf.RoundToInt(actor.Position.Y * 20);
            for (int i = 0; i < state.Rays.Length; i++)
            {
                state.Rays[i].enabled = Effects && state.SparkTime > 0;
                state.Rays[i].color = new Color(state.Color.r, state.Color.g, state.Color.b, 1 - progress);
                state.Rays[i].sortingOrder = depth;
            }
        }
        public void DrawNumber(ActorFeedback state, Camera camera)
        {
            if (!Numbers || state.NumberTime <= 0) return;
            if (numberStyle == null) numberStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            numberStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 55f, 14, 28) * (state.Critical ? 1.2f : 1));
            float rise = (1 - state.NumberTime / 0.65f) * 0.45f;
            Vector3 world = ProjectPoint != null ? ProjectPoint(new Point(state.NumberOrigin.x, state.NumberOrigin.y - .85f), .85f + rise) : state.NumberOrigin + Vector3.up * rise;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0) return;
            var rect = new Rect(screen.x - 110, Screen.height - screen.y - 22, 220, 44);
            float alpha = Mathf.Clamp01(state.NumberTime / 0.2f);
            numberStyle.normal.textColor = new Color(0, 0, 0, alpha);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), state.Number, numberStyle);
            numberStyle.normal.textColor = new Color(state.Color.r, state.Color.g, state.Color.b, alpha);
            GUI.Label(rect, state.Number, numberStyle);
        }
        public static Color AttackColor(AttackKind kind) => kind == AttackKind.Thrust ? new Color(0.55f, 1, 1) :
            kind == AttackKind.BossSweep || kind == AttackKind.BossCharge ? new Color(1, 0.35f, 0.15f) :
            kind == AttackKind.Breakthrough ? new Color(1, 0.4f, 0.15f) : kind == AttackKind.ReverseSlash ? new Color(1, 0.7f, 0.25f) : new Color(1, 0.95f, 0.6f);
        public void Dispose()
        {
            if (swingSource != null) swingSource.Stop();
            for (int i = 0; i < impactSources.Length; i++) if (impactSources[i] != null) impactSources[i].Stop();
            for (int i = 0; i < swings.Length; i++) Object.Destroy(swings[i]);
            Object.Destroy(hit); Object.Destroy(heavyHit); Object.Destroy(death);
        }
    }
}
