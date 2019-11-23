using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using NeverFoundry.WorldFoundry.CelestialBodies.Stars;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space.AsteroidFields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A region of space containing a system of stars, and the bodies which orbit that system.
    /// </summary>
    [Serializable]
    public class StarSystem : CelestialLocation
    {
        internal static readonly Number Space = new Number(3.5, 16);

        private protected override string BaseTypeName => "Star System";

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystem"/>.
        /// </summary>
        internal StarSystem() { }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystem"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="StarSystem"/>.</param>
        internal StarSystem(string? parentId, Vector3 position) : base(parentId, position) { }

        private StarSystem(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            string? parentId)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                parentId)
        { }

        private StarSystem(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string)))
        { }

        /// <summary>
        /// Gets a new <see cref="StarSystem"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the primary star.</typeparam>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// <paramref name="parent"/>.</param>
        /// <param name="spectralClass">The <see cref="SpectralClass"/> of the primary star.</param>
        /// <param name="luminosityClass">
        /// The <see cref="LuminosityClass"/> of the primary star.
        /// </param>
        /// <param name="populationII">Set to true if the primary star is to be a Population II
        /// star.</param>
        /// <returns>A new instance of the indicated <see cref="StarSystem"/> type, or <see
        /// langword="null"/> if no instance could be generated with the given parameters.</returns>
        public static async Task<StarSystem?> GetNewInstanceAsync<T>(
            Location? parent,
            Vector3 position,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) where T : Star
        {
            var instance = typeof(StarSystem).InvokeMember(
                null,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                new object?[] { parent?.Id, position }) as StarSystem;
            if (instance != null)
            {
                await instance.GenerateStarsAsync<T>(spectralClass, luminosityClass, populationII).ConfigureAwait(false);
                await instance.InitializeBaseAsync(parent).ConfigureAwait(false);
            }
            return instance;
        }

        private static Number GetTotalApoapsis(
            List<(
            Star star,
            Star orbited,
            double eccentricity,
            Number semiMajorAxis,
            Number periapsis,
            Number apoapsis)> companions,
            Star star,
            Number value)
        {
            var match = companions.FirstOrNull(x => x.star == star);
            if (match != null)
            {
                value += match.Value.apoapsis;
                return GetTotalApoapsis(companions, match.Value.orbited, value);
            }
            return value;
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
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(_albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(_material), _material);
            info.AddValue(nameof(ParentId), ParentId);
        }

        /// <summary>
        /// Enumerates the child stars of this instance.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of child <see cref="Star"/> instances of this
        /// <see cref="StarSystem"/>.</returns>
        public IAsyncEnumerable<Star> GetStarsAsync() => GetChildrenAsync().OfType<Star>();

        /// <summary>
        /// Updates the position and velocity of all direct child objects to correspond with the
        /// state predicted by their orbits at the current time of the containing <see
        /// cref="Universe"/>, assuming no influences on the bodies' motion have occurred aside from
        /// their orbits. Has no effect on bodies not in orbit (i.e. gravitational effects are not
        /// integrated over time for objects without defined orbits).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only affects direct children. Nested children of any contained regions are not affected.
        /// The child regions assigned to star systems by default are asteroid fields of various
        /// sorts, some of which are assigned orbits as a whole (e.g. trojan asteroid fields).
        /// </para>
        /// <para>
        /// The simplification of assuming that individual small bodies will keep their relative
        /// positions within a given field allows this method to perform relatively quickly even
        /// when many asteroids have been generated, but may become increasingly inaccurate if
        /// cometary bodies are generated which have eccentric orbits that should take them well
        /// away from their neighbors. It is left to calling code to decide when it is worth the
        /// calculation costs to update <i>all</i> children recursively.
        /// </para>
        /// </remarks>
        public async Task UpdateOrbitsAsync()
        {
            var universe = await GetContainingUniverseAsync().ConfigureAwait(false);
            if (universe is null)
            {
                return;
            }

            await GetChildrenAsync()
                .OfType<CelestialLocation>()
                .ForEachAwaitAsync(x => x.UpdateOrbitAsync())
                .ConfigureAwait(false);
        }

        internal override async Task<bool> GetIsHospitableAsync() => await GetStarsAsync().AllAsync(x => x.IsHospitable).ConfigureAwait(false);

        private protected override async Task PrepopulateRegionAsync()
        {
            if (_isPrepopulated)
            {
                return;
            }
            _isPrepopulated = true;

            var stars = await GetStarsAsync().ToListAsync().ConfigureAwait(false);

            var outerApoapsis = stars.Max(x => x.Orbit?.Apoapsis ?? 0);

            // All single and close-binary systems are presumed to have Oort clouds. Systems with
            // higher multiplicity are presumed to disrupt any Oort clouds.
            if (stars.Count == 1 || (stars.Count == 2 && outerApoapsis < new Number(1.5, 13)))
            {
                var cloud = await OortCloud.GetNewInstanceAsync(this, outerApoapsis, orbit: new OrbitalParameters(stars.Find(x => !x.Orbit.HasValue))).ConfigureAwait(false);
                if (cloud != null)
                {
                    await cloud.SaveAsync().ConfigureAwait(false);
                }
            }

            foreach (var star in stars)
            {
                await GeneratePlanetsForStarAsync(star).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds a companion <see cref="Star"/> orbiting the specified <see cref="Star"/> in the
        /// collection, with the indicated period.
        /// </summary>
        /// <param name="companions">A list of companion stars.</param>
        /// <param name="orbited">
        /// The existing <see cref="Star"/> in the collection which the new <see cref="Star"/> should orbit.
        /// </param>
        /// <param name="period">
        /// The period with which the new <see cref="Star"/> should orbit the <paramref
        /// name="orbited"/><see cref="Star"/>.
        /// </param>
        private async Task<(
            Star star,
            Star orbited,
            double eccentricity,
            Number semiMajorAxis,
            Number periapsis,
            Number apoapsis)?> AddCompanionStarAsync(List<(
            Star star,
            Star orbited,
            double eccentricity,
            Number semiMajorAxis,
            Number periapsis,
            Number apoapsis)> companions, Star orbited, Number period)
        {
            Star? star;

            // 20% chance that a white dwarf has a twin, and that a neutron star has a white dwarf companion.
            if ((orbited.GetType() == typeof(WhiteDwarf) || orbited.GetType() == typeof(NeutronStar))
                && Randomizer.Instance.NextDouble() <= 0.2)
            {
                star = await Star.GetNewInstanceAsync<WhiteDwarf>(this, Vector3.Zero).ConfigureAwait(false);
            }
            // There is a chance that a giant will have a giant companion.
            else if (orbited is GiantStar)
            {
                var chance = Randomizer.Instance.NextDouble();
                // Bright, super, and hypergiants are not generated as companions; if these exist in
                // the system, they are expected to be the primary.
                if (chance <= 0.25)
                {
                    star = await Star.GetNewInstanceAsync<RedGiant>(this, Vector3.Zero, luminosityClass: LuminosityClass.III).ConfigureAwait(false);
                }
                else if (chance <= 0.45)
                {
                    star = await Star.GetNewInstanceAsync<BlueGiant>(this, Vector3.Zero, luminosityClass: LuminosityClass.III).ConfigureAwait(false);
                }
                else if (chance <= 0.55)
                {
                    star = await Star.GetNewInstanceAsync<YellowGiant>(this, Vector3.Zero, luminosityClass: LuminosityClass.III).ConfigureAwait(false);
                }
                else
                {
                    star = await Star.GetNewInstanceAsync<Star>(this, Vector3.Zero, GetSpectralClassForCompanionStar(orbited)).ConfigureAwait(false);
                }
            }
            else
            {
                star = await Star.GetNewInstanceAsync<Star>(this, Vector3.Zero, GetSpectralClassForCompanionStar(orbited)).ConfigureAwait(false);
            }

            if (star != null)
            {
                // Eccentricity tends to be low but increase with longer periods.
                var eccentricity = Math.Abs((double)(Randomizer.Instance.NormalDistributionSample(0, 0.0001) * (period / new Number(3.1536, 9))));

                // Assuming an effective 2-body system, the period lets us determine the semi-major axis.
                var semiMajorAxis = ((period / MathConstants.TwoPI).Square() * ScienceConstants.G * (orbited.Mass + star.Mass)).CubeRoot();

                var periapsis = (1 - eccentricity) * semiMajorAxis;
                var apoapsis = (1 + eccentricity) * semiMajorAxis;

                var companion = (star, orbited, eccentricity, semiMajorAxis, periapsis, apoapsis);
                companions.Add(companion);
                return companion;
            }
            return null;
        }

        /// <summary>
        /// Generates a close period. Close periods are about 100 days, in a normal distribution
        /// constrained to 3-sigma.
        /// </summary>
        private Number GetClosePeriod()
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
                if (value >= min && value <= max)
                {
                    return value;
                }
                count++;
            } while (count < 100); // sanity check; should not be reached due to the nature of a normal distribution
            return value;
        }

        /// <summary>
        /// Starting with the initially-provided seed <see cref="Star"/>, the specified number of
        /// additional companions are added.
        /// </summary>
        /// <param name="amount">
        /// The number of additional <see cref="Star"/>s to add to this <see cref="StarSystem"/>.
        /// </param>
        private async Task<List<(
            Star star,
            Star orbited,
            double eccentricity,
            Number semiMajorAxis,
            Number periapsis,
            Number apoapsis)>> AddCompanionStarsAsync(int amount)
        {
            var companions = new List<(Star star, Star orbited, double eccentricity, Number semiMajorAxis, Number periapsis, Number apoapsis)>();
            var stars = await GetStarsAsync().ToListAsync().ConfigureAwait(false);
            if (stars.Count != 1 || amount <= 0)
            {
                return companions;
            }
            var primary = stars[0];
            var orbited = primary;

            // Most periods are about 50 years, in a log normal distribution. There is a chance of a
            // close binary, however.
            var close = false;
            Number companionPeriod;
            if (Randomizer.Instance.NextDouble() <= 0.2)
            {
                close = true;
                companionPeriod = GetClosePeriod();
            }
            else
            {
                companionPeriod = Randomizer.Instance.LogNormalDistributionSample(0, 1) * new Number(1.5768, 9);
            }
            var companion = await AddCompanionStarAsync(companions, orbited, companionPeriod).ConfigureAwait(false);
            if (!companion.HasValue)
            {
                return companions;
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
                await AddCompanionStarAsync(companions, orbited,
                    (Randomizer.Instance.LogNormalDistributionSample(0, 1) * new Number(1.5768, 9))
                    + (WorldFoundry.Space.Orbit.GetHillSphereRadius(
                        companion!.Value.star,
                        companion!.Value.orbited,
                        companion!.Value.semiMajorAxis,
                        companion!.Value.eccentricity) * 20)).ConfigureAwait(false);
            }
            else
            {
                await AddCompanionStarAsync(companions, orbited, GetClosePeriod()).ConfigureAwait(false);
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
                await AddCompanionStarAsync(companions, orbited, GetClosePeriod()).ConfigureAwait(false);
            }

            if (amount <= 3)
            {
                return companions;
            }
            // Any additional stars will orbit the entire system with long periods, possibly with
            // close companions of their own, forming increasingly large hierarchical systems (the
            // library does not generate 5-star or greater systems on its own, so this part of the
            // method will never get called unless a user implementing the library calls it directly
            // with a high number of stars).
            orbited = primary;
            var period = companionPeriod;
            var startingPeriod = period;
            for (var c = 4; c <= amount; c += 2) // Step of 2 since a companion is also generated.
            {
                // Period increases geometrically.
                period *= startingPeriod;

                companion = await AddCompanionStarAsync(companions, orbited, period).ConfigureAwait(false);

                // Add a close companion if an additional star is indicated.
                if (c < amount && companion.HasValue)
                {
                    await AddCompanionStarAsync(companions, companion!.Value.star, GetClosePeriod()).ConfigureAwait(false);
                }
            }

            return companions;
        }

        private Planemo? CapturePregenPlanet(
            List<Planemo> pregenPlanets,
            PlanetarySystemInfo planetarySystemInfo)
        {
            if (pregenPlanets.Count == 0)
            {
                return null;
            }

            var planet = pregenPlanets[0];

            planetarySystemInfo.Periapsis = planet.Orbit?.Periapsis ?? 0;

            if (planet is IceGiant)
            {
                planetarySystemInfo.NumIceGiants--;
            }
            else if (planet is GiantPlanet)
            {
                planetarySystemInfo.NumGiants--;
            }
            else
            {
                planetarySystemInfo.NumTerrestrials--;
                planetarySystemInfo.TotalTerrestrials++;
            }

            pregenPlanets.RemoveAt(0);

            return planet;
        }

        /// <summary>
        /// Single-planet orbital distance may follow a log-normal distribution, with the peak at 0.3
        /// AU (this does not conform to current observations exactly, but extreme biases in current
        /// observations make adequate overall distributions difficult to guess, and the
        /// approximation used is judged reasonably close). In multi-planet systems, migration and
        /// resonances result in a more widely-distributed system.
        /// </summary>
        /// <param name="star">The <see cref="Star"/> around which the planet will orbit.</param>
        /// <param name="planet">The <see cref="Planemo"/>.</param>
        /// <param name="minTerrestrialPeriapsis">The minimum periapsis for a terrestrial planet.</param>
        /// <param name="minGiantPeriapsis">The minimum periapsis for a giant planet.</param>
        /// <param name="maxApoapsis">The maximum apoapsis.</param>
        /// <param name="innerPlanet">The current innermost planet.</param>
        /// <param name="outerPlanet">The current outermost planet.</param>
        /// <param name="medianOrbit">The median orbit among the current planets.</param>
        /// <param name="totalGiants">The number of giant planets this <see cref="StarSystem"/> is to have.</param>
        /// <returns>The chosen periapsis, or null if no valid orbit is available.</returns>
        private async Task<Number?> ChoosePlanetPeriapsisAsync(
            Star star,
            Planemo? planet,
            Number minTerrestrialPeriapsis,
            Number minGiantPeriapsis,
            Number? maxApoapsis,
            Planemo? innerPlanet,
            Planemo? outerPlanet,
            Number medianOrbit,
            int totalGiants)
        {
            Number? periapsis = null;

            // If this is the first planet, the orbit is selected based on the number of giants the
            // system is to have.
            if (innerPlanet is null)
            {
                // Evaluates to ~0.3 AU if there is only 1 giant, ~5 AU if there are 4 giants (as
                // would be the case for the Solar system), and ~8 AU if there are 6 giants.
                var mean = 7.48e11 - ((4 - Math.Max(1, totalGiants)) * 2.34e11);
                var count = 0;
                while (count < 100
                    && (periapsis < (planet is GiantPlanet ? minGiantPeriapsis : minTerrestrialPeriapsis)
                    || (maxApoapsis.HasValue && periapsis > maxApoapsis)))
                {
                    periapsis = Randomizer.Instance.LogNormalDistributionSample(0, mean);
                    count++;
                }
                if (count == 100)
                {
                    periapsis = planet is GiantPlanet ? minGiantPeriapsis : minTerrestrialPeriapsis;
                }
            }
            // If there are already any planets and this planet is a giant, it is placed in a higher
            // orbit, never a lower one.
            else if (innerPlanet != null && planet is GiantPlanet)
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
                var otherMass = planet is GiantPlanet ? new Number(1.25, 28) : new Number(3, 25);
                if (periapsis < medianOrbit)
                {
                    // Inner orbital spacing is by an average of 21.7 mutual Hill radii, with a
                    // standard deviation of 9.5. An average planetary mass is used for the
                    // calculation since the planet hasn't been generated yet, which should produce
                    // reasonable values.
                    var spacing = await innerPlanet!.GetMutualHillSphereRadiusAsync(otherMass).ConfigureAwait(false)
                        * Randomizer.Instance.NormalDistributionSample(21.7, 9.5, minimum: 1);
                    periapsis = innerPlanet.Orbit!.Value.Periapsis - spacing;
                    if (periapsis < (planet is GiantPlanet ? minGiantPeriapsis : minTerrestrialPeriapsis))
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
                    if (!(planet is GiantPlanet) || outerPeriod <= 1.728e7)
                    {
                        var spacing = await outerPlanet.GetMutualHillSphereRadiusAsync(otherMass).ConfigureAwait(false)
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
                        var newPeriod = (Number)Randomizer.Instance.NormalDistributionSample(outerPeriod * 2.2, outerPeriod);

                        // Assuming no eccentricity and an average mass, calculate a periapsis from
                        // the selected period, but set their mutual Hill sphere radius as a minimum separation.
                        periapsis = Number.Max(outerPlanet.Orbit.Value.Apoapsis
                            + await outerPlanet.GetMutualHillSphereRadiusAsync(otherMass).ConfigureAwait(false),
                            ((newPeriod / MathConstants.TwoPI).Square() * ScienceConstants.G * (star.Mass + otherMass)).CubeRoot());
                    }
                }
            }

            return periapsis;
        }

        /// <summary>
        /// There is a chance of an inner-system asteroid belt inside the orbit of a giant.
        /// </summary>
        /// <param name="star">The <see cref="Star"/> around which the belt is to orbit.</param>
        /// <param name="planet">The <see cref="GiantPlanet"/> inside whose orbit the belt is to be placed.</param>
        /// <param name="periapsis">The periapsis of the belt.</param>
        private async Task GenerateAsteroidBeltAsync(Star star, GiantPlanet planet, Number periapsis)
        {
            var separation = periapsis - (await planet.GetMutualHillSphereRadiusAsync(new Number(3, 25)).ConfigureAwait(false) * Randomizer.Instance.NormalDistributionSample(21.7, 9.5));
            var belt = await AsteroidField.GetNewInstanceAsync(this, star.Position, separation * new Number(8, -1), separation * Number.Deci, orbit: new OrbitalParameters(star)).ConfigureAwait(false);
            if (belt != null)
            {
                await belt.SaveAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Systems with terrestrial planets are also likely to have debris disks (Kuiper belts)
        /// outside the orbit of the most distant planet.
        /// </summary>
        /// <param name="star">The <see cref="Star"/> around which the debris is to orbit.</param>
        /// <param name="outerPlanet">The outermost planet of the system.</param>
        /// <param name="maxApoapsis">The maximum apoapsis.</param>
        private async Task GenerateDebrisDiscAsync(Star star, Planemo outerPlanet, Number? maxApoapsis)
        {
            var outerApoapsis = outerPlanet.Orbit!.Value.Apoapsis;
            var innerRadius = outerApoapsis + (await outerPlanet.GetMutualHillSphereRadiusAsync(new Number(3, 25)).ConfigureAwait(false) * Randomizer.Instance.NormalDistributionSample(21.7, 9.5));
            var starCount = await GetStarsAsync().CountAsync().ConfigureAwait(false);
            var width = (starCount > 1 || Randomizer.Instance.NextBool())
                ? Randomizer.Instance.NextNumber(new Number(3, 12), new Number(4.5, 12))
                : Randomizer.Instance.LogNormalDistributionSample(0, 1) * new Number(7.5, 12);
            if (maxApoapsis.HasValue)
            {
                width = Number.Min(width, maxApoapsis.Value - innerRadius);
            }
            // Cannot be so wide that it overlaps the outermost planet's orbit.
            width = Number.Min(width, (innerRadius - outerApoapsis) * new Number(9, -1));
            if (width > 0)
            {
                var disc = await AsteroidField.GetNewInstanceAsync(this, star.Position, innerRadius + (width / 2), width, new OrbitalParameters(star)).ConfigureAwait(false);
                if (disc != null)
                {
                    await disc.SaveAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Determines the number of companion <see cref="Star"/>s this <see cref="StarSystem"/>
        /// will have, based on its primary star.
        /// </summary>
        /// <returns>
        /// The number of companion <see cref="Star"/>s this <see cref="StarSystem"/> will have.
        /// </returns>
        private async Task<int> GenerateNumCompanionsAsync()
        {
            var primary = await GetStarsAsync().FirstOrDefaultAsync().ConfigureAwait(false);
            if (primary is null)
            {
                return 0;
            }
            var chance = Randomizer.Instance.NextDouble();
            if (primary is BrownDwarf)
            {
                return 0;
            }
            else if (primary is WhiteDwarf)
            {
                if (chance <= 4.0 / 9.0)
                {
                    return 1;
                }
            }
            else if (primary is GiantStar || primary is NeutronStar)
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
                switch (primary?.SpectralClass ?? SpectralClass.None)
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

        private async Task GeneratePlanetAsync(
            Star star,
            Number minTerrestrialPeriapsis, Number minGiantPeriapsis, Number? maxApoapsis,
            PlanetarySystemInfo planetarySystemInfo,
            List<Planemo> pregenPlanets)
        {
            var planet = CapturePregenPlanet(
                pregenPlanets,
                planetarySystemInfo)
                ?? await GetPlanetAsync(star, minTerrestrialPeriapsis, minGiantPeriapsis, maxApoapsis, planetarySystemInfo).ConfigureAwait(false);
            if (planet is null)
            {
                return;
            }
            await planet.SaveAsync().ConfigureAwait(false);

            await planet.GenerateSatellitesAsync().ConfigureAwait(false);

            if (planet is GiantPlanet giant)
            {
                // Giants may get Trojan asteroid fields at their L4 & L5 Lagrangian points.
                if (Randomizer.Instance.NextBool())
                {
                    await GenerateTrojansAsync(star, giant, planetarySystemInfo.Periapsis!.Value).ConfigureAwait(false);
                }
                // There is a chance of an inner-system asteroid belt inside the orbit of a giant.
                if (planetarySystemInfo.Periapsis < planetarySystemInfo.MedianOrbit && Randomizer.Instance.NextDouble() <= 0.2)
                {
                    await GenerateAsteroidBeltAsync(star, giant, planetarySystemInfo.Periapsis!.Value).ConfigureAwait(false);
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
        }

        private async Task GeneratePlanetsForStarAsync(Star star)
        {
            var pregenPlanets = await GetChildrenAsync()
                .OfType<Planemo>()
                .WhereAwait(async x => !x.Orbit.HasValue || await x.Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false) == star)
                .ToListAsync()
                .ConfigureAwait(false);

            var (numGiants, numIceGiants, numTerrestrial) = star.GetNumPlanets();

            if (numGiants + numIceGiants + numTerrestrial == 0 && pregenPlanets.Count == 0)
            {
                return;
            }

            var (minPeriapsis, maxApoapsis) = await GetApsesLimitsAsync(star).ConfigureAwait(false);

            // The maximum mass and density are used to calculate an outer Roche limit (may not be
            // the actual Roche limit for the body which gets generated).
            var minGiantPeriapsis = Number.Max(minPeriapsis ?? 0, star.GetRocheLimit(GiantPlanet.MaxDensity));
            var minTerrestialPeriapsis = Number.Max(minPeriapsis ?? 0, star.GetRocheLimit(TerrestrialPlanet.BaseMaxDensity));

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
            while (planetarySystemInfo.NumTerrestrials + planetarySystemInfo.NumGiants + planetarySystemInfo.NumIceGiants > 0 || pregenPlanets.Count > 0)
            {
                await GeneratePlanetAsync(
                    star,
                    minTerrestialPeriapsis, minGiantPeriapsis, maxApoapsis,
                    planetarySystemInfo,
                    pregenPlanets).ConfigureAwait(false);
            }

            // Systems with terrestrial planets are also likely to have debris disks (Kuiper belts)
            // outside the orbit of the most distant planet.
            if (planetarySystemInfo.TotalTerrestrials > 0)
            {
                await GenerateDebrisDiscAsync(star, planetarySystemInfo.OuterPlanet!, maxApoapsis).ConfigureAwait(false);
            }
        }

        private async Task<List<Star>> GenerateStarsAsync(List<Star>? stars = null)
        {
            stars ??= new List<Star>();

            var numCompanions = await GenerateNumCompanionsAsync().ConfigureAwait(false);
            var companions = await AddCompanionStarsAsync(numCompanions).ConfigureAwait(false);

            // The Shape must be set before adding orbiting Stars, since it will be accessed during
            // the configuration of Orbits. However, the Shape of a StarSystem depends on the
            // configuration of the Stars, creating a circular dependency. This is resolved by
            // pre-calculating the orbit of the mutually-orbiting Stars (with ~75000 AU extra space,
            // or roughly 150% the outer limit for a potential Oort cloud), then adding the companion
            // Stars in their actual Orbits. This should give plenty of breathing room for any
            // objects with high eccentricity to stay within the system's local space, while not
            // placing the objects of interest (stars, planets) too close together in the center of
            // local space.
            var radius = new Number(1.125, 16) + companions.Sum(x => GetTotalApoapsis(companions, x.star, 0));

            // The mass of the stellar bodies is presumed to be at least 99% of the total, so it is used
            // as a close-enough approximation, plus a bit of extra.
            var mass = (stars.Sum(x => x.Mass) + companions.Sum(s => s.star.Mass)) * new Number(1001, -3);

            var shape = new Sphere(radius, Position);

            var substance = GetSubstance();
            if (substance is null)
            {
                Material = new Material(
                    (double)(mass / shape.Volume),
                    mass,
                    shape,
                    GetTemperature());
            }
            else
            {
                Material = new Material(
                    substance,
                    (double)(mass / shape.Volume),
                    mass,
                    shape,
                    GetTemperature());
            }

            foreach (var (star, orbited, eccentricity, semiMajorAxis, periapsis, apoapsis) in companions)
            {
                await WorldFoundry.Space.Orbit.SetOrbitAsync(
                    star,
                    orbited,
                    periapsis,
                    eccentricity,
                    Randomizer.Instance.NextDouble(Math.PI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI)).ConfigureAwait(false);

                await star.SaveAsync().ConfigureAwait(false);
                stars.Add(star);
            }

            return stars;
        }

        private async Task<List<Star>> GenerateStarsAsync<T>(
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) where T : Star
        {
            var stars = new List<Star>();
            var primary = await Star.GetNewInstanceAsync<T>(this, Vector3.Zero, spectralClass, luminosityClass, populationII).ConfigureAwait(false);
            if (primary != null)
            {
                await primary.SaveAsync().ConfigureAwait(false);
                stars.Add(primary);
            }

            return await GenerateStarsAsync(stars).ConfigureAwait(false);
        }

        private async Task<TerrestrialPlanet?> GenerateTerrestrialPlanetAsync(Star star, Number periapsis)
        {
            // Planets with very low orbits are lava planets due to tidal stress (plus a small
            // percentage of others due to impact trauma).

            // The maximum mass and density are used to calculate an outer Roche limit (may not be
            // the actual Roche limit for the body which gets generated).
            var chance = Randomizer.Instance.NextDouble();
            var position = star.Position + (Vector3.UnitX * periapsis);
            if (periapsis < star.GetRocheLimit(TerrestrialPlanet.BaseMaxDensity) * new Number(105, -2) || chance <= 0.01)
            {
                return await Planetoid.GetNewInstanceAsync<LavaPlanet>(this, position, star).ConfigureAwait(false);
            }
            // Planets with close orbits may be iron planets.
            else if (periapsis < star.GetRocheLimit(TerrestrialPlanet.BaseMaxDensity) * 200 && chance <= 0.5)
            {
                return await Planetoid.GetNewInstanceAsync<IronPlanet>(this, position, star).ConfigureAwait(false);
            }
            // Late-stage stars and brown dwarfs may have carbon planets.
            else if ((star is NeutronStar && chance <= 0.2) || (star is BrownDwarf && chance <= 0.75))
            {
                return await Planetoid.GetNewInstanceAsync<CarbonPlanet>(this, position, star).ConfigureAwait(false);
            }
            // Chance of an ocean planet.
            else if (chance <= 0.25)
            {
                return await Planetoid.GetNewInstanceAsync<OceanPlanet>(this, position, star).ConfigureAwait(false);
            }
            else
            {
                return await Planetoid.GetNewInstanceAsync<TerrestrialPlanet>(this, position, star).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Giants may get Trojan asteroid fields at their L4 and L5 Lagrangian points.
        /// </summary>
        /// <param name="star">The <see cref="Star"/> around which the Trojans are to orbit.</param>
        /// <param name="planet">The giant planet in whose orbit the Trojans will orbit.</param>
        /// <param name="periapsis">The periapsis of the orbit.</param>
        private async Task GenerateTrojansAsync(Star star, GiantPlanet planet, Number periapsis)
        {
            var doubleHillRadius = await planet.GetHillSphereRadiusAsync().ConfigureAwait(false) * 2;
            var trueAnomaly = planet.Orbit!.Value.TrueAnomaly + MathAndScience.Constants.Doubles.MathConstants.ThirdPI; // +60°
            while (trueAnomaly > MathConstants.TwoPI)
            {
                trueAnomaly -= MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            var orbit = new OrbitalParameters(
                star,
                periapsis,
                planet.Orbit.Value.Eccentricity,
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                trueAnomaly);
            var field = await AsteroidField.GetNewInstanceAsync(this, -Vector3.UnitZ * periapsis, doubleHillRadius, orbit: orbit).ConfigureAwait(false);
            if (field != null)
            {
                await field.SaveAsync().ConfigureAwait(false);
            }

            trueAnomaly = planet.Orbit.Value.TrueAnomaly - MathAndScience.Constants.Doubles.MathConstants.ThirdPI; // -60°
            while (trueAnomaly < 0)
            {
                trueAnomaly += MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            orbit = new OrbitalParameters(
                star,
                periapsis,
                planet.Orbit.Value.Eccentricity,
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                trueAnomaly);
            field = await AsteroidField.GetNewInstanceAsync(this, Vector3.UnitZ * periapsis, doubleHillRadius, orbit: orbit).ConfigureAwait(false);
            if (field != null)
            {
                await field.SaveAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Planets can orbit stably in a multiple-star system between the stars in a range up to
        /// ~33% of an orbiting star's Hill sphere, and ~33% of the distance to an orbited star's
        /// nearest orbiting star's Hill sphere. Alternatively, planets may orbit beyond the sphere
        /// of influence of a close companion, provided they are still not beyond the limits towards
        /// further orbiting stars.
        /// </summary>
        /// <param name="star">The <see cref="Star"/> whose apses' limits are to be calculated.</param>
        private async Task<(Number? minPeriapsis, Number? maxApoapsis)> GetApsesLimitsAsync(Star star)
        {
            Number? maxApoapsis = null;
            Number? minPeriapsis = null;
            if (star.Orbit != null)
            {
                maxApoapsis = await star.GetHillSphereRadiusAsync().ConfigureAwait(false) * 1 / 3;
            }

            await foreach (var entity in GetStarsAsync().WhereAwait(async x => x.Orbit.HasValue && await x.Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false) == star))
            {
                // If a star is orbiting within ~100 AU, it is considered too close for planets to
                // orbit in between, and orbits are only considered around them as a pair.
                if (entity.Orbit!.Value.Periapsis <= new Number(1.5, 13))
                {
                    minPeriapsis = await entity.GetHillSphereRadiusAsync().ConfigureAwait(false) * 20;
                    // Clear the maxApoapsis if it's within this outer orbit.
                    if (maxApoapsis.HasValue && maxApoapsis < minPeriapsis)
                    {
                        maxApoapsis = null;
                    }
                }
                else
                {
                    var candidateMaxApoapsis = (entity.Orbit.Value.Periapsis - await entity.GetHillSphereRadiusAsync().ConfigureAwait(false)) * Number.Third;
                    if (maxApoapsis.HasValue && maxApoapsis.Value < candidateMaxApoapsis)
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

        private protected override Task GenerateMaterialAsync()
        {
            if (_material is null)
            {
                // This should never be used, since Material is set when generating stars.
                Material = new Material(
                    0,
                    0,
                    new Sphere(new Number(1.125, 16), Position),
                    GetTemperature());
            }
            return Task.CompletedTask;
        }

        private async Task<Planemo?> GetPlanetAsync(
            Star star,
            Number minTerrestrialPeriapsis, Number minGiantPeriapsis, Number? maxApoapsis,
            PlanetarySystemInfo planetarySystemInfo)
        {
            Planemo? planet = null;

            // If this is the first planet generated, and there are to be any
            // giants, generate a giant first.
            if (planetarySystemInfo.InnerPlanet is null && planetarySystemInfo.TotalGiants > 0)
            {
                if (planetarySystemInfo.NumGiants > 0)
                {
                    planet = await Planetoid.GetNewInstanceAsync<GiantPlanet>(this, Vector3.Zero, star).ConfigureAwait(false);
                    planetarySystemInfo.NumGiants--;
                }
                else
                {
                    planet = await Planetoid.GetNewInstanceAsync<IceGiant>(this, Vector3.Zero, star).ConfigureAwait(false);
                    planetarySystemInfo.NumIceGiants--;
                }
            }
            // Otherwise, select the type to generate on this pass randomly.
            else
            {
                var chance = Randomizer.Instance.NextDouble();
                if (planetarySystemInfo.NumGiants > 0 && (planetarySystemInfo.NumTerrestrials + planetarySystemInfo.NumIceGiants == 0 || chance <= 0.333333))
                {
                    planet = await Planetoid.GetNewInstanceAsync<GiantPlanet>(this, Vector3.Zero, star).ConfigureAwait(false);
                    planetarySystemInfo.NumGiants--;
                }
                else if (planetarySystemInfo.NumIceGiants > 0 && (planetarySystemInfo.NumTerrestrials == 0 || chance <= (planetarySystemInfo.NumGiants > 0 ? 0.666666 : 0.5)))
                {
                    planet = await Planetoid.GetNewInstanceAsync<IceGiant>(this, Vector3.Zero, star).ConfigureAwait(false);
                    planetarySystemInfo.NumIceGiants--;
                }
                // If a terrestrial planet is to be generated, the exact type will be determined later.
                else
                {
                    planetarySystemInfo.NumTerrestrials--;
                }
            }

            planetarySystemInfo.Periapsis = await ChoosePlanetPeriapsisAsync(
                star,
                planet,
                minTerrestrialPeriapsis,
                minGiantPeriapsis,
                maxApoapsis,
                planetarySystemInfo.InnerPlanet,
                planetarySystemInfo.OuterPlanet,
                planetarySystemInfo.MedianOrbit,
                planetarySystemInfo.TotalGiants).ConfigureAwait(false);
            // If there is no room left for outer orbits, drop this planet and try again (until there
            // are none left to assign).
            if (!planetarySystemInfo.Periapsis.HasValue || planetarySystemInfo.Periapsis.Value.IsNaN)
            {
                return null;
            }

            // Now that a periapsis has been chosen, assign it as the position of giants.
            // (Terrestrials get their positions set during construction, below).
            if (planet is GiantPlanet)
            {
                planet.Position = star.Position + (Vector3.UnitX * planetarySystemInfo.Periapsis.Value);
            }
            else
            {
                planet = await GenerateTerrestrialPlanetAsync(star, planetarySystemInfo.Periapsis.Value).ConfigureAwait(false);
                planetarySystemInfo.TotalTerrestrials++;
            }

            return planet;
        }

        private SpectralClass GetSpectralClassForCompanionStar(Star primary)
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

        private protected override ISubstanceReference? GetSubstance()
            => Substances.All.InterplanetaryMedium.GetReference();

        private class PlanetarySystemInfo
        {
            public Planemo? InnerPlanet;
            public Planemo? OuterPlanet;
            public Number MedianOrbit = Number.Zero;
            public int NumTerrestrials;
            public int NumGiants;
            public int NumIceGiants;
            public Number? Periapsis;
            public int TotalTerrestrials;
            public int TotalGiants;
        }
    }
}
