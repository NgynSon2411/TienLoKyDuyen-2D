using System;
using System.Collections.Generic;

namespace TienLo
{
    public sealed class TrainingState
    {
        public const float WorldWidth = 3240f;
        public const float ViewWidth = 1120f;
        public const float Ground = 489f;
        public const float SkillDuration = 0.71f;
        public const float SkillCost = 18f;
        public const int HitDamage = 27;

        public sealed class Dummy
        {
            public readonly float X;
            public int Health = 100;
            public float Recovery;

            public Dummy(float x) => X = x;
        }

        public sealed class Bolt
        {
            public float X;
            public int Direction;
            public int Character;
            public float Life = 0.9f;
            public int HitMask;
        }

        public readonly Dummy[] Dummies = { new Dummy(745), new Dummy(1530), new Dummy(2470) };
        public readonly List<Bolt> Bolts = new List<Bolt>();

        public event Action<float, int> BoltEmitted;
        public event Action<float, int> DummyHit;

        public int Character { get; private set; }
        public float X { get; private set; }
        public float Camera { get; private set; }
        public float Mana { get; private set; }
        public float Cooldown { get; private set; }
        public float AttackTime { get; private set; }
        public float RunTime { get; private set; }
        public int Facing { get; private set; }
        public int Movement { get; private set; }
        public int Hits { get; private set; }
        public int Damage => Hits * HitDamage;
        public bool IsAttacking => AttackTime >= 0;
        public bool CanAttack => !IsAttacking && Cooldown <= 0 && Mana >= SkillCost;

        public TrainingState() => Reset();

        public void Choose(int character)
        {
            Character = character;
            AttackTime = -1;
            Cooldown = 0;
            Mana = 100;
        }

        public void Reset()
        {
            Choose(Character);
            X = 370;
            Camera = 0;
            Facing = 1;
            Movement = 0;
            RunTime = 0;
            Hits = 0;
            Bolts.Clear();
            foreach (Dummy dummy in Dummies)
            {
                dummy.Health = 100;
                dummy.Recovery = 0;
            }
        }

        public bool Attack()
        {
            if (!CanAttack)
                return false;

            Mana -= SkillCost;
            Cooldown = 1;
            AttackTime = 0;
            Movement = 0;
            return true;
        }

        public void Step(float seconds, int direction)
        {
            Mana = Math.Min(100, Mana + seconds * 6.2f);
            Cooldown = Math.Max(0, Cooldown - seconds);
            Movement = IsAttacking ? 0 : direction;
            if (Movement != 0)
            {
                Facing = Movement;
                X = Clamp(X + Movement * 235 * seconds, 90, WorldWidth - 90);
                RunTime += seconds;
            }

            if (IsAttacking)
            {
                float previous = AttackTime;
                AttackTime += seconds;
                if (previous < 0.29f && AttackTime >= 0.29f)
                {
                    Bolts.Add(new Bolt { X = X + 53 * Facing, Direction = Facing, Character = Character });
                    BoltEmitted?.Invoke(X + 62 * Facing, Character);
                }
                if (AttackTime >= SkillDuration)
                    AttackTime = -1;
            }

            for (int index = Bolts.Count - 1; index >= 0; index--)
            {
                Bolt bolt = Bolts[index];
                float start = bolt.X;
                bolt.X += bolt.Direction * 670 * Math.Min(seconds, bolt.Life);
                bolt.Life -= seconds;
                for (int target = 0; target < Dummies.Length; target++)
                {
                    Dummy dummy = Dummies[target];
                    int mask = 1 << target;
                    if (dummy.Recovery > 0 || (bolt.HitMask & mask) != 0 ||
                        dummy.X < Math.Min(start, bolt.X) - 49 ||
                        dummy.X > Math.Max(start, bolt.X) + 49)
                        continue;

                    bolt.HitMask |= mask;
                    Hits++;
                    dummy.Health = Math.Max(0, dummy.Health - HitDamage);
                    if (dummy.Health == 0)
                        dummy.Recovery = 2.2f;
                    DummyHit?.Invoke(dummy.X, bolt.Character);
                }
                if (bolt.Life <= 0)
                    Bolts.RemoveAt(index);
            }

            foreach (Dummy dummy in Dummies)
            {
                if (dummy.Recovery <= 0)
                    continue;

                dummy.Recovery = Math.Max(0, dummy.Recovery - seconds);
                if (dummy.Recovery == 0)
                    dummy.Health = 100;
            }

            float targetCamera = Math.Max(0, X - 300);
            Camera = Clamp(Camera + (targetCamera - Camera) * Math.Min(1, seconds * 3.7f),
                0, WorldWidth - ViewWidth);
        }

        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
