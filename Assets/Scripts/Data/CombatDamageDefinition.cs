using EnhancementTower.Combat;
using UnityEngine;

namespace EnhancementTower.Data
{
    [CreateAssetMenu(menuName = "Enhancement Tower/Combat Damage Definition")]
    public sealed class CombatDamageDefinition : ScriptableObject
    {
        [Range(0.1f, 3)] public float minimumScale = 0.85f, maximumScale = 1.15f;
        [Range(0, 1)] public float heroCriticalChance = 0.15f, monsterCriticalChance = 0.05f;
        [Range(1, 5)] public float criticalMultiplier = 1.5f;
        [Tooltip("Shared defense scale K: damage multiplier = K / (K + target DEF). Prototype value, not final balance.")]
        [Min(0.01f)] public float defenseConstant = 20;
        [Min(0)] public int heroDefense = 1, monsterDefense = 0, strongMonsterDefense = 3;
        public DamageProfile Snapshot(bool hero) => new DamageProfile(minimumScale, maximumScale,
            hero ? heroCriticalChance : monsterCriticalChance, criticalMultiplier, defenseConstant);
    }
}
