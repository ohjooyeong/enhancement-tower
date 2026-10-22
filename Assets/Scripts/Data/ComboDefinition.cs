using System;
using EnhancementTower.Combat;
using UnityEngine;

namespace EnhancementTower.Data
{
    [CreateAssetMenu(menuName = "Enhancement Tower/Combo Definition")]
    public sealed class ComboDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Step
        {
            public AttackKind kind;
            [Min(1)] public int maxTargets = 3;
            [Min(0.01f)] public float windup = 0.22f, active = 0.12f, recovery = 0.56f;
            [Range(1, 180)] public float arc = 180;
            [Min(0.1f)] public float rangeScale = 1, damageScale = 1;
            [Min(0)] public float knockbackScale = 0.65f;
            public AttackStep Snapshot() => new AttackStep(kind, maxTargets, windup, active, recovery,
                arc, rangeScale, damageScale, knockbackScale);
        }
        public Step[] steps = {
            new Step(),
            new Step { kind = AttackKind.ReverseSlash, windup = 0.18f, active = 0.14f, recovery = 0.54f, arc = 160, knockbackScale = 0.8f },
            new Step { kind = AttackKind.Thrust, maxTargets = 1, windup = 0.3f, active = 0.1f,
                recovery = 0.72f, arc = 24, rangeScale = 1.25f, damageScale = 1.8f, knockbackScale = 1.3f }
        };
        // Copy authored data at run start; never store mutable combat state in this asset.
        public AttackStep[] Snapshot()
        {
            if (steps == null || steps.Length == 0) throw new InvalidOperationException("Combo needs at least one step.");
            var result = new AttackStep[steps.Length];
            for (int i = 0; i < result.Length; i++) result[i] = steps[i].Snapshot();
            return result;
        }
    }
}
