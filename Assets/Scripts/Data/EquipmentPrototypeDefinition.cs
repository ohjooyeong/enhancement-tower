using System;
using EnhancementTower.Progression;
using UnityEngine;

namespace EnhancementTower.Data
{
    [CreateAssetMenu(menuName = "Enhancement Tower/Equipment Prototype Definition")]
    public sealed class EquipmentPrototypeDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id, displayName;
            public EquipmentSlot slot;
            [Min(0)] public int power, powerPerLevel, health, healthPerLevel;
            public EquipmentSpec Snapshot() => new EquipmentSpec(id, displayName, slot, power, powerPerLevel, health, healthPerLevel);
        }
        public Entry[] items = {
            new Entry { id = "iron-sword", displayName = "Iron longsword", slot = EquipmentSlot.Weapon, power = 4, powerPerLevel = 2 },
            new Entry { id = "steel-sword", displayName = "Steel longsword", slot = EquipmentSlot.Weapon, power = 8, powerPerLevel = 1 },
            new Entry { id = "iron-armor", displayName = "Iron armor", slot = EquipmentSlot.Armor, power = 1, powerPerLevel = 1, health = 40, healthPerLevel = 10 },
            new Entry { id = "leather-armor", displayName = "Leather armor", slot = EquipmentSlot.Armor, health = 80, healthPerLevel = 20 }
        };
        [Range(0, 1)] public float baseSuccess = .6f, decayPerLevel = .03f, minimumSuccess = .1f, pityPerFailure = .01f;
        [Range(0, 1)] public float blessingChance = .025f, regressionChance = .01f;
        [Min(0)] public int baseGoldCost = 10, goldPerLevel = 5, stoneCost = 1, startingGold = 200, startingStones = 20;
        public int equipmentSeed = 8241;
        public EquipmentSession CreateSession()
        {
            if (items == null || items.Length == 0) throw new InvalidOperationException("Equipment catalog is empty.");
            var snapshot = new EquipmentSpec[items.Length];
            for (int i = 0; i < items.Length; i++) snapshot[i] = items[i].Snapshot();
            return new EquipmentSession(snapshot, new EnhancementPolicy(baseSuccess, decayPerLevel, minimumSuccess, pityPerFailure,
                blessingChance, regressionChance, baseGoldCost, goldPerLevel, stoneCost), startingGold, startingStones, equipmentSeed);
        }
    }
}
