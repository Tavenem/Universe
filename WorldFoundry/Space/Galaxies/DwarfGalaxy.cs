using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.Place;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A small, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    [Serializable]
    public class DwarfGalaxy : Galaxy
    {
        internal static readonly Number Space = new Number(2.5, 18);

        private protected override string BaseTypeName => "Dwarf Galaxy";

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/>.
        /// </summary>
        internal DwarfGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="DwarfGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfGalaxy"/>.</param>
        internal DwarfGalaxy(Location parent, Vector3 position) : base(parent, position) { }

        private DwarfGalaxy(
            string id,
            string? name,
            string galacticCoreId,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children)
            : base(
                id,
                name,
                galacticCoreId,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                children) { }

        private DwarfGalaxy(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_galacticCoreId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        /// <remarks>
        /// The cores of dwarf galaxies are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override string GetGalacticCore()
            => new BlackHole(this, Vector3.Zero).Id;

        private protected override IShape GetShape()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(9.5, 18), new Number(2.5, 18)); // ~200–1800 ly
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 1);
            return new Ellipsoid(radius, axis, Position);
        }
    }
}
