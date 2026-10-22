using EnhancementTower.Combat;
using UnityEngine;

namespace EnhancementTower.Data
{
    [CreateAssetMenu(menuName = "Enhancement Tower/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [Range(1, 500)] public int totalEnemies = 50;
        [Range(1, 12)] public int initialEnemies = 5, maxAlive = 8;
        [Range(0.1f, 60)] public float spawnInterval = 1.2f;
        [Range(1, 600)] public float timeLimit = 60;
        public StageSettings Snapshot() => new StageSettings(totalEnemies, initialEnemies, maxAlive, spawnInterval, timeLimit);
    }
}
