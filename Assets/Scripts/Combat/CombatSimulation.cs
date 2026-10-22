using System;
using System.Collections.Generic;

namespace EnhancementTower.Combat
{
    public enum CombatState { Search, Approach, Attack, Reposition, Dead }

    // Value coordinates keep the simulation independent of Unity physics and render frames.
    public struct Point
    {
        public float X, Y;
        public Point(float x, float y) { X = x; Y = y; }
        public float Length => (float)Math.Sqrt(X * X + Y * Y);
        public Point Normalized => Length > 0.00001f ? this * (1f / Length) : new Point(0, 1);
        public static Point operator +(Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
        public static Point operator -(Point a, Point b) => new Point(a.X - b.X, a.Y - b.Y);
        public static Point operator *(Point a, float b) => new Point(a.X * b, a.Y * b);
    }

    public enum AttackKind { DiagonalSlash, ReverseSlash, Thrust, MonsterStrike }

    public readonly struct AttackStep
    {
        public readonly AttackKind Kind;
        public readonly int MaxTargets;
        public readonly float Windup, Active, Recovery, Arc, RangeScale, DamageScale, KnockbackScale;
        public float Duration => Windup + Active + Recovery;
        public AttackStep(AttackKind kind, int maxTargets, float windup, float active, float recovery,
            float arc, float rangeScale, float damageScale, float knockbackScale)
        {
            if (maxTargets < 1 || windup <= 0 || active <= 0 || recovery < 0 || arc <= 0 || arc > 180 ||
                rangeScale <= 0 || damageScale <= 0 || knockbackScale < 0 ||
                float.IsNaN(windup + active + recovery + arc + rangeScale + damageScale + knockbackScale) ||
                float.IsInfinity(windup + active + recovery + arc + rangeScale + damageScale + knockbackScale))
                throw new ArgumentOutOfRangeException(nameof(windup));
            Kind = kind; MaxTargets = maxTargets; Windup = windup; Active = active; Recovery = recovery;
            Arc = arc; RangeScale = rangeScale; DamageScale = damageScale; KnockbackScale = knockbackScale;
        }
        public static AttackStep[] DefaultCombo(float windup = 0.22f, float recovery = 0.68f) => new[] {
            new AttackStep(AttackKind.DiagonalSlash, 3, windup, recovery * 0.18f, recovery * 0.82f, 180, 1, 1, 0.65f),
            new AttackStep(AttackKind.ReverseSlash, 3, windup * 0.82f, recovery * 0.2f, recovery * 0.8f, 160, 1, 1, 0.8f),
            new AttackStep(AttackKind.Thrust, 1, windup * 1.4f, recovery * 0.15f, recovery * 1.05f, 24, 1.25f, 1.8f, 1.3f) };
    }

    public sealed class Fighter
    {
        public Point Position, Movement, Aim = new Point(0, 1), AttackAim;
        public CombatState State;
        public readonly int MaxHealth, Damage, Defense;
        public Fighter Target { get; internal set; }
        internal Fighter AttackTarget;
        public AttackStep CurrentAttack { get; internal set; }
        public int ComboIndex { get; internal set; }
        public int LastHitCount { get; internal set; }
        public int Health { get; internal set; }
        public readonly float Speed, Range, Windup, Recovery;
        public float PhaseTime { get; internal set; }
        public float HitFlash { get; internal set; }
        public Point HitDirection { get; internal set; }
        public AttackKind LastHitKind { get; internal set; }
        public float SlashTime { get; internal set; }
        public int Attacks { get; internal set; }
        public float KnockbackRemaining { get; internal set; }
        internal Point KnockbackDirection;
        internal float KnockbackCooldown;
        public bool Alive => Health > 0;
        public Fighter(Point position, int health, int damage, float speed, float range,
            float windup = 0.22f, float recovery = 0.68f, int defense = 0)
        {
            if (health <= 0 || damage <= 0 || speed < 0 || range < 0.6f || windup <= 0 || recovery <= 0 || defense < 0)
                throw new ArgumentOutOfRangeException(nameof(health), "Invalid prototype combat settings.");
            Position = position; Health = MaxHealth = health; Damage = damage;
            Defense = defense; Speed = speed; Range = range; Windup = windup; Recovery = recovery;
        }
    }

    public sealed class RepositionSettings
    {
        public readonly bool Enabled;
        public readonly int PressureThreshold;
        public readonly float Distance, Cooldown, RetreatDistance;
        public RepositionSettings(bool enabled = true, int pressureThreshold = 3, float distance = 0.5f,
            float cooldown = 3f, float retreatDistance = 0.8f)
        {
            if (pressureThreshold < 1 || !Finite(distance) || distance <= 0 || distance > 0.8f ||
                !Finite(cooldown) || cooldown < 1 || !Finite(retreatDistance) || retreatDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(distance));
            Enabled = enabled; PressureThreshold = pressureThreshold; Distance = distance;
            Cooldown = cooldown; RetreatDistance = retreatDistance;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class CombatSimulation
    {
        public const float Tick = 1f / 60f;
        public const float FootRadius = 0.28f;
        private const float RangeTolerance = 0.0001f;
        public readonly Fighter Hero;
        // Compatibility with the original duel tests and callers.
        public Fighter Monster => enemies[0];
        private readonly Fighter[] enemies;
        public IReadOnlyList<Fighter> Monsters { get; }
        private readonly AttackStep[] combo;
        private readonly int[] hitOnSwing;
        public readonly float KnockbackDistance;
        public int LivingMonsters
        {
            get { int count = 0; for (int i = 0; i < enemies.Length; i++) if (enemies[i].Alive) count++; return count; }
        }
        private double accumulator;
        public int Ticks { get; private set; }
        public readonly RepositionSettings RepositionRules;
        public readonly float RetreatFloor;
        public int Repositions { get; private set; }
        public int Pressure { get; private set; }
        public float RepositionCooldown { get; private set; }
        public float RepositionTravel { get; private set; }
        private bool repositioning;
        private Point repositionGoal, repositionNext;
        private bool hasRepositionNext;
        public CombatSimulation(Fighter hero, params Fighter[] monsters) : this(hero, monsters, null) { }
        public CombatSimulation(Fighter hero, Fighter[] monsters, AttackStep[] attacks, RepositionSettings reposition = null, float knockbackDistance = 0)
        {
            if (hero == null || monsters == null || monsters.Length == 0) throw new ArgumentException("A hero and enemies are required.");
            combo = attacks == null ? AttackStep.DefaultCombo(hero.Windup, hero.Recovery) : (AttackStep[])attacks.Clone();
            if (combo.Length == 0) throw new ArgumentException("Combo cannot be empty.");
            for (int i = 0; i < combo.Length; i++)
                if (combo[i].MaxTargets < 1 || combo[i].Active <= 0) throw new ArgumentException("Invalid combo step.");
            hitOnSwing = new int[monsters.Length];
            if (float.IsNaN(knockbackDistance) || float.IsInfinity(knockbackDistance) || knockbackDistance < 0 || knockbackDistance > 0.6f)
                throw new ArgumentOutOfRangeException(nameof(knockbackDistance));
            KnockbackDistance = knockbackDistance;
            RepositionRules = reposition ?? new RepositionSettings();
            RetreatFloor = Math.Max(-3, hero.Position.Y - RepositionRules.RetreatDistance);
            Hero = hero; enemies = (Fighter[])monsters.Clone();
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || enemies[i] == hero) throw new ArgumentException("Invalid enemy.");
                for (int j = 0; j < i; j++) if (enemies[j] == enemies[i]) throw new ArgumentException("Duplicate enemy.");
            }
            Monsters = Array.AsReadOnly(enemies);

        }
        private Fighter SelectTarget(Fighter actor)
        {
            if (actor != Hero) return Hero.Alive ? Hero : null;
            Fighter best = null; float distance = float.MaxValue;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].Alive) continue;
                float candidate = (enemies[i].Position - actor.Position).Length;
                if (candidate < distance) { best = enemies[i]; distance = candidate; }
            }
            return best;
        }
        public void Advance(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            accumulator += seconds;
            while (accumulator + 1e-9 >= Tick)
            {
                accumulator -= Tick;
                RepositionCooldown = Math.Max(0, RepositionCooldown - Tick);
                Pressure = NearbyCount(Hero.Position);
                Step(Hero);
                for (int i = 0; i < enemies.Length; i++) Step(enemies[i]);
                SeparateMonsters();
                ApplyKnockback();
                Ticks++;
            }
        }
        private void Step(Fighter actor)
        {
            actor.Movement = new Point();
            actor.KnockbackCooldown = Math.Max(0, actor.KnockbackCooldown - Tick);
            actor.HitFlash = Math.Max(0, actor.HitFlash - Tick);
            actor.SlashTime = Math.Max(0, actor.SlashTime - Tick);
            if (!actor.Alive) { actor.State = CombatState.Dead; actor.Target = null; return; }
            // Retain targets during approach/attack; reacquire between attacks or after death.
            if (actor.State == CombatState.Search || actor.Target == null || !actor.Target.Alive)
                actor.Target = SelectTarget(actor);
            Fighter target = actor.Target;
            if (target == null) { actor.State = CombatState.Search; if (actor == Hero) repositioning = false; return; }
            float approachRange = actor == Hero ? actor.Range * combo[actor.ComboIndex].RangeScale : actor.Range;
            // A moving crowd can put another enemy between the hero and its retained target.
            // Reconsider only while approaching an out-of-range target; never cancel an attack.
            if (actor == Hero && actor.State == CombatState.Approach &&
                !WithinRange(actor.Position, target.Position, approachRange))
            {
                Fighter nearest = SelectTarget(actor);
                if (nearest != null && nearest != target &&
                    (WithinRange(actor.Position, nearest.Position, approachRange) ||
                    (nearest.Position - actor.Position).Length + 0.05f < (target.Position - actor.Position).Length))
                    actor.Target = target = nearest;
            }
            Point delta = target.Position - actor.Position;
            if (actor.State != CombatState.Attack) actor.Aim = delta.Normalized;
            switch (actor.State)
            {
                case CombatState.Search: actor.State = CombatState.Approach; break;
                case CombatState.Approach:
                    if (WithinRange(actor.Position, target.Position, approachRange))
                    {
                        actor.State = CombatState.Attack; actor.AttackAim = actor.Aim;
                        actor.PhaseTime = 0; actor.AttackTarget = target; actor.LastHitCount = 0;
                        actor.CurrentAttack = actor == Hero ? combo[actor.ComboIndex] :
                            new AttackStep(AttackKind.MonsterStrike, 1, actor.Windup, 0.01f, Math.Max(0, actor.Recovery - 0.01f), 180, 1, 1, 0);
                    }
                    else
                    {
                        if (actor.KnockbackRemaining > 0) break;
                        float distance = Math.Min(actor.Speed * Tick, delta.Length - (approachRange - 0.005f));
                        distance = Math.Min(distance, Math.Max(0, delta.Length - FootRadius * 2));
                        Point before = actor.Position;
                        Point direction = actor.Aim;
                        if (actor != Hero)
                        {
                            // Weak approach separation; some monster overlap is intentional.
                            Point separation = new Point();
                            for (int i = 0; i < enemies.Length; i++)
                            {
                                Fighter other = enemies[i];
                                if (other == actor || !other.Alive) continue;
                                Point away = before - other.Position;
                                if (away.Length > 0.001f && away.Length < 0.8f)
                                    separation += away.Normalized * ((0.8f - away.Length) * 2f);
                            }
                            // Steer toward local gaps while retaining forward approach intent.
                            float forward = separation.X * actor.Aim.X + separation.Y * actor.Aim.Y;
                            separation = separation - actor.Aim * forward;
                            direction = (direction + separation).Normalized;
                        }
                        actor.Position = MoveWithoutCrossing(actor, Clamp(before + direction * distance));
                        actor.Movement = (actor.Position - before) * (1f / Tick);
                    }
                    break;
                case CombatState.Attack:
                    float previous = actor.PhaseTime;
                    actor.PhaseTime += Tick;
                    AttackStep step = actor.CurrentAttack;
                    if (previous < step.Windup && actor.PhaseTime >= step.Windup)
                    {
                        actor.Attacks++; actor.SlashTime = step.Active;
                        if (actor != Hero && target.Alive && InArc(actor.Position, actor.AttackAim, target.Position, actor.Range, 180))
                            DealHit(actor, target, actor.Damage);
                    }
                    if (actor == Hero && actor.PhaseTime >= step.Windup && previous < step.Windup + step.Active)
                        ResolveSwing(step);
                    if (actor.PhaseTime >= step.Duration)
                    {
                        if (actor == Hero) actor.ComboIndex = (actor.ComboIndex + 1) % combo.Length;
                        actor.State = CombatState.Reposition;
                    }
                    break;
                case CombatState.Reposition:
                    if (actor != Hero || (!repositioning && !TryStartReposition()))
                    { actor.State = CombatState.Search; break; }
                    Point beforeStep = actor.Position;
                    Point remaining = repositionGoal - beforeStep;
                    Point destination = beforeStep + remaining.Normalized * Math.Min(actor.Speed * Tick, remaining.Length);
                    actor.Position = MoveWithoutCrossing(actor, destination);
                    float travelled = (actor.Position - beforeStep).Length;
                    RepositionTravel += travelled;
                    actor.Movement = (actor.Position - beforeStep) * (1f / Tick);
                    if ((repositionGoal - actor.Position).Length < 0.001f && hasRepositionNext)
                    { repositionGoal = repositionNext; hasRepositionNext = false; }
                    else if (travelled < 0.0001f || (repositionGoal - actor.Position).Length < 0.001f)
                    { repositioning = false; hasRepositionNext = false; actor.State = CombatState.Search; }
                    break;
            }
        }
        private void ResolveSwing(AttackStep step)
        {
            while (Hero.LastHitCount < step.MaxTargets)
            {
                int best = -1; float distance = float.MaxValue;
                for (int i = 0; i < enemies.Length; i++)
                {
                    Fighter candidate = enemies[i];
                    if (!candidate.Alive || hitOnSwing[i] == Hero.Attacks ||
                        !InArc(Hero.Position, Hero.AttackAim, candidate.Position, Hero.Range * step.RangeScale, step.Arc)) continue;
                    float current = (candidate.Position - Hero.Position).Length;
                    if (current < distance) { best = i; distance = current; }
                }
                if (best < 0) break;
                hitOnSwing[best] = Hero.Attacks;
                int damage = Math.Max(1, (int)Math.Round(Hero.Damage * step.DamageScale, MidpointRounding.AwayFromZero));
                DealHit(Hero, enemies[best], damage);
            }
        }
        // Soft local relaxation also runs while attacking: stopping must not freeze a pile-up.
        // Small bounded corrections permit overlap; no fixed slots or hard crowd collision.
        private void SeparateMonsters()
        {
            if (!Hero.Alive) return;
            const float spacing = 0.52f;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < enemies.Length; i++)
                {
                    Fighter actor = enemies[i];
                    if (!actor.Alive || actor.Speed <= 0 || actor.KnockbackRemaining > 0) continue;
                    Point correction = new Point();
                    for (int j = 0; j < enemies.Length; j++)
                    {
                        if (i == j || !enemies[j].Alive) continue;
                        Point away = actor.Position - enemies[j].Position;
                        float distance = away.Length;
                        if (distance >= spacing) continue;
                        if (distance < 0.0001f)
                        {
                            Point towardHero = (Hero.Position - actor.Position).Normalized;
                            away = new Point(towardHero.Y, -towardHero.X) * (i < j ? 1 : -1);
                        }
                        correction += away.Normalized * ((spacing - distance) * 0.5f);
                    }
                    float length = correction.Length;
                    if (length < 0.00001f) continue;
                    float budget = Math.Min(actor.Speed, 0.6f) * Tick * 0.5f;
                    correction = correction * (Math.Min(length, budget) / length);
                    Point before = actor.Position;
                    actor.Position = MoveWithoutCrossing(actor, Clamp(before + correction));
                    actor.Movement += (actor.Position - before) * (1f / Tick);
                }
            }
        }
        private int NearbyCount(Point position)
        {
            int count = 0;
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i].Alive && (enemies[i].Position - position).Length <= 1.15f) count++;
            return count;
        }
        private float Crowding(Point position)
        {
            float score = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].Alive) continue;
                float distance = (enemies[i].Position - position).Length;
                if (distance < 1.6f) score += (1.6f - distance) * (1.6f - distance);
            }
            return score;
        }
        private bool TryStartReposition()
        {
            if (!RepositionRules.Enabled || Hero.Speed <= 0 || RepositionCooldown > 0 ||
                Pressure < RepositionRules.PressureThreshold) return false;
            float best = EscapeScore(Hero.Position) - 0.05f;
            bool found = false;
            hasRepositionNext = false;
            // Sample free headings, not lanes. Try straight and bounded two-leg routes.
            for (int i = 0; i < 16; i++)
            {
                Point direction = Heading(i);
                Point direct = ReachableStep(Hero.Position, direction, RepositionRules.Distance);
                if ((direct - Hero.Position).Length >= RepositionRules.Distance * 0.6f)
                {
                    float score = EscapeScore(direct);
                    if (score < best)
                    { best = score; repositionGoal = direct; hasRepositionNext = false; found = true; }
                }
                float half = RepositionRules.Distance * 0.5f;
                Point corner = ReachableStep(Hero.Position, direction, half);
                if ((corner - Hero.Position).Length < half * 0.8f) continue;
                for (int j = 0; j < 16; j++)
                {
                    Point end = ReachableStep(corner, Heading(j), half);
                    if ((end - corner).Length < half * 0.8f) continue;
                    float score = EscapeScore(end) + 0.015f; // Prefer straight routes when equally useful.
                    if (score >= best) continue;
                    best = score; repositionGoal = corner; repositionNext = end;
                    hasRepositionNext = true; found = true;
                }
            }
            if (!found) return false; // Hold ground and attack if no useful space exists.
            repositioning = true; Repositions++; RepositionCooldown = RepositionRules.Cooldown;
            return true;
        }
        private static Point Heading(int index)
        {
            double angle = index * Math.PI / 8;
            return new Point((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
        private float EscapeScore(Point position)
        {
            float wall = Math.Max(0, 0.6f - (2 - Math.Abs(position.X)));
            float rear = Math.Max(0, 0.6f - (position.Y - RetreatFloor));
            float front = Math.Max(0, 0.6f - (4 - position.Y));
            return Crowding(position) + 0.6f * (wall * wall + rear * rear + front * front);
        }
        private Point ReachableStep(Point start, Point direction, float distance)
        {
            Point destination = Clamp(start + direction * distance);
            destination.Y = Math.Max(RetreatFloor, destination.Y);
            Point motion = destination - start;
            float fraction = 1;
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i].Alive) fraction = Math.Min(fraction, SafeFraction(start, motion, enemies[i].Position));
            return start + motion * fraction;
        }
        private void DealHit(Fighter attacker, Fighter target, int rawDamage)
        {
            target.Health = Math.Max(0, target.Health - Math.Max(1, rawDamage - target.Defense));
            target.LastHitKind = attacker.CurrentAttack.Kind;
            target.HitDirection = (target.Position - attacker.Position).Normalized;
            target.HitFlash = target.LastHitKind == AttackKind.Thrust ? 0.2f : 0.14f;
            attacker.LastHitCount++;
            if (attacker == Hero && target.Alive && KnockbackDistance > 0 && target.KnockbackCooldown <= 0)
            {
                target.KnockbackDirection = (target.Position - attacker.Position).Normalized;
                target.KnockbackRemaining = Math.Min(0.6f, KnockbackDistance * attacker.CurrentAttack.KnockbackScale);
                target.KnockbackCooldown = 0.75f;
            }
            if (!target.Alive) { target.KnockbackRemaining = 0; target.State = CombatState.Dead; target.Movement = new Point(); target.Target = null; }
        }
        private void ApplyKnockback()
        {
            if (!Hero.Alive) return;
            for (int i = 0; i < enemies.Length; i++)
            {
                Fighter actor = enemies[i];
                if (!actor.Alive || actor.KnockbackRemaining <= 0) continue;
                float distance = Math.Min(actor.KnockbackRemaining, 2.5f * Tick);
                Point before = actor.Position;
                // Monster bodies are soft: the impulse passes through other monsters.
                // The hero foot circle and arena boundaries remain solid.
                actor.Position = MoveWithoutCrossing(actor, Clamp(before + actor.KnockbackDirection * distance));
                actor.Movement += (actor.Position - before) * (1f / Tick);
                actor.KnockbackRemaining = (actor.Position - before).Length < 0.0001f ? 0 : Math.Max(0, actor.KnockbackRemaining - distance);
            }
        }
        private Point MoveWithoutCrossing(Fighter actor, Point destination)
        {
            if (actor == Hero) destination.Y = Math.Max(RetreatFloor, destination.Y);
            Point motion = destination - actor.Position;
            float fraction = 1;
            if (actor == Hero)
            {
                for (int i = 0; i < enemies.Length; i++)
                    if (enemies[i].Alive) fraction = Math.Min(fraction, SafeFraction(actor.Position, motion, enemies[i].Position));
            }
            else if (Hero.Alive) fraction = SafeFraction(actor.Position, motion, Hero.Position);
            return actor.Position + motion * fraction;
        }
        // Sweep the foot circle, so approaching one target cannot pass through a different enemy.
        private static float SafeFraction(Point start, Point motion, Point obstacle)
        {
            Point offset = start - obstacle;
            float a = motion.X * motion.X + motion.Y * motion.Y;
            float b = offset.X * motion.X + offset.Y * motion.Y;
            float c = offset.X * offset.X + offset.Y * offset.Y - FootRadius * FootRadius * 4;
            if (a < 1e-12f || b >= 0) return 1;
            float discriminant = b * b - a * c;
            if (discriminant <= 0) return 1;
            return Math.Max(0, Math.Min(1, (-b - (float)Math.Sqrt(discriminant)) / a - 0.0001f));
        }
        private static Point Clamp(Point p) => new Point(Math.Max(-2, Math.Min(2, p.X)), Math.Max(-3, Math.Min(4, p.Y)));
        private static bool WithinRange(Point origin, Point target, float range) =>
            (target - origin).Length <= range + RangeTolerance;
        public static bool InArc(Point origin, Point aim, Point target, float range, float degrees)
        {
            Point offset = target - origin;
            if (!WithinRange(origin, target, range)) return false;
            Point a = aim.Normalized, b = offset.Normalized;
            return a.X * b.X + a.Y * b.Y + 0.00001f >= Math.Cos(degrees * Math.PI / 360);
        }
    }
}
