using System;
using EnhancementTower.Combat;
using EnhancementTower.Progression;
using NUnit.Framework;

public class EquipmentTests
{
    private static EquipmentSession Session(EnhancementPolicy policy = null, int gold = 1000, int stones = 100) => new EquipmentSession(new[] {
        new EquipmentSpec("iron", "Iron", EquipmentSlot.Weapon, 4, 2),
        new EquipmentSpec("steel", "Steel", EquipmentSlot.Weapon, 8, 1),
        new EquipmentSpec("armor", "Armor", EquipmentSlot.Armor, 1, 1, 40, 10)
    }, policy ?? new EnhancementPolicy(), gold, stones);
    [Test]
    public void EquipReplacesRatherThanStacksAndUnequipKeepsEnhancement()
    {
        var s = Session(); s.Equip(0); s.Equip(2); s.Enhance(0, 0, .9);
        Assert.AreEqual(16, s.Stats(100, 10, 1).Attack);
        Assert.AreEqual(140, s.Stats(100, 10, 1).Health);
        Assert.AreEqual(2, s.Stats(100, 10, 1).Defense);
        s.Equip(1); Assert.AreEqual(18, s.Stats(100, 10, 1).Attack);
        s.Unequip(EquipmentSlot.Weapon); Assert.AreEqual(10, s.Stats(100, 10, 1).Attack);
        s.Equip(0); Assert.AreEqual(1, s.Item(0).Level); Assert.AreEqual(16, s.Stats(100, 10, 1).Attack);
    }
    [Test]
    public void SuccessBlessingFailureRegressionAndPityRespectTheirOrder()
    {
        var s = Session(); var item = s.Item(0);
        Assert.AreEqual(EnhancementOutcome.Failure, s.Enhance(0, .99, .9));
        Assert.AreEqual(0, item.Level); Assert.AreEqual(1, item.Failures);
        Assert.AreEqual(.61, s.Policy.Chance(item), 1e-9);
        Assert.AreEqual(EnhancementOutcome.Blessed, s.Enhance(0, 0, 0));
        Assert.AreEqual(2, item.Level); Assert.AreEqual(0, item.Failures);
        Assert.AreEqual(EnhancementOutcome.Regression, s.Enhance(0, .99, 0));
        Assert.AreEqual(1, item.Level); Assert.AreEqual(1, item.Failures);
        Assert.AreEqual(EnhancementOutcome.Success, s.Enhance(0, 0, .9));
        Assert.AreEqual(2, item.Level); Assert.AreEqual(0, item.Failures);
        Assert.AreEqual(945, s.Gold); Assert.AreEqual(96, s.Stones);
    }
    [Test]
    public void RegressionAtZeroDoesNotDestroyAndPityCanReachGuaranteedSuccess()
    {
        var s = Session(new EnhancementPolicy(baseChance: 0, minimum: 0, pity: .5));
        s.Enhance(0, .9, 0); Assert.AreEqual(0, s.Item(0).Level);
        s.Enhance(0, .9, .9); Assert.AreEqual(1, s.Policy.Chance(s.Item(0)));
        Assert.AreEqual(EnhancementOutcome.Success, s.Enhance(0, .999, .9));
        Assert.AreEqual(1, s.Item(0).Level); Assert.AreEqual(0, s.Item(0).Failures);
    }
    [Test]
    public void InsufficientFundsAndInvalidRollsDoNotChargeOrMutate()
    {
        var s = Session(gold: 9); Assert.AreEqual(EnhancementOutcome.InsufficientMaterials, s.Enhance(0));
        Assert.AreEqual(9, s.Gold); Assert.AreEqual(100, s.Stones); Assert.AreEqual(0, s.Item(0).Failures);
        var enough = Session(); Assert.Throws<ArgumentOutOfRangeException>(() => enough.Enhance(0, double.NaN, 0));
        Assert.AreEqual(1000, enough.Gold); Assert.AreEqual(0, enough.Item(0).Level);
        var noStones = Session(stones: 0); Assert.IsFalse(noStones.CanEnhance(0));
    }
    [Test]
    public void ItemPityIsIndependentAndSeededEnhancementReplays()
    {
        var a = Session(); var b = Session();
        a.Enhance(0, .99, .9); Assert.AreEqual(0, a.Item(1).Failures);
        a = Session();
        for (int i = 0; i < 40; i++) Assert.AreEqual(a.Enhance(i % 3), b.Enhance(i % 3));
        Assert.AreEqual(a.Gold, b.Gold); Assert.AreEqual(a.Stones, b.Stones);
        for (int i = 0; i < 3; i++) { Assert.AreEqual(a.Item(i).Level, b.Item(i).Level); Assert.AreEqual(a.Item(i).Failures, b.Item(i).Failures); }
    }
    [Test]
    public void MaterialRewardIsPaidOncePerCompletedRun()
    {
        var s = Session(gold: 0, stones: 0);
        Assert.IsTrue(s.RewardMaterialRun(1)); Assert.IsFalse(s.RewardMaterialRun(1)); Assert.IsFalse(s.RewardMaterialRun(0));
        Assert.AreEqual(40, s.Gold); Assert.AreEqual(3, s.Stones);
        Assert.IsTrue(s.RewardMaterialRun(2)); Assert.AreEqual(80, s.Gold);
    }
    private static CombatSimulation Battle(EquipmentCombatStats stats) => new CombatSimulation(
        new Fighter(new Point(0, 0), stats.Health, stats.Attack, 0, .85f, defense: stats.Defense, damageProfile: new DamageProfile(1, 1, 0)),
        new Fighter(new Point(0, .7f), 1000, 20, 0, .85f, damageProfile: new DamageProfile(1, 1, 0)));
    [Test]
    public void EnhancedWeaponAndArmorChangeRealCombatWithoutMutatingAnExistingRun()
    {
        var s = Session(); s.Equip(0); s.Equip(2);
        var before = Battle(s.Stats(500, 10, 1));
        for (int i = 0; i < 4; i++) { s.Enhance(0, 0, .9); s.Enhance(2, 0, .9); }
        var after = Battle(s.Stats(500, 10, 1));
        before.Advance(4); after.Advance(4);
        Assert.Greater(after.Monster.DamageTaken, before.Monster.DamageTaken);
        Assert.Less(after.Hero.DamageTaken, before.Hero.DamageTaken);
        Assert.Greater(after.Hero.Health, before.Hero.Health);
        Assert.AreEqual(14, before.Hero.Damage); Assert.AreEqual(540, before.Hero.MaxHealth);
    }
    [Test]
    public void DefinitionsRejectAmbiguousSlotsAndDuplicateIds()
    {
        Assert.Throws<ArgumentException>(() => new EquipmentSpec("bad", "Bad", (EquipmentSlot)99, 1, 1));
        Assert.Throws<ArgumentException>(() => new EquipmentSpec("bad", "Bad", EquipmentSlot.Weapon, 1, 1, 1));
        var spec = new EquipmentSpec("same", "Same", EquipmentSlot.Weapon, 1, 1);
        Assert.Throws<ArgumentException>(() => new EquipmentSession(new[] { spec, spec }, new EnhancementPolicy()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnhancementPolicy(baseChance: double.NaN));
    }
}
