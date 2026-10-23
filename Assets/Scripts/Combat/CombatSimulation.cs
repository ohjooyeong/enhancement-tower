using System;
using System.Collections.Generic;

namespace EnhancementTower.Combat
{
    public enum CombatState { Search, Approach, Attack, Reposition, Dead, BreakthroughAdvance, AvoidTelegraph }
    public enum ChargePhase { Ready, Telegraph, Charging, Recovery, Reacquire, Sweep, SweepRecovery }

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

    public enum AttackKind { DiagonalSlash, ReverseSlash, Thrust, MonsterStrike, Breakthrough, BossCharge, BossSweep }

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
        public readonly DamageProfile DamageRules;
        public readonly float ThreatWeight;
        public Fighter Target { get; internal set; }
        internal Fighter AttackTarget;
        internal float TargetHold;
        public AttackStep CurrentAttack { get; internal set; }
        public int ComboIndex { get; internal set; }
        public int LastHitCount { get; internal set; }
        public int Health { get; internal set; }
        public readonly float Speed, Range, Windup, Recovery;
        public float PhaseTime { get; internal set; }
        public float HitFlash { get; internal set; }
        public int HitsTaken { get; private set; }
        public long DamageTaken { get; private set; }
        public int CriticalHitsTaken { get; private set; }
        internal void RecordDamage(int actualDamage, bool critical)
        { HitsTaken++; DamageTaken += actualDamage; if (critical) CriticalHitsTaken++; }
        public Point HitDirection { get; internal set; }
        public AttackKind LastHitKind { get; internal set; }
        public float SlashTime { get; internal set; }
        public int Attacks { get; internal set; }
        public float KnockbackRemaining { get; internal set; }
        internal Point KnockbackDirection;
        internal float KnockbackCooldown;
        public bool Alive => Health > 0;
        public Fighter(Point position, int health, int damage, float speed, float range,
            float windup = 0.22f, float recovery = 0.68f, int defense = 0, float threatWeight = 1, DamageProfile damageProfile = null)
        {
            if (health <= 0 || damage <= 0 || speed < 0 || range < 0.6f || windup <= 0 || recovery <= 0 || defense < 0 || threatWeight <= 0 || float.IsNaN(threatWeight) || float.IsInfinity(threatWeight))
                throw new ArgumentOutOfRangeException(nameof(health), "Invalid prototype combat settings.");
            Position = position; Health = MaxHealth = health; Damage = damage;
            DamageRules = damageProfile ?? new DamageProfile();
            ThreatWeight = threatWeight; Defense = defense; Speed = speed; Range = range; Windup = windup; Recovery = recovery;
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

    public sealed class TacticalSettings
    {
        public readonly bool Enabled;
        public readonly float BlockSeconds, Cooldown, AdvanceDistance, PushDistance, MinimumImprovement;
        public readonly float ThreatWeight, WallWeight, EngagementWeight, MovementTemperature, TargetTemperature;
        public readonly AttackStep Breakthrough;
        public TacticalSettings(bool enabled = true, float blockSeconds = 1.2f, float cooldown = 8,
            float advanceDistance = 0.4f, float pushDistance = 0.5f, float minimumImprovement = 0.12f,
            float threatWeight = 1, float wallWeight = 0.8f, float engagementWeight = 0.8f, AttackStep? breakthrough = null, float movementTemperature = 0, float targetTemperature = 0)
        {
            float sum = movementTemperature + targetTemperature + blockSeconds + cooldown + advanceDistance + pushDistance + minimumImprovement + threatWeight + wallWeight + engagementWeight;
            if (float.IsNaN(sum) || float.IsInfinity(sum) || blockSeconds < 0.5f || cooldown < 3 ||
                advanceDistance <= 0 || advanceDistance > 0.6f || pushDistance <= 0 || pushDistance > 0.6f ||
                movementTemperature < 0 || movementTemperature > 3 || targetTemperature < 0 || targetTemperature > 3 || minimumImprovement < 0 || threatWeight < 0 || wallWeight < 0 || engagementWeight < 0)
                throw new ArgumentOutOfRangeException(nameof(blockSeconds));
            MovementTemperature = movementTemperature; TargetTemperature = targetTemperature;
            Enabled = enabled; BlockSeconds = blockSeconds; Cooldown = cooldown; AdvanceDistance = advanceDistance;
            PushDistance = pushDistance; MinimumImprovement = minimumImprovement; ThreatWeight = threatWeight;
            WallWeight = wallWeight; EngagementWeight = engagementWeight;
            Breakthrough = breakthrough ?? new AttackStep(AttackKind.Breakthrough, 3, 0.32f, 0.16f, 0.45f, 100, 1.1f, 1.1f, 1);
            if (Breakthrough.Kind != AttackKind.Breakthrough) throw new ArgumentException("Expected breakthrough action.");
        }
    }

    public sealed class CombatSimulation
    {
        public const float Tick = 1f / 60f;
        public const float FootRadius = 0.28f;
        private const float RangeTolerance = 0.0001f;
        public readonly Fighter Hero;
        public Fighter ChargeBoss { get; private set; }
        public ChargePhase BossPhase { get; private set; }
        public Point ChargeOrigin { get; private set; }
        public Point ChargeEnd { get; private set; }
        public int Charges { get; private set; }
        public int BossSweeps { get; private set; }
        public bool BossEnraged => ChargeBoss != null && ChargeBoss.Health <= ChargeBoss.MaxHealth / 2;
        public float BossSweepRange => ChargeBoss == null ? 0 : ChargeBoss.Range * 1.55f;
        public const float BossSweepArc = 150;
        public bool ManualBossDefense { get; private set; }
        public const float GuardDuration = 0.45f, GuardCooldown = 4f;
        public bool GuardActive => Ticks < guardEndTick;
        public float GuardSecondsRemaining => Math.Max(0, guardReadyTick - Ticks) * Tick;
        public int GuardUses { get; private set; }
        public int GuardedHits { get; private set; }
        private int guardEndTick, guardReadyTick;
        private bool guardRequested;
        // Queue an edge-triggered command; simulation consumes it on the next fixed tick.
        public bool RequestBossGuard()
        {
            if (!ManualBossDefense || !Hero.Alive || ChargeBoss == null || !ChargeBoss.Alive ||
                guardRequested || Ticks < guardReadyTick) return false;
            guardRequested = true;
            return true;
        }
        private void ConsumeGuardRequest()
        {
            if (!guardRequested) return;
            guardRequested = false;
            if (!Hero.Alive || ChargeBoss == null || !ChargeBoss.Alive) return;
            guardEndTick = Ticks + (int)Math.Round(GuardDuration / Tick);
            guardReadyTick = Ticks + (int)Math.Round(GuardCooldown / Tick);
            GuardUses++;
        }
        private float chargeTime, chargeRemaining, chargeCooldown = 3;
        private bool sweepHit, sweepStarted;
        private Point chargeAim, dodgeGoal;
        private bool dodgeChosen;
        private float telegraphSeconds, chargeSpeed, chargeDistance;
        public void EnableChargeBoss(Fighter boss, float telegraph = 1.1f, float speed = 5, float distance = 3.2f, bool manualDefense = true)
        {
            float sum = telegraph + speed + distance;
            if (Ticks != 0 || boss == null || !enemies.Contains(boss) || ChargeBoss != null ||
                float.IsNaN(sum) || float.IsInfinity(sum) || telegraph < 0.1f || speed <= 0 || distance <= 0)
                throw new ArgumentException("Invalid boss configuration or simulation already started.");
            ManualBossDefense = manualDefense;
            ChargeBoss = boss; telegraphSeconds = telegraph; chargeSpeed = speed; chargeDistance = distance;
        }
        private bool StepChargeBoss()
        {
            if (ChargeBoss == null || !ChargeBoss.Alive || !Hero.Alive) return false;
            Fighter boss = ChargeBoss;
            chargeCooldown = Math.Max(0, chargeCooldown - Tick);
            if (BossPhase == ChargePhase.Ready)
            {
                if (chargeCooldown > 0 || boss.State == CombatState.Attack) return false;
                ChargeOrigin = boss.Position; chargeAim = (Hero.Position - boss.Position).Normalized;
                float length = chargeDistance;
                if (chargeAim.X > 0) length = Math.Min(length, (2 - ChargeOrigin.X) / chargeAim.X);
                if (chargeAim.X < 0) length = Math.Min(length, (-2 - ChargeOrigin.X) / chargeAim.X);
                if (chargeAim.Y > 0) length = Math.Min(length, (4 - ChargeOrigin.Y) / chargeAim.Y);
                if (chargeAim.Y < 0) length = Math.Min(length, (-3 - ChargeOrigin.Y) / chargeAim.Y);
                ChargeEnd = ChargeOrigin + chargeAim * length;
                chargeRemaining = (ChargeEnd - ChargeOrigin).Length;
                boss.Aim = boss.AttackAim = chargeAim;
                boss.CurrentAttack = new AttackStep(AttackKind.BossCharge, 1, telegraphSeconds * (BossEnraged ? 0.8f : 1), 0.1f, 0.5f, 180, 1, 2.5f, 0);
                boss.State = CombatState.Search;
                BossPhase = ChargePhase.Telegraph; chargeTime = 0; dodgeChosen = false;
            }
            boss.Movement = new Point();
            boss.HitFlash = Math.Max(0, boss.HitFlash - Tick);
            boss.SlashTime = Math.Max(0, boss.SlashTime - Tick);
            chargeTime += Tick;
            if (BossPhase == ChargePhase.Telegraph && chargeTime >= boss.CurrentAttack.Windup)
            { BossPhase = ChargePhase.Charging; chargeTime = 0; Charges++; }
            else if (BossPhase == ChargePhase.Charging)
            {
                Point before = boss.Position;
                float distance = Math.Min(chargeRemaining, chargeSpeed * Tick);
                boss.Position = MoveWithoutCrossing(boss, Clamp(before + chargeAim * distance));
                float moved = (boss.Position - before).Length;
                boss.Movement = (boss.Position - before) * (1f / Tick);
                chargeRemaining -= moved;
                bool blocked = moved + 0.0001f < distance;
                if (blocked || chargeRemaining < 0.001f)
                {
                    if (blocked && (boss.Position - Hero.Position).Length <= FootRadius * 2 + 0.005f)
                        DealHit(boss, Hero, 2.5f);
                    BossPhase = ChargePhase.Recovery; chargeTime = 0;
                }
            }
            else if (BossPhase == ChargePhase.Recovery && chargeTime >= 0.55f)
            { BossPhase = ChargePhase.Reacquire; chargeTime = 0; }
            else if (BossPhase == ChargePhase.Reacquire)
            {
                // Turn and close physical distance using the same swept feet as ordinary approach.
                Point delta = Hero.Position - boss.Position;
                boss.Aim = delta.Normalized;
                if (delta.Length <= BossSweepRange || chargeTime >= 2)
                {
                    boss.AttackAim = boss.Aim; boss.PhaseTime = 0;
                    boss.CurrentAttack = new AttackStep(AttackKind.BossSweep, 1, BossEnraged ? 0.32f : 0.4f,
                        0.16f, 0.65f, BossSweepArc, 1.55f, 1.8f, 0);
                    boss.State = CombatState.Attack; boss.LastHitCount = 0;
                    BossPhase = ChargePhase.Sweep; chargeTime = 0; sweepHit = sweepStarted = false;
                }
                else
                {
                    Point before = boss.Position;
                    boss.Position = MoveWithoutCrossing(boss, Clamp(before + boss.Aim * Math.Min(boss.Speed * Tick, delta.Length - BossSweepRange)));
                    boss.Movement = (boss.Position - before) * (1f / Tick);
                }
            }
            else if (BossPhase == ChargePhase.Sweep)
            {
                AttackStep step = boss.CurrentAttack;
                boss.PhaseTime = chargeTime;
                if (chargeTime >= step.Windup)
                {
                    if (!sweepStarted)
                    { boss.SlashTime = step.Active; boss.Attacks++; BossSweeps++; sweepStarted = true; }
                    if (chargeTime < step.Windup + step.Active && !sweepHit && InArc(boss.Position, boss.AttackAim, Hero.Position, BossSweepRange, BossSweepArc))
                    { DealHit(boss, Hero, step.DamageScale); sweepHit = true; }
                    if (chargeTime >= step.Windup + step.Active)
                    { BossPhase = ChargePhase.SweepRecovery; chargeTime = 0; boss.State = CombatState.Reposition; }
                }
            }
            else if (BossPhase == ChargePhase.SweepRecovery && chargeTime >= boss.CurrentAttack.Recovery)
            { BossPhase = ChargePhase.Ready; chargeCooldown = BossEnraged ? 2.6f : 3.4f; boss.State = CombatState.Search; }
            return true;
        }
        private bool AvoidCharge()
        {
            if (ManualBossDefense || ChargeBoss == null || !ChargeBoss.Alive || !Hero.Alive ||
                (BossPhase != ChargePhase.Telegraph && BossPhase != ChargePhase.Charging) ||
                Hero.State == CombatState.Attack || Hero.State == CombatState.BreakthroughAdvance) return false;
            if (!dodgeChosen)
            {
                dodgeChosen = true; dodgeGoal = Hero.Position;
                Point side = new Point(-chargeAim.Y, chargeAim.X);
                float best = 0;
                Point first = Hero.Position, second = Hero.Position;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Point candidate = ReachableStep(Hero.Position, side * sign, 0.8f);
                    Point offset = candidate - ChargeOrigin;
                    float clearance = Math.Abs(offset.X * side.X + offset.Y * side.Y);
                    if (sign < 0) first = candidate; else second = candidate;
                    choiceScores[sign < 0 ? 0 : 1] = clearance >= FootRadius * 2 + 0.04f ? -clearance : double.PositiveInfinity;
                    if (clearance > best) { best = clearance; dodgeGoal = candidate; }
                }
                if (Tactics.Enabled && Tactics.MovementTemperature > 0)
                {
                    int selected = WeightedIndex(2, Tactics.MovementTemperature);
                    if (selected >= 0) dodgeGoal = selected == 0 ? first : second;
                }
                repositioning = false; hasRepositionNext = false;
            }
            Hero.State = CombatState.AvoidTelegraph;
            Hero.Aim = (ChargeBoss.Position - Hero.Position).Normalized;
            Point before = Hero.Position, delta = dodgeGoal - before;
            if (delta.Length > 0.001f)
                Hero.Position = MoveWithoutCrossing(Hero, Clamp(before + delta.Normalized * Math.Min(delta.Length, Hero.Speed * Tick)));
            Hero.Movement = (Hero.Position - before) * (1f / Tick);
            return true;
        }
        // Compatibility with the original duel tests and callers.
        public Fighter Monster => enemies[0];
        private readonly List<Fighter> enemies;
        public IReadOnlyList<Fighter> Monsters { get; }
        private readonly AttackStep[] combo;
        private readonly int[] hitOnSwing;
        public int MonsterCapacity => hitOnSwing.Length;
        public readonly float KnockbackDistance;
        public int LivingMonsters
        {
            get { int count = 0; for (int i = 0; i < enemies.Count; i++) if (enemies[i].Alive) count++; return count; }
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
        public readonly TacticalSettings Tactics;
        public int Breakthroughs { get; private set; }
        public float BreakthroughCooldown { get; private set; }
        public float BlockedSeconds { get; private set; }
        public float BreakthroughTravel { get; private set; }
        public Point LastRepositionDirection { get; private set; }
        private Point breakthroughDirection;
        private float advanceRemaining, advanceTime;
        private bool constrained, advanceEnded;
        private int nextConstraintCheck;
        public readonly int Seed;
        private uint randomState, damageRandomState;
        private struct MoveOption { public Point Goal, Next; public bool Bent; public float Cost; }
        private readonly MoveOption[] moveOptions = new MoveOption[48];
        private readonly double[] choiceScores;
        public int MovementChoices { get; private set; }
        public int TargetChoices { get; private set; }
        public CombatSimulation(Fighter hero, params Fighter[] monsters) : this(hero, monsters, null) { }
        public CombatSimulation(Fighter hero, Fighter[] monsters, AttackStep[] attacks, RepositionSettings reposition = null, float knockbackDistance = 0, TacticalSettings tactics = null, int seed = 1, int monsterCapacity = 0)
        {
            if (hero == null || monsters == null || (monsters.Length == 0 && monsterCapacity < 1)) throw new ArgumentException("A hero and enemies are required.");
            Seed = seed;
            unchecked
            {
                randomState = (uint)seed ^ 0x9e3779b9u;
                randomState = (randomState ^ (randomState >> 16)) * 0x85ebca6bu;
                randomState = (randomState ^ (randomState >> 13)) * 0xc2b2ae35u;
                randomState ^= randomState >> 16;
            }
            if (randomState == 0) randomState = 0x6d2b79f5u;
            damageRandomState = randomState ^ 0xa511e9b3u;
            if (damageRandomState == 0) damageRandomState = 1;
            int capacity = Math.Max(monsters.Length, monsterCapacity);
            choiceScores = new double[Math.Max(48, capacity)];
            Tactics = tactics ?? new TacticalSettings(false);
            combo = attacks == null ? AttackStep.DefaultCombo(hero.Windup, hero.Recovery) : (AttackStep[])attacks.Clone();
            if (combo.Length == 0) throw new ArgumentException("Combo cannot be empty.");
            for (int i = 0; i < combo.Length; i++)
                if (combo[i].MaxTargets < 1 || combo[i].Active <= 0) throw new ArgumentException("Invalid combo step.");
            hitOnSwing = new int[capacity];
            if (float.IsNaN(knockbackDistance) || float.IsInfinity(knockbackDistance) || knockbackDistance < 0 || knockbackDistance > 0.6f)
                throw new ArgumentOutOfRangeException(nameof(knockbackDistance));
            KnockbackDistance = knockbackDistance;
            RepositionRules = reposition ?? new RepositionSettings();
            RetreatFloor = Math.Max(-3, hero.Position.Y - RepositionRules.RetreatDistance);
            Hero = hero; enemies = new List<Fighter>(capacity);
            enemies.AddRange(monsters);
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null || enemies[i] == hero) throw new ArgumentException("Invalid enemy.");
                for (int j = 0; j < i; j++) if (enemies[j] == enemies[i]) throw new ArgumentException("Duplicate enemy.");
            }
            Monsters = enemies.AsReadOnly();

        }
        // Stage reservations allocate buffers once; actual fighters are added only on arrival.
        public bool TryAddMonster(Fighter monster)
        {
            if (monster == null || !monster.Alive || !Hero.Alive || monster == Hero ||
                enemies.Count >= hitOnSwing.Length || enemies.Contains(monster)) return false;
            Point p = monster.Position;
            if (float.IsNaN(p.X) || float.IsNaN(p.Y) || p.X < -2 || p.X > 2 || p.Y < -3 || p.Y > 4 ||
                (p - Hero.Position).Length < FootRadius * 2) return false;
            enemies.Add(monster);
            return true;
        }
        private Fighter NearestTarget(Fighter actor)
        {
            if (actor != Hero) return Hero.Alive ? Hero : null;
            Fighter best = null; float distance = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemies[i].Alive) continue;
                float candidate = (enemies[i].Position - actor.Position).Length;
                if (candidate < distance) { best = enemies[i]; distance = candidate; }
            }
            return best;
        }
        // AI focus is sampled at decision boundaries. Swing victims still follow geometry/nearest-first.
        private Fighter SelectTarget(Fighter actor)
        {
            Fighter nearest = NearestTarget(actor);
            if (actor != Hero || nearest == null || !Tactics.Enabled || Tactics.TargetTemperature <= 0) return nearest;
            float nearestDistance = (nearest.Position - actor.Position).Length;
            float range = actor.Range * combo[actor.ComboIndex].RangeScale;
            for (int i = 0; i < enemies.Count; i++)
            {
                Fighter enemy = enemies[i];
                float distance = (enemy.Position - actor.Position).Length;
                choiceScores[i] = double.PositiveInfinity;
                if (!enemy.Alive || distance > nearestDistance + 0.75f) continue;
                if (distance > range + RangeTolerance &&
                    (ReachableStep(actor.Position, (enemy.Position - actor.Position).Normalized, 0.25f) - actor.Position).Length < 0.1f) continue;
                float health = (float)enemy.Health / enemy.MaxHealth;
                choiceScores[i] = distance / range + health * 0.25f - Math.Min(3, enemy.ThreatWeight) * 0.15f;
            }
            int selected = WeightedIndex(enemies.Count, Tactics.TargetTemperature);
            TargetChoices++;
            return selected < 0 ? nearest : enemies[selected];
        }
        private int WeightedIndex(int count, float temperature)
        {
            double best = double.PositiveInfinity, total = 0;
            for (int i = 0; i < count; i++) best = Math.Min(best, choiceScores[i]);
            if (double.IsPositiveInfinity(best)) return -1;
            for (int i = 0; i < count; i++)
            {
                choiceScores[i] = double.IsPositiveInfinity(choiceScores[i]) ? 0 : Math.Exp(-(choiceScores[i] - best) / temperature);
                total += choiceScores[i];
            }
            // Local xorshift32: deterministic across render rates, independent from UnityEngine.Random.
            randomState ^= randomState << 13; randomState ^= randomState >> 17; randomState ^= randomState << 5;
            double pick = (randomState / 4294967296.0) * total;
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                if (choiceScores[i] <= 0) continue;
                last = i; pick -= choiceScores[i];
                if (pick < 0) return i;
            }
            return last;
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
                BreakthroughCooldown = Math.Max(0, BreakthroughCooldown - Tick);
                UpdateConstraint();
                ConsumeGuardRequest();
                bool bossActing = StepChargeBoss();
                Step(Hero);
                for (int i = 0; i < enemies.Count; i++)
                    if (!bossActing || enemies[i] != ChargeBoss) Step(enemies[i]);
                SeparateMonsters();
                ApplyKnockback();
                Ticks++;
            }
        }
        private void Step(Fighter actor)
        {
            float previousMovement = actor.Movement.Length;
            actor.TargetHold = Math.Max(0, actor.TargetHold - Tick);
            actor.Movement = new Point();
            actor.KnockbackCooldown = Math.Max(0, actor.KnockbackCooldown - Tick);
            actor.HitFlash = Math.Max(0, actor.HitFlash - Tick);
            actor.SlashTime = Math.Max(0, actor.SlashTime - Tick);
            if (!actor.Alive) { actor.State = CombatState.Dead; actor.Target = null; return; }
            if (actor == Hero)
            {
                if (AvoidCharge()) return;
                if (actor.State == CombatState.AvoidTelegraph) actor.State = CombatState.Search;
            }
            // Retain targets during approach/attack; reacquire between attacks or after death.
            bool keepSwingFocus = actor == Hero && actor.State == CombatState.Attack && actor.Target != null &&
                !actor.Target.Alive && LivingMonsters > 0;
            if (!keepSwingFocus && (actor.State == CombatState.Search || actor.Target == null || !actor.Target.Alive))
            { actor.Target = SelectTarget(actor); actor.TargetHold = 0.35f; }
            Fighter target = actor.Target;
            if (target == null) { actor.State = CombatState.Search; if (actor == Hero) repositioning = false; return; }
            float approachRange = actor == Hero ? actor.Range * combo[actor.ComboIndex].RangeScale : actor.Range;
            // A moving crowd can put another enemy between the hero and its retained target.
            // Reconsider only while approaching an out-of-range target; never cancel an attack.
            if (actor == Hero && actor.State == CombatState.Approach &&
                !WithinRange(actor.Position, target.Position, approachRange))
            {
                Fighter nearest = NearestTarget(actor);
                if (nearest != null && nearest != target &&
                    (WithinRange(actor.Position, nearest.Position, approachRange) ||
                    ((!Tactics.Enabled || Tactics.TargetTemperature <= 0 || (actor.TargetHold <= 0 && previousMovement < 0.0001f)) &&
                    (nearest.Position - actor.Position).Length + 0.05f < (target.Position - actor.Position).Length)))
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
                            new AttackStep(AttackKind.MonsterStrike, 1, actor.Windup * (actor == ChargeBoss && BossEnraged ? 0.85f : 1),
                                0.01f, Math.Max(0, actor.Recovery * (actor == ChargeBoss && BossEnraged ? 0.85f : 1) - 0.01f), 180, 1, actor == ChargeBoss ? 1.5f : 1, 0);
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
                            for (int i = 0; i < enemies.Count; i++)
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
                            DealHit(actor, target, step.DamageScale);
                    }
                    if (actor == Hero && actor.PhaseTime >= step.Windup && previous < step.Windup + step.Active)
                        ResolveSwing(step);
                    if (actor == Hero && step.Kind == AttackKind.Breakthrough && actor.PhaseTime >= step.Windup + step.Active)
                    {
                        actor.State = CombatState.BreakthroughAdvance;
                        advanceRemaining = Tactics.AdvanceDistance; advanceTime = 0; advanceEnded = false;
                    }
                    else if (actor.PhaseTime >= step.Duration)
                    {
                        if (actor == Hero) actor.ComboIndex = (actor.ComboIndex + 1) % combo.Length;
                        actor.State = CombatState.Reposition;
                    }
                    break;
                case CombatState.BreakthroughAdvance:
                    actor.PhaseTime += Tick;
                    if (!advanceEnded)
                    {
                        Point beforeAdvance = actor.Position;
                        actor.Position = MoveWithoutCrossing(actor, Clamp(beforeAdvance + breakthroughDirection * Math.Min(advanceRemaining, actor.Speed * Tick)));
                        float advanced = (actor.Position - beforeAdvance).Length;
                        advanceRemaining -= advanced; advanceTime += Tick; BreakthroughTravel += advanced;
                        actor.Movement = (actor.Position - beforeAdvance) * (1f / Tick);
                        advanceEnded = advanced < 0.0001f || advanceRemaining < 0.001f || advanceTime >= 0.6f;
                    }
                    if (actor.PhaseTime >= actor.CurrentAttack.Duration)
                    { BlockedSeconds = 0; actor.State = CombatState.Search; }
                    break;
                case CombatState.Reposition:
                    if (actor == Hero && !repositioning && TryStartBreakthrough()) break;
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
                for (int i = 0; i < enemies.Count; i++)
                {
                    Fighter candidate = enemies[i];
                    if (!candidate.Alive || hitOnSwing[i] == Hero.Attacks ||
                        !InArc(Hero.Position, Hero.AttackAim, candidate.Position, Hero.Range * step.RangeScale, step.Arc)) continue;
                    float current = (candidate.Position - Hero.Position).Length;
                    if (current < distance) { best = i; distance = current; }
                }
                if (best < 0) break;
                hitOnSwing[best] = Hero.Attacks;
                DealHit(Hero, enemies[best], step.DamageScale);
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
                for (int i = 0; i < enemies.Count; i++)
                {
                    Fighter actor = enemies[i];
                    if (!actor.Alive || actor.Speed <= 0 || actor.KnockbackRemaining > 0) continue;
                    Point correction = new Point();
                    for (int j = 0; j < enemies.Count; j++)
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
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].Alive && (enemies[i].Position - position).Length <= 1.15f) count++;
            return count;
        }
        private float Crowding(Point position)
        {
            float score = 0;
            for (int i = 0; i < enemies.Count; i++)
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
            if (Tactics.Enabled && Tactics.MovementTemperature > 0) return TrySampleReposition();
            float best = EscapeScore(Hero.Position) - (Tactics.Enabled ? Tactics.MinimumImprovement : 0.05f);
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
            LastRepositionDirection = (repositionGoal - Hero.Position).Normalized;
            repositioning = true; Repositions++; RepositionCooldown = RepositionRules.Cooldown;
            return true;
        }
        private bool TrySampleReposition()
        {
            float threshold = EscapeScore(Hero.Position) - Tactics.MinimumImprovement;
            int count = 0;
            for (int heading = 0; heading < 16; heading++)
            {
                for (int length = 0; length < 3; length++)
                {
                    float distance = RepositionRules.Distance * (0.5f + length * 0.25f);
                    Point direct = ReachableStep(Hero.Position, Heading(heading), distance);
                    var option = new MoveOption { Goal = direct, Cost = float.MaxValue };
                    if ((direct - Hero.Position).Length >= distance * 0.6f) option.Cost = EscapeScore(direct);
                    float half = distance * 0.5f;
                    Point corner = ReachableStep(Hero.Position, Heading(heading), half);
                    if ((corner - Hero.Position).Length >= half * 0.8f)
                    {
                        for (int turn = 0; turn < 16; turn++)
                        {
                            Point end = ReachableStep(corner, Heading(turn), half);
                            if ((end - corner).Length < half * 0.8f) continue;
                            float cost = EscapeScore(end) + 0.015f;
                            if (cost >= option.Cost) continue;
                            option = new MoveOption { Goal = corner, Next = end, Bent = true, Cost = cost };
                        }
                    }
                    if (option.Cost >= threshold) continue;
                    // One route per heading/length prevents many similar bends inflating a direction's odds.
                    moveOptions[count] = option; choiceScores[count++] = option.Cost;
                }
            }
            int selected = WeightedIndex(count, Tactics.MovementTemperature);
            if (selected < 0) return false;
            MoveOption chosen = moveOptions[selected];
            repositionGoal = chosen.Goal; repositionNext = chosen.Next; hasRepositionNext = chosen.Bent;
            LastRepositionDirection = (repositionGoal - Hero.Position).Normalized;
            MovementChoices++; Repositions++; repositioning = true; RepositionCooldown = RepositionRules.Cooldown;
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
            float boundary = wall * wall + rear * rear + front * front;
            if (!Tactics.Enabled) return Crowding(position) + 0.6f * boundary;
            float risk = 0, nearest = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                Fighter enemy = enemies[i];
                if (!enemy.Alive) continue;
                float distance = (enemy.Position - position).Length;
                nearest = Math.Min(nearest, distance);
                float proximity = Math.Max(0, 1.6f - distance);
                float readiness = enemy.State == CombatState.Attack ?
                    (enemy.PhaseTime < enemy.CurrentAttack.Windup ? 1.5f : 0.7f) : 1;
                risk += proximity * proximity * enemy.ThreatWeight * readiness;
                if (enemy.State == CombatState.Attack && enemy.PhaseTime < enemy.CurrentAttack.Windup &&
                    InArc(enemy.Position, enemy.AttackAim, position, enemy.Range, 180)) risk += enemy.ThreatWeight * 0.4f;
            }
            float lostContact = nearest == float.MaxValue ? 0 : Math.Max(0, nearest - Hero.Range);
            Point displacement = position - Hero.Position;
            float reverse = displacement.Length < 0.001f || LastRepositionDirection.Length < 0.001f ? 0 :
                Math.Max(0, -(displacement.Normalized.X * LastRepositionDirection.X + displacement.Normalized.Y * LastRepositionDirection.Y)) * 0.04f;
            return risk * Tactics.ThreatWeight + boundary * Tactics.WallWeight +
                lostContact * lostContact * Tactics.EngagementWeight + reverse;
        }
        private void UpdateConstraint()
        {
            if (!Tactics.Enabled || !Hero.Alive || Pressure < RepositionRules.PressureThreshold)
            { BlockedSeconds = 0; constrained = false; return; }
            if (Ticks >= nextConstraintCheck)
            {
                nextConstraintCheck = Ticks + 12;
                int blocked = 0;
                for (int i = 0; i < 16; i++)
                    if ((ReachableStep(Hero.Position, Heading(i), 0.35f) - Hero.Position).Length < 0.21f) blocked++;
                bool wall = 2 - Math.Abs(Hero.Position.X) < 0.3f || Hero.Position.Y - RetreatFloor < 0.3f || 4 - Hero.Position.Y < 0.3f;
                constrained = blocked >= 12 || (wall && blocked >= 8);
            }
            BlockedSeconds = constrained ? BlockedSeconds + Tick : 0;
        }
        private bool TryStartBreakthrough()
        {
            if (!Tactics.Enabled || !RepositionRules.Enabled || Hero.Speed <= 0 || BreakthroughCooldown > 0 ||
                BlockedSeconds < Tactics.BlockSeconds || Pressure < RepositionRules.PressureThreshold) return false;
            float best = float.MaxValue; bool found = false;
            for (int i = 0; i < 16; i++)
            {
                Point direction = Heading(i);
                Point destination = Clamp(Hero.Position + direction * Tactics.AdvanceDistance);
                destination.Y = Math.Max(RetreatFloor, destination.Y);
                if ((destination - Hero.Position).Length < Tactics.AdvanceDistance * 0.85f) continue;
                int targets = 0;
                for (int j = 0; j < enemies.Count; j++)
                    if (enemies[j].Alive && InArc(Hero.Position, direction, enemies[j].Position,
                        Hero.Range * Tactics.Breakthrough.RangeScale, Tactics.Breakthrough.Arc)) targets++;
                if (targets == 0) continue;
                float score = EscapeScore(destination);
                if (score >= best) continue;
                best = score; breakthroughDirection = direction; found = true;
            }
            if (!found) return false;
            Hero.CurrentAttack = Tactics.Breakthrough;
            Hero.Aim = Hero.AttackAim = breakthroughDirection;
            Hero.PhaseTime = 0; Hero.LastHitCount = 0; Hero.State = CombatState.Attack;
            Breakthroughs++; BreakthroughCooldown = Tactics.Cooldown;
            return true;
        }
        private Point ReachableStep(Point start, Point direction, float distance)
        {
            Point destination = Clamp(start + direction * distance);
            destination.Y = Math.Max(RetreatFloor, destination.Y);
            Point motion = destination - start;
            float fraction = 1;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].Alive) fraction = Math.Min(fraction, SafeFraction(start, motion, enemies[i].Position));
            return start + motion * fraction;
        }
        private double DamageRoll()
        {
            damageRandomState ^= damageRandomState << 13;
            damageRandomState ^= damageRandomState >> 17;
            damageRandomState ^= damageRandomState << 5;
            return damageRandomState / 4294967296.0;
        }
        private void DealHit(Fighter attacker, Fighter target, float actionScale)
        {
            int beforeHealth = target.Health;
            int damage = attacker.DamageRules.Resolve(attacker.Damage, actionScale, target.Defense,
                DamageRoll(), DamageRoll(), out bool critical);
            if (ManualBossDefense && GuardActive && attacker == ChargeBoss && target == Hero)
            {
                damage = Math.Max(1, (int)Math.Ceiling(damage * 0.3));
                GuardedHits++;
            }
            target.Health = Math.Max(0, target.Health - damage);
            target.RecordDamage(beforeHealth - target.Health, critical);
            target.LastHitKind = attacker.CurrentAttack.Kind;
            target.HitDirection = (target.Position - attacker.Position).Normalized;
            target.HitFlash = target.LastHitKind == AttackKind.Thrust ? 0.2f : 0.14f;
            attacker.LastHitCount++;
            bool breakthrough = attacker == Hero && attacker.CurrentAttack.Kind == AttackKind.Breakthrough;
            if (attacker == Hero && target != ChargeBoss && target.Alive && (breakthrough || KnockbackDistance > 0) && target.KnockbackCooldown <= 0)
            {
                target.KnockbackDirection = (target.Position - attacker.Position).Normalized;
                target.KnockbackRemaining = Math.Min(0.6f, (breakthrough ? Tactics.PushDistance : KnockbackDistance) * attacker.CurrentAttack.KnockbackScale);
                target.KnockbackCooldown = 0.75f;
            }
            if (!target.Alive) { target.KnockbackRemaining = 0; target.State = CombatState.Dead; target.Movement = new Point(); target.Target = null; }
        }
        private void ApplyKnockback()
        {
            if (!Hero.Alive) return;
            for (int i = 0; i < enemies.Count; i++)
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
                for (int i = 0; i < enemies.Count; i++)
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
