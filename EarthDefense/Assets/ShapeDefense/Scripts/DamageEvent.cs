using UnityEngine;

namespace ShapeDefense.Scripts
{
    public readonly struct DamageEvent
    {
        public readonly float Amount;
        public readonly Vector2 Point;
        public readonly Vector2 Direction;
        public readonly GameObject Source;

        public DamageEvent(float amount, Vector2 point, Vector2 direction, GameObject source)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            Source = source;
        }
    }
}
