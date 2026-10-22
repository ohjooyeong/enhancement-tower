using System;

namespace EnhancementTower.Combat
{
    public enum PrototypeMonsterKind { Normal, Fast, Heavy }

    public sealed class FastMonsterSettings
    {
        public readonly float HealthScale, DamageScale, Speed, Windup, Recovery, ThreatWeight;
        public FastMonsterSettings(float healthScale = 0.6f, float damageScale = 0.65f,
            float speed = 2.1f, float windup = 0.16f, float recovery = 0.32f, float threatWeight = 1.35f)
        {
            double sum = (double)healthScale + damageScale + speed + windup + recovery + threatWeight;
            if (double.IsNaN(sum) || double.IsInfinity(sum) || healthScale <= 0 || healthScale > 1 ||
                damageScale <= 0 || damageScale > 1 || speed <= 0 || windup <= 0 || recovery <= 0 || threatWeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(healthScale));
            HealthScale = healthScale; DamageScale = damageScale; Speed = speed;
            Windup = windup; Recovery = recovery; ThreatWeight = threatWeight;
        }
        public Fighter Create(Point position, int baseHealth, int baseDamage, int defense,
            DamageProfile damageProfile, int minimumHealth = 1)
        {
            if (baseHealth < 1 || baseDamage < 1 || minimumHealth < 1)
                throw new ArgumentOutOfRangeException(nameof(baseHealth));
            int health = Math.Max(minimumHealth, (int)Math.Round(baseHealth * (double)HealthScale, MidpointRounding.AwayFromZero));
            int damage = Math.Max(1, (int)Math.Round(baseDamage * (double)DamageScale, MidpointRounding.AwayFromZero));
            return new Fighter(position, health, damage, Speed, 0.85f, Windup, Recovery,
                defense, ThreatWeight, damageProfile);
        }
        // Composition is authored, independent from movement and damage random streams.
        // Keep the existing fourth-slot heavy; fast enemies replace only the second slot.
        public static PrototypeMonsterKind KindAt(int index, bool mixedThreats, bool includeFast)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (!mixedThreats) return PrototypeMonsterKind.Normal;
            if (index % 4 == 3) return PrototypeMonsterKind.Heavy;
            return includeFast && index % 4 == 1 ? PrototypeMonsterKind.Fast : PrototypeMonsterKind.Normal;
        }
    }
}
