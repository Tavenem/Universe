using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Runtime.Serialization;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A gravitational singularity.
    /// </summary>
    [Serializable]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJson.BlackHoleConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(BlackHoleConverter))]
    public class BlackHole : CosmicLocation
    {
        internal static readonly Number BlackHoleSpace = new(60000);
        internal static readonly Number SupermassiveBlackHoleThreshold = new(1, 33);

        internal readonly bool _supermassive;

        private protected override string BaseTypeName => _supermassive ? "Supermassive Black Hole" : "Black Hole";

        /// <summary>
        /// The type discriminator for this type.
        /// </summary>
        public const string BlackHoleIdItemTypeName = ":Location:CosmicLocation:BlackHole:";
        /// <summary>
        /// A built-in, read-only type discriminator.
        /// </summary>
        public override string IdItemTypeName => BlackHoleIdItemTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing parent location for which to generate a child.
        /// </param>
        /// <param name="position">The position for the child.</param>
        /// <param name="orbit">
        /// <para>
        /// An optional orbit to assign to the child.
        /// </para>
        /// <para>
        /// Depending on the parameters, may override <paramref name="position"/>.
        /// </para>
        /// </param>
        /// <param name="supermassive">
        /// Whether this is to be a supermassive black hole.
        /// </param>
        public BlackHole(
            CosmicLocation? parent,
            Vector3 position,
            OrbitalParameters? orbit = null,
            bool supermassive = false) : base(parent?.Id, CosmicStructureType.BlackHole)
        {
            _supermassive = supermassive;

            Configure(position);

            if (parent is not null && !orbit.HasValue)
            {
                if (parent is AsteroidField asteroidField)
                {
                    orbit = asteroidField.GetChildOrbit();
                }
                else
                {
                    orbit = parent.StructureType switch
                    {
                        CosmicStructureType.GalaxySubgroup => Position.IsZero() ? null : parent.GetGalaxySubgroupChildOrbit(),
                        CosmicStructureType.SpiralGalaxy
                            or CosmicStructureType.EllipticalGalaxy
                            or CosmicStructureType.DwarfGalaxy => Position.IsZero() ? (OrbitalParameters?)null : parent.GetGalaxyChildOrbit(),
                        CosmicStructureType.GlobularCluster => Position.IsZero() ? (OrbitalParameters?)null : parent.GetGlobularClusterChildOrbit(),
                        CosmicStructureType.StarSystem => parent is StarSystem && !Position.IsZero()
                            ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                            : (OrbitalParameters?)null,
                        _ => null,
                    };
                }
            }
            if (orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(this, orbit.Value);
            }
        }

        internal BlackHole(
            string id,
            uint seed,
            string? parentId,
            Vector3[]? absolutePosition,
            string? name,
            Vector3 velocity,
            Orbit? orbit,
            Vector3 position,
            bool supermassive) : base(
                id,
                seed,
                CosmicStructureType.BlackHole,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit)
        {
            _supermassive = supermassive;
            Reconstitute(position);
        }

        private BlackHole(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            (uint?)info.GetValue(nameof(Seed), typeof(uint)) ?? default,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (Vector3?)info.GetValue(nameof(Velocity), typeof(Vector3)) ?? default,
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (Vector3?)info.GetValue(nameof(Position), typeof(Vector3)) ?? default,
            (bool?)info.GetValue(nameof(_supermassive), typeof(bool)) ?? default)
        { }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Seed), Seed);
            info.AddValue(nameof(StructureType), StructureType);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), Orbit);
            info.AddValue(nameof(Position), Position);
            info.AddValue(nameof(_supermassive), _supermassive);
        }

        internal static Number GetBlackHoleMassForSeed(uint seed, bool supermassive) => supermassive
            ? new Randomizer(seed).NextNumber(new Number(2, 35), new Number(2, 40)) // ~10e5–10e10 solar masses
            : new Randomizer(seed).NextNumber(new Number(6, 30), new Number(4, 31)); // ~3–20 solar masses

        private void Configure(Vector3 position)
        {
            Seed = Randomizer.Instance.NextUIntInclusive();
            Reconstitute(position);
        }

        private void Reconstitute(Vector3 position)
        {
            var mass = GetBlackHoleMassForSeed(Seed, _supermassive);

            Material = new Material(
                Substances.All.Fuzzball.GetReference(),
                mass,

                // The shape given is presumed to refer to the shape of the event horizon.
                new Sphere(ScienceConstants.TwoG * mass / ScienceConstants.SpeedOfLightSquared, position),

                // Hawking radiation = solar mass / mass * constant
                (double)(new Number(6.169, -8) * new Number(1.98847, 30) / mass));
        }
    }
}
