using EnhancementTower.Combat;
using NUnit.Framework;

public class CombatSimulationTests
{
    private static CombatSimulation ManualBattle()
    {
        var battle = new CombatSimulation(new Fighter(new Point(0, -1), 10000, 1, 0, 0.85f),
            new Fighter(new Point(0, 2), 10000, 100, 0, 0.85f));
        battle.EnableChargeBoss(battle.Monster);
        return battle;
    }
    [Test]
    public void ManualGuardIsQueuedHasCooldownAndExpires()
    {
        var battle = ManualBattle();
        Assert.IsTrue(battle.RequestBossGuard());
        Assert.IsFalse(battle.GuardActive);
        Assert.IsFalse(battle.RequestBossGuard());
        battle.Advance(CombatSimulation.Tick);
        Assert.IsTrue(battle.GuardActive);
        Assert.AreEqual(1, battle.GuardUses);
        battle.Advance(0.5);
        Assert.IsFalse(battle.GuardActive);
        Assert.IsFalse(battle.RequestBossGuard());
        battle.Advance(3.5);
        Assert.IsTrue(battle.RequestBossGuard());
    }
    [Test]
    public void TimedGuardReducesChargeDamageWithoutInvulnerabilityOrAutoDodge()
    {
        var guarded = ManualBattle(); var idle = ManualBattle();
        while (guarded.BossPhase != ChargePhase.Charging)
        { guarded.Advance(CombatSimulation.Tick); idle.Advance(CombatSimulation.Tick); }
        // Wait until impact is inside the guard window, rather than guarding at charge start.
        while ((guarded.Monster.Position - guarded.Hero.Position).Length > 2)
        { guarded.Advance(CombatSimulation.Tick); idle.Advance(CombatSimulation.Tick); }
        Assert.IsTrue(guarded.RequestBossGuard());
        while (guarded.BossPhase != ChargePhase.Recovery)
        { guarded.Advance(CombatSimulation.Tick); idle.Advance(CombatSimulation.Tick); }
        Assert.AreEqual(1, guarded.GuardedHits);
        Assert.Greater(guarded.Hero.DamageTaken, 0);
        Assert.Less(guarded.Hero.DamageTaken, idle.Hero.DamageTaken);
        Assert.AreEqual(1, idle.Hero.HitsTaken);
        Assert.GreaterOrEqual((guarded.Hero.Position - guarded.Monster.Position).Length, 0.559f);
    }
    [Test]
    public void GuardRejectsNormalCombatAndDeadBossAndReplaysFixedTickInputs()
    {
        var normal = new CombatSimulation(new Fighter(new Point(), 100, 1, 0, .85f),
            new Fighter(new Point(0, 2), 100, 1, 0, .85f));
        Assert.IsFalse(normal.RequestBossGuard());
        var a = ManualBattle(); var b = ManualBattle();
        for (int block = 0; block < 180; block++)
        {
            if (block % 120 == 0) { a.RequestBossGuard(); b.RequestBossGuard(); }
            a.Advance(1.0 / 30);
            for (int j = 0; j < 4; j++) b.Advance(1.0 / 120);
        }
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.GuardedHits, b.GuardedHits);
        Assert.AreEqual(a.GuardUses, b.GuardUses);
        Assert.AreEqual(a.Ticks, b.Ticks);
        var dead = new CombatSimulation(new Fighter(new Point(0, 1.3f), 1000, 1000, 0, .85f),
            new Fighter(new Point(0, 2), 1, 1, 0, .85f));
        dead.EnableChargeBoss(dead.Monster);
        dead.Advance(1);
        Assert.IsFalse(dead.Monster.Alive);
        Assert.IsFalse(dead.RequestBossGuard());
    }
    private static CombatSimulation BossBattle(float heroSpeed = 1.9f)
    {
        var battle = new CombatSimulation(new Fighter(new Point(0, -1), 10000, 1, heroSpeed, 0.85f),
            new Fighter(new Point(0, 2), 10000, 6, 0, 0.85f));
        battle.EnableChargeBoss(battle.Monster, manualDefense: false);
        return battle;
    }
    [Test]
    public void ChargeLocksPathPassesDodgingHeroAndReengages()
    {
        var battle = BossBattle();
        while (battle.BossPhase != ChargePhase.Telegraph) battle.Advance(CombatSimulation.Tick);
        Point end = battle.ChargeEnd;
        bool passed = false;
        for (int i = 0; i < 420; i++)
        {
            battle.Advance(CombatSimulation.Tick);
            Assert.GreaterOrEqual((battle.Hero.Position - battle.Monster.Position).Length, 0.559f);
            if (battle.BossPhase != ChargePhase.Ready)
            { Assert.AreEqual(end.X, battle.ChargeEnd.X); Assert.AreEqual(end.Y, battle.ChargeEnd.Y); }
            if (battle.Monster.Position.Y < battle.Hero.Position.Y) passed = true;
            if (battle.Charges > 0 && battle.BossPhase == ChargePhase.Ready) break;
        }
        Assert.IsTrue(passed);
        Assert.Greater(battle.Hero.Attacks, 0);
        Assert.Greater(battle.BossSweeps, 0);
    }
    [Test]
    public void BlockedChargeHitsOnceAndCannotTunnelThroughHero()
    {
        var battle = BossBattle(0);
        for (int i = 0; i < 600 && battle.BossPhase != ChargePhase.Recovery; i++) battle.Advance(CombatSimulation.Tick);
        Assert.AreEqual(1, battle.Hero.HitsTaken);
        Assert.GreaterOrEqual((battle.Hero.Position - battle.Monster.Position).Length, 0.559f);
        Assert.Greater(battle.Monster.Position.Y, battle.Hero.Position.Y);
    }
    [Test]
    public void BossRenderIntervalsProduceIdenticalResults()
    {
        var a = BossBattle(); var b = BossBattle();
        for (int i = 0; i < 300; i++) a.Advance(1.0 / 30);
        for (int i = 0; i < 1200; i++) b.Advance(1.0 / 120);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Monster.Position.X, b.Monster.Position.X);
        Assert.AreEqual(a.Hero.Position.Y, b.Hero.Position.Y);
        Assert.AreEqual(a.Charges, b.Charges);
    }
    [Test]
    public void ChargeEndpointStaysOnDiagonalAndInsideArena()
    {
        var battle = new CombatSimulation(new Fighter(new Point(1.8f, -2.8f), 10000, 1, 0, 0.85f),
            new Fighter(new Point(-1.7f, 2), 10000, 1, 0, 0.85f));
        battle.EnableChargeBoss(battle.Monster, distance: 20);
        while (battle.BossPhase != ChargePhase.Telegraph) battle.Advance(CombatSimulation.Tick);
        Point path = battle.ChargeEnd - battle.ChargeOrigin;
        Point aim = battle.Hero.Position - battle.ChargeOrigin;
        Assert.AreEqual(0, path.X * aim.Y - path.Y * aim.X, 0.0001f);
        Assert.That(battle.ChargeEnd.X, Is.InRange(-2f, 2f));
        Assert.That(battle.ChargeEnd.Y, Is.InRange(-3f, 4f));
        Assert.Throws<System.ArgumentException>(() => battle.EnableChargeBoss(battle.Monster));
    }
    [Test]
    public void BossSweepLocksFacingAndHitsOnlyOnceDuringActiveWindow()
    {
        var battle = BossBattle();
        for (int i = 0; i < 1200 && battle.BossPhase != ChargePhase.Sweep; i++) battle.Advance(CombatSimulation.Tick);
        Assert.AreEqual(ChargePhase.Sweep, battle.BossPhase);
        Point aim = battle.Monster.AttackAim;
        // Position a stationary target in the authored forward sector for the whole action.
        battle.Hero.Position = battle.Monster.Position + aim * 0.7f;
        int hits = battle.Hero.HitsTaken;
        for (int i = 0; i < 90 && battle.BossPhase == ChargePhase.Sweep; i++)
        {
            battle.Advance(CombatSimulation.Tick);
            Assert.AreEqual(aim.X, battle.Monster.AttackAim.X);
            Assert.AreEqual(aim.Y, battle.Monster.AttackAim.Y);
        }
        Assert.AreEqual(hits + 1, battle.Hero.HitsTaken);
        Assert.AreEqual(1, battle.BossSweeps);
        Assert.AreEqual(AttackKind.BossSweep, battle.Hero.LastHitKind);
    }
    [Test]
    public void SweepDoesNotHitBehindOrBeyondItsRange()
    {
        foreach (bool behind in new[] { true, false })
        {
            var battle = BossBattle(0);
            for (int i = 0; i < 1200 && battle.BossPhase != ChargePhase.Sweep; i++) battle.Advance(CombatSimulation.Tick);
            Assert.AreEqual(ChargePhase.Sweep, battle.BossPhase);
            Point aim = battle.Monster.AttackAim;
            battle.Hero.Position = battle.Monster.Position + aim * (behind ? -0.7f : battle.BossSweepRange + 0.3f);
            int hits = battle.Hero.HitsTaken;
            while (battle.BossPhase == ChargePhase.Sweep) battle.Advance(CombatSimulation.Tick);
            Assert.AreEqual(hits, battle.Hero.HitsTaken);
        }
    }
    [Test]
    public void HalfHealthChangesTempoWithoutChangingChargeGeometry()
    {
        CombatSimulation Prepare(bool lowerHealth)
        {
            var battle = new CombatSimulation(
                new Fighter(new Point(0, 1.3f), 10000, lowerHealth ? 6000 : 1, 0, 0.85f),
                new Fighter(new Point(0, 2), 10000, 6, 0, 0.85f));
            battle.EnableChargeBoss(battle.Monster, manualDefense: false);
            for (int i = 0; i < 120 && battle.Monster.HitsTaken == 0; i++) battle.Advance(CombatSimulation.Tick);
            Assert.AreEqual(1, battle.Monster.HitsTaken);
            battle.Hero.Position = new Point(0, -1);
            return battle;
        }
        var normal = Prepare(false); var enraged = Prepare(true);
        Assert.IsTrue(enraged.BossEnraged);
        int normalWarning = 0, fastWarning = 0;
        for (int i = 0; i < 400; i++)
        {
            normal.Advance(CombatSimulation.Tick); enraged.Advance(CombatSimulation.Tick);
            if (normal.BossPhase == ChargePhase.Telegraph) normalWarning++;
            if (enraged.BossPhase == ChargePhase.Telegraph) fastWarning++;
        }
        Assert.Less(fastWarning, normalWarning);
        Assert.AreEqual(normal.ChargeEnd.X, enraged.ChargeEnd.X);
        Assert.AreEqual(normal.ChargeEnd.Y, enraged.ChargeEnd.Y);
    }
    [TestCase(3)]
    [TestCase(71)]
    [TestCase(804)]
    public void MobilePrototypeBossMaintainsMeleePressureAndRepeatedPattern(int seed)
    {
        var hero = new Fighter(new Point(-0.7f, -1.8f), 10000, 12, 1.9f, 0.85f);
        var boss = new Fighter(new Point(0.8f, 1.8f), 10000, 10, 1.55f, 0.85f, 0.32f, 0.48f, defense: 3);
        var battle = new CombatSimulation(hero, new[] { boss }, null,
            tactics: new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f), seed: seed);
        battle.EnableChargeBoss(boss, manualDefense: false);
        int meleeHits = 0, lastHits = 0;
        for (int i = 0; i < 1800; i++)
        {
            battle.Advance(CombatSimulation.Tick);
            if (hero.HitsTaken > lastHits && hero.LastHitKind == AttackKind.MonsterStrike) meleeHits++;
            lastHits = hero.HitsTaken;
            Assert.GreaterOrEqual((hero.Position - boss.Position).Length, 0.559f);
            Assert.GreaterOrEqual(hero.Position.Y, battle.RetreatFloor - 0.0001f);
        }
        Assert.Greater(meleeHits, 0, "Melee must apply pressure between large patterns.");
        Assert.GreaterOrEqual(battle.Charges, 2);
        Assert.GreaterOrEqual(battle.BossSweeps, 2);
        Assert.Greater(hero.Attacks, 5, "Boss must leave opportunities to fight back.");
    }
    private static CombatSimulation Battle() => new CombatSimulation(
        new Fighter(new Point(-0.7f, -1.8f), 100, 12, 1.9f, 0.85f),
        new Fighter(new Point(0.8f, 1.8f), 60, 6, 1.4f, 0.85f));
    [Test]
    public void ArcRejectsBehindAndOutsideRange()
    {
        Assert.IsTrue(CombatSimulation.InArc(new Point(), new Point(0, 1), new Point(0.4f, 0.4f), 1, 180));
        Assert.IsFalse(CombatSimulation.InArc(new Point(), new Point(0, 1), new Point(0, -0.4f), 1, 180));
        Assert.IsFalse(CombatSimulation.InArc(new Point(), new Point(0, 1), new Point(0, 2), 1, 180));
    }
    [Test]
    public void RenderRateDoesNotChangeCombat()
    {
        var a = Battle(); var b = Battle();
        for (int i = 0; i < 300; i++) a.Advance(1.0 / 30);
        for (int i = 0; i < 1200; i++) b.Advance(1.0 / 120);
        Assert.AreEqual(a.Ticks, b.Ticks);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Monster.Health, b.Monster.Health);
        Assert.AreEqual(a.Hero.Position.X, b.Hero.Position.X);
        Assert.AreEqual(a.Hero.Attacks, b.Hero.Attacks);
    }
    [Test]
    public void DuelMaintainsFeetSeparationAndStopsAfterDeath()
    {
        var battle = Battle();
        for (int i = 0; i < 1200; i++)
        {
            battle.Advance(CombatSimulation.Tick);
            Assert.GreaterOrEqual((battle.Hero.Position - battle.Monster.Position).Length, CombatSimulation.FootRadius * 2);
        }
        Assert.IsFalse(battle.Monster.Alive);
        Assert.AreEqual(CombatState.Dead, battle.Monster.State);
        int health = battle.Hero.Health, attacks = battle.Hero.Attacks;
        battle.Advance(10);
        Assert.AreEqual(health, battle.Hero.Health);
        Assert.AreEqual(attacks, battle.Hero.Attacks);
    }
    [Test]
    public void WindupDoesNotDealImmediateDamage()
    {
        var battle = new CombatSimulation(new Fighter(new Point(0, 0), 100, 12, 1, 0.85f),
            new Fighter(new Point(0, 0.8f), 60, 6, 1, 0.85f));
        battle.Advance(0.15);
        Assert.AreEqual(60, battle.Monster.Health);
        Assert.AreEqual(0, battle.Hero.Movement.Length);
        Assert.Greater(battle.Hero.Aim.Y, 0);
    }
    [Test]
    public void SlashHitsFrontEquallyWithIndividualDefenseButNotBehindOrOutOfRange()
    {
        var hero = new Fighter(new Point(), 1000, 20, 0, 1.2f);
        var primary = new Fighter(new Point(0, 0.65f), 100, 1, 0, 0.6f);
        var side = new Fighter(new Point(0.5f, 0.5f), 100, 1, 0, 0.6f, defense: 3);
        var behind = new Fighter(new Point(0, -0.7f), 100, 1, 0, 0.6f);
        var far = new Fighter(new Point(0, 1.4f), 100, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, primary, side, behind, far);
        battle.Advance(0.4);
        Assert.AreSame(primary, hero.Target);
        Assert.AreEqual(80, primary.Health);
        Assert.AreEqual(83, side.Health);
        Assert.AreEqual(100, behind.Health);
        Assert.AreEqual(100, far.Health);
        Assert.AreEqual(2, hero.LastHitCount);
    }
    [Test]
    public void SlashRespectsTotalLimitAndDoesNotRepeatDuringRecovery()
    {
        var hero = new Fighter(new Point(), 1000, 20, 0, 1.2f);
        var a = new Fighter(new Point(0, 0.6f), 100, 1, 0, 0.6f);
        var b = new Fighter(new Point(-0.5f, 0.5f), 100, 1, 0, 0.6f);
        var c = new Fighter(new Point(0.5f, 0.5f), 100, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, new[] { a, b, c }, new[] { new AttackStep(AttackKind.DiagonalSlash, 2, 0.22f, 0.12f, 0.56f, 180, 1, 1, 0.65f) });
        battle.Advance(0.7);
        Assert.AreEqual(80, a.Health);
        Assert.AreEqual(80, b.Health);
        Assert.AreEqual(100, c.Health);
        Assert.AreEqual(1, hero.Attacks);
    }
    [Test]
    public void RetargetsAfterKillAndTurnsToEnemyBehind()
    {
        var hero = new Fighter(new Point(), 1000, 100, 0, 0.85f);
        var front = new Fighter(new Point(0, 0.65f), 60, 1, 0, 0.6f);
        var behind = new Fighter(new Point(0, -0.7f), 60, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, front, behind);
        battle.Advance(0.4);
        Assert.IsFalse(front.Alive);
        Assert.IsTrue(behind.Alive);
        battle.Advance(0.9);
        Assert.IsFalse(behind.Alive);
        Assert.Less(hero.AttackAim.Y, 0);
        battle.Advance(1);
        Assert.IsNull(hero.Target);
        Assert.AreEqual(CombatState.Search, hero.State);
    }
    private static CombatSimulation Crowd()
    {
        return new CombatSimulation(new Fighter(new Point(-0.7f, -1.8f), 240, 12, 1.9f, 0.85f),
            new Fighter(new Point(0.8f, 1.8f), 60, 6, 1.4f, 0.85f, 0.3f, 0.7f),
            new Fighter(new Point(-0.45f, 1.55f), 60, 6, 1.4f, 0.85f, 0.3f, 0.7f),
            new Fighter(new Point(1.5f, 2.65f), 60, 6, 1.4f, 0.85f, 0.3f, 0.7f),
            new Fighter(new Point(-0.85f, 3), 60, 6, 1.4f, 0.85f, 0.3f, 0.7f),
            new Fighter(new Point(0.55f, 3.45f), 60, 6, 1.4f, 0.85f, 0.3f, 0.7f));
    }
    [Test]
    public void CrowdHasNoHeroOverlapAndAlwaysTerminates()
    {
        var battle = Crowd();
        for (int tick = 0; tick < 3600; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            if (battle.Hero.Alive)
                foreach (var enemy in battle.Monsters)
                    if (enemy.Alive) Assert.GreaterOrEqual((enemy.Position - battle.Hero.Position).Length, 0.5599f);
            Assert.That(battle.Hero.Position.X, Is.InRange(-2, 2));
            Assert.That(battle.Hero.Position.Y, Is.InRange(-3, 4));
        }
        Assert.IsTrue(!battle.Hero.Alive || battle.LivingMonsters == 0, "Crowd must not stall indefinitely.");
    }
    [Test]
    public void CrowdIsIdenticalAcrossFrameRates()
    {
        var a = Crowd(); var b = Crowd();
        for (int i = 0; i < 900; i++) a.Advance(1.0 / 30);
        for (int i = 0; i < 3600; i++) b.Advance(1.0 / 120);
        Assert.AreEqual(a.Ticks, b.Ticks);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Hero.Position.X, b.Hero.Position.X);
        for (int i = 0; i < a.Monsters.Count; i++)
        {
            Assert.AreEqual(a.Monsters[i].Health, b.Monsters[i].Health);
            Assert.AreEqual(a.Monsters[i].Position.X, b.Monsters[i].Position.X);
            Assert.AreEqual(a.Monsters[i].Position.Y, b.Monsters[i].Position.Y);
            Assert.AreEqual(a.Monsters[i].Attacks, b.Monsters[i].Attacks);
        }
    }
    private static CombatSimulation PressureBattle(bool enabled = true, float heroY = 0, float retreat = 0.8f)
    {
        var hero = new Fighter(new Point(0, heroY), 5000, 1, 1.9f, 0.85f);
        var enemies = new[] {
            new Fighter(new Point(0, heroY + 0.7f), 5000, 1, 0, 0.85f),
            new Fighter(new Point(-0.5f, heroY + 0.5f), 5000, 1, 0, 0.85f),
            new Fighter(new Point(0.5f, heroY + 0.5f), 5000, 1, 0, 0.85f) };
        return new CombatSimulation(hero, enemies, null, new RepositionSettings(enabled, retreatDistance: retreat));
    }
    [Test]
    public void PressureStepsAreShortHaveCooldownAndReturnToAttacking()
    {
        var battle = PressureBattle();
        int previousCount = 0, previousStart = -10000;
        bool sideways = false;
        for (int tick = 0; tick < 900; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            if (battle.Repositions != previousCount)
            {
                Assert.GreaterOrEqual(tick - previousStart, 179);
                previousStart = tick; previousCount = battle.Repositions;
            }
            if (battle.Hero.State == CombatState.Reposition && battle.Hero.Movement.Length > 0)
                sideways |= System.Math.Abs(battle.Hero.Movement.X * battle.Hero.Aim.Y - battle.Hero.Movement.Y * battle.Hero.Aim.X) > 0.01f;
            Assert.GreaterOrEqual(battle.Hero.Position.Y, battle.RetreatFloor - 0.0001f);
        }
        Assert.Greater(battle.Repositions, 0);
        Assert.IsTrue(sideways, "Movement must be independent from aim.");
        Assert.Greater(battle.Hero.Attacks, battle.Repositions);
        Assert.LessOrEqual(battle.RepositionTravel, battle.Repositions * 0.5001f);
        Assert.Greater(battle.Hero.Attacks, 5);
    }
    [Test]
    public void RearBoundaryAndDisabledRepositionKeepHeroFighting()
    {
        var battle = PressureBattle(heroY: -3, retreat: 0);
        var disabled = PressureBattle(false);
        for (int tick = 0; tick < 600; tick++)
        {
            battle.Advance(CombatSimulation.Tick); disabled.Advance(CombatSimulation.Tick);
            Assert.GreaterOrEqual(battle.Hero.Position.Y, -3);
            Assert.That(battle.Hero.Position.X, Is.InRange(-2, 2));
        }
        Assert.Greater(battle.Hero.Attacks, 5);
        Assert.AreEqual(0, disabled.Repositions);
        Assert.Greater(disabled.Hero.Attacks, 5);
    }
    [Test]
    public void FullyBlockedHeroHoldsGroundWithoutCancellingAttacks()
    {
        var enemies = new Fighter[8];
        for (int i = 0; i < enemies.Length; i++)
        {
            double angle = i * System.Math.PI / 4;
            enemies[i] = new Fighter(new Point((float)System.Math.Cos(angle) * 0.6f,
                (float)System.Math.Sin(angle) * 0.6f), 5000, 1, 0, 0.85f);
        }
        var battle = new CombatSimulation(new Fighter(new Point(), 5000, 1, 1.9f, 0.85f), enemies);
        battle.Advance(10);
        Assert.AreEqual(0, battle.Repositions);
        Assert.Greater(battle.Hero.Attacks, 8);
        Assert.Less(battle.Hero.Position.Length, 0.001f);
    }
    private static CombatSimulation Stress(int count)
    {
        Point[] positions = { new Point(0.8f, 1.8f), new Point(-0.45f, 1.55f), new Point(1.5f, 2.65f),
            new Point(-0.85f, 3), new Point(0.55f, 3.45f), new Point(-1.55f, 2.05f),
            new Point(1.75f, 3.55f), new Point(0.15f, 2.35f), new Point(-1.6f, 3.7f),
            new Point(1.1f, 2.2f), new Point(-0.35f, 3.85f), new Point(-1.1f, 1.7f) };
        var enemies = new Fighter[count];
        for (int i = 0; i < count; i++) enemies[i] = new Fighter(positions[i], 60, 6, 1.4f, 0.85f, 0.3f, 0.7f);
        return new CombatSimulation(new Fighter(new Point(-0.7f, -1.8f), 500, 12, 1.9f, 0.85f), enemies);
    }
    [TestCase(5)]
    [TestCase(8)]
    [TestCase(12)]
    public void StressStaysSeparatedAndBoundedAndMatchesFrameRates(int count)
    {
        var a = Stress(count); var b = Stress(count);
        for (int tick = 0; tick < 3600; tick++)
        {
            a.Advance(CombatSimulation.Tick);
            Assert.GreaterOrEqual(a.Hero.Position.Y, a.RetreatFloor - 0.0001f);
            Assert.That(a.Hero.Position.X, Is.InRange(-2, 2));
            foreach (var enemy in a.Monsters)
            {
                Assert.That(enemy.Position.X, Is.InRange(-2, 2));
                Assert.That(enemy.Position.Y, Is.InRange(-3, 4));
                if (enemy.Alive && a.Hero.Alive)
                    Assert.GreaterOrEqual((enemy.Position - a.Hero.Position).Length, 0.5599f);
            }
        }
        // Same simulated ticks delivered in batches of four (15 render FPS).
        for (int frame = 0; frame < 900; frame++) b.Advance(CombatSimulation.Tick * 4.0);
        Assert.AreEqual(a.Ticks, b.Ticks);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Hero.Attacks, b.Hero.Attacks);
        Assert.AreEqual(a.Hero.Position.X, b.Hero.Position.X);
        Assert.AreEqual(a.Hero.Position.Y, b.Hero.Position.Y);
        Assert.AreEqual(a.Repositions, b.Repositions);
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(a.Monsters[i].Health, b.Monsters[i].Health);
            Assert.AreEqual(a.Monsters[i].Position.X, b.Monsters[i].Position.X);
        }
        Assert.IsTrue(!a.Hero.Alive || a.LivingMonsters == 0, "Encounter must not stall.");
    }
    [Test]
    public void CoincidentMonstersSpreadWhileAttackingWithoutInterruptingTheirAttacks()
    {
        var enemies = new Fighter[5];
        for (int i = 0; i < enemies.Length; i++)
            enemies[i] = new Fighter(new Point(0, 0.7f), 5000, 1, 1.4f, 0.85f);
        var battle = new CombatSimulation(new Fighter(new Point(), 5000, 1, 0, 0.85f), enemies);
        battle.Advance(4);
        float closest = float.MaxValue;
        for (int i = 0; i < enemies.Length; i++)
        {
            Assert.Greater(enemies[i].Attacks, 0, "Separation must not cancel attack timing.");
            Assert.GreaterOrEqual(enemies[i].Position.Length, 0.5599f);
            for (int j = i + 1; j < enemies.Length; j++)
                closest = System.Math.Min(closest, (enemies[i].Position - enemies[j].Position).Length);
        }
        Assert.Greater(closest, 0.2f, "A coincident group should visibly spread, including during attacks.");
    }
    [Test]
    public void CrowdCorrectionsAreBoundedAndDeadMonstersDoNotPushSurvivors()
    {
        var a = new Fighter(new Point(-0.1f, 0.75f), 5000, 1, 1.4f, 0.85f);
        var b = new Fighter(new Point(0.1f, 0.75f), 1, 1, 1.4f, 0.85f);
        var battle = new CombatSimulation(new Fighter(new Point(), 5000, 20, 0, 1.2f), a, b);
        Point previous = a.Position;
        for (int i = 0; i < 20; i++)
        {
            battle.Advance(CombatSimulation.Tick);
            Assert.LessOrEqual((a.Position - previous).Length, (a.Speed + 0.6f) * CombatSimulation.Tick + 0.0001f);
            previous = a.Position;
        }
        Assert.IsFalse(b.Alive);
        Point settled = a.Position;
        battle.Advance(0.3);
        Assert.Less((a.Position - settled).Length, 0.001f);
    }
    [TestCase(-2f)]
    [TestCase(2f)]
    public void WallCrowdUsesOpenRouteAndResumesAttacking(float wallX)
    {
        float inward = wallX < 0 ? 1 : -1;
        var hero = new Fighter(new Point(wallX, -2), 5000, 1, 1.9f, 0.85f);
        var enemies = new[] {
            new Fighter(new Point(wallX, -1.35f), 5000, 1, 0, 0.85f),
            new Fighter(new Point(wallX + inward * 0.6f, -1.9f), 5000, 1, 0, 0.85f),
            new Fighter(new Point(wallX + inward * 0.6f, -1.35f), 5000, 1, 0, 0.85f) };
        var battle = new CombatSimulation(hero, enemies);
        bool moved = false;
        for (int tick = 0; tick < 600; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            moved |= (hero.Position - new Point(wallX, -2)).Length > 0.2f;
            Assert.That(hero.Position.X, Is.InRange(-2, 2));
            Assert.GreaterOrEqual(hero.Position.Y, battle.RetreatFloor - 0.0001f);
            foreach (var enemy in enemies) Assert.GreaterOrEqual((hero.Position - enemy.Position).Length, 0.5599f);
        }
        Assert.IsTrue(moved, "An open wall-side route must be used.");
        Assert.Greater(battle.Repositions, 0);
        Assert.Greater(hero.Attacks, 5);
        Assert.LessOrEqual(battle.RepositionTravel, battle.Repositions * 0.5001f);
    }
    [Test]
    public void AttackEntryMatchesHitRangeAtFloatingPointBoundary()
    {
        var hero = new Fighter(new Point(-2, -3), 1000, 12, 0, 0.85f);
        var enemy = new Fighter(new Point(-1.14995f, -3), 1000, 1, 0, 0.85f);
        Assert.IsTrue(CombatSimulation.InArc(hero.Position, new Point(1, 0), enemy.Position, hero.Range, 180));
        var battle = new CombatSimulation(hero, enemy);
        battle.Advance(0.4);
        Assert.Greater(hero.Attacks, 0);
        Assert.Less(enemy.Health, 1000);
    }
    [Test]
    public void ApproachingHeroSwitchesToNearbyEnemyInsteadOfPursuingBlockedTarget()
    {
        var hero = new Fighter(new Point(-2, -3), 1000, 12, 1.9f, 0.85f);
        var original = new Fighter(new Point(-1, -2), 1000, 1, 0, 0.85f);
        var closer = new Fighter(new Point(1, 2), 1000, 1, 0, 0.85f);
        var battle = new CombatSimulation(hero, original, closer);
        battle.Advance(CombatSimulation.Tick); // Acquire original target.
        original.Position = new Point(1.8f, -2);
        closer.Position = new Point(-1.4f, -3); // Blocks the original approach while already attackable.
        battle.Advance(0.5);
        Assert.AreSame(closer, hero.Target);
        Assert.Greater(hero.Attacks, 0);
        Assert.Less(closer.Health, 1000);
    }
    [TestCase(-2f)]
    [TestCase(2f)]
    public void RearCornerCrowdDoesNotSilentlyStopAttacking(float wallX)
    {
        float inward = wallX < 0 ? 1 : -1;
        Point[] offsets = { new Point(0, 0.7f), new Point(0.6f, 0.1f), new Point(0.6f, 0.6f),
            new Point(1.2f, 0.2f), new Point(0.2f, 1.2f), new Point(1.2f, 0.8f),
            new Point(0.7f, 1.3f), new Point(1.5f, 1.2f) };
        var enemies = new Fighter[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
            enemies[i] = new Fighter(new Point(wallX + offsets[i].X * inward, -3 + offsets[i].Y), 5000, 1, 1.4f, 0.85f);
        var hero = new Fighter(new Point(wallX, -3), 5000, 1, 1.9f, 0.85f);
        var battle = new CombatSimulation(hero, enemies);
        int previousAttacks = 0, silentTicks = 0;
        for (int tick = 0; tick < 1200; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            silentTicks = hero.Attacks == previousAttacks ? silentTicks + 1 : 0;
            previousAttacks = hero.Attacks;
            Assert.Less(silentTicks, 240, "A living hero must not get stuck without attacking for four seconds.");
            Assert.GreaterOrEqual(hero.Position.Y, battle.RetreatFloor - 0.0001f);
            foreach (var enemy in enemies) Assert.GreaterOrEqual((hero.Position - enemy.Position).Length, 0.5599f);
        }
        Assert.Greater(hero.Attacks, 10);
    }
    [Test]
    public void KnockbackPassesThroughRearMonsterWithoutCancellingAttack()
    {
        var front = new Fighter(new Point(0, 0.7f), 1000, 1, 0, 0.85f, 0.4f, 0.6f);
        var rear = new Fighter(new Point(0, 0.9f), 1000, 1, 0, 0.85f);
        var battle = new CombatSimulation(new Fighter(new Point(), 1000, 12, 0, 0.85f), new[] { front, rear },
            null, knockbackDistance: 0.5f);
        battle.Advance(0.7);
        Assert.Greater(front.Position.Y, rear.Position.Y, "Rear monster must not block knockback.");
        Assert.AreEqual(1, front.Attacks, "Knockback must not reset windup.");
        Assert.AreEqual(0.9f, rear.Position.Y);
        Assert.AreEqual(0, front.KnockbackRemaining);
        Assert.GreaterOrEqual(front.Position.Length, 0.5599f);
    }
    [Test]
    public void KnockbackStopsAtWallAndDoesNotDisplaceDeadActor()
    {
        var enemy = new Fighter(new Point(1.9f, 0), 1000, 1, 0, 0.85f);
        var battle = new CombatSimulation(new Fighter(new Point(1.2f, 0), 1000, 12, 0, 0.85f),
            new[] { enemy }, null, knockbackDistance: 0.3f);
        battle.Advance(0.6);
        Assert.That(enemy.Position.X, Is.EqualTo(2).Within(0.0001f));
        Assert.AreEqual(0, enemy.KnockbackRemaining);
        var dead = new Fighter(new Point(0, 0.7f), 1, 1, 0, 0.85f);
        var lethal = new CombatSimulation(new Fighter(new Point(), 1000, 12, 0, 0.85f), new[] { dead },
            null, knockbackDistance: 0.3f);
        lethal.Advance(1);
        Assert.IsFalse(dead.Alive);
        Assert.AreEqual(0.7f, dead.Position.Y);
    }
    [Test]
    public void KnockbackRemainsDeterministicInMovingCrowd()
    {
        var originalA = Stress(12); var originalB = Stress(12);
        var enemiesA = new Fighter[12]; var enemiesB = new Fighter[12];
        for (int i = 0; i < 12; i++) { enemiesA[i] = originalA.Monsters[i]; enemiesB[i] = originalB.Monsters[i]; }
        var a = new CombatSimulation(originalA.Hero, enemiesA, null, knockbackDistance: 0.3f);
        var b = new CombatSimulation(originalB.Hero, enemiesB, null, knockbackDistance: 0.3f);
        for (int tick = 0; tick < 3600; tick++)
        {
            a.Advance(CombatSimulation.Tick);
            foreach (var enemy in a.Monsters)
            {
                Assert.That(enemy.Position.X, Is.InRange(-2, 2));
                Assert.That(enemy.Position.Y, Is.InRange(-3, 4));
                if (a.Hero.Alive && enemy.Alive) Assert.GreaterOrEqual((enemy.Position - a.Hero.Position).Length, 0.5599f);
            }
        }
        for (int frame = 0; frame < 900; frame++) b.Advance(CombatSimulation.Tick * 4.0);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        for (int i = 0; i < 12; i++)
        {
            Assert.AreEqual(a.Monsters[i].Health, b.Monsters[i].Health);
            Assert.AreEqual(a.Monsters[i].Position.X, b.Monsters[i].Position.X);
            Assert.AreEqual(a.Monsters[i].Position.Y, b.Monsters[i].Position.Y);
        }
        Assert.IsTrue(!a.Hero.Alive || a.LivingMonsters == 0);
    }
    [Test]
    public void RapidHitsDoNotStackOrContinuouslyRefreshKnockback()
    {
        var hero = new Fighter(new Point(), 5000, 1, 0, 2, 0.01f, 0.01f);
        var enemy = new Fighter(new Point(0, 0.7f), 5000, 1, 0, 0.85f);
        var battle = new CombatSimulation(hero, new[] { enemy }, null, knockbackDistance: 0.3f);
        battle.Advance(0.65);
        Assert.Greater(hero.Attacks, 3);
        Assert.That(enemy.Position.Y, Is.EqualTo(0.895f).Within(0.0001f));
        Assert.AreEqual(0, enemy.KnockbackRemaining);
        Assert.Greater(enemy.Attacks, 0);
    }
    [Test]
    public void SlashSelectsNearestThreeRegardlessOfSpawnOrder()
    {
        var hero = new Fighter(new Point(), 5000, 20, 0, 1.2f);
        var far = new Fighter(new Point(0, 1.1f), 1000, 1, 0, 0.6f);
        var near = new Fighter(new Point(0, 0.6f), 1000, 1, 0, 0.6f);
        var second = new Fighter(new Point(0.3f, 0.6f), 1000, 1, 0, 0.6f);
        var third = new Fighter(new Point(-0.4f, 0.6f), 1000, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, far, third, second, near);
        battle.Advance(0.5);
        Assert.AreEqual(1000, far.Health);
        Assert.AreEqual(980, near.Health);
        Assert.AreEqual(980, second.Health);
        Assert.AreEqual(980, third.Health);
        Assert.AreEqual(3, hero.LastHitCount);
    }
    [Test]
    public void SwingAcceptsLateEntrantButNeverHitsSameEnemyTwice()
    {
        var hero = new Fighter(new Point(), 5000, 20, 0, 1.2f);
        var first = new Fighter(new Point(0, 0.7f), 1000, 1, 0, 0.6f);
        var entering = new Fighter(new Point(0, 2), 1000, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, first, entering);
        battle.Advance(0.28);
        Assert.AreEqual(980, first.Health);
        entering.Position = new Point(0.5f, 0.5f);
        battle.Advance(0.1);
        Assert.AreEqual(980, first.Health);
        Assert.AreEqual(980, entering.Health);
        battle.Advance(0.3);
        Assert.AreEqual(980, entering.Health);
        Assert.AreEqual(2, hero.LastHitCount);
    }
    [Test]
    public void ThrustHitsOnlyOneInNarrowForwardArcWithHigherDamage()
    {
        var thrust = new AttackStep(AttackKind.Thrust, 1, 0.3f, 0.1f, 0.72f, 24, 1.25f, 1.8f, 1.3f);
        var hero = new Fighter(new Point(), 5000, 20, 0, 1);
        var front = new Fighter(new Point(0, 0.7f), 1000, 1, 0, 0.6f);
        var behindFront = new Fighter(new Point(0, 1.1f), 1000, 1, 0, 0.6f);
        var side = new Fighter(new Point(0.6f, 0.5f), 1000, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, new[] { front, behindFront, side }, new[] { thrust });
        battle.Advance(0.5);
        Assert.AreEqual(964, front.Health);
        Assert.AreEqual(1000, behindFront.Health);
        Assert.AreEqual(1000, side.Health);
        Assert.AreEqual(1, hero.LastHitCount);
    }
    [Test]
    public void ComboCyclesDifferentAttacksAndRestartsFromFirstStepInNewBattle()
    {
        var hero = new Fighter(new Point(), 5000, 1, 0, 1.2f);
        var enemy = new Fighter(new Point(0, 0.7f), 5000, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, enemy);
        AttackKind[] expected = { AttackKind.DiagonalSlash, AttackKind.ReverseSlash, AttackKind.Thrust, AttackKind.DiagonalSlash };
        int count = 0, previous = 0;
        for (int tick = 0; tick < 300 && count < expected.Length; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            if (hero.Attacks == previous) continue;
            Assert.AreEqual(expected[count++], hero.CurrentAttack.Kind); previous = hero.Attacks;
        }
        Assert.AreEqual(4, count);
        var restarted = new CombatSimulation(new Fighter(new Point(), 5000, 1, 0, 1.2f),
            new Fighter(new Point(0, 0.7f), 5000, 1, 0, 0.6f));
        restarted.Advance(0.3);
        Assert.AreEqual(AttackKind.DiagonalSlash, restarted.Hero.CurrentAttack.Kind);
    }
    private static CombatSimulation TrappedBattle(bool enabled = true, bool pinned = false)
    {
        var enemies = new Fighter[8];
        for (int i = 0; i < enemies.Length; i++)
        {
            double angle = i * System.Math.PI / 4;
            enemies[i] = new Fighter(new Point((float)System.Math.Cos(angle) * 0.65f,
                (float)System.Math.Sin(angle) * 0.65f), 5000, 1, 0, 0.85f);
        }
        var hero = new Fighter(new Point(), 5000, 1, pinned ? 0 : 1.9f, 0.85f);
        return new CombatSimulation(hero, enemies, null, knockbackDistance: 0,
            tactics: new TacticalSettings(enabled));
    }
    [Test]
    public void TrappedHeroPushesThenAdvancesWithoutPhasingOrSpendingComboStep()
    {
        var battle = TrappedBattle();
        int previousCount = 0, lastStart = -10000, savedCombo = -1, attacksAtStart = 0;
        bool advanced = false, completed = false;
        for (int tick = 0; tick < 1200; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            if (battle.Breakthroughs != previousCount)
            {
                Assert.GreaterOrEqual(tick - lastStart, 479);
                Assert.GreaterOrEqual(tick, 72);
                previousCount = battle.Breakthroughs; lastStart = tick;
                savedCombo = battle.Hero.ComboIndex; attacksAtStart = battle.Hero.Attacks;
            }
            if (battle.Hero.State == CombatState.BreakthroughAdvance)
            {
                advanced = true;
                Assert.AreEqual(savedCombo, battle.Hero.ComboIndex);
            }
            if (advanced && battle.Hero.Attacks > attacksAtStart + 1) completed = true;
            Assert.GreaterOrEqual(battle.Hero.Position.Y, battle.RetreatFloor - 0.0001f);
            foreach (var enemy in battle.Monsters)
                Assert.GreaterOrEqual((enemy.Position - battle.Hero.Position).Length, 0.5599f);
        }
        Assert.Greater(battle.Breakthroughs, 0);
        Assert.IsTrue(advanced);
        Assert.IsTrue(completed, "Hero must return to normal attacks after advancing.");
        Assert.Greater(battle.BreakthroughTravel, 0.05f);
        Assert.LessOrEqual(battle.BreakthroughTravel, battle.Breakthroughs * 0.4001f);
    }
    [Test]
    public void DisabledOrImmobileHeroNeverUsesBreakthrough()
    {
        var disabled = TrappedBattle(false); var pinned = TrappedBattle(pinned: true);
        disabled.Advance(12); pinned.Advance(12);
        Assert.AreEqual(0, disabled.Breakthroughs);
        Assert.AreEqual(0, pinned.Breakthroughs);
        Assert.Greater(disabled.Hero.Attacks, 5);
        Assert.Greater(pinned.Hero.Attacks, 5);
    }
    [Test]
    public void BreakthroughIsDeterministicAcrossFrameBatches()
    {
        var a = TrappedBattle(); var b = TrappedBattle();
        for (int tick = 0; tick < 900; tick++) a.Advance(CombatSimulation.Tick);
        for (int frame = 0; frame < 225; frame++) b.Advance(CombatSimulation.Tick * 4.0);
        Assert.AreEqual(a.Breakthroughs, b.Breakthroughs);
        Assert.AreEqual(a.BreakthroughTravel, b.BreakthroughTravel);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Hero.Attacks, b.Hero.Attacks);
        Assert.AreEqual(a.Hero.Position.X, b.Hero.Position.X);
        Assert.AreEqual(a.Hero.Position.Y, b.Hero.Position.Y);
    }
    [Test]
    public void OpenDuelKeepsAttackingWithoutBreakthroughOrRepeatedEvading()
    {
        var hero = new Fighter(new Point(), 5000, 1, 1.9f, 0.85f);
        var enemy = new Fighter(new Point(0, 0.7f), 5000, 1, 0, 0.85f);
        var battle = new CombatSimulation(hero, new[] { enemy }, null, tactics: new TacticalSettings());
        battle.Advance(12);
        Assert.AreEqual(0, battle.Breakthroughs);
        Assert.AreEqual(0, battle.Repositions);
        Assert.Greater(hero.Attacks, 8);
    }
    private static CombatSimulation WeightedBattle(bool leftDangerous)
    {
        var hero = new Fighter(new Point(), 5000, 1, 1.9f, 0.85f);
        var enemies = new[] {
            new Fighter(new Point(0, 0.7f), 5000, 1, 0, 0.85f),
            new Fighter(new Point(-0.65f, 0.6f), 5000, 1, 0, 0.85f, threatWeight: leftDangerous ? 4 : 1),
            new Fighter(new Point(0.65f, 0.6f), 5000, 1, 0, 0.85f, threatWeight: leftDangerous ? 1 : 4) };
        return new CombatSimulation(hero, enemies, null, tactics: new TacticalSettings());
    }
    [Test]
    public void MirroredEnemyThreatChangesChosenRepositionDirection()
    {
        var left = WeightedBattle(true); var right = WeightedBattle(false);
        for (int tick = 0; tick < 180 && (left.Repositions == 0 || right.Repositions == 0); tick++)
        { left.Advance(CombatSimulation.Tick); right.Advance(CombatSimulation.Tick); }
        Assert.Greater(left.Repositions, 0); Assert.Greater(right.Repositions, 0);
        Assert.Greater(left.LastRepositionDirection.X, 0.05f);
        Assert.Less(right.LastRepositionDirection.X, -0.05f);
    }
    [TestCase(8)]
    [TestCase(12)]
    public void MixedThreatTacticalCrowdStaysBoundedAndTerminates(int count)
    {
        var original = Stress(count);
        var enemies = new Fighter[count];
        for (int i = 0; i < count; i++)
        {
            bool strong = i % 4 == 3;
            enemies[i] = new Fighter(original.Monsters[i].Position, 60, strong ? 9 : 6,
                strong ? 1.05f : 1.4f, 0.85f, strong ? 0.5f : 0.3f, strong ? 0.9f : 0.7f,
                threatWeight: strong ? 1.8f : 1);
        }
        var battle = new CombatSimulation(original.Hero, enemies, null, knockbackDistance: 0.3f, tactics: new TacticalSettings());
        int lastAttack = 0, silent = 0;
        for (int tick = 0; tick < 3600; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            Assert.That(battle.Hero.Position.X, Is.InRange(-2, 2));
            Assert.GreaterOrEqual(battle.Hero.Position.Y, battle.RetreatFloor - 0.0001f);
            if (battle.Hero.Alive && battle.LivingMonsters > 0)
            {
                silent = battle.Hero.Attacks == lastAttack ? silent + 1 : 0;
                Assert.Less(silent, 300);
            }
            lastAttack = battle.Hero.Attacks;
            foreach (var enemy in enemies)
            {
                Assert.That(enemy.Position.X, Is.InRange(-2, 2));
                Assert.That(enemy.Position.Y, Is.InRange(-3, 4));
                if (enemy.Alive && battle.Hero.Alive) Assert.GreaterOrEqual((enemy.Position - battle.Hero.Position).Length, 0.5599f);
            }
        }
        Assert.IsTrue(!battle.Hero.Alive || battle.LivingMonsters == 0);
    }
    [Test]
    public void BreakthroughAgainstMovingEnemiesAtCornerDoesNotCrossWallsOrFeet()
    {
        var hero = new Fighter(new Point(-2, -3), 5000, 1, 1.9f, 0.85f);
        var enemies = new Fighter[5];
        for (int i = 0; i < 5; i++)
        {
            double angle = i * System.Math.PI / 8;
            enemies[i] = new Fighter(new Point(-2 + (float)System.Math.Cos(angle) * 0.65f,
                -3 + (float)System.Math.Sin(angle) * 0.65f), 5000, 1, 1.4f, 0.85f);
        }
        var battle = new CombatSimulation(hero, enemies, null, tactics: new TacticalSettings());
        for (int tick = 0; tick < 600; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            Assert.That(hero.Position.X, Is.InRange(-2, 2));
            Assert.GreaterOrEqual(hero.Position.Y, -3);
            foreach (var enemy in enemies) Assert.GreaterOrEqual((hero.Position - enemy.Position).Length, 0.5599f);
        }
        Assert.Greater(battle.Breakthroughs, 0);
        Assert.Greater(battle.BreakthroughTravel, 0.03f);
        Assert.Greater(hero.Attacks, 5);
    }
    [Test]
    public void RuntimeTrapPresetCreatesSpaceAgainstMovingMixedEnemies()
    {
        var hero = new Fighter(new Point(0, -1.5f), 500, 12, 1.9f, 0.85f);
        var enemies = new Fighter[8];
        for (int i = 0; i < 8; i++)
        {
            double angle = i * System.Math.PI / 4;
            bool strong = i % 4 == 3;
            enemies[i] = new Fighter(new Point((float)System.Math.Cos(angle) * 0.65f,
                -1.5f + (float)System.Math.Sin(angle) * 0.65f), 120, strong ? 9 : 6,
                strong ? 1.05f : 1.4f, 0.85f, strong ? 0.5f : 0.3f, strong ? 0.9f : 0.7f,
                threatWeight: strong ? 1.8f : 1);
        }
        var battle = new CombatSimulation(hero, enemies, null, tactics: new TacticalSettings());
        for (int tick = 0; tick < 600; tick++)
        {
            battle.Advance(CombatSimulation.Tick);
            if (hero.Alive)
                foreach (var enemy in enemies)
                    if (enemy.Alive) Assert.GreaterOrEqual((hero.Position - enemy.Position).Length, 0.5599f);
        }
        Assert.Greater(battle.Breakthroughs, 0);
        Assert.Greater(battle.BreakthroughTravel, 0.03f);
        Assert.Greater(hero.Attacks, 5);
    }
    private static CombatSimulation RandomChoices(int seed)
    {
        var hero = new Fighter(new Point(), 5000, 1, 1.9f, 1.2f);
        var enemies = new[] {
            new Fighter(new Point(0, 0.6f), 5000, 1, 0, 0.6f),
            new Fighter(new Point(0.4f, 0.65f), 5000, 1, 0, 0.6f),
            new Fighter(new Point(0, 1.1f), 5000, 1, 0, 0.6f) };
        return new CombatSimulation(hero, enemies, null, tactics:
            new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f), seed: seed);
    }
    [Test]
    public void WeightedTargetsPreferCloserEnemyButSometimesChooseLowerScoredEnemy()
    {
        var counts = new int[3];
        for (int seed = 0; seed < 512; seed++)
        {
            var battle = RandomChoices(seed);
            battle.Advance(CombatSimulation.Tick);
            for (int i = 0; i < counts.Length; i++)
                if (battle.Hero.Target == battle.Monsters[i]) counts[i]++;
        }
        Assert.Greater(counts[0], counts[2], "Nearest should be more likely than furthest.");
        for (int i = 0; i < counts.Length; i++) Assert.Greater(counts[i], 10, "Lower ranked legal targets must remain possible.");
    }
    [Test]
    public void WeightedFocusStaysLockedDuringAttackAndDoesNotRandomizeSwingVictims()
    {
        var battle = RandomChoices(7);
        battle.Advance(CombatSimulation.Tick);
        Fighter chosen = battle.Hero.Target;
        battle.Advance(0.5);
        Assert.AreSame(chosen, battle.Hero.Target);
        Assert.AreEqual(1, battle.TargetChoices);
        Assert.AreEqual(3, battle.Hero.LastHitCount);
        for (int i = 0; i < 3; i++) Assert.AreEqual(4999, battle.Monsters[i].Health);
    }
    [Test]
    public void RandomMovementVariesDirectionOrDistanceAcrossSeeds()
    {
        var endpoints = new System.Collections.Generic.HashSet<string>();
        var distances = new System.Collections.Generic.HashSet<int>();
        for (int seed = 0; seed < 48; seed++)
        {
            var battle = RandomChoices(seed);
            for (int tick = 0; tick < 180 && battle.Repositions == 0; tick++) battle.Advance(CombatSimulation.Tick);
            for (int tick = 0; tick < 60 && battle.Hero.State == CombatState.Reposition; tick++) battle.Advance(CombatSimulation.Tick);
            Assert.Greater(battle.MovementChoices, 0);
            endpoints.Add(battle.Hero.Position.X.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ":" +
                battle.Hero.Position.Y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            distances.Add((int)System.Math.Round(battle.RepositionTravel * 100));
            Assert.LessOrEqual(battle.RepositionTravel, 0.5001f);
            Assert.GreaterOrEqual(battle.Hero.Position.Y, battle.RetreatFloor - 0.0001f);
            foreach (var enemy in battle.Monsters) Assert.GreaterOrEqual((enemy.Position - battle.Hero.Position).Length, 0.5599f);
        }
        Assert.Greater(endpoints.Count, 3);
        Assert.Greater(distances.Count, 1);
    }
    [Test]
    public void RandomDecisionsWithSameSeedMatchAcrossFrameBatches()
    {
        var a = RandomChoices(725); var b = RandomChoices(725);
        for (int tick = 0; tick < 1200; tick++) a.Advance(CombatSimulation.Tick);
        for (int frame = 0; frame < 300; frame++) b.Advance(CombatSimulation.Tick * 4.0);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Hero.Position.X, b.Hero.Position.X);
        Assert.AreEqual(a.Hero.Position.Y, b.Hero.Position.Y);
        Assert.AreEqual(a.MovementChoices, b.MovementChoices);
        Assert.AreEqual(a.TargetChoices, b.TargetChoices);
        Assert.AreEqual(a.RepositionTravel, b.RepositionTravel);
        for (int i = 0; i < 3; i++) Assert.AreEqual(a.Monsters[i].Health, b.Monsters[i].Health);
    }
    [Test]
    public void DeadFocusDoesNotRerollOrCancelRemainingActiveSwing()
    {
        var hero = new Fighter(new Point(), 5000, 20, 0, 0.85f);
        var focus = new Fighter(new Point(0, 0.6f), 1, 1, 0, 0.6f);
        var late = new Fighter(new Point(0, 2), 100, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, new[] { focus, late }, null,
            tactics: new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f));
        battle.Advance(0.28);
        Assert.IsFalse(focus.Alive);
        Assert.AreEqual(1, battle.TargetChoices);
        late.Position = new Point(0, 0.75f);
        battle.Advance(0.1);
        Assert.AreEqual(80, late.Health);
        Assert.AreEqual(1, battle.TargetChoices);
        Assert.AreEqual(1, hero.Attacks);
    }
    [TestCase(3)]
    [TestCase(71)]
    [TestCase(804)]
    public void RandomizedTwelveEnemyRunStaysSafeAndReplaysExactly(int seed)
    {
        var originalA = Stress(12); var originalB = Stress(12);
        var enemiesA = new Fighter[12]; var enemiesB = new Fighter[12];
        for (int i = 0; i < 12; i++)
        {
            bool strong = i % 4 == 3;
            enemiesA[i] = new Fighter(originalA.Monsters[i].Position, 60, strong ? 9 : 6,
                strong ? 1.05f : 1.4f, 0.85f, strong ? 0.5f : 0.3f, strong ? 0.9f : 0.7f, threatWeight: strong ? 1.8f : 1);
            enemiesB[i] = new Fighter(originalB.Monsters[i].Position, 60, strong ? 9 : 6,
                strong ? 1.05f : 1.4f, 0.85f, strong ? 0.5f : 0.3f, strong ? 0.9f : 0.7f, threatWeight: strong ? 1.8f : 1);
        }
        var settings = new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f);
        var a = new CombatSimulation(originalA.Hero, enemiesA, null, knockbackDistance: 0.3f, tactics: settings, seed: seed);
        var b = new CombatSimulation(originalB.Hero, enemiesB, null, knockbackDistance: 0.3f, tactics: settings, seed: seed);
        int attacks = 0, silent = 0;
        for (int tick = 0; tick < 3600; tick++)
        {
            a.Advance(CombatSimulation.Tick);
            Assert.That(a.Hero.Position.X, Is.InRange(-2, 2));
            Assert.GreaterOrEqual(a.Hero.Position.Y, a.RetreatFloor - 0.0001f);
            if (a.Hero.Alive && a.LivingMonsters > 0)
            {
                silent = a.Hero.Attacks == attacks ? silent + 1 : 0;
                Assert.Less(silent, 300);
                foreach (var enemy in a.Monsters)
                    if (enemy.Alive) Assert.GreaterOrEqual((a.Hero.Position - enemy.Position).Length, 0.5599f);
            }
            attacks = a.Hero.Attacks;
        }
        for (int frame = 0; frame < 900; frame++) b.Advance(CombatSimulation.Tick * 4.0);
        Assert.IsTrue(!a.Hero.Alive || a.LivingMonsters == 0);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Hero.Position.X, b.Hero.Position.X);
        Assert.AreEqual(a.Hero.Position.Y, b.Hero.Position.Y);
        Assert.AreEqual(a.MovementChoices, b.MovementChoices);
        Assert.AreEqual(a.TargetChoices, b.TargetChoices);
    }
    [Test]
    public void DamageFeedbackFactsIncludeLethalHitWithoutOverkillOrDuplicateHits()
    {
        var hero = new Fighter(new Point(), 1000, 20, 0, 1.2f);
        var lethal = new Fighter(new Point(0, 0.65f), 7, 1, 0, 0.6f);
        var defended = new Fighter(new Point(0.5f, 0.5f), 100, 1, 0, 0.6f, defense: 3);
        var battle = new CombatSimulation(hero, lethal, defended);
        battle.Advance(0.4);
        Assert.AreEqual(1, lethal.HitsTaken);
        Assert.AreEqual(7L, lethal.DamageTaken);
        Assert.AreEqual(1, defended.HitsTaken);
        Assert.AreEqual(17L, defended.DamageTaken);
        battle.Advance(0.3);
        Assert.AreEqual(1, lethal.HitsTaken);
        Assert.AreEqual(1, defended.HitsTaken);
        Assert.AreEqual(17L, defended.DamageTaken);
    }
    [Test]
    public void FeedbackDamageTotalsMatchHealthAcrossLargeTimeStepsAndFrameBatches()
    {
        var a = Crowd(); var b = Crowd();
        a.Advance(12);
        for (int i = 0; i < 720; i++) b.Advance(1.0 / 60);
        Assert.AreEqual(a.Hero.DamageTaken, b.Hero.DamageTaken);
        Assert.AreEqual((long)(a.Hero.MaxHealth - a.Hero.Health), a.Hero.DamageTaken);
        for (int i = 0; i < a.Monsters.Count; i++)
        {
            Assert.AreEqual((long)(a.Monsters[i].MaxHealth - a.Monsters[i].Health), a.Monsters[i].DamageTaken);
            Assert.AreEqual(a.Monsters[i].DamageTaken, b.Monsters[i].DamageTaken);
            Assert.AreEqual(a.Monsters[i].HitsTaken, b.Monsters[i].HitsTaken);
        }
    }

    [Test]
    public void DamageRollIncludesBothIntegerEndpointsAndCriticalBeforeDefense()
    {
        var rules = new DamageProfile(0.85f, 1.15f, 0.15f, 1.5f);
        Assert.AreEqual(10, rules.Minimum(12)); Assert.AreEqual(14, rules.Maximum(12));
        Assert.AreEqual(9, rules.Resolve(12, 1, 3, 0, 0.9, out bool lowCrit));
        Assert.AreEqual(12, rules.Resolve(12, 1, 3, 0.999999, 0.9, out _));
        Assert.AreEqual(33, rules.Resolve(12, 1.8f, 3, 0.999999, 0, out bool highCrit));
        Assert.IsFalse(lowCrit); Assert.IsTrue(highCrit);
        Assert.AreEqual(1, rules.Resolve(12, 1, 999, 0, 0, out _));
    }
    [Test]
    public void DamageProfileRejectsInvalidAuthoredValues()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new DamageProfile(1.2f, 0.8f));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new DamageProfile(criticalChance: float.NaN));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new DamageProfile(criticalMultiplier: 0.5f));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new DamageProfile(float.PositiveInfinity));
    }
    [Test]
    public void RatioDefenseHasPredictableScalingAndNeverIncreasesDamage()
    {
        var rules = new DamageProfile(defenseConstant: 20);
        Assert.AreEqual(1000, rules.Resolve(1000, 1, 0, 0, 0, out _));
        Assert.AreEqual(500, rules.Resolve(1000, 1, 20, 0, 0, out _));
        Assert.AreEqual(333, rules.Resolve(1000, 1, 40, 0, 0, out _));
        Assert.AreEqual(250, rules.Resolve(1000, 1, 60, 0, 0, out _));
        int previous = 1000;
        for (int defense = 0; defense <= 1000; defense++)
        {
            int damage = rules.Resolve(1000, 1, defense, 0, 0, out _);
            Assert.That(damage, Is.InRange(1, previous)); previous = damage;
        }
        Assert.AreEqual(1, rules.Resolve(1, 1, int.MaxValue, 0, 0, out _));
        Assert.AreEqual(20, rules.Resolve(int.MaxValue, 1, int.MaxValue, 0, 0, out _));
        Assert.AreEqual(500, new DamageProfile(defenseConstant: 40).Resolve(1000, 1, 40, 0, 0, out _));
    }
    [Test]
    public void DefenseConstantMustBeFiniteAndPositive()
    {
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new DamageProfile(defenseConstant: invalid));
    }
    [Test]
    public void CriticalChanceZeroAndOneHaveExactBoundaries()
    {
        var never = new DamageProfile(); var always = new DamageProfile(criticalChance: 1);
        Assert.AreEqual(12, never.Resolve(12, 1, 0, 0, 0, out bool no));
        Assert.AreEqual(18, always.Resolve(12, 1, 0, 0, 0.999999, out bool yes));
        Assert.IsFalse(no); Assert.IsTrue(yes);
    }
    private static CombatSimulation VariableDamageBattle(int seed, DamageProfile profile = null)
    {
        var rules = profile ?? new DamageProfile(0.85f, 1.15f, 0.2f, 1.5f);
        return new CombatSimulation(new Fighter(new Point(0, 0), 10000, 12, 0, 0.85f,
            defense: 1, damageProfile: rules), new[] {
                new Fighter(new Point(0, 0.7f), 10000, 6, 0, 0.85f, defense: 3, damageProfile: rules) },
            null, new RepositionSettings(false), seed: seed);
    }
    [Test]
    public void VariableDamageReplaysAcrossRenderRatesAndCountsCriticalHits()
    {
        var a = VariableDamageBattle(42); var b = VariableDamageBattle(42);
        for (int i = 0; i < 1800; i++) a.Advance(1.0 / 30);
        for (int i = 0; i < 7200; i++) b.Advance(1.0 / 120);
        Assert.AreEqual(a.Monster.Health, b.Monster.Health);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Monster.CriticalHitsTaken, b.Monster.CriticalHitsTaken);
        Assert.Greater(a.Monster.CriticalHitsTaken, 0);
        Assert.Less(a.Monster.CriticalHitsTaken, a.Monster.HitsTaken);
        Assert.AreEqual(a.Monster.MaxHealth - a.Monster.Health, a.Monster.DamageTaken);
    }
    [Test]
    public void CriticalLethalHitReportsActualHealthLossOnce()
    {
        var hero = new Fighter(new Point(), 100, 12, 0, 0.85f,
            damageProfile: new DamageProfile(criticalChance: 1));
        var enemy = new Fighter(new Point(0, 0.7f), 7, 1, 0, 0.85f);
        var sim = new CombatSimulation(hero, enemy); sim.Advance(5);
        Assert.AreEqual(7, enemy.DamageTaken); Assert.AreEqual(1, enemy.CriticalHitsTaken);
        Assert.AreEqual(1, enemy.HitsTaken);
    }
    [Test]
    public void ManySeedsShowDamageVariationAndExpectedCriticalFrequency()
    {
        long damage = 0; int crits = 0; var results = new System.Collections.Generic.HashSet<long>();
        for (int seed = 0; seed < 1000; seed++)
        {
            var sim = VariableDamageBattle(seed); sim.Advance(0.5);
            Assert.AreEqual(1, sim.Monster.HitsTaken);
            damage += sim.Monster.DamageTaken; crits += sim.Monster.CriticalHitsTaken;
            results.Add(sim.Monster.DamageTaken);
        }
        Assert.Greater(results.Count, 5);
        Assert.That(crits, Is.InRange(150, 250));
        Assert.That(damage / 1000.0, Is.InRange(11.0, 12.0)); // Exact expectation: .8 * mean(9,10,10,11,12) + .2 * mean(13,15,16,17,18) = 11.48
    }
    [Test]
    public void DamageRandomDrawsDoNotChangeTacticalRandomStream()
    {
        // No deaths in this interval: damage changes must not consume focus/movement random draws.
        CombatSimulation Build(DamageProfile profile) => new CombatSimulation(
            new Fighter(new Point(), 100000, 12, 1.9f, 0.85f, damageProfile: profile), new[] {
                new Fighter(new Point(0, 0.7f), 100000, 1, 0, 0.85f),
                new Fighter(new Point(-0.7f, 0), 100000, 1, 0, 0.85f),
                new Fighter(new Point(0.7f, 0), 100000, 1, 0, 0.85f) }, null,
            tactics: new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f), seed: 73);
        var a = Build(new DamageProfile()); var b = Build(new DamageProfile(0.85f, 1.15f, 0.2f));
        a.Advance(15); b.Advance(15);
        Assert.AreEqual(a.Hero.Position.X, b.Hero.Position.X);
        Assert.AreEqual(a.Hero.Position.Y, b.Hero.Position.Y);
        Assert.AreEqual(a.MovementChoices, b.MovementChoices);
        Assert.AreEqual(a.TargetChoices, b.TargetChoices);
    }

    [Test]
    public void FastEnemyIsWeakerAndClosesDistanceSoonerWithoutChangingReach()
    {
        var settings = new FastMonsterSettings();
        var fast = settings.Create(new Point(0, 3), 60, 6, 0, new DamageProfile());
        var normal = new Fighter(new Point(0, 3), 60, 6, 1.4f, 0.85f, 0.3f, 0.7f);
        Assert.AreEqual(36, fast.MaxHealth); Assert.AreEqual(4, fast.Damage);
        Assert.AreEqual(normal.Range, fast.Range); Assert.Greater(fast.Speed, normal.Speed);
        var a = new CombatSimulation(new Fighter(new Point(), 500, 1, 0, 0.85f), fast);
        var b = new CombatSimulation(new Fighter(new Point(), 500, 1, 0, 0.85f), normal);
        a.Advance(0.75); b.Advance(0.75);
        Assert.Less(fast.Position.Y, normal.Position.Y);
        Assert.GreaterOrEqual((a.Hero.Position - fast.Position).Length, CombatSimulation.FootRadius * 2 - 0.00001f);
    }
    [Test]
    public void FastEnemyLowStatsAndTrapHealthFloorRemainValid()
    {
        var settings = new FastMonsterSettings();
        var tiny = settings.Create(new Point(), 1, 1, 2, new DamageProfile());
        Assert.AreEqual(1, tiny.Health); Assert.AreEqual(1, tiny.Damage); Assert.AreEqual(2, tiny.Defense);
        var trapped = settings.Create(new Point(), 60, 6, 0, new DamageProfile(), 120);
        Assert.AreEqual(120, trapped.Health);
        trapped.Position = new Point(1, 1);
        Assert.AreEqual(36, settings.Create(new Point(), 60, 6, 0, null).Health);
    }
    [Test]
    public void FastEnemyRejectsInvalidSettings()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new FastMonsterSettings(speed: float.NaN));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new FastMonsterSettings(healthScale: 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new FastMonsterSettings(damageScale: 2));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new FastMonsterSettings(recovery: -1));
    }
    [Test]
    public void MixedRosterPreservesHeavySlotsAndCanDisableFastOrAllVariants()
    {
        int normal = 0, fast = 0, heavy = 0;
        for (int i = 0; i < 12; i++)
        {
            var kind = FastMonsterSettings.KindAt(i, true, true);
            if (kind == PrototypeMonsterKind.Normal) normal++;
            if (kind == PrototypeMonsterKind.Fast) fast++;
            if (kind == PrototypeMonsterKind.Heavy) heavy++;
            Assert.AreEqual(i % 4 == 3 ? PrototypeMonsterKind.Heavy : PrototypeMonsterKind.Normal,
                FastMonsterSettings.KindAt(i, true, false));
            Assert.AreEqual(PrototypeMonsterKind.Normal, FastMonsterSettings.KindAt(i, false, true));
        }
        Assert.AreEqual(6, normal); Assert.AreEqual(3, fast); Assert.AreEqual(3, heavy);
    }
    private static CombatSimulation ThreeTypeBattle(int count, int seed)
    {
        Point[] offsets = { new Point(0, 0), new Point(-1.25f, -0.25f), new Point(0.7f, 0.85f),
            new Point(-1.65f, 1.2f), new Point(-0.25f, 1.65f), new Point(-2.35f, 0.25f),
            new Point(0.95f, 1.75f), new Point(-0.65f, 0.55f), new Point(-2.4f, 1.9f),
            new Point(0.3f, 0.4f), new Point(-1.15f, 2.05f), new Point(-1.9f, -0.1f) };
        var enemies = new Fighter[count]; var fast = new FastMonsterSettings();
        var damage = new DamageProfile(0.85f, 1.15f, 0.05f);
        for (int i = 0; i < count; i++)
        {
            Point position = new Point(0.8f, 1.8f) + offsets[i];
            var kind = FastMonsterSettings.KindAt(i, true, true);
            bool heavy = kind == PrototypeMonsterKind.Heavy;
            enemies[i] = kind == PrototypeMonsterKind.Fast ? fast.Create(position, 60, 6, 0, damage) :
                new Fighter(position, 60, heavy ? 9 : 6, heavy ? 1.05f : 1.4f, 0.85f,
                    heavy ? 0.5f : 0.3f, heavy ? 0.9f : 0.7f, heavy ? 3 : 0, heavy ? 1.8f : 1, damage);
        }
        return new CombatSimulation(new Fighter(new Point(-0.7f, -1.8f), 500, 12, 1.9f, 0.85f,
            defense: 1, damageProfile: new DamageProfile(0.85f, 1.15f, 0.15f)), enemies, null,
            knockbackDistance: 0.3f, tactics: new TacticalSettings(movementTemperature: 0.35f, targetTemperature: 0.55f), seed: seed);
    }
    [TestCase(5, 3)]
    [TestCase(8, 12345)]
    [TestCase(12, 73)]
    public void ThreeMonsterTypesStayBoundedFinishAndReplay(int count, int seed)
    {
        var a = ThreeTypeBattle(count, seed); var b = ThreeTypeBattle(count, seed);
        for (int frame = 0; frame < 3600; frame++)
        {
            a.Advance(1.0 / 30);
            for (int i = 0; i < count; i++)
            {
                var enemy = a.Monsters[i];
                Assert.That(enemy.Position.X, Is.InRange(-2f, 2f));
                Assert.That(enemy.Position.Y, Is.InRange(-3f, 4f));
                if (a.Hero.Alive && enemy.Alive)
                    Assert.GreaterOrEqual((a.Hero.Position - enemy.Position).Length, CombatSimulation.FootRadius * 2 - 0.00001f);
            }
            Assert.GreaterOrEqual(a.Hero.Position.Y, a.RetreatFloor);
        }
        for (int frame = 0; frame < 14400; frame++) b.Advance(1.0 / 120);
        Assert.IsTrue(!a.Hero.Alive || a.LivingMonsters == 0);
        Assert.Greater(a.Hero.Attacks, 3);
        Assert.AreEqual(a.Hero.Health, b.Hero.Health);
        Assert.AreEqual(a.Hero.Position.X, b.Hero.Position.X);
        Assert.AreEqual(a.Hero.Position.Y, b.Hero.Position.Y);
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(a.Monsters[i].Health, b.Monsters[i].Health);
            Assert.AreEqual(a.Monsters[i].CriticalHitsTaken, b.Monsters[i].CriticalHitsTaken);
            Assert.AreEqual(a.Monsters[i].Position.X, b.Monsters[i].Position.X);
            Assert.AreEqual(a.Monsters[i].Position.Y, b.Monsters[i].Position.Y);
        }
    }
}
