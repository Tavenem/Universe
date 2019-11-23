using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// A carbonaceous asteroid (mostly rock).
    /// </summary>
    [Serializable]
    public class CTypeAsteroid : Asteroid
    {
        private protected override string BaseTypeName => "C-Type Asteroid";

        private protected override double DensityForType => 1380;

        private protected override string DesignatorPrefix => "c";

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/>.
        /// </summary>
        internal CTypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="CTypeAsteroid"/>.</param>
        internal CTypeAsteroid(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="CTypeAsteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="CTypeAsteroid"/> during random generation, in kg.
        /// </param>
        internal CTypeAsteroid(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private protected override async Task GenerateAlbedoAsync() => await SetAlbedoAsync(Randomizer.Instance.NextDouble(0.03, 0.1)).ConfigureAwait(false);

        private protected override async Task<Planetoid?> GenerateSatelliteAsync(Number periapsis, double eccentricity, Number maxMass)
            => await GetNewInstanceAsync<CTypeAsteroid>(ParentId, Vector3.Zero, maxMass, GetAsteroidSatelliteOrbit(periapsis, eccentricity)).ConfigureAwait(false);

        private protected override IMaterial GetComposition(double density, Number mass, IShape shape, double? temperature)
        {
            var rock = 1m;

            var clay = Randomizer.Instance.NextDecimal(0.1m, 0.2m);
            rock -= clay;

            var ice = Randomizer.Instance.NextDecimal(0.22m);
            rock -= ice;

            var substances = new List<(ISubstanceReference, decimal)>();
            foreach (var (material, proportion) in CelestialSubstances.ChondriticRockMixture)
            {
                substances.Add((material, proportion * rock));
            }
            substances.Add((Substances.All.BallClay.GetReference(), clay));
            substances.Add((Substances.All.Water.GetChemicalReference(), ice));

            return new Material(
                substances,
                density,
                mass,
                shape,
                temperature);
        }
    }
}
