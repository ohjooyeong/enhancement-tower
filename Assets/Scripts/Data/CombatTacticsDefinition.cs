using EnhancementTower.Combat;
using UnityEngine;

namespace EnhancementTower.Data
{
    [CreateAssetMenu(menuName = "Enhancement Tower/Combat Tactics Definition")]
    public sealed class CombatTacticsDefinition : ScriptableObject
    {
        public bool enabledTactics = true;
        public bool mixedThreats = true;
        [Range(0, 3)] public float movementTemperature = 0.35f, targetTemperature = 0.55f;
        [Min(0.1f)] public float strongEnemyThreat = 1.8f;
        [Min(0.5f)] public float blockedSeconds = 1.2f;
        [Min(3)] public float breakthroughCooldown = 8;
        [Range(0.1f, 0.6f)] public float advanceDistance = 0.4f, pushDistance = 0.5f;
        [Min(0)] public float minimumImprovement = 0.12f, threatWeight = 1, wallWeight = 0.8f, engagementWeight = 0.8f;
        public ComboDefinition.Step breakthrough = new ComboDefinition.Step {
            kind = AttackKind.Breakthrough, maxTargets = 3, windup = 0.32f, active = 0.16f,
            recovery = 0.45f, arc = 100, rangeScale = 1.1f, damageScale = 1.1f, knockbackScale = 1 };
        public TacticalSettings Snapshot() => new TacticalSettings(enabledTactics, blockedSeconds, breakthroughCooldown,
            advanceDistance, pushDistance, minimumImprovement, threatWeight, wallWeight, engagementWeight, breakthrough.Snapshot(), movementTemperature, targetTemperature);
    }
}
