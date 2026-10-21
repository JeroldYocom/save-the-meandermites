using System;

namespace Meandermites.Sim
{
    /// <summary>
    /// Deterministic 2D vector. Uses only + - * / and sqrt, which are exactly
    /// rounded by IEEE 754. No trig: a 90 degree turn is a component swap, so
    /// it is exact and identical on every platform.
    /// </summary>
    public readonly struct SimVec2
    {
        public readonly float X;
        public readonly float Y;

        public SimVec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static SimVec2 Zero => new SimVec2(0f, 0f);

        public float SqrLength => X * X + Y * Y;
        public float Length => (float)Math.Sqrt(SqrLength);

        /// <summary>Unit vector, or zero if the length is (nearly) zero.</summary>
        public SimVec2 Normalized
        {
            get
            {
                float len = Length;
                if (len <= 1e-6f) return Zero;
                return new SimVec2(X / len, Y / len);
            }
        }

        /// <summary>Rotate 90 degrees counter-clockwise (y up).</summary>
        public SimVec2 Rot90Left => new SimVec2(-Y, X);

        /// <summary>Rotate 90 degrees clockwise (y up).</summary>
        public SimVec2 Rot90Right => new SimVec2(Y, -X);

        public SimVec2 ClampLength(float max)
        {
            float sqr = SqrLength;
            if (sqr <= max * max) return this;
            float len = (float)Math.Sqrt(sqr);
            return new SimVec2(X / len * max, Y / len * max);
        }

        public static SimVec2 operator +(SimVec2 a, SimVec2 b) => new SimVec2(a.X + b.X, a.Y + b.Y);
        public static SimVec2 operator -(SimVec2 a, SimVec2 b) => new SimVec2(a.X - b.X, a.Y - b.Y);
        public static SimVec2 operator -(SimVec2 a) => new SimVec2(-a.X, -a.Y);
        public static SimVec2 operator *(SimVec2 a, float s) => new SimVec2(a.X * s, a.Y * s);
        public static SimVec2 operator *(float s, SimVec2 a) => new SimVec2(a.X * s, a.Y * s);

        public override string ToString() => "(" + X + ", " + Y + ")";
    }
}
