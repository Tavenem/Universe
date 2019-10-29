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
    /// A silicate asteroid (rocky with significant metal content).
    /// </summary>
    [Serializable]
    public class STypeAsteroid : Asteroid
    {
        private protected override string BaseTypeName => "S-Type Asteroid";

        private protected override double DensityForType => 2710;

        private protected override string DesignatorPrefix => "s";

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/>.
        /// </summary>
        internal STypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="STypeAsteroid"/>.</param>
        internal STypeAsteroid(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="STypeAsteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="STypeAsteroid"/> during random generation, in kg.
        /// </param>
        internal STypeAsteroid(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private protected override async Task GenerateAlbedoAsync() => await SetAlbedoAsync(Randomizer.Instance.NextDouble(0.1, 0.22)).ConfigureAwait(false);

        private protected override async Task<Planetoid?> GenerateSatelliteAsync(Number periapsis, double eccentricity, Number maxMass)
            => await GetNewInstanceAsync<STypeAsteroid>(ParentId, Vector3.Zero, maxMass, GetAsteroidSatelliteOrbit(periapsis, eccentricity)).ConfigureAwait(false);

        private protected override IMaterial GetComposition(double density, Number mass, IShape shape, double? temperature)
        {
            var gold = Randomizer.Instance.NextDecimal(0.005m);

            var substances = new List<(ISubstanceReference, decimal)>();
            foreach (var (material, proportion) in CelestialSubstances.ChondriticRockMixture_NoMetal)
            {
                substances.Add((material, proportion * 0.427m));
            }
            substances.Add((Substances.GetSolutionReference(Substances.Solutions.IronNickelAlloy), 0.568m));
            substances.Add((Substances.GetChemicalReference(Substances.Chemicals.Gold), gold));
            substances.Add((Substances.GetChemicalReference(Substances.Chemicals.Platinum), 0.005m - gold));

            return new Material(
                substances,
                density,
                mass,
                shape,
                temperature);
        }
    }
}
