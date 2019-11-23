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
    /// A metallic asteroid (mostly iron-nickel, with some rock and other heavy metals).
    /// </summary>
    [Serializable]
    public class MTypeAsteroid : Asteroid
    {
        private protected override string BaseTypeName => "M-Type Asteroid";

        private protected override double DensityForType => 5320;

        private protected override string DesignatorPrefix => "m";

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/>.
        /// </summary>
        internal MTypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="MTypeAsteroid"/>.</param>
        internal MTypeAsteroid(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="MTypeAsteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="MTypeAsteroid"/> during random generation, in kg.
        /// </param>
        internal MTypeAsteroid(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private protected override async Task GenerateAlbedoAsync() => await SetAlbedoAsync(Randomizer.Instance.NextDouble(0.1, 0.2)).ConfigureAwait(false);

        private protected override async Task<Planetoid?> GenerateSatelliteAsync(Number periapsis, double eccentricity, Number maxMass)
            => await GetNewInstanceAsync<MTypeAsteroid>(ParentId, Vector3.Zero, maxMass, GetAsteroidSatelliteOrbit(periapsis, eccentricity)).ConfigureAwait(false);

        private protected override IMaterial GetComposition(double density, Number mass, IShape shape, double? temperature)
        {
            var ironNickel = 0.95m;

            var rock = Randomizer.Instance.NextDecimal(0.2m);
            ironNickel -= rock;

            var gold = Randomizer.Instance.NextDecimal(0.05m);

            var platinum = 0.05m - gold;

            var substances = new List<(ISubstanceReference, decimal)>();
            foreach (var (material, proportion) in CelestialSubstances.ChondriticRockMixture_NoMetal)
            {
                substances.Add((material, proportion * rock));
            }
            substances.Add((Substances.All.IronNickelAlloy.GetHomogeneousReference(), ironNickel));
            substances.Add((Substances.All.Gold.GetChemicalReference(), gold));
            substances.Add((Substances.All.Platinum.GetChemicalReference(), platinum));

            return new Material(
                substances,
                density,
                mass,
                shape,
                temperature);
        }
    }
}
