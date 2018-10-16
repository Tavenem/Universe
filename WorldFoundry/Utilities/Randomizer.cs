using MathAndScience.Numerics;
using System;
using Troschuetz.Random;

namespace WorldFoundry
{
    internal static class Randomizer
    {
        internal static TRandom Instance = new TRandom();

        static Randomizer()
        {
            // Prime randomizer to avoid bug: https://github.com/pomma89/Troschuetz.Random/issues/6
            Instance.Next();
            Instance.Next();
            Instance.Next();
            Instance.Next();
        }

        /// <summary>
        /// Gets a randomly oriented vector whose length is between 0 and <paramref
        /// name="maxLength"/>.
        /// </summary>
        /// <param name="maxLength">The exclusive upper bound of the length of the vector to be
        /// generated.</param>
        /// <returns>A randomly oriented vector whose length is between 0 and <paramref
        /// name="maxLength"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> must be
        /// greater than or equal to 0.0.</exception>
        /// <exception cref="ArgumentException"><paramref name="maxValue"/> cannot be <see
        /// cref="double.PositiveInfinity"/>.</exception>
        /// <remarks>
        /// A random rotation is generated with <see cref="GetRandomQuaternion"/>, then the vector
        /// is scaled randomly according to <paramref name="maxLength"/>, to avoid unintentionally
        /// generating lengths in a quasi-Gaussian distribution, as would occur if the vector
        /// components were generated independently.
        /// </remarks>
        internal static Vector3 GetRandomVector(double maxLength)
            => Vector3.UnitX.Transform(GetRandomQuaternion()) * Instance.NextDouble(maxLength);

        /// <summary>
        /// Gets a randomly oriented vector whose length is between <paramref name="minLength"/> and
        /// <paramref name="maxLength"/>.
        /// </summary>
        /// <param name="minLength">The inclusive lower bound of the length of the vector to be
        /// generated.</param>
        /// <param name="maxLength">The exclusive upper bound of the length of the vector to be
        /// generated.</param>
        /// <returns>A randomly oriented vector whose length is between <paramref name="minLength"/>
        /// and <paramref name="maxLength"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> must be
        /// greater than <paramref name="minValue"/>.</exception>
        /// <exception cref="ArgumentException">The difference between <paramref name="maxValue"/>
        /// and <paramref name="minValue"/> cannot be <see
        /// cref="double.PositiveInfinity"/>.</exception>
        /// <remarks>
        /// A random rotation is generated with <see cref="GetRandomQuaternion"/>, then the vector
        /// is scaled randomly according to <paramref name="minLength"/> and <paramref
        /// name="maxLength"/>, to avoid unintentionally generating lengths in a quasi-Gaussian
        /// distribution, as would occur if the vector components were generated independently.
        /// </remarks>
        internal static Vector3 GetRandomVector(double minLength, double maxLength)
            => Vector3.UnitX.Transform(GetRandomQuaternion()).Normalize() * Instance.NextDouble(minLength, maxLength);

        /// <summary>
        /// Gets a random, normalized quaternion.
        /// </summary>
        /// <returns>A random, normalized quaternion.</returns>
        /// <remarks>
        /// Each component is selected randomly as a value between 0 and 1, then the result is
        /// normalized. This may cause some form of quasi-Gaussian distribution of results, as the
        /// result is a composite of four independently determined variables, but the
        /// interdependence between components of a quaternion is loose enough that this effect is
        /// considered to be inconsequential.
        /// </remarks>
        internal static Quaternion GetRandomQuaternion()
            => new Quaternion(Instance.NextDouble(), Instance.NextDouble(), Instance.NextDouble(), Instance.NextDouble()).Normalize();
    }
}
