using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A small, dense collection of stars.
    /// </summary>
    /// <remarks>
    /// Technically these are not galaxies, but they are most easily modeled with the same mechanics,
    /// making it simplest to inherit from the <see cref="Galaxy"/> base class.
    /// </remarks>
    [Serializable]
    public class GlobularCluster : Galaxy
    {
        internal static readonly Number Space = new Number(2.1, 7);

        private static readonly Number _ChildDensity = new Number(1.3, -17);
        private static readonly Number _RedDensity = _ChildDensity * new Number(36, -2);
        private static readonly Number _KDensity = _ChildDensity * new Number(23, -3);
        private static readonly Number _GDensity = _ChildDensity * new Number(37, -3);
        private static readonly Number _FDensity = _ChildDensity * new Number(1425, -5);
        private static readonly Number _RedGiantDensity = _ChildDensity * new Number(25, -4);
        private static readonly Number _BlueGiantDensity = _ChildDensity * new Number(2, -3);
        private static readonly Number _YellowGiantDensity = _ChildDensity * new Number(1, -3);

        private static readonly List<ChildDefinition> _ChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(47, -2), typeof(BrownDwarf)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedDensity * new Number(998, -3), typeof(Star), SpectralClass.M, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedDensity * new Number(2, -3), typeof(Star), SpectralClass.M, LuminosityClass.sd),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _KDensity * new Number(989, -3), typeof(Star), SpectralClass.K, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _KDensity * new Number(7, -3), typeof(Star), SpectralClass.K, LuminosityClass.IV),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _KDensity * new Number(4, -3), typeof(Star), SpectralClass.K, LuminosityClass.sd),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(48, -3), typeof(WhiteDwarf)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _GDensity * new Number(986, -3), typeof(Star), SpectralClass.G, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _GDensity * new Number(14, -3), typeof(Star), SpectralClass.G, LuminosityClass.IV),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _FDensity * new Number(982, -3), typeof(Star), SpectralClass.F, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _FDensity * new Number(18, -3), typeof(Star), SpectralClass.F, LuminosityClass.IV),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(35, -4), typeof(NeutronStar)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(29, -4), typeof(Star), SpectralClass.A, LuminosityClass.V),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(96, -2), typeof(RedGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(18, -3), typeof(RedGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(16, -3), typeof(RedGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(55, -4), typeof(RedGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(5, -4), typeof(RedGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(95, -2), typeof(BlueGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(25, -3), typeof(BlueGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(2, -2), typeof(BlueGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(45, -4), typeof(BlueGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(5, -4), typeof(BlueGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(95, -2), typeof(YellowGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(2, -2), typeof(YellowGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(23, -3), typeof(YellowGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(6, -3), typeof(YellowGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(1, -3), typeof(YellowGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(6, -4), typeof(Star), SpectralClass.B, LuminosityClass.V),

            new ChildDefinition(typeof(BlackHole), BlackHole.Space, _ChildDensity * new Number(25, -5)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(1.25, -5), typeof(Star), SpectralClass.O, LuminosityClass.V),
        };

        private protected override string BaseTypeName => "Globular Cluster";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_ChildDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/>.
        /// </summary>
        internal GlobularCluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="GlobularCluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GlobularCluster"/>.</param>
        internal GlobularCluster(Location parent, Vector3 position) : base(parent, position) { }

        private GlobularCluster(
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

        private GlobularCluster(SerializationInfo info, StreamingContext context) : this(
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
        /// The cores of globular clusters are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override string GetGalacticCore()
            => new BlackHole(this, Vector3.Zero).Id;

        private protected override IShape GetShape()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(8, 6), new Number(2.1, 7));
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 1);
            return new Ellipsoid(radius, axis, Position);
        }
    }
}
