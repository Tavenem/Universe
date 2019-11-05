using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.CelestialBodies.BlackHoles;
using NeverFoundry.WorldFoundry.CelestialBodies.Stars;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space.Galaxies
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

        private static readonly List<IChildDefinition> _ChildDefinitions = new List<IChildDefinition>
        {
            new StarSystemChildDefinition<BrownDwarf>(_ChildDensity * new Number(47, -2)),

            new StarSystemChildDefinition<Star>(_RedDensity * new Number(998, -3), SpectralClass.M, LuminosityClass.V),
            new StarSystemChildDefinition<Star>(_RedDensity * new Number(2, -3), SpectralClass.M, LuminosityClass.sd),

            new StarSystemChildDefinition<Star>(_KDensity * new Number(989, -3), SpectralClass.K, LuminosityClass.V),
            new StarSystemChildDefinition<Star>(_KDensity * new Number(7, -3), SpectralClass.K, LuminosityClass.IV),
            new StarSystemChildDefinition<Star>(_KDensity * new Number(4, -3), SpectralClass.K, LuminosityClass.sd),

            new StarSystemChildDefinition<WhiteDwarf>(_ChildDensity * new Number(48, -3)),

            new StarSystemChildDefinition<Star>(_GDensity * new Number(986, -3), SpectralClass.G, LuminosityClass.V),
            new StarSystemChildDefinition<Star>(_GDensity * new Number(14, -3), SpectralClass.G, LuminosityClass.IV),

            new StarSystemChildDefinition<Star>(_FDensity * new Number(982, -3), SpectralClass.F, LuminosityClass.V),
            new StarSystemChildDefinition<Star>(_FDensity * new Number(18, -3), SpectralClass.F, LuminosityClass.IV),

            new StarSystemChildDefinition<NeutronStar>(_ChildDensity * new Number(35, -4)),

            new StarSystemChildDefinition<Star>(_ChildDensity * new Number(29, -4), SpectralClass.A, LuminosityClass.V),

            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(96, -2)),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(18, -3), null, LuminosityClass.II),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(16, -3), null, LuminosityClass.Ib),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(55, -4), null, LuminosityClass.Ia),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(5, -4), null, LuminosityClass.Zero),

            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(95, -2)),
            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(25, -3), null, LuminosityClass.II),
            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(2, -2), null, LuminosityClass.Ib),
            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(45, -4), null, LuminosityClass.Ia),
            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(5, -4), null, LuminosityClass.Zero),

            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(95, -2)),
            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(2, -2), null, LuminosityClass.II),
            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(23, -3), null, LuminosityClass.Ib),
            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(6, -3), null, LuminosityClass.Ia),
            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(1, -3), null, LuminosityClass.Zero),

            new StarSystemChildDefinition<Star>(_ChildDensity * new Number(6, -4), SpectralClass.B, LuminosityClass.V),

            new ChildDefinition<BlackHole>(BlackHole.Space, _ChildDensity * new Number(25, -5)),

            new StarSystemChildDefinition<Star>(_ChildDensity * new Number(1.25, -5), SpectralClass.O, LuminosityClass.V),
        };

        private protected override string BaseTypeName => "Globular Cluster";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _ChildDefinitions;

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/>.
        /// </summary>
        internal GlobularCluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="GlobularCluster"/>.</param>
        internal GlobularCluster(string? parentId, Vector3 position) : base(parentId, position) { }

        private GlobularCluster(
            string id,
            string? name,
            string galacticCoreId,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            string? parentId)
            : base(
                id,
                name,
                galacticCoreId,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                parentId)
        { }

        private GlobularCluster(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_galacticCoreId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string)))
        { }

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        /// <remarks>
        /// The cores of globular clusters are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override Task<BlackHole?> GenerateGalacticCoreAsync()
            => GetNewInstanceAsync<BlackHole>(this, Vector3.Zero);

        private protected override ValueTask<IShape> GetShapeAsync()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(8, 6), new Number(2.1, 7));
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 1);
            return new ValueTask<IShape>(new Ellipsoid(radius, axis, Position));
        }
    }
}
