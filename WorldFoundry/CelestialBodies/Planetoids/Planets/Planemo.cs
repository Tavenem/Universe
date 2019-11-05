using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets
{
    /// <summary>
    /// Any planetary-mass object (massive enough to be rounded under its own gravity), such as dwarf planets, some moons, and planets.
    /// </summary>
    [Serializable]
    public class Planemo : Planetoid
    {
        private static readonly Number _IcyRingDensity = 300;
        private static readonly Number _RockyRingDensity = 1380;

        private protected List<PlanetaryRing>? _rings;
        /// <summary>
        /// The collection of rings around this <see cref="Planemo"/>.
        /// </summary>
        public List<PlanetaryRing> Rings => _rings ??= new List<PlanetaryRing>();

        /// <summary>
        /// The name for this type of <see cref="CelestialLocation"/>.
        /// </summary>
        public override string TypeName
        {
            get
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(PlanemoClassPrefix))
                {
                    sb.Append(PlanemoClassPrefix);
                    sb.Append(" ");
                }
                sb.Append(BaseTypeName);
                return sb.ToString();
            }
        }

        private protected override string BaseTypeName => "Planet";

        // Set to 5 for Planemo. For reference, Pluto has 5 moons, the most of any
        // planemo in the Solar System apart from the giants. No others are known to have more than 2.
        private protected override int MaxSatellites => 5;

        private protected virtual string? PlanemoClassPrefix => null;

        private protected virtual double RingChance => 0;

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/>.
        /// </summary>
        internal Planemo() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="Planemo"/>.</param>
        internal Planemo(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="Planemo"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planemo"/> during random generation, in kg.
        /// </param>
        internal Planemo(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private protected Planemo(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            double normalizedSeaLevel,
            int seed1,
            int seed2,
            int seed3,
            int seed4,
            int seed5,
            double? angleOfRotation,
            Atmosphere? atmosphere,
            double? axialPrecession,
            bool? hasMagnetosphere,
            double? maxElevation,
            Number? rotationalOffset,
            Number? rotationalPeriod,
            List<Resource>? resources,
            List<string>? satelliteIds,
            List<SurfaceRegion>? surfaceRegions,
            Number? maxMass,
            Orbit? orbit,
            IMaterial? material,
            List<PlanetaryRing>? rings,
            string? parentId,
            byte[]? depthMap,
            byte[]? elevationMap,
            byte[]? flowMap,
            byte[][]? precipitationMaps,
            byte[][]? snowfallMaps,
            byte[]? temperatureMapSummer,
            byte[]? temperatureMapWinter,
            double? maxFlow)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                normalizedSeaLevel,
                seed1,
                seed2,
                seed3,
                seed4,
                seed5,
                angleOfRotation,
                atmosphere,
                axialPrecession,
                hasMagnetosphere,
                maxElevation,
                rotationalOffset,
                rotationalPeriod,
                resources,
                satelliteIds,
                surfaceRegions,
                maxMass,
                orbit,
                material,
                parentId,
                depthMap,
                elevationMap,
                flowMap,
                precipitationMaps,
                snowfallMaps,
                temperatureMapSummer,
                temperatureMapWinter,
                maxFlow) => _rings = rings;

        private Planemo(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (double)info.GetValue(nameof(_normalizedSeaLevel), typeof(double)),
            (int)info.GetValue(nameof(_seed1), typeof(int)),
            (int)info.GetValue(nameof(_seed2), typeof(int)),
            (int)info.GetValue(nameof(_seed3), typeof(int)),
            (int)info.GetValue(nameof(_seed4), typeof(int)),
            (int)info.GetValue(nameof(_seed5), typeof(int)),
            (double?)info.GetValue(nameof(_angleOfRotation), typeof(double?)),
            (Atmosphere?)info.GetValue(nameof(Atmosphere), typeof(Atmosphere)),
            (double?)info.GetValue(nameof(_axialPrecession), typeof(double?)),
            (bool?)info.GetValue(nameof(HasMagnetosphere), typeof(bool?)),
            (double?)info.GetValue(nameof(MaxElevation), typeof(double?)),
            (Number?)info.GetValue(nameof(RotationalOffset), typeof(Number?)),
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number?)),
            (List<Resource>?)info.GetValue(nameof(Resources), typeof(List<Resource>)),
            (List<string>?)info.GetValue(nameof(_satelliteIDs), typeof(List<string>)),
            (List<SurfaceRegion>?)info.GetValue(nameof(SurfaceRegions), typeof(List<SurfaceRegion>)),
            (Number?)info.GetValue(nameof(MaxMass), typeof(Number?)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (List<PlanetaryRing>?)info.GetValue(nameof(Rings), typeof(List<PlanetaryRing>)),
            (string)info.GetValue(nameof(ParentId), typeof(string)),
            (byte[])info.GetValue(nameof(_depthMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_elevationMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_flowMap), typeof(byte[])),
            (byte[][])info.GetValue(nameof(_precipitationMaps), typeof(byte[][])),
            (byte[][])info.GetValue(nameof(_snowfallMaps), typeof(byte[][])),
            (byte[])info.GetValue(nameof(_temperatureMapSummer), typeof(byte[])),
            (byte[])info.GetValue(nameof(_temperatureMapWinter), typeof(byte[])),
            (double?)info.GetValue(nameof(_maxFlow), typeof(double?)))
        { }

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
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(_albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(_normalizedSeaLevel), _normalizedSeaLevel);
            info.AddValue(nameof(_seed1), _seed1);
            info.AddValue(nameof(_seed2), _seed2);
            info.AddValue(nameof(_seed3), _seed3);
            info.AddValue(nameof(_seed4), _seed4);
            info.AddValue(nameof(_seed5), _seed5);
            info.AddValue(nameof(_angleOfRotation), _angleOfRotation);
            info.AddValue(nameof(Atmosphere), _atmosphere);
            info.AddValue(nameof(_axialPrecession), _axialPrecession);
            info.AddValue(nameof(HasMagnetosphere), _hasMagnetosphere);
            info.AddValue(nameof(MaxElevation), _maxElevation);
            info.AddValue(nameof(RotationalOffset), _rotationalOffset);
            info.AddValue(nameof(RotationalPeriod), _rotationalPeriod);
            info.AddValue(nameof(Resources), _resources);
            info.AddValue(nameof(_satelliteIDs), _satelliteIDs);
            info.AddValue(nameof(SurfaceRegions), _surfaceRegions);
            info.AddValue(nameof(MaxMass), _maxMass);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(_material), _material);
            info.AddValue(nameof(Rings), _rings);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(_depthMap), _depthMap);
            info.AddValue(nameof(_elevationMap), _elevationMap);
            info.AddValue(nameof(_flowMap), _flowMap);
            info.AddValue(nameof(_precipitationMaps), _precipitationMaps);
            info.AddValue(nameof(_snowfallMaps), _snowfallMaps);
            info.AddValue(nameof(_temperatureMapSummer), _temperatureMapSummer);
            info.AddValue(nameof(_temperatureMapWinter), _temperatureMapWinter);
            info.AddValue(nameof(_maxFlow), _maxFlow);
        }

        internal override async Task GenerateOrbitAsync(CelestialLocation orbitedObject)
            => await Space.Orbit.SetOrbitAsync(
                this,
                orbitedObject,
                await GetDistanceToAsync(orbitedObject).ConfigureAwait(false),
                Eccentricity,
                Randomizer.Instance.NextDouble(0.9),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI))
            .ConfigureAwait(false);

        private protected override IMaterial GetComposition(double density, Number mass, IShape shape, double? temperature)
        {
            var coreProportion = GetCoreProportion();
            var crustProportion = GetCrustProportion(shape);

            var coreLayers = GetCore(shape, coreProportion, crustProportion, mass).ToList();
            var topCoreLayer = coreLayers.Last();
            var coreShape = topCoreLayer.material.Shape;
            var coreTemp = topCoreLayer.material.Temperature ?? 0;

            var mantleProportion = 1 - (coreProportion + crustProportion);
            var mantleLayers = GetMantle(shape, mantleProportion, crustProportion, mass, coreShape, coreTemp).ToList();
            if (mantleLayers.Count == 0
                && mantleProportion.IsPositive)
            {
                crustProportion += mantleProportion;
            }

            var crustLayers = GetCrust(shape, crustProportion, mass).ToList();
            if (crustLayers.Count == 0
                && crustProportion.IsPositive)
            {
                if (mantleLayers.Count == 0)
                {
                    coreProportion = Number.One;
                }
                else
                {
                    var ratio = Number.One / (coreProportion + mantleProportion);
                    coreProportion *= ratio;
                    mantleProportion *= ratio;
                }
            }

            var layers = new List<(IMaterial, decimal)>();
            var coreP = (decimal)coreProportion;
            var mantleP = (decimal)mantleProportion;
            var crustP = (decimal)crustProportion;
            foreach (var (layer, proportion) in coreLayers)
            {
                layers.Add((layer, proportion * coreP));
            }
            foreach (var (layer, proportion) in mantleLayers)
            {
                layers.Add((layer, proportion * mantleP));
            }
            foreach (var (layer, proportion) in crustLayers)
            {
                layers.Add((layer, proportion * crustP));
            }
            return new LayeredComposite(
                layers,
                shape,
                density,
                mass,
                temperature);
        }

        private protected virtual IEnumerable<(IMaterial material, decimal proportion)> GetCore(
            IShape planetShape,
            Number coreProportion,
            Number crustProportion,
            Number planetMass)
        {
            var coreMass = planetMass * coreProportion;

            var coreRadius = planetShape.ContainingRadius * coreProportion;
            var shape = new Sphere(coreRadius, planetShape.Position);

            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;

            yield return (new Material(
                (double)(coreMass / shape.Volume),
                coreMass,
                shape,
                (double)((mantleBoundaryDepth * new Number(115, -2)) + (planetShape.ContainingRadius - coreRadius - mantleBoundaryDepth)),
                GetCoreConstituents()), 1);
        }

        private protected virtual Number GetCoreProportion() => new Number(15, -2);

        private protected virtual (ISubstanceReference, decimal)[] GetCoreConstituents()
            => new (ISubstanceReference, decimal)[] { (Substances.GetSolutionReference(Substances.Solutions.IronNickelAlloy), 1) };

        private protected virtual IEnumerable<(IMaterial, decimal)> GetCrust(
            IShape planetShape,
            Number crustProportion,
            Number planetMass)
        {
            var crustMass = planetMass * crustProportion;

            var shape = new HollowSphere(
                planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
                planetShape.ContainingRadius,
                planetShape.Position);

            var dust = Randomizer.Instance.NextDecimal();
            var total = dust;

            // 50% chance of not including the following:
            var waterIce = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += waterIce;

            var n2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += n2;

            var ch4 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += ch4;

            var co = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += co;

            var co2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += co2;

            var nh3 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += nh3;

            var ratio = 1 / total;
            dust *= ratio;
            waterIce *= ratio;
            n2 *= ratio;
            ch4 *= ratio;
            co *= ratio;
            co2 *= ratio;
            nh3 *= ratio;

            var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.GetSolutionReference(Substances.Solutions.CosmicDust), dust),
            };
            if (waterIce > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Water), waterIce));
            }
            if (n2 > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Nitrogen), n2));
            }
            if (ch4 > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Methane), ch4));
            }
            if (co > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.CarbonMonoxide), co));
            }
            if (co2 > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide), co2));
            }
            if (nh3 > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Ammonia), nh3));
            }
            yield return (new Material(
                components,
                (double)(crustMass / shape.Volume),
                crustMass,
                shape), 1);
        }

        // Smaller planemos have thicker crusts due to faster proto-planetary cooling.
        private protected virtual Number GetCrustProportion(IShape shape)
            => 400000 / Number.Pow(shape.ContainingRadius, new Number(16, -1));

        private protected virtual IEnumerable<(IMaterial, decimal)> GetMantle(
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
            var mantleBoundaryTemp = (double)mantleBoundaryDepth * 1.15;
            var mantleTemp = (mantleBoundaryTemp + coreTemp) / 2;

            var shape = new HollowSphere(coreShape.ContainingRadius, planetShape.ContainingRadius * mantleProportion, planetShape.Position);

            var mantleMass = planetMass * mantleProportion;

            yield return (new Material(
                GetMantleSubstance(),
                (double)(mantleMass / shape.Volume),
                mantleMass,
                shape,
                mantleTemp),
                1);
        }

        private protected virtual ISubstanceReference GetMantleSubstance()
            => Substances.GetChemicalReference(Substances.Chemicals.Water);

        private protected override async ValueTask<Number> GetMassAsync()
        {
            var maxMass = MaxMass;
            if (!string.IsNullOrEmpty(ParentId))
            {
                maxMass = Number.Min(maxMass, await GetSternLevisonLambdaMassAsync().ConfigureAwait(false) / 100);
                if (maxMass < MinMass)
                {
                    maxMass = MinMass; // sanity check; may result in a "dwarf" planet which *can* clear its neighborhood
                }
            }

            return Randomizer.Instance.NextNumber(MinMass, maxMass);
        }

        private protected override async ValueTask<(double density, Number mass, IShape shape)> GetMatterAsync()
        {
            var density = GetDensity();
            var mass = await GetMassAsync().ConfigureAwait(false);
            return (density, mass, GetShape(density, mass));
        }

        private protected Number GetMaxRadius(Number density)
            => MaxMassForType.HasValue ? GetRadiusForMass(density, MaxMassForType.Value) : Number.PositiveInfinity;

        private Number GetRadiusForMass(Number density, Number mass) => (mass / density / MathConstants.FourThirdsPI).CubeRoot();

        /// <summary>
        /// Calculates the approximate outer distance at which rings of the given density may be
        /// found, in meters.
        /// </summary>
        /// <param name="density">The density of the rings, in kg/m³.</param>
        /// <returns>The approximate outer distance at which rings of the given density may be
        /// found, in meters.</returns>
        private Number GetRingDistance(Number density)
            => new Number(126, -2)
            * Shape.ContainingRadius
            * (Density / density).CubeRoot();

        private async Task<List<PlanetaryRing>> GetRingsAsync()
        {
            var rings = new List<PlanetaryRing>();

            var innerLimit = (Number)Atmosphere.AtmosphericHeight;

            var outerLimit_Icy = GetRingDistance(_IcyRingDensity);
            if (Orbit != null)
            {
                outerLimit_Icy = Number.Min(outerLimit_Icy, await GetHillSphereRadiusAsync().ConfigureAwait(false) / 3);
            }
            if (innerLimit >= outerLimit_Icy)
            {
                return rings;
            }

            var outerLimit_Rocky = GetRingDistance(_RockyRingDensity);
            if (Orbit != null)
            {
                outerLimit_Rocky = Number.Min(outerLimit_Rocky, await GetHillSphereRadiusAsync().ConfigureAwait(false) / 3);
            }

            var _ringChance = RingChance;
            while (Randomizer.Instance.NextDouble() <= _ringChance && innerLimit <= outerLimit_Icy)
            {
                if (innerLimit < outerLimit_Rocky && Randomizer.Instance.NextBool())
                {
                    var innerRadius = Randomizer.Instance.NextNumber(innerLimit, outerLimit_Rocky);

                    rings.Add(new PlanetaryRing(false, innerRadius, outerLimit_Rocky));

                    outerLimit_Rocky = innerRadius;
                    if (outerLimit_Rocky <= outerLimit_Icy)
                    {
                        outerLimit_Icy = innerRadius;
                    }
                }
                else
                {
                    var innerRadius = Randomizer.Instance.NextNumber(innerLimit, outerLimit_Icy);

                    rings.Add(new PlanetaryRing(true, innerRadius, outerLimit_Icy));

                    outerLimit_Icy = innerRadius;
                    if (outerLimit_Icy <= outerLimit_Rocky)
                    {
                        outerLimit_Rocky = innerRadius;
                    }
                }

                _ringChance *= 0.5;
            }

            return rings;
        }

        private protected IShape GetShape(Number? density = null, Number? mass = null, Number? knownRadius = null)
        {
            if (!mass.HasValue && !knownRadius.HasValue)
            {
                return new SinglePoint(Position);
            }

            // If no known radius is provided, an approximate radius as if the shape was a sphere is
            // determined, which is no less than the minimum required for hydrostatic equilibrium.
            var radius = knownRadius ?? Number.Max(MinimumRadius, GetRadiusForMass(density!.Value, mass!.Value));
            var flattening = Randomizer.Instance.NextNumber(Number.Deci);
            return new Ellipsoid(radius, radius * (1 - flattening), Position);
        }

        /// <summary>
        /// Calculates the mass at which the Stern-Levison parameter for this <see cref="Planemo"/>
        /// will be 1, given its orbital characteristics.
        /// </summary>
        /// <remarks>
        /// Also sets <see cref="Planetoid.Eccentricity"/> as a side effect, if the <see
        /// cref="Planemo"/> doesn't already have a defined orbit.
        /// </remarks>
        /// <exception cref="Exception">Cannot be called if this <see cref="Planemo"/> has no Orbit
        /// or parent.</exception>
        private protected async Task<Number> GetSternLevisonLambdaMassAsync()
        {
            Number semiMajorAxis;
            if (Orbit.HasValue)
            {
                semiMajorAxis = Orbit.Value.SemiMajorAxis;
            }
            else
            {
                var parent = await GetParentAsync().ConfigureAwait(false);
                if (parent is null)
                {
                    semiMajorAxis = 0;
                }
                else
                {
                    // Even if this planetoid is not yet in a defined orbit, some orbital
                    // characteristics must be determined early, in order to distinguish a dwarf planet
                    // from a planet, which depends partially on orbital distance.
                    semiMajorAxis = await GetDistanceToAsync(parent).ConfigureAwait(false) * ((1 + Eccentricity) / (1 - Eccentricity));
                }
            }

            return (Number.Pow(semiMajorAxis, new Number(15, -1)) / new Number(2.5, -28)).Sqrt();
        }

        private protected override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);
            await GetRingsAsync().ConfigureAwait(false);
        }
    }
}
