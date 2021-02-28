using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using NeverFoundry.WorldFoundry.Space.Stars;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A place in a universe, with a location that defines its position, and a shape that defines
    /// its extent, as well as a mass, density, and temperature. It may or may not also have a
    /// particular chemical composition.
    /// </summary>
    /// <remarks>
    /// Locations can exist in a hierarchy. Any location may contain other locations, and be
    /// contained by a location in turn. The relative positions of locations within the same
    /// hierarchy can be analyzed using the methods available on this class.
    /// </remarks>
    [Serializable]
    [System.Text.Json.Serialization.JsonConverter(typeof(CosmicLocationConverter))]
    public partial class CosmicLocation : Location
    {
        /// <summary>
        /// A string that uniquely identifies this <see cref="CosmicLocation"/> for display
        /// purposes. Includes a description of its type, and its <see cref="IIdItem.Id"/>.
        /// </summary>
        public string Designation => $"{TypeName} {Id}";

        /// <summary>
        /// The type discriminator for this type.
        /// </summary>
        public const string CosmicLocationIdItemTypeName = ":Location:CosmicLocation:";
        /// <summary>
        /// A built-in, read-only type discriminator.
        /// </summary>
        public override string IdItemTypeName => CosmicLocationIdItemTypeName;

        /// <summary>
        /// The mass of this location's material, in kg.
        /// </summary>
        public Number Mass => Material.Mass;

        /// <summary>
        /// The physical material which comprises this location.
        /// </summary>
        public IMaterial Material { get; private protected set; } = MathAndScience.Chemistry.Material.Empty;

        /// <summary>
        /// An optional name for this <see cref="CosmicLocation"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="CosmicLocation"/> must have a name. They may be uniquely identified
        /// by their <see cref="IIdItem.Id"/>, instead.
        /// </remarks>
        public string? Name { get; set; }

        /// <summary>
        /// The orbit occupied by this <see cref="CosmicLocation"/> (may be <see langword="null"/>).
        /// </summary>
        public Orbit? Orbit { get; internal set; }

        private Number? _radiusSquared;
        /// <summary>
        /// The containing radius of the location, squared.
        /// </summary>
        /// <remarks>
        /// The value is calculated on first request and cached.
        /// </remarks>
        public Number RadiusSquared => _radiusSquared ??= Shape.ContainingRadius.Square();

        /// <summary>
        /// A value which deterministically allows this <see cref="CosmicLocation"/> to be
        /// regenerated, given identical values for its other properties.
        /// </summary>
        public uint Seed { get; private protected set; }

        /// <summary>
        /// The shape of this location.
        /// </summary>
        public override IShape Shape
        {
            get => Material.Shape;
            private protected set => Material.Shape = value;
        }

        /// <summary>
        /// The type of this <see cref="CosmicLocation"/>.
        /// </summary>
        public CosmicStructureType StructureType { get; }

        private protected Number? _surfaceGravity;
        /// <summary>
        /// The average force of gravity at the surface of this object, in N.
        /// </summary>
        public Number SurfaceGravity => _surfaceGravity ??= Material.GetSurfaceGravity();

        /// <summary>
        /// The average temperature of this location's <see cref="Material"/>, in K.
        /// </summary>
        /// <returns>
        /// The average temperature of this location's <see cref="Material"/>, in K.
        /// </returns>
        /// <remarks>
        /// No less than the ambient temperature of its parent, if any.
        /// </remarks>
        public double Temperature => Material.Temperature ?? 0;

        /// <summary>
        /// The <see cref="CosmicLocation"/>'s <see cref="Name"/>, if it has one; otherwise its
        /// <see cref="Designation"/>.
        /// </summary>
        public string Title => Name ?? Designation;

        private string? _typeName;
        /// <summary>
        /// The name for this type of <see cref="CosmicLocation"/>.
        /// </summary>
        public string TypeName
        {
            get
            {
                if (string.IsNullOrEmpty(_typeName))
                {
                    var sb = new StringBuilder();
                    if (!string.IsNullOrEmpty(TypeNamePrefix))
                    {
                        sb.Append(TypeNamePrefix).Append(' ');
                    }
                    sb.Append(BaseTypeName);
                    if (!string.IsNullOrEmpty(TypeNameSuffix))
                    {
                        sb.Append(' ').Append(TypeNameSuffix);
                    }
                    _typeName = sb.ToString();
                }
                return _typeName;
            }
        }

        /// <summary>
        /// The velocity of the <see cref="CosmicLocation"/> in m/s.
        /// </summary>
        public Vector3 Velocity { get; internal set; }

        private protected virtual string BaseTypeName => StructureType switch
        {
            CosmicStructureType.Universe => "Universe",
            CosmicStructureType.Supercluster => "Supercluster",
            CosmicStructureType.GalaxyCluster => "Galaxy Cluster",
            CosmicStructureType.GalaxyGroup => "Galaxy Group",
            CosmicStructureType.GalaxySubgroup => "Galaxy Subgroup",
            CosmicStructureType.SpiralGalaxy => "Spiral Galaxy",
            CosmicStructureType.EllipticalGalaxy => "Elliptical Galaxy",
            CosmicStructureType.DwarfGalaxy => "Dwarf Galaxy",
            CosmicStructureType.GlobularCluster => "Globular Cluster",
            CosmicStructureType.Nebula => "Nebula",
            CosmicStructureType.HIIRegion => "HII Region",
            CosmicStructureType.PlanetaryNebula => "Planetary Nebula",
            CosmicStructureType.StarSystem => "Star System",
            CosmicStructureType.AsteroidField => Orbit.HasValue ? "Asteroid Belt" : "Asteroid Field",
            CosmicStructureType.OortCloud => "Oort Cloud",
            CosmicStructureType.Star => "Star",
            _ => "Location",
        };

        private protected virtual IEnumerable<ChildDefinition> ChildDefinitions => StructureType switch
        {
            CosmicStructureType.Universe => _UniverseChildDefinitions,
            CosmicStructureType.Supercluster => _SuperclusterChildDefinitions,
            CosmicStructureType.GalaxyCluster => _GalaxyClusterChildDefinitions,
            CosmicStructureType.GalaxySubgroup => _GalaxySubgroupChildDefinitions,
            CosmicStructureType.SpiralGalaxy => _GalaxyChildDefinitions,
            CosmicStructureType.EllipticalGalaxy => _EllipticalGalaxyChildDefinitions,
            CosmicStructureType.DwarfGalaxy => _GalaxyChildDefinitions,
            CosmicStructureType.GlobularCluster => _GlobularClusterChildDefinitions,
            CosmicStructureType.HIIRegion => _HIIRegionChildDefinitions,
            _ => Enumerable.Empty<ChildDefinition>(),
        };

        private protected virtual string DesignatorPrefix => string.Empty;

        private protected virtual string? TypeNamePrefix => null;

        private protected virtual string? TypeNameSuffix => null;

        private protected CosmicLocation(string? parentId, CosmicStructureType structureType) : base(parentId) => StructureType = structureType;

        private protected CosmicLocation(
            string id,
            uint seed,
            CosmicStructureType structureType,
            string? parentId,
            Vector3[]? absolutePosition,
            string? name,
            Vector3 velocity,
            Orbit? orbit) : base(id, parentId, absolutePosition)
        {
            Seed = seed;
            StructureType = structureType;
            Name = name;
            Orbit = orbit;
            Velocity = velocity;
        }

        /// <summary>
        /// Initialize a new instance of <see cref="CosmicLocation"/>.
        /// </summary>
        /// <param name="id">
        /// The unique ID of this item.
        /// </param>
        /// <param name="seed">
        /// A unique seed which can be used to deterministically recreate this instance.
        /// </param>
        /// <param name="structureType">
        /// The <see cref="CosmicStructureType"/> of this location.
        /// </param>
        /// <param name="parentId">
        /// The ID of the location which contains this one.
        /// </param>
        /// <param name="absolutePosition">
        /// <para>
        /// The position of this location, as a set of relative positions starting with the position
        /// of its outermost containing parent within the universe, down to the relative position of
        /// its most immediate parent.
        /// </para>
        /// <para>
        /// The location's own relative <see cref="Location.Position"/> is not expected to be
        /// included.
        /// </para>
        /// <para>
        /// May be <see langword="null"/> for a location with no containing parent, or whose parent
        /// is the universe itself (i.e. there is no intermediate container).
        /// </para>
        /// </param>
        /// <param name="name">
        /// An optional name for this <see cref="CosmicLocation"/>.
        /// </param>
        /// <param name="velocity">
        /// The velocity of the <see cref="CosmicLocation"/> in m/s.
        /// </param>
        /// <param name="orbit">
        /// The orbit occupied by this <see cref="CosmicLocation"/> (may be <see langword="null"/>).
        /// </param>
        /// <param name="position">
        /// The position of this <see cref="CosmicLocation"/>.
        /// </param>
        /// <param name="temperature">
        /// The temperature of this <see cref="CosmicLocation"/>, in K. May be <see
        /// langword="null"/>, which indicates that it shares the ambient temperature of its
        /// containing location (if any).
        /// </param>
        /// <remarks>
        /// Note: this constructor is most useful for deserializers. Consider using <see
        /// cref="New(CosmicStructureType, CosmicLocation?, Vector3, out List{CosmicLocation},
        /// OrbitalParameters?)"/> to generate a new instance instead.
        /// </remarks>
        internal CosmicLocation(
            string id,
            uint seed,
            CosmicStructureType structureType,
            string? parentId,
            Vector3[]? absolutePosition,
            string? name,
            Vector3 velocity,
            Orbit? orbit,
            Vector3 position,
            double? temperature) : this(id, seed, structureType, parentId, absolutePosition, name, velocity, orbit)
        {
            switch (structureType)
            {
                case CosmicStructureType.Universe:
                    ReconstituteUniverseInstance();
                    break;
                case CosmicStructureType.Supercluster:
                    ReconstituteSuperclusterInstance(position, temperature);
                    break;
                case CosmicStructureType.GalaxyCluster:
                    ReconstituteGalaxyClusterInstance(position, temperature);
                    break;
                case CosmicStructureType.GalaxyGroup:
                    ReconstituteGalaxyGroupInstance(position, temperature);
                    break;
                case CosmicStructureType.GalaxySubgroup:
                    ReconstituteGalaxySubgroupInstance(position, temperature);
                    break;
                case CosmicStructureType.SpiralGalaxy:
                case CosmicStructureType.EllipticalGalaxy:
                case CosmicStructureType.DwarfGalaxy:
                    ReconstituteGalaxyInstance(position, temperature);
                    break;
                case CosmicStructureType.GlobularCluster:
                    ReconstituteGlobularClusterInstance(position, temperature);
                    break;
                case CosmicStructureType.Nebula:
                case CosmicStructureType.HIIRegion:
                    ReconstituteNebulaInstance(position, temperature);
                    break;
                case CosmicStructureType.PlanetaryNebula:
                    ReconstitutePlanetaryNebulaInstance(position, temperature);
                    break;
            }
        }

        private CosmicLocation(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            (uint?)info.GetValue(nameof(Seed), typeof(uint)) ?? default,
            (CosmicStructureType?)info.GetValue(nameof(StructureType), typeof(CosmicStructureType)) ?? CosmicStructureType.Universe,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (Vector3?)info.GetValue(nameof(Velocity), typeof(Vector3)) ?? default,
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (Vector3?)info.GetValue(nameof(Position), typeof(Vector3)) ?? default,
            (double?)info.GetValue(nameof(Temperature), typeof(double?)))
        { }

        /// <summary>
        /// Generates a new <see cref="CosmicLocation"/> instance as a child of the given containing
        /// <paramref name="parent"/> location.
        /// </summary>
        /// <param name="structureType">The type of cosmic structure to generate.</param>
        /// <param name="parent">
        /// The containing parent location for which to generate a child.
        /// </param>
        /// <param name="position">The position for the child.</param>
        /// <param name="children">
        /// <para>
        /// When this method returns, will be set to a <see cref="List{T}"/> of <see
        /// cref="CosmicLocation"/>s containing any child objects generated for the location during
        /// the creation process.
        /// </para>
        /// <para>
        /// This list may be useful, for instance, to ensure that these additional objects are also
        /// persisted to data storage.
        /// </para>
        /// </param>
        /// <param name="orbit">
        /// <para>
        /// An optional orbit to assign to the child.
        /// </para>
        /// <para>
        /// Depending on the parameters, may override <paramref name="position"/>.
        /// </para>
        /// </param>
        /// <returns>
        /// <para>
        /// The generated child location.
        /// </para>
        /// <para>
        /// If no child could be generated, returns <see langword="null"/>.
        /// </para>
        /// </returns>
        public static CosmicLocation? New(
            CosmicStructureType structureType,
            CosmicLocation? parent,
            Vector3 position,
            out List<CosmicLocation> children,
            OrbitalParameters? orbit = null)
        {
            children = new List<CosmicLocation>();
            var starSystemChildren = new List<CosmicLocation>();
            var satellites = new List<Planetoid>();

            var instance = structureType switch
            {
                CosmicStructureType.Universe => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.Supercluster => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.GalaxyCluster => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.GalaxyGroup => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.GalaxySubgroup => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.Galaxy => Randomizer.Instance.NextDouble() <= 0.7
                    ? new CosmicLocation(parent?.Id, CosmicStructureType.SpiralGalaxy)
                    : new CosmicLocation(parent?.Id, CosmicStructureType.EllipticalGalaxy),
                CosmicStructureType.SpiralGalaxy => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.EllipticalGalaxy => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.DwarfGalaxy => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.GlobularCluster => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.Nebula => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.HIIRegion => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.PlanetaryNebula => new CosmicLocation(parent?.Id, structureType),
                CosmicStructureType.AnyNebula => new CosmicLocation(parent?.Id, CosmicStructureType.Nebula),
                CosmicStructureType.AsteroidField => new AsteroidField(parent, position, orbit),
                CosmicStructureType.OortCloud => new AsteroidField(parent, position, orbit, oort: true),
                CosmicStructureType.BlackHole => new BlackHole(parent, position, orbit),
                CosmicStructureType.StarSystem => new StarSystem(parent, position, out starSystemChildren, orbit),
                CosmicStructureType.Star => new Star(StarType.MainSequence, parent, position, orbit),
                CosmicStructureType.Planetoid => new Planetoid(PlanetType.Terrestrial, parent, null, new List<Star>(), position, out satellites, orbit),
                _ => null,
            };
            if (structureType == CosmicStructureType.StarSystem)
            {
                children.AddRange(starSystemChildren);
            }
            else if (structureType == CosmicStructureType.Planetoid)
            {
                children.AddRange(satellites);
            }

            if (instance is null)
            {
                return null;
            }
            if (structureType == CosmicStructureType.AsteroidField
                || structureType == CosmicStructureType.OortCloud
                || structureType == CosmicStructureType.BlackHole
                || structureType == CosmicStructureType.StarSystem
                || structureType == CosmicStructureType.Planetoid)
            {
                return instance;
            }

            switch (instance.StructureType)
            {
                case CosmicStructureType.Universe:
                    instance.ConfigureUniverseInstance();
                    break;
                case CosmicStructureType.Supercluster:
                    instance.ConfigureSuperclusterInstance(position, parent?.Material.Temperature);
                    break;
                case CosmicStructureType.GalaxyCluster:
                    instance.ConfigureGalaxyClusterInstance(position, parent?.Material.Temperature);
                    break;
                case CosmicStructureType.GalaxyGroup:
                    children.AddRange(instance.ConfigureGalaxyGroupInstance(position, parent?.Material.Temperature));
                    break;
                case CosmicStructureType.GalaxySubgroup:
                    var mainGalaxy = instance.ConfigureGalaxySubgroupInstance(position, out var subgroupChildren, parent?.Material.Temperature);
                    if (mainGalaxy is not null)
                    {
                        children.Add(mainGalaxy);
                        children.AddRange(subgroupChildren);
                    }
                    break;
                case CosmicStructureType.SpiralGalaxy:
                case CosmicStructureType.EllipticalGalaxy:
                case CosmicStructureType.DwarfGalaxy:
                    var galaxyCore = instance.ConfigureGalaxyInstance(position, parent?.Material.Temperature);
                    if (galaxyCore is not null)
                    {
                        children.Add(galaxyCore);
                    }
                    break;
                case CosmicStructureType.GlobularCluster:
                    var clusterCore = instance.ConfigureGlobularClusterInstance(position, parent?.Material.Temperature);
                    if (clusterCore is not null)
                    {
                        children.Add(clusterCore);
                    }
                    break;
                case CosmicStructureType.Nebula:
                case CosmicStructureType.HIIRegion:
                    instance.ConfigureNebulaInstance(position, parent?.Material.Temperature);
                    break;
                case CosmicStructureType.PlanetaryNebula:
                    var nebulaStar = instance.ConfigurePlanetaryNebulaInstance(position, parent?.Material.Temperature);
                    if (nebulaStar is not null)
                    {
                        children.Add(nebulaStar);
                    }
                    break;
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
                        CosmicStructureType.GalaxySubgroup => instance.Position.IsZero() ? null : parent.GetGalaxySubgroupChildOrbit(),
                        CosmicStructureType.SpiralGalaxy
                            or CosmicStructureType.EllipticalGalaxy
                            or CosmicStructureType.DwarfGalaxy => instance.Position.IsZero() ? (OrbitalParameters?)null : parent.GetGalaxyChildOrbit(),
                        CosmicStructureType.GlobularCluster => instance.Position.IsZero() ? (OrbitalParameters?)null : parent.GetGlobularClusterChildOrbit(),
                        CosmicStructureType.StarSystem => parent is StarSystem && !instance.Position.IsZero()
                            ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                            : (OrbitalParameters?)null,
                        _ => null,
                    };
                }
            }
            if (orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(instance, orbit.Value);
            }

            return instance;
        }

        /// <summary>
        /// Generates a new <see cref="CosmicLocation"/> as the containing parent location of the
        /// given <paramref name="child"/> location.
        /// </summary>
        /// <param name="structureType">The type of cosmic structure to generate.</param>
        /// <param name="child">The child location for which to generate a parent.</param>
        /// <param name="children">
        /// <para>
        /// When this method returns, will be set to a <see cref="List{T}"/> of <see
        /// cref="CosmicLocation"/>s containing any child objects generated for the location during
        /// the creation process.
        /// </para>
        /// <para>
        /// This list may be useful, for instance, to ensure that these additional objects are also
        /// persisted to data storage.
        /// </para>
        /// </param>
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
            CosmicStructureType structureType,
            CosmicLocation child,
            out List<CosmicLocation> children,
            Vector3? position = null,
            OrbitalParameters? orbit = null)
        {
            children = new List<CosmicLocation>();
            var starSystemChildren = new List<CosmicLocation>();

            var instance = structureType switch
            {
                CosmicStructureType.Universe => new CosmicLocation(null, structureType),
                CosmicStructureType.Supercluster => new CosmicLocation(null, structureType),
                CosmicStructureType.GalaxyCluster => new CosmicLocation(null, structureType),
                CosmicStructureType.GalaxyGroup => new CosmicLocation(null, structureType),
                CosmicStructureType.GalaxySubgroup => new CosmicLocation(null, structureType),
                CosmicStructureType.Galaxy => Randomizer.Instance.NextDouble() <= 0.7
                    ? new CosmicLocation(null, CosmicStructureType.SpiralGalaxy)
                    : new CosmicLocation(null, CosmicStructureType.EllipticalGalaxy),
                CosmicStructureType.SpiralGalaxy => new CosmicLocation(null, structureType),
                CosmicStructureType.EllipticalGalaxy => new CosmicLocation(null, structureType),
                CosmicStructureType.DwarfGalaxy => new CosmicLocation(null, structureType),
                CosmicStructureType.GlobularCluster => new CosmicLocation(null, structureType),
                CosmicStructureType.Nebula => new CosmicLocation(null, structureType),
                CosmicStructureType.HIIRegion => new CosmicLocation(null, structureType),
                CosmicStructureType.PlanetaryNebula => new CosmicLocation(null, structureType),
                CosmicStructureType.AnyNebula => new CosmicLocation(null, CosmicStructureType.Nebula),
                CosmicStructureType.AsteroidField => AsteroidField.GetParentForChild(child, position, orbit),
                CosmicStructureType.OortCloud => AsteroidField.GetParentForChild(child, position, orbit, oort: true),
                CosmicStructureType.StarSystem => StarSystem.GetParentForChild(child, out starSystemChildren, position),
                _ => null,
            };
            if (structureType == CosmicStructureType.StarSystem)
            {
                children.AddRange(starSystemChildren);
            }

            if (instance is null)
            {
                return null;
            }
            if (structureType == CosmicStructureType.AsteroidField
                || structureType == CosmicStructureType.OortCloud
                || structureType == CosmicStructureType.StarSystem)
            {
                return instance;
            }

            child.AssignParent(instance);

            switch (instance.StructureType)
            {
                case CosmicStructureType.Universe:
                    instance.ConfigureUniverseInstance();
                    break;
                case CosmicStructureType.Supercluster:
                    instance.ConfigureSuperclusterInstance(Vector3.Zero);
                    break;
                case CosmicStructureType.GalaxyCluster:
                    instance.ConfigureGalaxyClusterInstance(Vector3.Zero);
                    break;
                case CosmicStructureType.GalaxyGroup:
                    children.AddRange(instance.ConfigureGalaxyGroupInstance(Vector3.Zero, null, child));
                    break;
                case CosmicStructureType.GalaxySubgroup:
                    var mainGalaxy = instance.ConfigureGalaxySubgroupInstance(Vector3.Zero, out var subgroupChildren, null, child);
                    if (mainGalaxy is not null)
                    {
                        children.Add(mainGalaxy);
                        children.AddRange(subgroupChildren);
                    }
                    break;
                case CosmicStructureType.SpiralGalaxy:
                case CosmicStructureType.EllipticalGalaxy:
                case CosmicStructureType.DwarfGalaxy:
                    var galacticCore = instance.ConfigureGalaxyInstance(Vector3.Zero, null, child);
                    if (galacticCore is not null)
                    {
                        children.Add(galacticCore);
                    }
                    break;
                case CosmicStructureType.GlobularCluster:
                    var clusterCore = instance.ConfigureGlobularClusterInstance(Vector3.Zero, null, child);
                    if (clusterCore is not null)
                    {
                        children.Add(clusterCore);
                    }
                    break;
                case CosmicStructureType.Nebula:
                case CosmicStructureType.HIIRegion:
                    instance.ConfigureNebulaInstance(Vector3.Zero);
                    break;
                case CosmicStructureType.PlanetaryNebula:
                    var star = instance.ConfigurePlanetaryNebulaInstance(Vector3.Zero, null, child);
                    if (star is not null)
                    {
                        children.Add(star);
                    }
                    break;
            }

            if (!position.HasValue)
            {
                if (child.StructureType == CosmicStructureType.Universe)
                {
                    position = Vector3.Zero;
                }
                else if (instance.StructureType == CosmicStructureType.GalaxySubgroup
                    && (CosmicStructureType.SpiralGalaxy | CosmicStructureType.EllipticalGalaxy).HasFlag(child.StructureType))
                {
                    position = Vector3.Zero;
                }
                else if ((CosmicStructureType.SpiralGalaxy | CosmicStructureType.EllipticalGalaxy).HasFlag(instance.StructureType)
                    && child.StructureType == CosmicStructureType.BlackHole
                    && child.Mass > BlackHole.SupermassiveBlackHoleThreshold)
                {
                    position = Vector3.Zero;
                }
                else if ((CosmicStructureType.DwarfGalaxy | CosmicStructureType.GlobularCluster).HasFlag(instance.StructureType)
                    && child.StructureType == CosmicStructureType.BlackHole)
                {
                    position = Vector3.Zero;
                }
                else if (instance.StructureType == CosmicStructureType.PlanetaryNebula
                    && child is Star star
                    && star.StarType == StarType.WhiteDwarf)
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
                        CosmicStructureType.AsteroidField => AsteroidField.AsteroidFieldSpace,
                        CosmicStructureType.OortCloud => AsteroidField.OortCloudSpace,
                        CosmicStructureType.BlackHole => BlackHole.BlackHoleSpace,
                        CosmicStructureType.Star => StarSystem.StarSystemSpace,
                        CosmicStructureType.Planetoid => Planetoid.GiantSpace,
                        _ => Number.Zero,
                    };
                    position = instance.GetOpenSpace(space, children.Select(x => x as Location).ToList());
                }
            }
            if (position.HasValue)
            {
                child.Position = position.Value;
            }

            if (!child.Orbit.HasValue)
            {
                if (!orbit.HasValue)
                {
                    orbit = instance.StructureType switch
                    {
                        CosmicStructureType.GalaxySubgroup => child.Position.IsZero() ? null : instance.GetGalaxySubgroupChildOrbit(),
                        CosmicStructureType.SpiralGalaxy
                            or CosmicStructureType.EllipticalGalaxy
                            or CosmicStructureType.DwarfGalaxy => child.Position.IsZero() ? (OrbitalParameters?)null : instance.GetGalaxyChildOrbit(),
                        CosmicStructureType.GlobularCluster => child.Position.IsZero() ? (OrbitalParameters?)null : instance.GetGlobularClusterChildOrbit(),
                        CosmicStructureType.StarSystem => instance is StarSystem && !instance.Position.IsZero()
                            ? OrbitalParameters.GetFromEccentricity(instance.Mass, instance.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                            : (OrbitalParameters?)null,
                        _ => null,
                    };
                }
                if (orbit.HasValue)
                {
                    Space.Orbit.AssignOrbit(child, orbit.Value);
                }
            }

            return instance;
        }

        /// <summary>
        /// Generates a random child in this location.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="condition">
        /// An optional <see cref="ChildDefinition"/> the child must match.
        /// </param>
        /// <returns>
        /// A randomly generated child, or <see langword="null"/> if no child could be generated.
        /// This might occur if no children occur in this location, or if insufficient free space
        /// remains.
        /// </returns>
        public async Task<(CosmicLocation? child, List<CosmicLocation> subChildren)> GenerateChildAsync(IDataStore dataStore, ChildDefinition? condition = null)
        {
            await foreach (var item in GenerateChildrenAsync(dataStore, 1, condition))
            {
                return item;
            }
            return (null, new List<CosmicLocation>());
        }

        /// <summary>
        /// Generates a random child of the given <paramref name="type"/> in this location.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="type">
        /// The type of child to generate.
        /// </param>
        /// <returns>
        /// <para>
        /// A randomly generated child, or <see langword="null"/> if no child could be generated.
        /// This might occur if no children occur in this location, or if insufficient free space
        /// remains.
        /// </para>
        /// <para>
        /// Also, a <see cref="List{T}"/> of child <see cref="CosmicLocation"/>s which may have been
        /// generated as part of its creation process. This list may be useful, for instance, to
        /// ensure that all such sub-entities are also persisted to data storage.
        /// </para>
        /// </returns>
        public async Task<(CosmicLocation? child, List<CosmicLocation> subChildren)> GenerateChildAsync(IDataStore dataStore, CosmicStructureType type)
        {
            var space = type switch
            {
                CosmicStructureType.Universe => Number.PositiveInfinity,
                CosmicStructureType.Supercluster => _SuperclusterSpace,
                CosmicStructureType.GalaxyCluster => _GalaxyClusterSpace,
                CosmicStructureType.GalaxyGroup => _GalaxyGroupSpace,
                CosmicStructureType.SpiralGalaxy => _GalaxySpace,
                CosmicStructureType.EllipticalGalaxy => _GalaxySpace,
                CosmicStructureType.DwarfGalaxy => _DwarfGalaxySpace,
                CosmicStructureType.Galaxy => _GalaxySpace,
                CosmicStructureType.GlobularCluster => _GlobularClusterSpace,
                CosmicStructureType.Nebula => _NebulaSpace,
                CosmicStructureType.HIIRegion => _NebulaSpace,
                CosmicStructureType.PlanetaryNebula => _PlanetaryNebulaSpace,
                CosmicStructureType.AnyNebula => _NebulaSpace,
                CosmicStructureType.StarSystem => StarSystem.StarSystemSpace,
                CosmicStructureType.AsteroidField => AsteroidField.AsteroidFieldSpace,
                CosmicStructureType.OortCloud => AsteroidField.OortCloudSpace,
                CosmicStructureType.BlackHole => BlackHole.BlackHoleSpace,
                CosmicStructureType.Star => StarSystem.StarSystemSpace,
                CosmicStructureType.Planetoid => Planetoid.GiantSpace,
                _ => Number.Zero,
            };

            await foreach (var item in GenerateChildrenAsync(dataStore, 1, new ChildDefinition(space, Number.Zero, type)))
            {
                return item;
            }
            return (null, new List<CosmicLocation>());
        }

        /// <summary>
        /// Generates a random child of the indicated type at a random distance from the given
        /// <paramref name="position"/>, appropriate to its specified density.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="condition">
        /// An optional <see cref="ChildDefinition"/> the child must match.
        /// </param>
        /// <param name="position">
        /// The position near which the child is to be generated.
        /// </param>
        /// <returns>
        /// <para>
        /// A randomly generated child, or <see langword="null"/> if no child could be generated.
        /// This might occur if no children occur in this location, or if insufficient free space
        /// remains.
        /// </para>
        /// <para>
        /// Also, a <see cref="List{T}"/> of child <see cref="CosmicLocation"/>s which may have been
        /// generated as part of its creation process. This list may be useful, for instance, to
        /// ensure that all such sub-entities are also persisted to data storage.
        /// </para>
        /// </returns>
        /// <remarks>
        /// The distance is chosen by assuming that a hypothetical object of the given type already
        /// exists at the given position, then selecting a new position in a random direction, at a
        /// distance indicated by the child's specified density within this location's volume. The
        /// exact distance is adjusted according to a normal distribution, with the sigma set as one
        /// sixth of the ideal distance.
        /// </remarks>
        public async Task<(CosmicLocation? child, List<CosmicLocation> subChildren)> GenerateChildNearAsync(IDataStore dataStore, ChildDefinition condition, Vector3 position)
        {
            var childTotals = GetChildTotals(condition: condition).ToList();
            var childAmounts = new List<(ChildDefinition def, Number rem)>();
            var children = new List<Location>();
            await foreach (var child in GetChildrenAsync<CosmicLocation>(dataStore))
            {
                foreach (var (totalType, totalAmount) in childTotals)
                {
                    if (totalType.IsSatisfiedBy(child))
                    {
                        var found = false;
                        for (var i = 0; i < childAmounts.Count; i++)
                        {
                            if (childAmounts[i].def.IsSatisfiedBy(totalType))
                            {
                                found = true;
                                childAmounts[i] = (childAmounts[i].def, childAmounts[i].rem - 1);
                            }
                        }
                        if (!found)
                        {
                            childAmounts.Add((totalType, totalAmount - 1));
                        }
                    }
                }
                children.Add(child);
            }

            var defs = childAmounts
                .Select(x => (x.def, weight: (double)(Number.One / x.def.Density), x.rem))
                .ToList();
            while (defs.Count > 0
                && defs.Sum(x => x.rem).IsPositive)
            {
                var index = Randomizer.Instance.NextIndex(defs, x => x.weight);
                var def = defs[index];
                defs.RemoveAt(index);

                var idealDistance = (double)(2 * (def.def.Density * MathConstants.ThreeQuartersPI).CubeRoot());
                var distance = Randomizer.Instance.NormalDistributionSample(idealDistance, idealDistance / 6);

                var insanityCheck = 0;
                Vector3? childPosition;
                do
                {
                    childPosition = position + Randomizer.Instance.NextVector3Number(distance);
                    if (childPosition.Value.Length() + def.def.Space > Shape.ContainingRadius)
                    {
                        childPosition = null;
                    }
                    else
                    {
                        var shape = new Sphere(def.def.Space, childPosition.Value);
                        var any = false;
                        foreach (var c in children)
                        {
                            if (c.Shape.Intersects(shape))
                            {
                                any = true;
                                break;
                            }
                        }
                        if (any)
                        {
                            childPosition = null;
                        }
                    }
                    insanityCheck++;
                } while (!childPosition.HasValue && insanityCheck < 100);
                if (childPosition.HasValue)
                {
                    var child = def.def.GetChild(this, childPosition.Value, out var subChildren);
                    if (child is not null)
                    {
                        return (child, subChildren);
                    }
                }
            }
            return (null, new List<CosmicLocation>());
        }

        /// <summary>
        /// Generates new child entities within this <see cref="CosmicLocation"/>.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="total">
        /// The maximum total number of children generated. Since a large, dense region can
        /// potentially contains billions or even trillions of children, this ensures that the
        /// enumeration does not continue indefinitely.
        /// </param>
        /// <param name="condition">
        /// An optional <see cref="ChildDefinition"/> which generated children must match.
        /// </param>
        /// <remarks>
        /// <para>
        /// Each result contains the generated child, along with a <see cref="List{T}"/> of child
        /// <see cref="CosmicLocation"/>s which may have been generated as part of its creation
        /// process. This list may be useful, for instance, to ensure that all such sub-entities are
        /// also persisted to data storage.
        /// </para>
        /// <para>
        /// If the region is small, the space and density requirements of the defined child types
        /// may result in no children being generated at all.
        /// </para>
        /// <para>
        /// See <see cref="GetRadiusWithChildren(int, ChildDefinition)"/> and <see
        /// cref="GenerateChildrenAsync(IDataStore, Location, int, ChildDefinition)"/>, which can be
        /// used in combination to generate children only in a sub-region of predetermined size
        /// suitable to the number of children desired.
        /// </para>
        /// <para>
        /// See also <see cref="GenerateChildNearAsync(IDataStore, ChildDefinition, Vector3)"/>,
        /// which can be used to generate a particular type of child in a specific region of a
        /// location's space.
        /// </para>
        /// </remarks>
        public IAsyncEnumerable<(CosmicLocation child, List<CosmicLocation> subChildren)> GenerateChildrenAsync(IDataStore dataStore, int total = 10, ChildDefinition? condition = null)
            => GenerateChildrenAsync(dataStore, GetChildTotals(condition: condition).ToList(), total);

        /// <summary>
        /// Generates and returns new child entities within this <see cref="CosmicLocation"/>,
        /// inside the boundaries of the given <paramref name="location"/>.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="location">The location within which children will be generated.</param>
        /// <param name="total">
        /// The maximum total number of children generated. Since a large, dense region can
        /// potentially contains billions or even trillions of children, this ensures that the
        /// enumeration does not continue indefinitely.
        /// </param>
        /// <param name="condition">
        /// An optional <see cref="ChildDefinition"/> which generated children must match.
        /// </param>
        /// <remarks>
        /// <para>
        /// Each result contains the generated child, along with a <see cref="List{T}"/> of child
        /// <see cref="CosmicLocation"/>s which may have been generated as part of its creation
        /// process. This list may be useful, for instance, to ensure that all such sub-entities are
        /// also persisted to data storage.
        /// </para>
        /// <para>
        /// If the region is small, the space and density requirements of the defined child types
        /// may result in no children being generated at all.
        /// </para>
        /// <para>
        /// See <see cref="GetRadiusWithChildren(int, ChildDefinition)"/>, which can be used in
        /// combination with this method to get children only in a sub-region of predetermined size
        /// suitable to the number of children desired.
        /// </para>
        /// <para>
        /// See also <see cref="GenerateChildNearAsync(IDataStore, ChildDefinition, Vector3)"/>,
        /// which can be used to generate a particular type of child in a specific region of a
        /// location's space.
        /// </para>
        /// </remarks>
        public IAsyncEnumerable<(CosmicLocation child, List<CosmicLocation> subChildren)> GenerateChildrenAsync(
            IDataStore dataStore,
            Location location,
            int total = 10,
            ChildDefinition? condition = null)
            => GenerateChildrenAsync(dataStore, GetChildTotals(location, condition).ToList(), total);

        /// <summary>
        /// Enumerates the children of this instance of the given type.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="structureType">
        /// <para>
        /// The <see cref="CosmicStructureType"/> to retrieve.
        /// </para>
        /// <para>
        /// Note: the given structure type must match the child's exactly. Although <see
        /// cref="CosmicStructureType"/> is a <see cref="FlagsAttribute"/> enum, combination values
        /// will not return children of any included member.
        /// </para>
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of child <see cref="CosmicLocation"/> instances of this
        /// one.
        /// </returns>
        public IAsyncEnumerable<CosmicLocation> GetChildrenAsync(IDataStore dataStore, CosmicStructureType structureType)
            => dataStore.Query<CosmicLocation>().Where(x => x.ParentId == Id && x.StructureType == structureType).AsAsyncEnumerable();

        /// <summary>
        /// Calculates the total number of children in this region. The totals are approximate,
        /// based on the defined densities of the possible children it might have.
        /// </summary>
        /// <param name="location">
        /// <para>
        /// An optional location within this one within which to determine the totals. If omitted,
        /// the totals for the entire area are calculated.
        /// </para>
        /// <para>
        /// If the given location does not intersect this one, the totals will all be zero.
        /// </para>
        /// </param>
        /// <param name="condition">
        /// An optional <see cref="ChildDefinition"/> which children must match.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="ChildDefinition"/> instances along with
        /// the total number of children present in this region (as a <see cref="Number"/> due to
        /// the potentially vast numbers involved).
        /// </returns>
        public IEnumerable<(ChildDefinition type, Number total)> GetChildTotals(Location? location = null, ChildDefinition? condition = null)
        {
            if (location is null)
            {
                return condition is null
                    ? ChildDefinitions.Select(x => (x, Shape.Volume * x.Density))
                    : ChildDefinitions
                        .Where(x => condition.IsSatisfiedBy(x))
                        .Select(x => (x, Shape.Volume * x.Density));
            }

            var commonAbsolutePosition = GetCommonAbsolutePosition(location);
            if (commonAbsolutePosition is null
                || commonAbsolutePosition.Length == 0
                || commonAbsolutePosition[^1] != Position)
            {
                return Enumerable.Empty<(ChildDefinition, Number)>();
            }

            return condition is null
                ? ChildDefinitions
                    .Select(x => (x, location.Shape.Volume * x.Density))
                : ChildDefinitions
                    .Where(x => condition.IsSatisfiedBy(x))
                    .Select(x => (x, location.Shape.Volume * x.Density));
        }

        /// <summary>
        /// Calculates the escape velocity from this location, in m/s.
        /// </summary>
        /// <returns>The escape velocity from this location, in m/s.</returns>
        public Number GetEscapeVelocity() => Number.Sqrt(ScienceConstants.TwoG * Mass / Shape.ContainingRadius);

        /// <summary>
        /// Calculates the force of gravity on this <see cref="CosmicLocation"/> from another as
        /// a vector, in N.
        /// </summary>
        /// <param name="other">A <see cref="CosmicLocation"/> from which the force gravity will
        /// be calculated. If <see langword="null"/>, or if the two do not share a common parent,
        /// the result will be zero.</param>
        /// <returns>
        /// The force of gravity from this <see cref="CosmicLocation"/> to the other, in N, as a
        /// vector.
        /// </returns>
        /// <exception cref="Exception">
        /// An exception will be thrown if the two <see cref="CosmicLocation"/> instances do not
        /// share a <see cref="Location"/> parent at some point.
        /// </exception>
        /// <remarks>
        /// Newton's law is used.
        /// </remarks>
        public Vector3 GetGravityFromObject(CosmicLocation? other)
        {
            if (other is null)
            {
                return Vector3.Zero;
            }

            var distance = GetDistanceTo(other);

            if (distance.IsFinite)
            {
                return Vector3.Zero;
            }

            var scale = -ScienceConstants.G * (Mass * other.Mass / (distance * distance));

            // Get the normalized vector
            var normalized = (other.Position - Position) / distance;

            return normalized * scale;
        }

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
            info.AddValue(nameof(Temperature), Material.Temperature);
        }

        /// <summary>
        /// <para>
        /// Calculates the position of this <see cref="CosmicLocation"/> after the given amount of
        /// time has passed since the theoretical moment at which its current position was defined,
        /// taking its orbit or velocity into account.
        /// </para>
        /// <para>
        /// The location's position is not actually changed by this calculation.
        /// </para>
        /// </summary>
        /// <param name="time">The amount of time after which to get a position.</param>
        /// <returns>
        /// A <see cref="Vector3"/> representing position relative to the center of the parent.
        /// </returns>
        /// <remarks>
        /// Note: this does not take the motion of the orbited object into account, or any other
        /// motions of an orbital hierarchy which might exist, nor does it perform integration over
        /// time of gravitational influences other than those reflected by the <see cref="Orbit"/>.
        /// </remarks>
        public Vector3 GetPositionAfterDuration(Duration time)
        {
            if (Orbit.HasValue)
            {
                var (position, _) = Orbit.Value.GetStateVectorsAtTime(new Instant(Orbit.Value.Epoch + time));

                return Orbit.Value.OrbitedPosition + position;
            }

            return Position + (Velocity * time.ToSeconds());
        }

        /// <summary>
        /// <para>
        /// Calculates the position of this <see cref="CosmicLocation"/> at the given time, taking
        /// its orbit into account, without actually updating its current position.
        /// </para>
        /// <para>
        /// Does not take <see cref="Velocity"/> into account, as the difference between the
        /// theoretical moment at which its position was defined and the given <paramref
        /// name="moment"/> cannot be known.
        /// </para>
        /// <para>
        /// Also does not perform integration over time of gravitational influences other than those
        /// reflected by the <see cref="Orbit"/>.
        /// </para>
        /// </summary>
        /// <param name="moment">The time at which to get a position.</param>
        /// <returns>
        /// A <see cref="Vector3"/> representing position relative to the center of the parent.
        /// </returns>
        public Vector3 GetPositionAtTime(Instant moment)
        {
            if (!Orbit.HasValue)
            {
                return Position;
            }

            var (position, _) = Orbit.Value.GetStateVectorsAtTime(moment);

            return Orbit.Value.OrbitedPosition + position;
        }

        /// <summary>
        /// Calculates the radius of a spherical region which contains at most the given amount of
        /// child entities, given the densities of the child definitions for this region.
        /// </summary>
        /// <param name="maxAmount">
        /// The maximum desired number of child entities in the region.
        /// </param>
        /// <param name="condition">
        /// An optional <see cref="ChildDefinition"/> the children must match.
        /// </param>
        /// <returns>
        /// The radius of a spherical region containing at most the given amount of children, in
        /// meters. May be zero, if this location does not contain children of the given type.
        /// </returns>
        public Number GetRadiusWithChildren(int maxAmount, ChildDefinition? condition = null)
        {
            if (maxAmount <= 0)
            {
                return 0;
            }
            var numInM3 = condition is null
                ? ChildDefinitions.Sum(x => x.Density)
                : ChildDefinitions
                    .Where(x => condition.IsSatisfiedBy(x))
                    .Sum(x => x.Density);
            var v = maxAmount / numInM3;
            // The number in a single m³ may be so small that this goes to infinity; if so, perform
            // the calculation in reverse.
            if (v.IsInfinite)
            {
                var total = Shape.Volume * numInM3;
                var ratio = maxAmount / total;
                v = Shape.Volume * ratio;
            }
            return (3 * v / MathConstants.FourPI).CubeRoot();
        }

        /// <summary>
        /// Calculates the total force of gravity on this <see cref="CosmicLocation"/>, in N, as
        /// a vector. Note that results may be highly inaccurate if the parent region has not been
        /// populated thoroughly enough in the vicinity of this entity (with the scale of "vicinity"
        /// depending strongly on the mass of the region's potential children).
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <returns>
        /// The total force of gravity on this <see cref="CosmicLocation"/> from all
        /// currently-generated children, in N, as a vector.
        /// </returns>
        /// <remarks>
        /// Newton's law is used. Children of sibling objects are not counted individually; instead
        /// the entire sibling is treated as a single entity, with total mass including all its
        /// children. Objects outside the parent are ignored entirely, assuming they are either too
        /// far to be of significance, or operate in a larger frame of reference (e.g. the Earth
        /// moves within the gravity of the Milky Way, but when determining its movement within the
        /// solar system, the effects of the greater galaxy are not relevant).
        /// </remarks>
        public async Task<Vector3> GetTotalLocalGravityAsync(IDataStore dataStore)
        {
            var totalGravity = Vector3.Zero;

            var parent = await GetParentAsync(dataStore).ConfigureAwait(false);
            // No gravity for a parent-less object
            if (parent is null)
            {
                return totalGravity;
            }

            await foreach (var sibling in parent.GetAllChildrenAsync<CosmicLocation>(dataStore))
            {
                totalGravity += GetGravityFromObject(sibling);
            }

            return totalGravity;
        }

        /// <summary>
        /// Sets the orbit occupied by this <see cref="CosmicLocation"/> (may be null).
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="value">An <see cref="Orbit"/>.</param>
        public ValueTask SetOrbitAsync(IDataStore dataStore, Orbit? value)
        {
            Orbit = value;
            return ResetOrbitAsync(dataStore);
        }

        /// <summary>
        /// Sets the <see cref="Shape"/> of this location.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="shape">The new shape.</param>
        /// <remarks>
        /// Note: clears any previously assigned <see cref="Orbit"/> if the shape's <see
        /// cref="IShape.Position"/> is not the same as the current position of the location.
        /// </remarks>
        public override async Task SetShapeAsync(IDataStore dataStore, IShape shape)
        {
            if (shape.Position != Position)
            {
                await SetOrbitAsync(dataStore, null).ConfigureAwait(false);
            }
            Shape = shape;
            _radiusSquared = null;
            _surfaceGravity = null;
        }

        /// <summary>
        /// Sets the <see cref="Velocity"/> of this location.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="velocity">The new velocity.</param>
        /// <remarks>
        /// Note: clears any previously assigned <see cref="Orbit"/> if the <paramref
        /// name="velocity"/> is not the same as the current velocity.
        /// </remarks>
        public async Task SetVelocityAsync(IDataStore dataStore, Vector3 velocity)
        {
            if (Velocity != velocity)
            {
                await SetOrbitAsync(dataStore, null).ConfigureAwait(false);
            }
            Velocity = velocity;
        }

        /// <summary>
        /// Returns a string that represents the celestial object.
        /// </summary>
        /// <returns>A string that represents the celestial object.</returns>
        public override string ToString() => string.IsNullOrEmpty(Name)
            ? Designation
            : $"{TypeName} {Name}";

        internal Number GetHillSphereRadius() => Orbit?.GetHillSphereRadius(Mass) ?? Number.Zero;

        internal Number GetRocheLimit(Number orbitingDensity)
            => new Number(8947, -4) * (Mass / orbitingDensity).CubeRoot();

        internal virtual ValueTask ResetOrbitAsync(IDataStore dataStore) => new();

        private protected override void AssignPosition(Vector3 position)
            => Position = position;

        private CosmicLocation? GenerateChild(ChildDefinition definition, List<Location> children, out List<CosmicLocation> newChildren)
        {
            var position = GetOpenSpace(definition.Space, children);
            if (!position.HasValue)
            {
                newChildren = new List<CosmicLocation>();
                return null;
            }
            return definition.GetChild(this, position.Value, out newChildren);
        }

        private async IAsyncEnumerable<(CosmicLocation child, List<CosmicLocation> subChildren)> GenerateChildrenAsync(
            IDataStore dataStore,
            List<(ChildDefinition totalType, Number totalAmount)> childTotals,
            int total = 10)
        {
            var childAmounts = new List<(ChildDefinition def, Number rem)>();
            var children = new List<Location>();
            await foreach (var child in GetChildrenAsync<CosmicLocation>(dataStore))
            {
                foreach (var (totalType, totalAmount) in childTotals)
                {
                    if (totalType.IsSatisfiedBy(child))
                    {
                        var found = false;
                        for (var i = 0; i < childAmounts.Count; i++)
                        {
                            if (childAmounts[i].def.IsSatisfiedBy(totalType))
                            {
                                found = true;
                                childAmounts[i] = (childAmounts[i].def, childAmounts[i].rem - 1);
                            }
                        }
                        if (!found)
                        {
                            childAmounts.Add((totalType, totalAmount - 1));
                        }
                    }
                }
                children.Add(child);
            }
            childAmounts.AddRange(childTotals.Where(x => !childAmounts.Any(y => x.totalType.IsSatisfiedBy(y.def))));

            var defs = childAmounts
                .Select(x => (x.def, weight: (double)(Number.One / x.def.Density), x.rem))
                .ToList();
            while (total > 0
                && defs.Count > 0
                && defs.Sum(x => x.rem).IsPositive)
            {
                var index = Randomizer.Instance.NextIndex(defs, x => x.weight);
                var def = defs[index];
                defs.RemoveAt(index);

                var child = GenerateChild(def.def, children, out var subChildren);
                if (child is not null)
                {
                    var rem = def.rem - Number.One;
                    if (rem.IsPositive)
                    {
                        defs.Insert(index, (def.def, def.weight, rem));
                    }
                    total--;
                    yield return (child, subChildren);
                }
            }
        }

        private protected override ValueTask ResetPosition(IDataStore dataStore) => SetOrbitAsync(dataStore, null);
    }
}
