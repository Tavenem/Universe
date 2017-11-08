using System;

namespace WorldFoundry.Utilities.MathUtil
{
    internal class Constants
    {
        /// <summary>
        /// A floating-point value close to zero, intended to determine near-equivalence to 0.
        /// </summary>
        public const float NearlyZero = 1e-30f;

        /// <summary>
        /// Double Pi
        /// </summary>
        public const double TwoPI = Math.PI * 2;

        /// <summary>
        /// Triple Pi
        /// </summary>
        public const double ThreePI = Math.PI * 3;

        /// <summary>
        /// Half Pi
        /// </summary>
        public const double HalfPI = Math.PI / 2;

        /// <summary>
        /// Four thirds Pi
        /// </summary>
        public const double FourThirdsPI = Math.PI * 4 / 3;

        /// <summary>
        /// Pi squared
        /// </summary>
        public const double PISquared = Math.PI * Math.PI;

        /// <summary>
        /// Double Pi squared
        /// </summary>
        public const double TwoPISquared = 2 * Math.PI * Math.PI;
    }
}
