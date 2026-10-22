using System;
using EnhancementTower.Combat;
using NUnit.Framework;

public class StageEncounterTests
{
    private static CombatSimulation Empty(int capacity, Fighter hero = null, int seed = 1) => new CombatSimulation(
        hero ?? new Fighter(new Point(), 500, 12, 0, 0.85f), new Fighter[0], null,
        new RepositionSettings(false), seed: seed, monsterCapacity: capacity);
    private static Fighter Idle(int index, Point p) => new Fighter(p, 60, 1, 0, 0.85f);

    [Test]
    public void ScheduleAddsActualFightersGraduallyAndRespectsAliveCap()
    {
        var sim = Empty(10);
        var stage = new StageEncounter(sim, new StageSettings(10, 1, 3, 1, 20), Idle);
        Assert.AreEqual(1, sim.Monsters.Count);
        stage.Advance(0.9); Assert.AreEqual(1, stage.Spawned);
        stage.Advance(1.2); Assert.AreEqual(3, stage.Spawned);
        stage.Advance(5); Assert.AreEqual(3, stage.Spawned);
        Assert.AreEqual(7, stage.Pending); Assert.AreEqual(StageResult.Running, stage.Result);
    }
    [Test]
    public void EmptyFieldWaitsForReservationsAndFinalKillClearsAndFreezes()
    {
        var sim = Empty(2, new Fighter(new Point(0, 3), 500, 1000, 0, 4));
        var stage = new StageEncounter(sim, new StageSettings(2, 1, 2, 2, 10), Idle);
        stage.Advance(1);
        Assert.AreEqual(0, sim.LivingMonsters); Assert.AreEqual(1, stage.Pending);
        Assert.AreEqual(StageResult.Running, stage.Result);
        stage.Advance(5);
        Assert.AreEqual(StageResult.Clear, stage.Result); Assert.AreEqual(2, stage.Kills);
        int ticks = sim.Ticks; stage.Advance(50); Assert.AreEqual(ticks, sim.Ticks);
    }
    [Test]
    public void TimeoutStopsAtExactTickAndNeverSpawnsAfterEnd()
    {
        var sim = Empty(10);
        var stage = new StageEncounter(sim, new StageSettings(10, 1, 3, 2, 1), Idle);
        stage.Advance(100);
        Assert.AreEqual(StageResult.Timeout, stage.Result); Assert.AreEqual(60, sim.Ticks);
        Assert.AreEqual(0, stage.SecondsRemaining); Assert.AreEqual(1, stage.Spawned);
        stage.Advance(100); Assert.AreEqual(60, sim.Ticks);
    }
    [Test]
    public void HeroDeathStopsFutureArrivalsAndCombat()
    {
        var sim = Empty(3, new Fighter(new Point(), 1, 1, 0, 0.85f));
        var stage = new StageEncounter(sim, new StageSettings(3, 1, 3, 1, 5),
            (i, p) => new Fighter(p, 999, 999, 0, 10, 0.1f));
        stage.Advance(10);
        Assert.AreEqual(StageResult.Defeat, stage.Result); Assert.AreEqual(1, stage.Spawned);
        int ticks = sim.Ticks; stage.Advance(10); Assert.AreEqual(ticks, sim.Ticks);
    }
    [Test]
    public void ForwardArrivalsAvoidHeroAndExistingFeet()
    {
        var sim = Empty(12, new Fighter(new Point(0, 3.8f), 1000, 1, 0, 0.85f));
        var stage = new StageEncounter(sim, new StageSettings(12, 1, 12, 0.1f, 5), Idle);
        stage.Advance(2);
        foreach (var enemy in sim.Monsters)
        {
            Assert.That(enemy.Position.Y, Is.InRange(3.15f, 3.8f));
            Assert.GreaterOrEqual((enemy.Position - sim.Hero.Position).Length, 1.1f);
            foreach (var other in sim.Monsters)
                if (enemy != other) Assert.GreaterOrEqual((enemy.Position - other.Position).Length, 0.52f);
        }
        Assert.Greater(stage.Pending, 0); // Occupied spots defer reservations rather than force overlap.
        Assert.AreEqual(StageResult.Running, stage.Result);
    }
    [Test]
    public void DynamicCapacityAndSpawnValidationProtectSimulation()
    {
        var sim = Empty(1);
        Assert.Throws<ArgumentException>(() => new StageEncounter(sim, new StageSettings(2, 1, 2), Idle));
        Assert.IsFalse(sim.TryAddMonster(new Fighter(new Point(), 60, 6, 0, 0.85f)));
        Assert.IsFalse(sim.TryAddMonster(new Fighter(new Point(3, 3), 60, 6, 0, 0.85f)));
        Assert.IsTrue(sim.TryAddMonster(Idle(0, new Point(0, 2))));
        Assert.IsFalse(sim.TryAddMonster(Idle(1, new Point(1, 2))));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StageSettings(initial: 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StageSettings(spawnInterval: float.NaN));
    }
    [Test]
    public void FastEnemyAttacksMoreFrequentlyThanNormalInSustainedContact()
    {
        var fast = new FastMonsterSettings().Create(new Point(0, 0.7f), 10000, 6, 0, null);
        var normal = new Fighter(new Point(0, 0.7f), 10000, 6, 1.4f, 0.85f, 0.3f, 0.7f);
        var a = new CombatSimulation(new Fighter(new Point(), 10000, 1, 0, 0.85f), fast);
        var b = new CombatSimulation(new Fighter(new Point(), 10000, 1, 0, 0.85f), normal);
        a.Advance(10); b.Advance(10);
        Assert.GreaterOrEqual(fast.Attacks, normal.Attacks * 1.8f);
        Assert.Greater(a.Hero.DamageTaken, b.Hero.DamageTaken);
    }
    private static StageEncounter Battle(int seed)
    {
        var sim = new CombatSimulation(new Fighter(new Point(-0.7f, -1.8f), 500, 12, 1.9f, 0.85f,
            defense: 1, damageProfile: new DamageProfile(0.85f, 1.15f, 0.15f)), new Fighter[0], null,
            knockbackDistance: 0.3f, tactics: new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f),
            seed: seed, monsterCapacity: 50);
        var fast = new FastMonsterSettings(); var damage = new DamageProfile(0.85f, 1.15f, 0.05f);
        return new StageEncounter(sim, new StageSettings(), (i, p) =>
        {
            var kind = FastMonsterSettings.KindAt(i, true, true); bool heavy = kind == PrototypeMonsterKind.Heavy;
            return kind == PrototypeMonsterKind.Fast ? fast.Create(p, 60, 6, 0, damage) :
                new Fighter(p, 60, heavy ? 9 : 6, heavy ? 1.05f : 1.4f, 0.85f,
                    heavy ? 0.5f : 0.3f, heavy ? 0.9f : 0.7f, heavy ? 3 : 0, heavy ? 1.8f : 1, damage);
        });
    }
    [TestCase(1)]
    [TestCase(12345)]
    [TestCase(73)]
    public void FiftyEnemyStageReplaysAndEndsWithoutCrossingBounds(int seed)
    {
        var a = Battle(seed); var b = Battle(seed);
        for (int frame = 0; frame < 3000; frame++)
        {
            a.Advance(1.0 / 30);
            Assert.LessOrEqual(a.Simulation.LivingMonsters, a.Settings.MaxAlive);
            Assert.GreaterOrEqual(a.Simulation.Hero.Position.Y, a.Simulation.RetreatFloor);
            foreach (var enemy in a.Simulation.Monsters)
            {
                Assert.That(enemy.Position.X, Is.InRange(-2f, 2f));
                Assert.That(enemy.Position.Y, Is.InRange(-3f, 4f));
                if (a.Simulation.Hero.Alive && enemy.Alive)
                    Assert.GreaterOrEqual((enemy.Position - a.Simulation.Hero.Position).Length, 0.55999f);
            }
        }
        for (int frame = 0; frame < 12000; frame++) b.Advance(1.0 / 120);
        Assert.AreNotEqual(StageResult.Running, a.Result);
        Assert.AreEqual(a.Result, b.Result); Assert.AreEqual(a.Spawned, b.Spawned);
        Assert.AreEqual(a.Simulation.Ticks, b.Simulation.Ticks);
        Assert.AreEqual(a.Simulation.Hero.Health, b.Simulation.Hero.Health);
        Assert.AreEqual(a.Simulation.Hero.Position.X, b.Simulation.Hero.Position.X);
        Assert.AreEqual(a.Simulation.Hero.Position.Y, b.Simulation.Hero.Position.Y);
        for (int i = 0; i < a.Spawned; i++) Assert.AreEqual(a.Simulation.Monsters[i].Health, b.Simulation.Monsters[i].Health);
    }
}
