using System;

namespace EnhancementTower.Progression
{
    public enum EquipmentSlot { Weapon, Armor }
    public enum EnhancementOutcome { Success, Blessed, Failure, Regression, InsufficientMaterials }
    public sealed class EquipmentSpec
    {
        public readonly string Id, Name;
        public readonly EquipmentSlot Slot;
        public readonly int Power, PowerPerLevel, Health, HealthPerLevel;
        public EquipmentSpec(string id, string name, EquipmentSlot slot, int power, int perLevel, int health = 0, int healthPerLevel = 0)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) ||
                (slot != EquipmentSlot.Weapon && slot != EquipmentSlot.Armor) ||
                power < 0 || perLevel < 0 || health < 0 || healthPerLevel < 0 ||
                power > 1000000 || perLevel > 1000000 || health > 1000000 || healthPerLevel > 1000000 ||
                (slot == EquipmentSlot.Weapon && (health != 0 || healthPerLevel != 0)))
                throw new ArgumentException("Invalid prototype equipment definition.");
            Id = id; Name = name; Slot = slot; Power = power; PowerPerLevel = perLevel; Health = health; HealthPerLevel = healthPerLevel;
        }
    }
    public sealed class EquipmentInstance
    {
        public readonly EquipmentSpec Spec;
        public int Level { get; private set; }
        public int Failures { get; private set; }
        public EquipmentInstance(EquipmentSpec spec) { Spec = spec ?? throw new ArgumentNullException(nameof(spec)); }
        public long Power => Spec.Power + (long)Spec.PowerPerLevel * Level;
        public long Health => Spec.Health + (long)Spec.HealthPerLevel * Level;
        internal void Apply(EnhancementOutcome outcome)
        {
            if (outcome == EnhancementOutcome.Success || outcome == EnhancementOutcome.Blessed)
            { Level = checked(Level + (outcome == EnhancementOutcome.Blessed ? 2 : 1)); Failures = 0; }
            else { if (outcome == EnhancementOutcome.Regression) Level = Math.Max(0, Level - 1); Failures = (int)Math.Min(int.MaxValue, (long)Failures + 1); }
        }
    }
    public sealed class EnhancementPolicy
    {
        public readonly double BaseChance, ChanceDecay, MinimumChance, PityPerFailure, BlessingChance, RegressionChance;
        public readonly int BaseGoldCost, GoldPerLevel, StoneCost;
        public EnhancementPolicy(double baseChance = .6, double decay = .03, double minimum = .1, double pity = .01,
            double blessing = .025, double regression = .01, int gold = 10, int goldPerLevel = 5, int stones = 1)
        {
            double[] rates = { baseChance, decay, minimum, pity, blessing, regression };
            foreach (double rate in rates) if (double.IsNaN(rate) || rate < 0 || rate > 1) throw new ArgumentOutOfRangeException(nameof(baseChance));
            if (minimum > baseChance || gold < 0 || goldPerLevel < 0 || stones < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            BaseChance = baseChance; ChanceDecay = decay; MinimumChance = minimum; PityPerFailure = pity;
            BlessingChance = blessing; RegressionChance = regression; BaseGoldCost = gold; GoldPerLevel = goldPerLevel; StoneCost = stones;
        }
        public double Chance(EquipmentInstance item) => Math.Min(1, Math.Max(MinimumChance, BaseChance - item.Level * ChanceDecay) + item.Failures * PityPerFailure);
        public long GoldCost(EquipmentInstance item) => BaseGoldCost + (long)GoldPerLevel * item.Level;
        public EnhancementOutcome Resolve(EquipmentInstance item, double successRoll, double extraRoll)
        {
            if (!(successRoll >= 0 && successRoll < 1) || !(extraRoll >= 0 && extraRoll < 1)) throw new ArgumentOutOfRangeException(nameof(successRoll));
            return successRoll < Chance(item) ? extraRoll < BlessingChance ? EnhancementOutcome.Blessed : EnhancementOutcome.Success :
                extraRoll < RegressionChance ? EnhancementOutcome.Regression : EnhancementOutcome.Failure;
        }
    }
    public readonly struct EquipmentCombatStats
    {
        public readonly int Health, Attack, Defense;
        public EquipmentCombatStats(int health, int attack, int defense) { Health = health; Attack = attack; Defense = defense; }
    }
    // Session-only local prototype. No economy authority or save/backend contract is implied.
    public sealed class EquipmentSession
    {
        private readonly EquipmentInstance[] items;
        public int Count => items.Length;
        public EquipmentInstance Item(int index) => items[index];
        public int WeaponIndex { get; private set; } = -1;
        public int ArmorIndex { get; private set; } = -1;
        public int Gold { get; private set; }
        public int Stones { get; private set; }
        public readonly EnhancementPolicy Policy;
        private uint random;
        private int lastRewardRun = -1;
        public EquipmentSession(EquipmentSpec[] definitions, EnhancementPolicy policy, int gold = 200, int stones = 20, int seed = 8241)
        {
            if (definitions == null || definitions.Length == 0 || gold < 0 || stones < 0) throw new ArgumentException("Invalid equipment session.");
            Policy = policy ?? throw new ArgumentNullException(nameof(policy)); items = new EquipmentInstance[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                items[i] = new EquipmentInstance(definitions[i]);
                for (int j = 0; j < i; j++) if (items[j].Spec.Id == items[i].Spec.Id) throw new ArgumentException("Duplicate item ID.");
            }
            Gold = gold; Stones = stones; random = seed == 0 ? 1u : unchecked((uint)seed);
        }
        public void Equip(int index)
        {
            var item = items[index];
            if (item.Spec.Slot == EquipmentSlot.Weapon) WeaponIndex = index; else ArmorIndex = index;
        }
        public void Unequip(EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.Weapon) WeaponIndex = -1;
            else if (slot == EquipmentSlot.Armor) ArmorIndex = -1;
            else throw new ArgumentOutOfRangeException(nameof(slot));
        }
        public bool IsEquipped(int index) => WeaponIndex == index || ArmorIndex == index;
        public EquipmentCombatStats Stats(int baseHealth, int baseAttack, int baseDefense)
        {
            if (baseHealth < 1 || baseAttack < 1 || baseDefense < 0) throw new ArgumentOutOfRangeException(nameof(baseHealth));
            long attack = baseAttack, health = baseHealth, defense = baseDefense;
            if (WeaponIndex >= 0) attack += items[WeaponIndex].Power;
            if (ArmorIndex >= 0) { health += items[ArmorIndex].Health; defense += items[ArmorIndex].Power; }
            return new EquipmentCombatStats((int)Math.Min(int.MaxValue, health), (int)Math.Min(int.MaxValue, attack), (int)Math.Min(int.MaxValue, defense));
        }
        public bool CanEnhance(int index) => Gold >= Policy.GoldCost(items[index]) && Stones >= Policy.StoneCost && items[index].Level <= int.MaxValue - 2;
        public EnhancementOutcome Enhance(int index)
        {
            if (!CanEnhance(index)) return EnhancementOutcome.InsufficientMaterials;
            return Enhance(index, Roll(), Roll());
        }
        public EnhancementOutcome Enhance(int index, double successRoll, double extraRoll)
        {
            if (!CanEnhance(index)) return EnhancementOutcome.InsufficientMaterials;
            var item = items[index]; var outcome = Policy.Resolve(item, successRoll, extraRoll);
            Gold -= (int)Policy.GoldCost(item); Stones -= Policy.StoneCost; item.Apply(outcome); return outcome;
        }
        public bool RewardMaterialRun(int run, int gold = 40, int stones = 3)
        {
            if (run < 0 || gold < 0 || stones < 0) throw new ArgumentOutOfRangeException(nameof(run));
            if (run <= lastRewardRun) return false;
            Gold = (int)Math.Min(int.MaxValue, (long)Gold + gold); Stones = (int)Math.Min(int.MaxValue, (long)Stones + stones);
            lastRewardRun = run; return true;
        }
        private double Roll()
        { random ^= random << 13; random ^= random >> 17; random ^= random << 5; return random / 4294967296.0; }
    }
}
