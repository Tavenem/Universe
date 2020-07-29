using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A region of space with a high concentration of asteroids.
    /// </summary>
    [Serializable]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJson.AsteroidFieldConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(AsteroidFieldConverter))]
    public class AsteroidField : CosmicLocation
    {
        internal static readonly Number AsteroidFieldSpace = new Number(3.15, 12);
        internal static readonly Number OortCloudSpace = new Number(7.5, 15);

        private static readonly Number _AsteroidFieldChildDensity = new Number(13, -31);
        private static readonly List<ChildDefinition> _AsteroidFieldChildDefinitions = new List<ChildDefinition>
        {
            new PlanetChildDefinition(_AsteroidFieldChildDensity * new Number(74, -2), PlanetType.AsteroidC),
            new PlanetChildDefinition(_AsteroidFieldChildDensity * new Number(14, -2), PlanetType.AsteroidS),
            new PlanetChildDefinition(_AsteroidFieldChildDensity * Number.Deci, PlanetType.AsteroidM),
            new PlanetChildDefinition(_AsteroidFieldChildDensity * new Number(2, -2), PlanetType.Comet),
            new PlanetChildDefinition(_AsteroidFieldChildDensity * new Number(3, -10), PlanetType.Dwarf),
            new PlanetChildDefinition(_AsteroidFieldChildDensity * new Number(1, -10), PlanetType.RockyDwarf),
        };

        private static readonly Number _OortCloudChildDensity = new Number(8.31, -38);
        private static readonly List<ChildDefinition> _OortCloudChildDefinitions = new List<ChildDefinition>
        {
            new PlanetChildDefinition(_OortCloudChildDensity * new Number(85, -2), PlanetType.Comet),
            new PlanetChildDefinition(_OortCloudChildDensity * new Number(11, -2), PlanetType.AsteroidC),
            new PlanetChildDefinition(_OortCloudChildDensity * new Number(25, -3), PlanetType.AsteroidS),
            new PlanetChildDefinition(_OortCloudChildDensity * new Number(15, -3), PlanetType.AsteroidM),
        };

        internal OrbitalParameters? _childOrbitalParameters;
        internal bool _toroidal;

        /// <summary>
        /// The type discriminator for this type.
        /// </summary>
        public const string AsteroidFieldIdItemTypeName = "IdItemType_AsteroidField";
        /// <summary>
        /// A built-in, read-only type discriminator.
        /// </summary>
        public override string IdItemTypeName => AsteroidFieldIdItemTypeName;

        internal Number MajorRadius
        {
            get
            {
                if (Material.Shape is HollowSphere hollowSphere)
                {
                    return hollowSphere.OuterRadius;
                }
                if (Material.Shape is Torus torus)
                {
                    return torus.MajorRadius;
                }
                if (Material.Shape is Ellipsoid ellipsoid)
                {
                    return ellipsoid.AxisX;
                }
                return Number.Zero;
            }
        }

        internal Number MinorRadius
        {
            get
            {
                if (Material.Shape is HollowSphere hollowSphere)
                {
                    return hollowSphere.InnerRadius;
                }
                if (Material.Shape is Torus torus)
                {
                    return torus.MinorRadius;
                }
                return Number.Zero;
            }
        }

        private protected override IEnumerable<ChildDefinition> ChildDefinitions => StructureType == CosmicStructureType.OortCloud
            ? _OortCloudChildDefinitions
            : _AsteroidFieldChildDefinitions;

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
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
        /// <param name="oort">
        /// If <see langword="true"/>, generates an Oort cloud. Otherwise, generates an asteroid field.
        /// </param>
        /// <param name="majorRadius">
        /// <para>
        /// The major radius of the field.
        /// </para>
        /// <para>
        /// In the case of an Oort cloud, this should refer instead to the radius of the star system.
        /// </para>
        /// </param>
        /// <param name="minorRadius">
        /// The minor radius of the field.
        /// </param>
        /// <param name="childOrbit">
        /// The orbital parameters to assign to any new child instances (if any).
        /// </param>
        public AsteroidField(
            CosmicLocation? parent,
            Vector3 position,
            OrbitalParameters? orbit = null,
            bool oort = false,
            Number? majorRadius = null,
            Number? minorRadius = null,
            OrbitalParameters? childOrbit = null) : base(parent?.Id, oort ? CosmicStructureType.OortCloud : CosmicStructureType.AsteroidField)
        {
            _childOrbitalParameters = childOrbit;

            if (oort)
            {
                Configure(parent, position, majorRadius);
            }
            else
            {
                Configure(parent, position, majorRadius, minorRadius);
            }

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

        private AsteroidField(string? parentId, CosmicStructureType structureType = CosmicStructureType.AsteroidField) : base(parentId, structureType) { }

        internal AsteroidField(
            string id,
            uint seed,
            CosmicStructureType structureType,
            string? parentId,
            Vector3[]? absolutePosition,
            string? name,
            Vector3 velocity,
            Orbit? orbit,
            Vector3 position,
            double? temperature,
            Number majorRadius,
            Number minorRadius,
            bool toroidal,
            OrbitalParameters? childOrbitalParameters) : base(
                id,
                seed,
                structureType,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit)
        {
            _toroidal = toroidal;
            _childOrbitalParameters = childOrbitalParameters;
            Reconstitute(position, temperature, majorRadius, minorRadius);
        }

        private AsteroidField(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            (uint?)info.GetValue(nameof(_seed), typeof(uint)) ?? default,
            (CosmicStructureType?)info.GetValue(nameof(StructureType), typeof(CosmicStructureType)) ?? CosmicStructureType.AsteroidField,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (Vector3?)info.GetValue(nameof(Velocity), typeof(Vector3)) ?? default,
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (Vector3?)info.GetValue(nameof(Position), typeof(Vector3)) ?? Vector3.Zero,
            (double?)info.GetValue(nameof(Temperature), typeof(double?)) ?? default,
            (Number?)info.GetValue(nameof(MajorRadius), typeof(Number)) ?? Number.Zero,
            (Number?)info.GetValue(nameof(MinorRadius), typeof(Number)) ?? Number.Zero,
            (bool?)info.GetValue(nameof(_toroidal), typeof(bool)) ?? default,
            (OrbitalParameters?)info.GetValue(nameof(_childOrbitalParameters), typeof(OrbitalParameters)))
        { }

        /// <summary>
        /// Generates a new <see cref="AsteroidField"/> as the containing parent location of the
        /// given <paramref name="child"/> location.
        /// </summary>
        /// <param name="child">The child location for which to generate a parent.</param>
        /// <param name="position">
        /// An optional position for the child within the new containing parent. If no position is
        /// given, one is randomly determined.
        /// </param>
        /// <param name="orbit">
        /// <para>
        /// An optional orbit for the child to follow in the new parent.
        /// </para>
        /// <para>
        /// Depending on the type of parent location generated, the child may also be placed in a
        /// randomly-determined orbit if none is given explicitly, usually based on its position.
        /// </para>
        /// </param>
        /// <param name="oort">
        /// If <see langword="true"/>, generates an Oort cloud. Otherwise, generates an asteroid field.
        /// </param>
        /// <param name="majorRadius">
        /// <para>
        /// The major radius of the field.
        /// </para>
        /// <para>
        /// In the case of an Oort cloud, this should refer instead to the radius of the star system.
        /// </para>
        /// </param>
        /// <param name="minorRadius">
        /// The minor radius of the field.
        /// </param>
        /// <param name="childOrbit">
        /// The orbital parameters to assign to any new child instances (if any).
        /// </param>
        /// <returns>
        /// <para>
        /// The generated containing parent. Also sets the <see cref="Location.ParentId"/> of the
        /// <paramref name="child"/> accordingly.
        /// </para>
        /// <para>
        /// If no parent could be generated, returns <see langword="null"/>.
        /// </para>
        /// </returns>
        public static CosmicLocation? GetParentForChild(
            CosmicLocation child,
            Vector3? position = null,
            OrbitalParameters? orbit = null,
            bool oort = false,
            Number? majorRadius = null,
            Number? minorRadius = null,
            OrbitalParameters? childOrbit = null)
        {
            var instance = oort
                ? new AsteroidField(null, CosmicStructureType.OortCloud)
                : new AsteroidField(null);
            instance._childOrbitalParameters = childOrbit;
            child.AssignParent(instance);

            switch (instance.StructureType)
            {
                case CosmicStructureType.AsteroidField:
                    instance.Configure(null, Vector3.Zero, majorRadius, minorRadius);
                    break;
                case CosmicStructureType.OortCloud:
                    instance.Configure(null, Vector3.Zero, majorRadius);
                    break;
            }

            if (!position.HasValue)
            {
                if (child.StructureType == CosmicStructureType.Universe)
                {
                    position = Vector3.Zero;
                }
                else
                {
                    var space = child.StructureType switch
                    {
                        CosmicStructureType.Supercluster => _SuperclusterSpace,
                        CosmicStructureType.GalaxyCluster => _GalaxyClusterSpace,
                        CosmicStructureType.GalaxyGroup => _GalaxyGroupSpace,
                        CosmicStructureType.GalaxySubgroup => _GalaxySubgroupSpace,
                        CosmicStructureType.SpiralGalaxy => _GalaxySpace,
                        CosmicStructureType.EllipticalGalaxy => _GalaxySpace,
                        CosmicStructureType.DwarfGalaxy => _DwarfGalaxySpace,
                        CosmicStructureType.GlobularCluster => _GlobularClusterSpace,
                        CosmicStructureType.Nebula => _NebulaSpace,
                        CosmicStructureType.HIIRegion => _NebulaSpace,
                        CosmicStructureType.PlanetaryNebula => _PlanetaryNebulaSpace,
                        CosmicStructureType.StarSystem => StarSystem.StarSystemSpace,
                        CosmicStructureType.AsteroidField => AsteroidFieldSpace,
                        CosmicStructureType.OortCloud => OortCloudSpace,
                        CosmicStructureType.BlackHole => BlackHole.BlackHoleSpace,
                        CosmicStructureType.Star => StarSystem.StarSystemSpace,
                        CosmicStructureType.Planetoid => Planetoid.GiantSpace,
                        _ => Number.Zero,
                    };
                    position = instance.GetOpenSpace(space, new List<Location>());
                }
            }
            if (position.HasValue)
            {
                child.Position = position.Value;
            }

            if (!child.Orbit.HasValue)
            {
                orbit ??= instance.GetChildOrbit();
                if (orbit.HasValue)
                {
                    Space.Orbit.AssignOrbit(child, orbit.Value);
                }
            }

            return instance;
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(_seed), _seed);
            info.AddValue(nameof(StructureType), StructureType);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), Orbit);
            info.AddValue(nameof(Position), Position);
            info.AddValue(nameof(Temperature), Material.Temperature);
            info.AddValue(nameof(MajorRadius), MajorRadius);
            info.AddValue(nameof(MinorRadius), MinorRadius);
            info.AddValue(nameof(_toroidal), _toroidal);
            info.AddValue(nameof(_childOrbitalParameters), _childOrbitalParameters);
        }

        private void Configure(CosmicLocation? parent, Vector3 position, Number? majorRadius = null, Number? minorRadius = null)
        {
            if (StructureType == CosmicStructureType.OortCloud)
            {
                majorRadius = majorRadius.HasValue ? majorRadius.Value + OortCloudSpace : OortCloudSpace;
                minorRadius = majorRadius.HasValue ? majorRadius.Value + new Number(3, 15) : new Number(3, 15);
            }
            else if (position != Vector3.Zero || parent is not StarSystem || !majorRadius.HasValue)
            {
                majorRadius ??= Randomizer.Instance.NextNumber(new Number(1.5, 11), AsteroidFieldSpace);
                minorRadius = Number.Zero;
            }
            else
            {
                _toroidal = true;
                majorRadius ??= Number.Zero;
                minorRadius ??= Number.Zero;
            }

            _seed = Randomizer.Instance.NextUIntInclusive();
            Reconstitute(
                position,
                parent?.Material.Temperature ?? UniverseAmbientTemperature,
                majorRadius.Value,
                minorRadius.Value);
        }

        private void Reconstitute(Vector3 position, double? temperature, Number majorRadius, Number minorRadius)
        {
            if (StructureType == CosmicStructureType.OortCloud)
            {
                Material = new Material(
                    Substances.All.InterplanetaryMedium.GetReference(),
                    new Number(3, 25),
                    new HollowSphere(
                        minorRadius,
                        majorRadius,
                        position),
                    temperature);
                return;
            }

            var randomizer = new Randomizer(_seed);

            var shape = _toroidal
                ? new Torus(majorRadius, minorRadius, position)
                : (IShape)new Ellipsoid(
                    majorRadius,
                    randomizer.NextNumber(Number.Half, new Number(15, -1)) * majorRadius,
                    randomizer.NextNumber(Number.Half, new Number(15, -1)) * majorRadius,
                    position);

            Material = new Material(
                Substances.All.InterplanetaryMedium.GetReference(),
                shape.Volume * new Number(7, -8),
                shape,
                temperature);
        }

        internal OrbitalParameters? GetChildOrbit()
        {
            if (_childOrbitalParameters.HasValue)
            {
                return _childOrbitalParameters;
            }

            if (Orbit.HasValue)
            {
                return OrbitalParameters.GetFromEccentricity(
                    Orbit.Value.OrbitedMass,
                    Orbit.Value.OrbitedPosition,
                    Orbit.Value.Eccentricity);
            }

            return null;
        }
    }
}
