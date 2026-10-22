using System;

namespace EnhancementTower.Combat
{
    public enum StageResult { Running, Clear, Defeat, Timeout }

    public sealed class StageSettings
    {
        public readonly int Total, Initial, MaxAlive, IntervalTicks, LimitTicks;
        public StageSettings(int total = 50, int initial = 5, int maxAlive = 8,
            float spawnInterval = 1.2f, float timeLimit = 60)
        {
            double sum = (double)spawnInterval + timeLimit;
            if (total < 1 || total > 500 || initial < 1 || initial > total || maxAlive < initial || maxAlive > 12 ||
                double.IsNaN(sum) || double.IsInfinity(sum) || spawnInterval < 0.1f || spawnInterval > 60 || timeLimit < 1 || timeLimit > 600)
                throw new ArgumentOutOfRangeException(nameof(total));
            Total = total; Initial = initial; MaxAlive = maxAlive;
            IntervalTicks = (int)Math.Ceiling(spawnInterval * 60.0 - 0.00001);
            LimitTicks = (int)Math.Ceiling(timeLimit * 60.0 - 0.00001);
        }
    }

    // Owns a single finite encounter, using the same fixed clock and fighters as crowd tests.
    public sealed class StageEncounter
    {
        public readonly CombatSimulation Simulation;
        public readonly StageSettings Settings;
        private readonly Func<int, Point, Fighter> createMonster;
        private double accumulator;
        private int nextSpawnTick;
        public int Spawned { get; private set; }
        public int Kills => Spawned - Simulation.LivingMonsters;
        public int Pending => Settings.Total - Spawned;
        public float SecondsRemaining => Math.Max(0, (Settings.LimitTicks - Simulation.Ticks) / 60f);
        public StageResult Result { get; private set; }
        public StageEncounter(CombatSimulation simulation, StageSettings settings, Func<int, Point, Fighter> factory)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            createMonster = factory ?? throw new ArgumentNullException(nameof(factory));
            if (simulation.Monsters.Count != 0 || simulation.Ticks != 0 || simulation.MonsterCapacity < settings.Total)
                throw new ArgumentException("Stage requires a fresh empty simulation with enough reserved capacity.");
            for (int i = 0; i < settings.Initial; i++) if (!TrySpawn()) break;
            nextSpawnTick = settings.IntervalTicks;
        }
        private bool TrySpawn()
        {
            if (Pending == 0 || Simulation.LivingMonsters >= Settings.MaxAlive) return false;
            // Open spots along the visual forward edge, never movement lanes or hero-relative teleports.
            for (int offset = 0; offset < 14; offset++)
            {
                int slot = (Spawned * 5 + offset) % 14;
                Point point = new Point(-1.8f + (slot % 7) * 0.6f, slot < 7 ? 3.8f : 3.15f);
                if ((point - Simulation.Hero.Position).Length < 1.1f) continue;
                bool open = true;
                for (int i = 0; i < Simulation.Monsters.Count; i++)
                    if (Simulation.Monsters[i].Alive && (point - Simulation.Monsters[i].Position).Length < 0.52f)
                    { open = false; break; }
                if (!open) continue;
                Fighter monster = createMonster(Spawned, point);
                if (!Simulation.TryAddMonster(monster)) throw new InvalidOperationException("Invalid stage fighter or exhausted capacity.");
                Spawned++; return true;
            }
            return false; // Occupied entry edge: retry later, without overlap or a catch-up burst.
        }
        public void Advance(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (Result != StageResult.Running) return;
            accumulator += seconds;
            while (accumulator + 1e-9 >= CombatSimulation.Tick && Result == StageResult.Running)
            {
                accumulator -= CombatSimulation.Tick;
                if (!Simulation.Hero.Alive) { Result = StageResult.Defeat; break; }
                if (Simulation.Ticks >= nextSpawnTick)
                    nextSpawnTick = Simulation.Ticks + (TrySpawn() ? Settings.IntervalTicks : 12);
                Simulation.Advance(CombatSimulation.Tick);
                if (!Simulation.Hero.Alive) Result = StageResult.Defeat;
                else if (Pending == 0 && Simulation.LivingMonsters == 0) Result = StageResult.Clear;
                else if (Simulation.Ticks >= Settings.LimitTicks) Result = StageResult.Timeout;
            }
        }
    }
}
