using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Tavenem.Chemistry;
using Tavenem.Chemistry.HugeNumbers;
using Tavenem.DataStorage;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics;
using Tavenem.Mathematics.HugeNumbers;
using Tavenem.Randomize;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space.Planetoids;
using Tavenem.Universe.Space.Stars;

namespace Tavenem.Universe.Space
{
    /// <summary>
    /// A region of space containing a system of stars, and the bodies which orbit that system.
    /// </summary>
    [Serializable]
    [System.Text.Json.Serialization.JsonConverter(typeof(StarSystemConverter))]
    public class StarSystem : CosmicLocation
    {
        internal static readonly HugeNumber StarSystemSpace = new(3.5, 16);

        /// <summary>
        /// The type discriminator for this type.
        /// </summary>
        public const string StarSystemIdItemTypeName = ":Location:CosmicLocation:StarSystem:";
        /// <summary>
        /// A built-in, read-only type discriminator.
        /// </summary>
        public override string IdItemTypeName => StarSystemIdItemTypeName;

        /// <summary>
        /// True if the primary <see cref="Star"/> in this system is a Population II <see
        /// cref="Star"/>; false if it is a Population I <see cref="Star"/>.
        /// </summary>
        public bool IsPopulationII { get; internal set; }

        /// <summary>
        /// The <see cref="Stars.LuminosityClass"/> of the primary <see cref="Star"/> in this
        /// system.
        /// </summary>
        public LuminosityClass LuminosityClass { get; private set; }

        /// <summary>
        /// The <see cref="Stars.SpectralClass"/> of the primary <see cref="Star"/> in this
        /// system.
        /// </summary>
        public SpectralClass SpectralClass { get; private set; }

        /// <summary>
        /// The IDs of the stars in this system.
        /// </summary>
        public IReadOnlyList<string> StarIDs { get; private set; }

        /// <summary>
        /// The type of the primary <see cref="Star"/> in this system.
        /// </summary>
        public StarType StarType { get; }

        private protected override string BaseTypeName => "Star System";

        private HugeNumber Radius => Material.Shape.ContainingRadius;

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystem"/> with the given parameters.
        /// </summary>
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
        /// <param name="starType">The type of the primary star.</param>
        /// <param name="spectralClass">The <see cref="SpectralClass"/> of the primary star.</param>
        /// <param name="luminosityClass">
        /// The <see cref="LuminosityClass"/> of the primary star.
        /// </param>
        /// <param name="populationII">
        /// Set to true if the primary star is to be a Population II star.
        /// </param>
        /// <param name="allowBinary">
        /// Whether a multiple-star system will be permitted.
        /// </param>
        /// <param name="sunlike">
        /// <para>
        /// If <see langword="true"/>, the system must have a single star similar to Sol, Earth's
        /// sun.
        /// </para>
        /// <para>
        /// Overrides the values of <paramref name="luminosityClass"/>, <paramref
        /// name="spectralClass"/>, <paramref name="populationII"/>, and <paramref
        /// name="allowBinary"/> if set to <see langword="true"/>.
        /// </para>
        /// </param>
        public StarSystem(
            CosmicLocation? parent,
            Vector3 position,
            out List<CosmicLocation> children,
            OrbitalParameters? orbit = null,
            StarType starType = StarType.MainSequence,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false,
            bool allowBinary = true,
            bool sunlike = false) : base(parent?.Id, CosmicStructureType.StarSystem)
        {
            StarIDs = ImmutableList<string>.Empty;
            children = Configure(parent, position, starType, spectralClass, luminosityClass, populationII, null, allowBinary, sunlike);

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
                        CosmicStructureType.Nebula => null,
                        CosmicStructureType.HIIRegion => null,
                        CosmicStructureType.PlanetaryNebula => null,
                        CosmicStructureType.StarSystem => null,
                        _ => null,
                    };
                }
            }
            if (orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(this, orbit.Value);
            }
        }

        private StarSystem(string? parentId) : base(parentId, CosmicStructureType.StarSystem) => StarIDs = ImmutableList<string>.Empty;

        internal StarSystem(
            string id,
            uint seed,
            StarType starType,
            string? parentId,
            Vector3[]? absolutePosition,
            string? name,
            Vector3 velocity,
            Orbit? orbit,
            Vector3 position,
            double? temperature,
            HugeNumber radius,
            HugeNumber mass,
            IReadOnlyList<string> starIds,
            bool isPopulationII,
            LuminosityClass luminosityClass,
            SpectralClass spectralClass)
            : base(
                id,
                seed,
                CosmicStructureType.StarSystem,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit)
        {
            StarIDs = starIds;
            StarType = starType;
            IsPopulationII = isPopulationII;
            LuminosityClass = luminosityClass;
            SpectralClass = spectralClass;
            Reconstitute(position, radius, mass, temperature);
        }

        private StarSystem(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            (uint?)info.GetValue(nameof(Seed), typeof(uint)) ?? default,
            (StarType?)info.GetValue(nameof(StarType), typeof(StarType)) ?? StarType.None,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (Vector3?)info.GetValue(nameof(Velocity), typeof(Vector3)) ?? default,
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (Vector3?)info.GetValue(nameof(Position), typeof(Vector3)) ?? Vector3.Zero,
            (double?)info.GetValue(nameof(Temperature), typeof(double?)),
            (HugeNumber?)info.GetValue(nameof(Radius), typeof(HugeNumber)) ?? HugeNumber.Zero,
            (HugeNumber?)info.GetValue(nameof(Mass), typeof(HugeNumber)) ?? HugeNumber.Zero,
            (IReadOnlyList<string>?)info.GetValue(nameof(StarIDs), typeof(IReadOnlyList<string>)) ?? ImmutableList<string>.Empty,
            (bool?)info.GetValue(nameof(IsPopulationII), typeof(bool)) ?? default,
            (LuminosityClass?)info.GetValue(nameof(LuminosityClass), typeof(LuminosityClass)) ?? LuminosityClass.None,
            (SpectralClass?)info.GetValue(nameof(SpectralClass), typeof(SpectralClass)) ?? SpectralClass.None)
        { }

        /// <summary>
        /// Generates a new <see cref="StarSystem"/> as the containing parent location of the
        /// given <paramref name="child"/> location.
        /// </summary>
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
        /// <param name="starType">The type of the primary star.</param>
        /// <param name="spectralClass">The <see cref="SpectralClass"/> of the primary star.</param>
        /// <param name="luminosityClass">
        /// The <see cref="LuminosityClass"/> of the primary star.
        /// </param>
        /// <param name="populationII">
        /// Set to true if the primary star is to be a Population II star.
        /// </param>
        /// <param name="allowBinary">
        /// Whether a multiple-star system will be permitted.
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
            out List<CosmicLocation> children,
            Vector3? position = null,
            OrbitalParameters? orbit = null,
            StarType starType = StarType.MainSequence,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false,
            bool allowBinary = true)
        {
            var instance = new StarSystem(null);
            child.AssignParent(instance);

            children = new List<CosmicLocation>();

            children.AddRange(instance.Configure(null, Vector3.Zero, starType, spectralClass, luminosityClass, populationII, child, allowBinary));

            // Stars, planetoids, and oort clouds will have their place in the system assigned during configuration.
            if (!position.HasValue && child.StructureType != CosmicStructureType.Planetoid)
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
                        CosmicStructureType.StarSystem => StarSystemSpace,
                        CosmicStructureType.AsteroidField => AsteroidField.AsteroidFieldSpace,
                        CosmicStructureType.BlackHole => BlackHole.BlackHoleSpace,
                        _ => HugeNumber.Zero,
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
                    orbit = OrbitalParameters.GetFromEccentricity(instance.Mass, instance.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05));
                }
                if (orbit.HasValue)
                {
                    Space.Orbit.AssignOrbit(child, orbit.Value);
                }
            }

            return instance;
        }

        private static HugeNumber GetTotalApoapsis(
            List<(
            Star star,
            Star orbited,
            double eccentricity,
            HugeNumber semiMajorAxis,
            HugeNumber periapsis,
            HugeNumber apoapsis)> companions,
            Star star,
            HugeNumber value)
        {
            var match = companions.FirstOrNull(x => x.star == star);
            if (match != null)
            {
                value += match.Value.apoapsis;
                return GetTotalApoapsis(companions, match.Value.orbited, value);
            }
            return value;
        }

        /// <summary>
        /// Adds a <see cref="Star"/> to this system.
        /// </summary>
        /// <param name="star">The <see cref="Star"/> to add.</param>
        public void AddStar(Star star) => StarIDs = ImmutableList<string>.Empty.AddRange(StarIDs).Add(star.Id);

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
            info.AddValue(nameof(StarType), StarType);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), Orbit);
            info.AddValue(nameof(Position), Position);
            info.AddValue(nameof(Temperature), Material.Temperature);
            info.AddValue(nameof(Radius), Radius);
            info.AddValue(nameof(Mass), Mass);
            info.AddValue(nameof(StarIDs), StarIDs);
            info.AddValue(nameof(IsPopulationII), IsPopulationII);
            info.AddValue(nameof(LuminosityClass), LuminosityClass);
            info.AddValue(nameof(SpectralClass), SpectralClass);
        }

        /// <summary>
        /// Enumerates the child stars of this instance.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of child <see cref="Star"/> instances of this <see
        /// cref="StarSystem"/>.
        /// </returns>
        public async IAsyncEnumerable<Star> GetStarsAsync(IDataStore dataStore)
        {
            foreach (var id in StarIDs)
            {
                var star = await dataStore.GetItemAsync<Star>(id).ConfigureAwait(false);
                if (star is not null)
                {
                    yield return star;
                }
            }
        }

        /// <summary>
        /// Removes a star from this system.
        /// </summary>
        /// <param name="id">The ID of the star to remove.</param>
        public void RemoveStar(string id) => StarIDs = ImmutableList<string>.Empty.AddRange(StarIDs).Remove(id);

        /// <summary>
        /// Single-planet orbital distance may follow a log-normal distribution, with the peak at 0.3
        /// AU (this does not conform to current observations exactly, but extreme biases in current
        /// observations make adequate overall distributions difficult to guess, and the
        /// approximation used is judged reasonably close). In multi-planet systems, migration and
        /// resonances result in a more widely-distributed system.
        /// </summary>
        /// <param name="star">The <see cref="Star"/> around which the planet will orbit.</param>
        /// <param name="isGiant">Whether this is to be a giant planet (including ice giants).</param>
        /// <param name="minTerrestrialPeriapsis">The minimum periapsis for a terrestrial planet.</param>
        /// <param name="minGiantPeriapsis">The minimum periapsis for a giant planet.</param>
        /// <param name="maxApoapsis">The maximum apoapsis.</param>
        /// <param name="innerPlanet">The current innermost planet.</param>
        /// <param name="outerPlanet">The current outermost planet.</param>
        /// <param name="medianOrbit">The median orbit among the current planets.</param>
        /// <param name="totalGiants">The number of giant planets this <see cref="StarSystem"/> is to have.</param>
        /// <returns>The chosen periapsis, or null if no valid orbit is available.</returns>
        private static HugeNumber? ChoosePlanetPeriapsis(
            Star star,
            bool isGiant,
            HugeNumber minTerrestrialPeriapsis,
            HugeNumber minGiantPeriapsis,
            HugeNumber? maxApoapsis,
            Planetoid? innerPlanet,
            Planetoid? outerPlanet,
            HugeNumber medianOrbit,
            int totalGiants)
        {
            HugeNumber? periapsis = null;

            // If this is the first planet, the orbit is selected based on the number of giants the
            // system is to have.
            if (innerPlanet is null)
            {
                // Evaluates to ~0.3 AU if there is only 1 giant, ~5 AU if there are 4 giants (as
                // would be the case for the Solar system), and ~8 AU if there are 6 giants.
                var mean = 7.48e11 - ((4 - Math.Max(1, totalGiants)) * 2.34e11);
                var min = isGiant ? (double)minGiantPeriapsis : (double)minTerrestrialPeriapsis;
                var max = maxApoapsis.HasValue ? (double)maxApoapsis.Value : (double?)null;
                if (!max.HasValue || min < max)
                {
                    periapsis = min > mean * 1.25
                        ? min
                        : Randomizer.Instance.NormalDistributionSample(mean, mean / 3, min, max);
                }
            }
            // If there are already any planets and this planet is a giant, it is placed in a higher
            // orbit, never a lower one.
            else if (innerPlanet != null && isGiant)
            {
                // Forces reassignment to a higher orbit below.
                periapsis = medianOrbit;
            }
            // Terrestrial planets may be in either lower or higher orbits, with lower ones being
            // more likely.
            else if (Randomizer.Instance.NextDouble() <= 0.75)
            {
                periapsis = medianOrbit / 2;
            }
            else
            {
                periapsis = medianOrbit;
            }

            if (outerPlanet != null)
            {
                var otherMass = isGiant ? new HugeNumber(1.25, 28) : new HugeNumber(3, 25);
                if (periapsis < medianOrbit)
                {
                    // Inner orbital spacing is by an average of 21.7 mutual Hill radii, with a
                    // standard deviation of 9.5. An average planetary mass is used for the
                    // calculation since the planet hasn't been generated yet, which should produce
                    // reasonable values.
                    var spacing = innerPlanet!.GetMutualHillSphereRadius(otherMass)
                        * Randomizer.Instance.NormalDistributionSample(21.7, 9.5, minimum: 1);
                    periapsis = innerPlanet.Orbit!.Value.Periapsis - spacing;
                    if (periapsis < (isGiant ? minGiantPeriapsis : minTerrestrialPeriapsis))
                    {
                        periapsis = medianOrbit; // Force reassignment below.
                    }
                }
                if (periapsis >= medianOrbit)
                {
                    // For all terrestrial planets, and giant planets within a 200 day period,
                    // orbital spacing is by an average of 21.7 mutual Hill radii, with a standard
                    // deviation of 9.5. An average planetary mass is used for the calculation since
                    // the planet hasn't been generated yet, which should produce reasonable values.
                    var outerPeriod = (double)outerPlanet.Orbit!.Value.Period;
                    if (!isGiant || outerPeriod <= 1.728e7)
                    {
                        var spacing = outerPlanet.GetMutualHillSphereRadius(otherMass)
                            * Randomizer.Instance.NormalDistributionSample(21.7, 9.5, minimum: 1);
                        periapsis = outerPlanet.Orbit.Value.Apoapsis + spacing;
                        if (periapsis > maxApoapsis)
                        {
                            return null;
                        }
                    }
                    // Beyond 200 days, a Gaussian distribution of mean-motion resonance with a mean
                    // of 2.2 is used to determine orbital spacing for giant planets.
                    else
                    {
                        var newPeriod = (HugeNumber)Randomizer.Instance.NormalDistributionSample(outerPeriod * 2.2, outerPeriod);

                        // Assuming no eccentricity and an average mass, calculate a periapsis from
                        // the selected period, but set their mutual Hill sphere radius as a minimum separation.
                        periapsis = HugeNumber.Max(outerPlanet.Orbit.Value.Apoapsis
                            + outerPlanet.GetMutualHillSphereRadius(otherMass),
                            ((newPeriod / HugeNumber.TwoPI).Square() * HugeNumberConstants.G * (star.Mass + otherMass)).CubeRoot());
                    }
                }
            }

            return periapsis;
        }

        private static int GenerateNumCompanions(Star primary)
        {
            var chance = Randomizer.Instance.NextDouble();
            if (primary.StarType == StarType.BrownDwarf)
            {
                return 0;
            }
            else if (primary.StarType == StarType.WhiteDwarf)
            {
                if (chance <= 4.0 / 9.0)
                {
                    return 1;
                }
            }
            else if (primary.IsGiant || primary.StarType == StarType.Neutron)
            {
                if (chance <= 0.0625)
                {
                    return 2;
                }
                else if (chance <= 0.4375)
                {
                    return 1;
                }
            }
            else
            {
                switch (primary.SpectralClass)
                {
                    case SpectralClass.A:
                        if (chance <= 0.065)
                        {
                            return 2;
                        }
                        else if (chance <= 0.435)
                        {
                            return 1;
                        }
                        break;
                    case SpectralClass.B:
                        if (chance <= 0.8)
                        {
                            return 1;
                        }
                        break;
                    case SpectralClass.O:
                        if (chance <= 2.0 / 3.0)
                        {
                            return 1;
                        }
                        break;
                    default:
                        if (chance <= 0.01)
                        {
                            return 3;
                        }
                        else if (chance <= 0.03)
                        {
                            return 2;
                        }
                        else if (chance <= 0.3)
                        {
                            return 1;
                        }
                        break;
                }
            }
            return 0;
        }

        /// <summary>
        /// Planets can orbit stably in a multiple-star system between the stars in a range up to
        /// ~33% of an orbiting star's Hill sphere, and ~33% of the distance to an orbited star's
        /// nearest orbiting star's Hill sphere. Alternatively, planets may orbit beyond the sphere
        /// of influence of a close companion, provided they are still not beyond the limits towards
        /// further orbiting stars.
        /// </summary>
        /// <param name="stars">The stars in the system.</param>
        /// <param name="star">The <see cref="Star"/> whose apses' limits are to be calculated.</param>
        private static (HugeNumber? minPeriapsis, HugeNumber? maxApoapsis) GetApsesLimits(List<Star> stars, Star star)
        {
            HugeNumber? maxApoapsis = null;
            HugeNumber? minPeriapsis = null;
            if (star.Orbit != null)
            {
                maxApoapsis = star.GetHillSphereRadius() * 1 / 3;
            }

            foreach (var entity in stars)
            {
                if (!entity.Orbit.HasValue || entity.Orbit.Value.OrbitedPosition != star.Position)
                {
                    continue;
                }

                // If a star is orbiting within ~100 AU, it is considered too close for planets to
                // orbit in between, and orbits are only considered around them as a pair.
                if (entity.Orbit!.Value.Periapsis <= new HugeNumber(1.5, 13))
                {
                    minPeriapsis = entity.GetHillSphereRadius() * 20;
                    // Clear the maxApoapsis if it's within this outer orbit.
                    if (maxApoapsis.HasValue && maxApoapsis < minPeriapsis)
                    {
                        maxApoapsis = null;
                    }
                }
                else
                {
                    var candidateMaxApoapsis = (entity.Orbit.Value.Periapsis - entity.GetHillSphereRadius()) * HugeNumber.Third;
                    if (maxApoapsis < candidateMaxApoapsis)
                    {
                        candidateMaxApoapsis = maxApoapsis.Value;
                    }
                    if (!minPeriapsis.HasValue || candidateMaxApoapsis > minPeriapsis)
                    {
                        maxApoapsis = candidateMaxApoapsis;
                    }
                }
            }

            return (minPeriapsis, maxApoapsis);
        }

        /// <summary>
        /// Generates a close period. Close periods are about 100 days, in a normal distribution
        /// constrained to 3-sigma.
        /// </summary>
        private static HugeNumber GetClosePeriod()
        {
            var count = 0;
            double value;
            const int mu = 36000;
            const double sigma = 1.732e7;
            const double min = mu - (3 * sigma);
            const double max = mu + (3 * sigma);
            // loop rather than constraining to limits in order to avoid over-representing the limits
            do
            {
                value = Randomizer.Instance.NormalDistributionSample(mu, sigma);
                if (value is >= min and <= max)
                {
                    return value;
                }
                count++;
            } while (count < 100); // sanity check; should not be reached due to the nature of a normal distribution
            return value;
        }

        private static SpectralClass GetSpectralClassForCompanionStar(Star primary)
        {
            var chance = Randomizer.Instance.NextDouble();
            if (primary.SpectralClass == SpectralClass.O)
            {
                if (chance <= 0.2133)
                {
                    return SpectralClass.O; // 80%
                }
                else if (chance <= 0.4267)
                {
                    return SpectralClass.B; // 80%
                }
                else if (chance <= 0.5734)
                {
                    return SpectralClass.A; // 55%
                }
                else if (chance <= 0.7201)
                {
                    return SpectralClass.F; // 55%
                }
                else if (chance <= 0.8268)
                {
                    return SpectralClass.G; // 40%
                }
                else if (chance <= 0.9335)
                {
                    return SpectralClass.K; // 40%
                }
                else
                {
                    return SpectralClass.M; // 25%
                }
            }
            else if (primary.SpectralClass == SpectralClass.B)
            {
                if (chance <= 0.2712)
                {
                    return SpectralClass.B; // 80%
                }
                else if (chance <= 0.4576)
                {
                    return SpectralClass.A; // 55%
                }
                else if (chance <= 0.6440)
                {
                    return SpectralClass.F; // 55%
                }
                else if (chance <= 0.7796)
                {
                    return SpectralClass.G; // 40%
                }
                else if (chance <= 0.9152)
                {
                    return SpectralClass.K; // 40%
                }
                else
                {
                    return SpectralClass.M; // 25%
                }
            }
            else if (primary.SpectralClass == SpectralClass.A)
            {
                if (chance <= 0.2558)
                {
                    return SpectralClass.A; // 55%
                }
                else if (chance <= 0.5116)
                {
                    return SpectralClass.F; // 55%
                }
                else if (chance <= 0.6976)
                {
                    return SpectralClass.G; // 40%
                }
                else if (chance <= 0.8836)
                {
                    return SpectralClass.K; // 40%
                }
                else
                {
                    return SpectralClass.M; // 25%
                }
            }
            else if (primary.SpectralClass == SpectralClass.F)
            {
                if (chance <= 0.3438)
                {
                    return SpectralClass.F; // 55%
                }
                else if (chance <= 0.5938)
                {
                    return SpectralClass.G; // 40%
                }
                else if (chance <= 0.8438)
                {
                    return SpectralClass.K; // 40%
                }
                else
                {
                    return SpectralClass.M; // 25%
                }
            }
            else if (primary.SpectralClass == SpectralClass.G)
            {
                if (chance <= 0.3810)
                {
                    return SpectralClass.G; // 40%
                }
                else if (chance <= 0.7619)
                {
                    return SpectralClass.K; // 40%
                }
                else
                {
                    return SpectralClass.M; // 25%
                }
            }
            else if (primary.SpectralClass == SpectralClass.K)
            {
                if (chance <= 0.6154)
                {
                    return SpectralClass.K; // 40%
                }
                else
                {
                    return SpectralClass.M; // 25%
                }
            }
            else
            {
                return SpectralClass.M;
            }
        }

        private (Star star, HugeNumber totalApoapsis)? AddCompanionStar(
            List<(Star star, HugeNumber totalApoapsis)> companions,
            Star orbited,
            HugeNumber? orbitedTotalApoapsis,
            HugeNumber period,
            CosmicLocation? child = null)
        {
            Star? star;

            // 20% chance that a white dwarf has a twin, and that a neutron star has a white dwarf companion.
            if ((orbited.StarType == StarType.WhiteDwarf || orbited.StarType == StarType.Neutron)
                && Randomizer.Instance.NextDouble() <= 0.2)
            {
                if (child is Star candidate && candidate.StarType == StarType.WhiteDwarf)
                {
                    star = candidate;
                }
                else
                {
                    star = new Star(StarType.WhiteDwarf, this, Vector3.Zero);
                }
            }
            // There is a chance that a giant will have a giant companion.
            else if (orbited.IsGiant)
            {
                var chance = Randomizer.Instance.NextDouble();
                // Bright, super, and hypergiants are not generated as companions; if these exist in
                // the system, they are expected to be the primary.
                if (chance <= 0.25)
                {
                    if (child is Star candidate
                        && candidate.StarType == StarType.RedGiant
                        && candidate.LuminosityClass == LuminosityClass.III)
                    {
                        star = candidate;
                    }
                    else
                    {
                        star = new Star(StarType.RedGiant, this, Vector3.Zero, luminosityClass: LuminosityClass.III);
                    }
                }
                else if (chance <= 0.45)
                {
                    if (child is Star candidate
                        && candidate.StarType == StarType.BlueGiant
                        && candidate.LuminosityClass == LuminosityClass.III)
                    {
                        star = candidate;
                    }
                    else
                    {
                        star = new Star(StarType.BlueGiant, this, Vector3.Zero, luminosityClass: LuminosityClass.III);
                    }
                }
                else if (chance <= 0.55)
                {
                    if (child is Star candidate
                        && candidate.StarType == StarType.YellowGiant
                        && candidate.LuminosityClass == LuminosityClass.III)
                    {
                        star = candidate;
                    }
                    else
                    {
                        star = new Star(StarType.YellowGiant, this, Vector3.Zero, luminosityClass: LuminosityClass.III);
                    }
                }
                else if (child is Star candidate)
                {
                    star = candidate;
                }
                else
                {
                    star = new Star(this, Vector3.Zero, spectralClass: GetSpectralClassForCompanionStar(orbited));
                }
            }
            else if (child is Star candidate)
            {
                star = candidate;
            }
            else
            {
                star = new Star(this, Vector3.Zero, spectralClass: GetSpectralClassForCompanionStar(orbited));
            }

            if (star != null)
            {
                // Eccentricity tends to be low but increase with longer periods.
                var eccentricity = Math.Abs((double)(Randomizer.Instance.NormalDistributionSample(0, 0.0001) * (period / new HugeNumber(3.1536, 9))));

                // Assuming an effective 2-body system, the period lets us determine the semi-major axis.
                var semiMajorAxis = ((period / HugeNumber.TwoPI).Square() * HugeNumberConstants.G * (orbited.Mass + star.Mass)).CubeRoot();

                Space.Orbit.AssignOrbit(
                    star,
                    orbited,
                    (1 - eccentricity) * semiMajorAxis,
                    eccentricity,
                    Randomizer.Instance.NextDouble(Math.PI),
                    Randomizer.Instance.NextDouble(DoubleConstants.TwoPI),
                    Randomizer.Instance.NextDouble(DoubleConstants.TwoPI),
                    Randomizer.Instance.NextDouble(DoubleConstants.TwoPI));

                var companion = (star, orbitedTotalApoapsis.HasValue ? orbitedTotalApoapsis.Value + star.Orbit!.Value.Apoapsis : star.Orbit!.Value.Apoapsis);
                companions.Add(companion);
                return companion;
            }
            return null;
        }

        private List<(Star star, HugeNumber totalApoapsis)> AddCompanionStars(
            Star primary,
            int amount,
            CosmicLocation? child = null)
        {
            var companions = new List<(Star star, HugeNumber totalApoapsis)>();
            if (amount <= 0)
            {
                return companions;
            }
            var orbited = primary;

            // Most periods are about 50 years, in a log normal distribution. There is a chance of a
            // close binary, however.
            var close = false;
            HugeNumber companionPeriod;
            if (Randomizer.Instance.NextDouble() <= 0.2)
            {
                close = true;
                companionPeriod = GetClosePeriod();
            }
            else
            {
                companionPeriod = Randomizer.Instance.LogNormalDistributionSample(0, 1) * new HugeNumber(1.5768, 9);
            }
            var companion = AddCompanionStar(companions, orbited, null, companionPeriod, child);
            if (!companion.HasValue)
            {
                return companions;
            }
            if (companion.Value.star == child)
            {
                child = null;
            }

            if (amount <= 1)
            {
                return companions;
            }
            // A third star will orbit either the initial star, or will orbit the second in a close
            // orbit, establishing a hierarchical system.

            // If the second star was given a close orbit, the third will automatically orbit the
            // original star with a long period.
            orbited = close || !companion.HasValue || Randomizer.Instance.NextBool() ? primary : companion!.Value.star;

            // Long periods are about 50 years, in a log normal distribution, shifted out to avoid
            // being too close to the 2nd star's close orbit.
            if (close)
            {
                var c = AddCompanionStar(
                    companions,
                    orbited,
                    orbited.Orbit?.Apoapsis,
                    (Randomizer.Instance.LogNormalDistributionSample(0, 1) * new HugeNumber(1.5768, 9))
                    + (Space.Orbit.GetHillSphereRadius(
                        companion!.Value.star.Mass,
                        companion!.Value.star.Orbit!.Value.OrbitedMass,
                        companion!.Value.star.Orbit!.Value.SemiMajorAxis,
                        companion!.Value.star.Orbit!.Value.Eccentricity) * 20),
                    child);
                if (c.HasValue && c.Value.star == child)
                {
                    child = null;
                }
            }
            else
            {
                var c = AddCompanionStar(companions, orbited, orbited.Orbit?.Apoapsis, GetClosePeriod(), child);
                if (c.HasValue && c.Value.star == child)
                {
                    child = null;
                }
            }

            if (amount <= 2)
            {
                return companions;
            }
            // A fourth star will orbit whichever star of the original two does not already have a
            // close companion, in a close orbit of its own.
            orbited = orbited == primary && companion.HasValue ? companion.Value.star : primary;
            if (orbited != null)
            {
                AddCompanionStar(companions, orbited, orbited.Orbit?.Apoapsis, GetClosePeriod(), child);
            }

            return companions;
        }

        private List<CosmicLocation> Configure(
            CosmicLocation? parent,
            Vector3 position,
            StarType starType = StarType.MainSequence,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false,
            CosmicLocation? child = null,
            bool allowBinary = true,
            bool sunlike = false)
        {
            var addedChildren = new List<CosmicLocation>();
            var stars = new List<Star>();
            Star? primary = null;
            var childIsPrimary = false;
            if (child is Star candidate
                && starType.HasFlag(candidate.StarType)
                && (!spectralClass.HasValue || candidate.SpectralClass == spectralClass.Value)
                && (!luminosityClass.HasValue || candidate.LuminosityClass == luminosityClass.Value)
                && candidate.IsPopulationII == populationII)
            {
                primary = candidate;
                childIsPrimary = true;
                stars.Add(candidate);
            }
            else if (sunlike)
            {
                primary = Star.NewSunlike(this, Vector3.Zero);
                if (primary is not null)
                {
                    addedChildren.Add(primary);
                    stars.Add(primary);
                }
            }
            else
            {
                primary = new Star(starType, this, Vector3.Zero, null, spectralClass, luminosityClass, populationII);
                if (primary is not null)
                {
                    addedChildren.Add(primary);
                    stars.Add(primary);
                }
            }

            if (primary is null)
            {
                return addedChildren;
            }

            IsPopulationII = primary.IsPopulationII;
            LuminosityClass = primary.LuminosityClass;
            SpectralClass = primary.SpectralClass;

            var numCompanions = !allowBinary || sunlike ? 0 : GenerateNumCompanions(primary);
            var companions = AddCompanionStars(primary, numCompanions, childIsPrimary ? null : child);
            if (companions.Any(x => x.star == child))
            {
                child = null;
            }
            foreach (var (star, _) in companions)
            {
                stars.Add(star);
                if (star != child)
                {
                    addedChildren.Add(star);
                }
            }

            var outerApoapsis = numCompanions == 0 ? HugeNumber.Zero : companions.Max(x => x.totalApoapsis);

            // The Shape of a StarSystem depends on the configuration of the Stars (with ~75000
            // AU extra space, or roughly 150% the outer limit for a potential Oort cloud). This
            // should give plenty of breathing room for any objects with high eccentricity to
            // stay within the system's local space, while not placing the objects of interest
            // (stars, planets) too close together in the center of local space.
            var radius = new HugeNumber(1.125, 16) + outerApoapsis;

            // The mass of the stellar bodies is presumed to be at least 99% of the total, so it is used
            // as a close-enough approximation, plus a bit of extra.
            var mass = numCompanions == 0
                ? primary.Mass * new HugeNumber(1001, -3)
                : (primary.Mass + companions.Sum(s => s.star.Mass)) * new HugeNumber(1001, -3);

            Reconstitute(position, radius, mass, parent?.Material.Temperature ?? UniverseAmbientTemperature);

            StarIDs = ImmutableList<string>.Empty.AddRange(StarIDs).AddRange(stars.Select(x => x.Id));

            // All single and close-binary systems are presumed to have Oort clouds. Systems with
            // higher multiplicity are presumed to disrupt any Oort clouds.
            if (child?.StructureType == CosmicStructureType.OortCloud
                || stars.Count == 1
                || (stars.Count == 2 && outerApoapsis < new HugeNumber(1.5, 13)))
            {
                if (child?.StructureType == CosmicStructureType.OortCloud)
                {
                    child.Position = Vector3.Zero;
                }
                else
                {
                    var cloud = new AsteroidField(this, Vector3.Zero, null, oort: true, Shape.ContainingRadius);
                    if (cloud is not null)
                    {
                        addedChildren.Add(cloud);
                    }
                }
            }

            foreach (var star in stars)
            {
                addedChildren.AddRange(GeneratePlanetsForStar(stars, star, child));
            }

            return addedChildren;
        }

        private CosmicLocation? GenerateDebrisDisc(List<Star> stars, Star star, Planetoid outerPlanet, HugeNumber? maxApoapsis)
        {
            var outerApoapsis = outerPlanet.Orbit!.Value.Apoapsis;
            var innerRadius = outerApoapsis + (outerPlanet.GetMutualHillSphereRadius(new HugeNumber(3, 25)) * Randomizer.Instance.NormalDistributionSample(21.7, 9.5));
            var width = (stars.Count > 1 || Randomizer.Instance.NextBool())
                ? Randomizer.Instance.NextNumber(new HugeNumber(3, 12), new HugeNumber(4.5, 12))
                : Randomizer.Instance.LogNormalDistributionSample(0, 1) * new HugeNumber(7.5, 12);
            if (maxApoapsis.HasValue)
            {
                width = HugeNumber.Min(width, maxApoapsis.Value - innerRadius);
            }
            // Cannot be so wide that it overlaps the outermost planet's orbit.
            width = HugeNumber.Min(width, (innerRadius - outerApoapsis) * new HugeNumber(9, -1));
            if (width > 0)
            {
                var radius = width / 2;
                return new AsteroidField(this, star.Position, majorRadius: innerRadius + radius, minorRadius: radius);
            }

            return null;
        }

        private List<CosmicLocation> GeneratePlanet(
            Star star,
            List<Star> stars,
            HugeNumber minTerrestrialPeriapsis,
            HugeNumber minGiantPeriapsis,
            HugeNumber? maxApoapsis,
            ref PlanetarySystemInfo planetarySystemInfo,
            Planetoid? planet)
        {
            var addedChildren = new List<CosmicLocation>();

            if (planet is not null)
            {
                planetarySystemInfo.Periapsis = planet.Orbit?.Periapsis ?? 0;

                if (planet.PlanetType == PlanetType.IceGiant)
                {
                    planetarySystemInfo.NumIceGiants--;
                }
                else if (planet.PlanetType == PlanetType.Giant)
                {
                    planetarySystemInfo.NumGiants--;
                }
                else
                {
                    planetarySystemInfo.NumTerrestrials--;
                    planetarySystemInfo.TotalTerrestrials++;
                }
            }
            else
            {
                planet = GetPlanet(star, stars, minTerrestrialPeriapsis, minGiantPeriapsis, maxApoapsis, ref planetarySystemInfo, out var satellites);
                addedChildren.AddRange(satellites);
            }

            if (planet is null)
            {
                return addedChildren;
            }
            else
            {
                addedChildren.Add(planet);
            }

            if (planet.IsGiant)
            {
                // Giants may get Trojan asteroid fields at their L4 & L5 Lagrangian points.
                if (Randomizer.Instance.NextBool())
                {
                    addedChildren.AddRange(GenerateTrojans(star, planet, planetarySystemInfo.Periapsis!.Value));
                }
                // There is a chance of an inner-system asteroid belt inside the orbit of a giant.
                if (planetarySystemInfo.Periapsis < planetarySystemInfo.MedianOrbit && Randomizer.Instance.NextDouble() <= 0.2)
                {
                    var separation = planetarySystemInfo.Periapsis!.Value - (planet.GetMutualHillSphereRadius(new HugeNumber(3, 25)) * Randomizer.Instance.NormalDistributionSample(21.7, 9.5));
                    var belt = new AsteroidField(this, star.Position, majorRadius: separation * HugeNumber.Deci, minorRadius: separation * new HugeNumber(8, -1));
                    if (belt is not null)
                    {
                        addedChildren.Add(belt);
                    }
                }
            }

            if (planetarySystemInfo.InnerPlanet is null)
            {
                planetarySystemInfo.InnerPlanet = planet;
                planetarySystemInfo.OuterPlanet = planet;
            }
            else if (planetarySystemInfo.Periapsis < planetarySystemInfo.MedianOrbit)
            {
                planetarySystemInfo.InnerPlanet = planet;
            }
            else
            {
                planetarySystemInfo.OuterPlanet = planet;
            }

            planetarySystemInfo.MedianOrbit = planetarySystemInfo.InnerPlanet.Orbit!.Value.Periapsis
                + ((planetarySystemInfo.OuterPlanet!.Orbit!.Value.Apoapsis - planetarySystemInfo.InnerPlanet.Orbit!.Value.Periapsis) / 2);

            return addedChildren;
        }

        private List<CosmicLocation> GeneratePlanetsForStar(List<Star> stars, Star star, CosmicLocation? child = null)
        {
            var addedChildren = new List<CosmicLocation>();

            Planetoid? pregenPlanet = null;
            if (child is Planetoid candidate
                && (PlanetType.AnyTerrestrial.HasFlag(candidate.PlanetType)
                || PlanetType.Giant.HasFlag(candidate.PlanetType)))
            {
                pregenPlanet = candidate;
            }

            var (numGiants, numIceGiants, numTerrestrial) = star.GetNumPlanets();
            if (numGiants + numIceGiants + numTerrestrial == 0 && pregenPlanet is null)
            {
                return addedChildren;
            }

            var (minPeriapsis, maxApoapsis) = GetApsesLimits(stars, star);

            // The maximum mass and density are used to calculate an outer Roche limit (may not be
            // the actual Roche limit for the body which gets generated).
            var minGiantPeriapsis = HugeNumber.Max(minPeriapsis ?? 0, star.GetRocheLimit(Planetoid.GiantMaxDensity));
            var minTerrestialPeriapsis = HugeNumber.Max(minPeriapsis ?? 0, star.GetRocheLimit(Planetoid.DefaultTerrestrialMaxDensity));

            // If the calculated minimum and maximum orbits indicates that no stable orbits are
            // possible, eliminate the indicated type of planet.
            if (maxApoapsis.HasValue && minGiantPeriapsis > maxApoapsis)
            {
                numGiants = 0;
                numIceGiants = 0;
            }
            if (maxApoapsis.HasValue && minTerrestialPeriapsis > maxApoapsis)
            {
                numTerrestrial = 0;
            }

            // Generate planets one at a time until the specified number have been generated.
            var planetarySystemInfo = new PlanetarySystemInfo
            {
                NumTerrestrials = numTerrestrial,
                NumGiants = numGiants,
                NumIceGiants = numIceGiants,
                TotalGiants = numGiants + numIceGiants,
            };
            while (planetarySystemInfo.NumTerrestrials + planetarySystemInfo.NumGiants + planetarySystemInfo.NumIceGiants > 0 || pregenPlanet is not null)
            {
                addedChildren.AddRange(GeneratePlanet(
                    star,
                    stars,
                    minTerrestialPeriapsis,
                    minGiantPeriapsis,
                    maxApoapsis,
                    ref planetarySystemInfo,
                    pregenPlanet));
                pregenPlanet = null;
            }

            // Systems with terrestrial planets are also likely to have debris disks (Kuiper belts)
            // outside the orbit of the most distant planet.
            if (planetarySystemInfo.TotalTerrestrials > 0)
            {
                var belt = GenerateDebrisDisc(stars, star, planetarySystemInfo.OuterPlanet!, maxApoapsis);
                if (belt is not null)
                {
                    addedChildren.Add(belt);
                }
            }

            return addedChildren;
        }

        private Planetoid? GenerateTerrestrialPlanet(Star star, List<Star> stars, HugeNumber periapsis, out List<Planetoid> satellites)
        {
            // Planets with very low orbits are lava planets due to tidal stress (plus a small
            // percentage of others due to impact trauma).

            // The maximum mass and density are used to calculate an outer Roche limit (may not be
            // the actual Roche limit for the body which gets generated).
            var chance = Randomizer.Instance.NextDouble();
            var position = star.Position + (Vector3.UnitX * periapsis);
            var rocheLimit = star.GetRocheLimit(Planetoid.DefaultTerrestrialMaxDensity);
            if (periapsis < rocheLimit * new HugeNumber(105, -2) || chance <= 0.01)
            {
                return new Planetoid(PlanetType.Lava, this, star, stars, position, out satellites);
            }
            // Planets with close orbits may be iron planets.
            else if (periapsis < rocheLimit * 200 && chance <= 0.5)
            {
                return new Planetoid(PlanetType.Iron, this, star, stars, position, out satellites);
            }
            // Late-stage stars and brown dwarfs may have carbon planets.
            else if ((star.StarType == StarType.Neutron && chance <= 0.2) || (star.StarType == StarType.BrownDwarf && chance <= 0.75))
            {
                return new Planetoid(PlanetType.Carbon, this, star, stars, position, out satellites);
            }
            // Chance of an ocean planet.
            else if (chance <= 0.25)
            {
                return new Planetoid(PlanetType.Ocean, this, star, stars, position, out satellites);
            }
            else
            {
                return new Planetoid(PlanetType.Terrestrial, this, star, stars, position, out satellites);
            }
        }

        private List<CosmicLocation> GenerateTrojans(Star star, Planetoid planet, HugeNumber periapsis)
        {
            var addedChildren = new List<CosmicLocation>();

            var doubleHillRadius = planet.GetHillSphereRadius() * 2;
            var trueAnomaly = planet.Orbit!.Value.TrueAnomaly + DoubleConstants.ThirdPI; // +60°
            while (trueAnomaly > DoubleConstants.TwoPI)
            {
                trueAnomaly -= DoubleConstants.TwoPI;
            }
            var field = new AsteroidField(
                this,
                -Vector3.UnitZ * periapsis,
                new OrbitalParameters(
                    star.Mass,
                    star.Position,
                    periapsis,
                    planet.Orbit.Value.Eccentricity,
                    planet.Orbit.Value.Inclination,
                    Randomizer.Instance.NextDouble(DoubleConstants.TwoPI),
                    Randomizer.Instance.NextDouble(DoubleConstants.TwoPI),
                    trueAnomaly),
                majorRadius: doubleHillRadius);
            if (field is not null)
            {
                addedChildren.Add(field);
            }

            trueAnomaly = planet.Orbit.Value.TrueAnomaly - DoubleConstants.ThirdPI; // -60°
            while (trueAnomaly < 0)
            {
                trueAnomaly += DoubleConstants.TwoPI;
            }
            field = new AsteroidField(
                this,
                Vector3.UnitZ * periapsis,
                new OrbitalParameters(
                    star.Mass,
                    star.Position,
                    periapsis,
                    planet.Orbit.Value.Eccentricity,
                    planet.Orbit.Value.Inclination,
                    Randomizer.Instance.NextDouble(DoubleConstants.TwoPI),
                    Randomizer.Instance.NextDouble(DoubleConstants.TwoPI),
                    trueAnomaly),
                majorRadius: doubleHillRadius);
            if (field is not null)
            {
                addedChildren.Add(field);
            }

            return addedChildren;
        }

        private Planetoid? GetPlanet(
            Star star,
            List<Star> stars,
            HugeNumber minTerrestrialPeriapsis,
            HugeNumber minGiantPeriapsis,
            HugeNumber? maxApoapsis,
            ref PlanetarySystemInfo planetarySystemInfo,
            out List<Planetoid> satellites)
        {
            var isGiant = false;
            var isIceGiant = false;
            // If this is the first planet generated, and there are to be any
            // giants, generate a giant first.
            if (planetarySystemInfo.InnerPlanet is null && planetarySystemInfo.TotalGiants > 0)
            {
                if (planetarySystemInfo.NumGiants > 0)
                {
                    isGiant = true;
                    planetarySystemInfo.NumGiants--;
                }
                else
                {
                    isGiant = true;
                    isIceGiant = true;
                    planetarySystemInfo.NumIceGiants--;
                }
            }
            // Otherwise, select the type to generate on this pass randomly.
            else
            {
                var chance = Randomizer.Instance.NextDouble();
                if (planetarySystemInfo.NumGiants > 0 && (planetarySystemInfo.NumTerrestrials + planetarySystemInfo.NumIceGiants == 0 || chance <= 0.333333))
                {
                    isGiant = true;
                    planetarySystemInfo.NumGiants--;
                }
                else if (planetarySystemInfo.NumIceGiants > 0 && (planetarySystemInfo.NumTerrestrials == 0 || chance <= (planetarySystemInfo.NumGiants > 0 ? 0.666666 : 0.5)))
                {
                    isGiant = true;
                    isIceGiant = true;
                    planetarySystemInfo.NumIceGiants--;
                }
                // If a terrestrial planet is to be generated, the exact type will be determined later.
                else
                {
                    planetarySystemInfo.NumTerrestrials--;
                }
            }

            planetarySystemInfo.Periapsis = ChoosePlanetPeriapsis(
                star,
                isGiant,
                minTerrestrialPeriapsis,
                minGiantPeriapsis,
                maxApoapsis,
                planetarySystemInfo.InnerPlanet,
                planetarySystemInfo.OuterPlanet,
                planetarySystemInfo.MedianOrbit,
                planetarySystemInfo.TotalGiants);

            satellites = new List<Planetoid>();
            // If there is no room left for outer orbits, drop this planet and try again (until there
            // are none left to assign).
            if (!planetarySystemInfo.Periapsis.HasValue || planetarySystemInfo.Periapsis.Value.IsNaN)
            {
                return null;
            }

            // Now that a periapsis has been chosen, assign it as the position of giants.
            // (Terrestrials get their positions set during construction, below).
            Planetoid? planet;
            if (isGiant)
            {
                if (isIceGiant)
                {
                    planet = new Planetoid(PlanetType.IceGiant, this, star, stars, star.Position + (Vector3.UnitX * planetarySystemInfo.Periapsis.Value), out satellites);
                }
                else
                {
                    planet = new Planetoid(PlanetType.GasGiant, this, star, stars, star.Position + (Vector3.UnitX * planetarySystemInfo.Periapsis.Value), out satellites);
                }
            }
            else
            {
                planet = GenerateTerrestrialPlanet(star, stars, planetarySystemInfo.Periapsis.Value, out satellites);
                planetarySystemInfo.TotalTerrestrials++;
            }

            return planet;
        }

        private void Reconstitute(Vector3 position, HugeNumber radius, HugeNumber mass, double? temperature) => Material = new Material(
            Substances.All.InterplanetaryMedium,
            new Sphere(radius, position),
            mass,
            null,
            temperature);

        private struct PlanetarySystemInfo
        {
            public Planetoid? InnerPlanet;
            public Planetoid? OuterPlanet;
            public HugeNumber MedianOrbit;
            public int NumTerrestrials;
            public int NumGiants;
            public int NumIceGiants;
            public HugeNumber? Periapsis;
            public int TotalTerrestrials;
            public int TotalGiants;
        }
    }
}
