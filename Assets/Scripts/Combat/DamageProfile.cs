using System;

namespace EnhancementTower.Combat
{
    // Immutable run snapshot. Min/max scale the existing authored base attack stat.
    public sealed class DamageProfile
    {
        public readonly float MinimumScale, MaximumScale, CriticalChance, CriticalMultiplier;
        public DamageProfile(float minimumScale = 1, float maximumScale = 1,
            float criticalChance = 0, float criticalMultiplier = 1.5f)
        {
            double sum = (double)minimumScale + maximumScale + criticalChance + criticalMultiplier;
            if (double.IsNaN(sum) || double.IsInfinity(sum) || minimumScale <= 0 ||
                maximumScale < minimumScale || maximumScale > 3 || criticalChance < 0 ||
                criticalChance > 1 || criticalMultiplier < 1 || criticalMultiplier > 5)
                throw new ArgumentOutOfRangeException(nameof(minimumScale));
            MinimumScale = minimumScale; MaximumScale = maximumScale;
            CriticalChance = criticalChance; CriticalMultiplier = criticalMultiplier;
        }
        public int Minimum(int baseDamage) => Rounded(baseDamage * (double)MinimumScale);
        public int Maximum(int baseDamage) => Rounded(baseDamage * (double)MaximumScale);
        // Independent uniform rolls per eligible victim; geometry chooses the victims first.
        public int Resolve(int baseDamage, float actionScale, int defense, double damageRoll,
            double criticalRoll, out bool critical)
        {
            if (baseDamage < 1 || defense < 0 || actionScale <= 0 || float.IsNaN(actionScale) ||
                float.IsInfinity(actionScale) || !(damageRoll >= 0 && damageRoll < 1) ||
                !(criticalRoll >= 0 && criticalRoll < 1)) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            int min = Minimum(baseDamage), max = Maximum(baseDamage);
            double rolled = min + Math.Floor(damageRoll * ((double)max - min + 1));
            critical = criticalRoll < CriticalChance;
            int raw = Rounded(rolled * actionScale * (critical ? CriticalMultiplier : 1));
            return Math.Max(1, raw - defense);
        }
        private static int Rounded(double value) => (int)Math.Max(1,
            Math.Min(int.MaxValue, Math.Round(value, MidpointRounding.AwayFromZero)));
    }
}
