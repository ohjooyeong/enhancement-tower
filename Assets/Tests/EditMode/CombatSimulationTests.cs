using EnhancementTower.Combat;
using NUnit.Framework;

public class CombatSimulationTests
{
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
    public void CleaveHitsFrontWithIndividualDefenseButNotBehindOrOutOfRange()
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
        Assert.AreEqual(96, side.Health);
        Assert.AreEqual(100, behind.Health);
        Assert.AreEqual(100, far.Health);
        Assert.AreEqual(2, hero.LastHitCount);
    }
    [Test]
    public void CleaveRespectsSecondaryLimitAndDoesNotRepeatDuringRecovery()
    {
        var hero = new Fighter(new Point(), 1000, 20, 0, 1.2f);
        var a = new Fighter(new Point(0, 0.6f), 100, 1, 0, 0.6f);
        var b = new Fighter(new Point(-0.5f, 0.5f), 100, 1, 0, 0.6f);
        var c = new Fighter(new Point(0.5f, 0.5f), 100, 1, 0, 0.6f);
        var battle = new CombatSimulation(hero, new[] { a, b, c }, 0.35f, 1);
        battle.Advance(0.7);
        Assert.AreEqual(80, a.Health);
        Assert.AreEqual(93, b.Health);
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
        return new CombatSimulation(hero, enemies, 0.35f, 3, new RepositionSettings(enabled, retreatDistance: retreat));
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
            0.35f, 3, knockbackDistance: 0.3f);
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
            new[] { enemy }, 0.35f, 3, knockbackDistance: 0.3f);
        battle.Advance(0.6);
        Assert.That(enemy.Position.X, Is.EqualTo(2).Within(0.0001f));
        Assert.AreEqual(0, enemy.KnockbackRemaining);
        var dead = new Fighter(new Point(0, 0.7f), 1, 1, 0, 0.85f);
        var lethal = new CombatSimulation(new Fighter(new Point(), 1000, 12, 0, 0.85f), new[] { dead },
            0.35f, 3, knockbackDistance: 0.3f);
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
        var a = new CombatSimulation(originalA.Hero, enemiesA, 0.35f, 3, knockbackDistance: 0.3f);
        var b = new CombatSimulation(originalB.Hero, enemiesB, 0.35f, 3, knockbackDistance: 0.3f);
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
        var battle = new CombatSimulation(hero, new[] { enemy }, 0.35f, 3, knockbackDistance: 0.3f);
        battle.Advance(0.65);
        Assert.Greater(hero.Attacks, 3);
        Assert.That(enemy.Position.Y, Is.EqualTo(1).Within(0.0001f));
        Assert.AreEqual(0, enemy.KnockbackRemaining);
        Assert.Greater(enemy.Attacks, 0);
    }
}
