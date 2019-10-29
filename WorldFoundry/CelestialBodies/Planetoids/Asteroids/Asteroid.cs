using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// Base class for all asteroids.
    /// </summary>
    [Serializable]
    public class Asteroid : Planetoid
    {
        internal static readonly Number Space = new Number(400000);

        // Above this an object achieves hydrostatic equilibrium, and is considered a dwarf planet
        // rather than an asteroid.
        private protected override Number? MaxMassForType => new Number(3.4, 20);

        // Below this a body is considered a meteoroid, rather than an asteroid.
        private protected override Number? MinMassForType => new Number(5.9, 8);

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/>.
        /// </summary>
        internal Asteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        internal Asteroid(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Asteroid"/> during random generation, in kg.
        /// </param>
        internal Asteroid(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        internal override async Task GenerateOrbitAsync(CelestialLocation orbitedObject)
            => await NeverFoundry.WorldFoundry.Space.Orbit.SetOrbitAsync(
                this,
                orbitedObject,
                await GetDistanceToAsync(orbitedObject).ConfigureAwait(false),
                Randomizer.Instance.NextDouble(0.4),
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                0).ConfigureAwait(false);

        private protected OrbitalParameters GetAsteroidSatelliteOrbit(Number periapsis, double eccentricity)
            => new OrbitalParameters(
                this,
                periapsis,
                eccentricity,
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI));

        private protected override async ValueTask<(double density, Number mass, IShape shape)> GetMatterAsync()
        {
            var density = GetDensity();
            var mass = await GetMassAsync().ConfigureAwait(false);
            return (density, mass, GetShape(density, mass));
        }

        private protected override Number GetMinSatellitePeriapsis() => Shape.ContainingRadius + 20;

        private protected IShape GetShape(Number density, Number mass)
        {
            var axis = (mass * new Number(75, -2) / (density * MathConstants.PI)).CubeRoot();
            var irregularity = Randomizer.Instance.NextNumber(Number.Half, Number.One);
            return new Ellipsoid(axis, axis * irregularity, axis / irregularity, Position);
        }
    }
}
