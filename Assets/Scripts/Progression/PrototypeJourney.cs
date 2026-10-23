using System;

namespace EnhancementTower.Progression
{
    public enum JourneyRoster { Normal, Fast, Heavy, Boss }
    public enum JourneyScreen { Camp, Stages, Combat, Result, Travel }
    public sealed class JourneyStage
    {
        public readonly string Name, Description;
        public readonly JourneyRoster Roster;
        public readonly int Total, Initial, MaxAlive, Gold, Stones;
        public readonly float HealthScale, DamageScale, Interval, Seconds;
        public JourneyStage(string name, string description, JourneyRoster roster, int total, int initial,
            int maxAlive, float health, float damage, float interval, float seconds, int gold, int stones)
        {
            Name = name; Description = description; Roster = roster; Total = total; Initial = initial;
            MaxAlive = maxAlive; HealthScale = health; DamageScale = damage; Interval = interval;
            Seconds = seconds; Gold = gold; Stones = stones;
        }
    }
    // Small authored route, not final floor/economy balance. Run and unlock state is session-only.
    public sealed class PrototypeJourney
    {
        private static readonly JourneyStage[] Route = {
            new JourneyStage("01  THE GATE", "A small vanguard. Test your equipment.", JourneyRoster.Normal, 6, 2, 3, .65f, 1, 2, 60, 30, 2),
            new JourneyStage("02  THE RUSH", "Fast enemies arrive in greater numbers.", JourneyRoster.Fast, 12, 3, 5, .85f, 1.1f, 1.1f, 75, 45, 3),
            new JourneyStage("03  IRON WATCH", "Armored guards. Improve damage and survival.", JourneyRoster.Heavy, 8, 2, 4, 1.2f, 1.35f, 1.8f, 90, 60, 4),
            new JourneyStage("04  THE WARDEN", "Boss gate. Time your manual Focus Guard.", JourneyRoster.Boss, 1, 1, 1, 1, 1.5f, 1, 90, 90, 6),
            new JourneyStage("SUPPLY RUN", "Repeatable finite fight for enhancement materials.", JourneyRoster.Normal, 4, 1, 2, .5f, .75f, 2, 60, 40, 3)
        };
        public const int TowerCount = 4, SupplyIndex = 4;
        public JourneyStage Stage(int index) => Route[index];
        public int Unlocked { get; private set; } = 1;
        public int Current { get; private set; }
        public int ReturnFloor { get; private set; }
        public int Pending { get; private set; } = -1;
        public bool TowerCleared { get; private set; }
        public int Attempts { get; private set; }
        public bool Active { get; private set; }
        public bool Cleared { get; private set; }
        public JourneyScreen Screen { get; private set; } = JourneyScreen.Camp;
        public bool CanEnter(int index) => index >= 0 && index < Route.Length && (index == SupplyIndex || index < Unlocked);
        public void Navigate(JourneyScreen screen)
        {
            if (screen != JourneyScreen.Camp && screen != JourneyScreen.Stages && !(screen == JourneyScreen.Combat && Active))
                throw new InvalidOperationException("This screen requires a running encounter or a settled result.");
            Pending = -1; Screen = screen;
        }
        public void PrepareEntry(int index)
        {
            if (!CanEnter(index)) throw new InvalidOperationException("Stage is locked.");
            Pending = index; Active = false; Screen = JourneyScreen.Travel;
        }
        public void Begin(int index)
        {
            if (!CanEnter(index)) throw new InvalidOperationException("Stage is locked.");
            Pending = -1; if (index < TowerCount) ReturnFloor = index;
            Current = index; Attempts++; Active = true; Cleared = false; Screen = JourneyScreen.Combat;
        }
        public bool Complete(bool clear)
        {
            if (!Active) return false;
            Active = false; Cleared = clear; Screen = JourneyScreen.Result;
            if (clear && Current < TowerCount)
            {
                Unlocked = Math.Min(TowerCount, Math.Max(Unlocked, Current + 2));
                ReturnFloor = Math.Min(TowerCount - 1, Current + 1);
                if (Current == TowerCount - 1) TowerCleared = true;
            }
            return true;
        }
    }
}
