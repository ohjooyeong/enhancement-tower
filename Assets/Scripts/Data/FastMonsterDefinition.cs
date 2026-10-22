using EnhancementTower.Combat;
using UnityEngine;

namespace EnhancementTower.Data
{
    [CreateAssetMenu(menuName = "Enhancement Tower/Fast Monster Definition")]
    public sealed class FastMonsterDefinition : ScriptableObject
    {
        [Range(0.01f, 1)] public float healthScale = 0.6f, damageScale = 0.65f;
        [Min(0.01f)] public float speed = 2.1f, windup = 0.16f, recovery = 0.32f, threatWeight = 1.35f;
        public FastMonsterSettings Snapshot() => new FastMonsterSettings(healthScale, damageScale,
            speed, windup, recovery, threatWeight);
    }
}
