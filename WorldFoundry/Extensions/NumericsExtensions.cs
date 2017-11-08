using System;
using System.Numerics;
using WorldFoundry.Util;

namespace WorldFoundry.Extensions
{
    internal static class NumericsExtensions
    {
        /// <summary>
        /// Determines if this <see cref="Vector3"/> is parallel to the given <see cref="Vector3"/>.
        /// </summary>
        /// <returns>true if the vectors are parallel; otherwise false.</returns>
        public static bool IsParallelTo(this Vector3 v, Vector3 u)
            => Vector3.Cross(v, u) == Vector3.Zero;

        /// <summary>
        /// Computes the angle between this <see cref="Vector3"/> and the given <see cref="Vector3"/>.
        /// </summary>
        /// <returns>The angle between the vectors, in radians.</returns>
        public static double GetAngle(this Vector3 v, Vector3 u)
            => Math.Acos(Vector3.Dot(v, u) / (v.Length() * u.Length()));

        /// <summary>
        /// Calculates the <see cref="Quaternion"/> which, when multiplied by this one, rotates the
        /// given vector to the Y axis.
        /// </summary>
        public static Quaternion GetReferenceRotation(this Quaternion d, Vector3 v)
        {
            var u = Vector3.Transform(v, d);

            Quaternion h = Quaternion.Identity;
            if (u.X != 0 || u.Y != 0)
            {
                if (u.Y != 0)
                {
                    h = Vector3.Normalize(new Vector3(u.X, u.Y, 0)).GetRotationTo(Vector3.UnitY);
                }
                else if (u.X > 0)
                {
                    h = new Quaternion(Vector3.UnitZ, -(float)MathUtil.HalfPI);
                }
                else
                {
                    h = new Quaternion(Vector3.UnitZ, (float)MathUtil.HalfPI);
                }
            }
            var x = Vector3.Transform(u, h);

            Quaternion q = Quaternion.Identity;
            if (u.X == 0 && u.Y == 0)
            {
                if (u.Z < 0)
                {
                    q = new Quaternion(Vector3.UnitX, -(float)MathUtil.HalfPI);
                }
                else
                {
                    q = new Quaternion(Vector3.UnitX, (float)MathUtil.HalfPI);
                }
            }
            else
            {
                q = Vector3.Transform(u, h).GetRotationTo(Vector3.UnitY);
            }
            x = Vector3.Transform(u, q * h);

            return q * h * d;
        }

        /// <summary>
        /// Calculates the <see cref="Quaternion"/> which represents the rotation from this <see
        /// cref="Vector3"/> to the given <see cref="Vector3"/>.
        /// </summary>
        public static Quaternion GetRotationTo(this Vector3 v, Vector3 u)
        {
            if (v.IsParallelTo(u))
            {
                if (v != u)
                {
                    var inter = v.IsParallelTo(Vector3.UnitX) ? Vector3.UnitY : Vector3.UnitX;
                    return inter.GetRotationTo(u) * v.GetRotationTo(inter);
                }
                else
                {
                    return Quaternion.Identity;
                }
            }
            else
            {
                return Quaternion.Normalize(new Quaternion(Vector3.Cross(v, u), (float)Math.Sqrt(v.LengthSquared() * u.LengthSquared()) + Vector3.Dot(v, u)));
            }
        }

        /// <summary>
        /// Calculates the result of rotating this <see cref="Vector2"/> by the given angle.
        /// </summary>
        public static Vector2 Rotate(this Vector2 v, double angle)
            => new Vector2((float)(v.X * Math.Cos(angle) + v.Y * -Math.Sin(angle)),
                (float)(v.X * Math.Sin(angle) + v.Y * Math.Cos(angle)));
    }
}
