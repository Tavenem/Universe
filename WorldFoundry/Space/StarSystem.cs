using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Extensions;
using WorldFoundry.Orbits;
using WorldFoundry.Space.AsteroidFields;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A region of space containing a system of stars, and the bodies which orbit that system.
    /// </summary>
    public class StarSystem : CelestialObject
    {
        internal new static string baseTypeName = "Star System";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private string _name;
        /// <summary>
        /// An optional name for this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="StarSystem"/> without a name of its own takes its name from its first named
        /// <see cref="Star"/>, if it has any.
        /// </remarks>
        public override string Name
        {
            get => string.IsNullOrEmpty(_name)
                ? Stars?.Where(x => !string.IsNullOrEmpty(x.Name)).FirstOrDefault()?.Name
                : _name;
            set => _name = value;
        }

        /// <summary>
        /// The <see cref="Star"/>s in this <see cref="StarSystem"/>.
        /// </summary>
        public ICollection<Star> Stars { get; set; }

        /// <summary>
        /// The name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string TypeName
        {
            get
            {
                var sb = new StringBuilder();
                if (Stars?.Count == 2)
                {
                    sb.Append("Binary ");
                }
                else if (Stars?.Count == 3)
                {
                    sb.Append("Ternary ");
                }
                else if (Stars?.Count >= 3)
                {
                    sb.Append("Multiple ");
                }
                sb.Append(BaseTypeName);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystem"/>.
        /// </summary>
        public StarSystem() { }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystem"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="StarSystem"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="StarSystem"/>.</param>
        /// <param name="starType">The type of <see cref="Star"/> to include in this <see cref="StarSystem"/>.</param>
        /// <param name="spectralClass">
        /// The <see cref="Stars.SpectralClass"/> of the <see cref="Star"/> to include in this <see
        /// cref="StarSystem"/> (if null, will be pseudo-randomly determined).
        /// </param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the <see cref="Star"/> to include in this <see
        /// cref="StarSystem"/> (if null, will be pseudo-randomly determined).
        /// </param>
        /// <param name="populationII">
        /// Set to true if the <see cref="Star"/> to include in this <see cref="StarSystem"/> is to
        /// be a Population II <see cref="Star"/>.
        /// </param>
        public StarSystem(
            CelestialObject parent,
            Vector3 position,
            Type starType,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position) => GenerateStars(starType, spectralClass, luminosityClass, populationII);

        private static double GetTotalApoapsis(
            List<(
            Star star,
            Star orbited,
            float eccentricity,
            double semiMajorAxis,
            double periapsis,
            double apoapsis)> companions,
            Star orbiter,
            double value)
        {
            var match = companions.FirstOrNull(x => x.star == orbiter);
            if (match != null)
            {
                value += match.Value.apoapsis;
                return GetTotalApoapsis(companions, match.Value.orbited, value);
            }
            return value;
        }

        /// <summary>
        /// Adds a companion <see cref="Star"/> orbiting the specified <see cref="Star"/> in the
        /// collection, with the indicated period.
        /// </summary>
        /// <param name="orbited">
        /// The existing <see cref="Star"/> in the collection which the new <see cref="Star"/> should orbit.
        /// </param>
        /// <param name="period">
        /// The period with which the new <see cref="Star"/> should orbit the <paramref
        /// name="orbited"/><see cref="Star"/>.
        /// </param>
        private (
            Star star,
            Star orbited,
            float eccentricity,
            double semiMajorAxis,
            double periapsis,
            double apoapsis) AddCompanionStar(List<(
            Star star,
            Star orbited,
            float eccentricity,
            double semiMajorAxis,
            double periapsis,
            double apoapsis)> companions, Star orbited, double period)
        {
            Star star = null;

            // 20% chance that a white dwarf has a twin, and that a neutron star has a white dwarf companion.
            if ((orbited.GetType() == typeof(WhiteDwarf) || orbited.GetType() == typeof(NeutronStar))
                && Randomizer.Static.NextDouble() <= 0.2)
            {
                star = new WhiteDwarf(this, Vector3.Zero);
            }
            // There is a chance that a giant will have a giant companion.
            else if (orbited is GiantStar)
            {
                var chance = Randomizer.Static.NextDouble();
                // Bright, super, and hypergiants are not generated as companions; if these exist in
                // the system, they are expected to be the primary.
                if (chance <= 0.25)
                {
                    star = new RedGiant(this, Vector3.Zero, LuminosityClass.III);
                }
                else if (chance <= 0.45)
                {
                    star = new BlueGiant(this, Vector3.Zero, LuminosityClass.III);
                }
                else if (chance <= 0.55)
                {
                    star = new YellowGiant(this, Vector3.Zero, LuminosityClass.III);
                }
                else
                {
                    star = new Star(this, Vector3.Zero, GetSpectralClassForCompanionStar(orbited));
                }
            }
            else
            {
                star = new Star(this, Vector3.Zero, GetSpectralClassForCompanionStar(orbited));
            }

            // Eccentricity tends to be low but increase with longer periods.
            var eccentricity = (float)Math.Round(Math.Abs(Randomizer.Static.Normal(0, 0.0001)) * (period / 3.1536e9), 5);

            // Assuming an effective 2-body system, the period lets us determine the semi-major axis.
            var semiMajorAxis = Math.Pow(Math.Pow(period / Utilities.MathUtil.Constants.TwoPI, 2) * Utilities.Science.Constants.G * (orbited.Mass + star.Mass), 1.0 / 3.0);

            var periapsis = Math.Round((1 - eccentricity) * semiMajorAxis);
            var apoapsis = (1 + eccentricity) * semiMajorAxis;

            var companion = (star, orbited, eccentricity, semiMajorAxis, periapsis, apoapsis);
            companions.Add(companion);
            return companion;
        }

        /// <summary>
        /// Generates a close period. Close periods are about 100 days, in a normal distribution
        /// constrained to 3-sigma.
        /// </summary>
        private double GetClosePeriod()
        {
            var count = 0;
            var value = 0.0;
            var mu = 36000;
            var sigma = 1.732e7;
            var min = mu - 3 * sigma;
            var max = mu + 3 * sigma;
            // loop rather than constraining to limits in order to avoid over-representing the limits
            do
            {
                value = Randomizer.Static.Normal(mu, sigma);
                if (value >= min && value <= max)
                {
                    return value;
                }
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
        private List<(
            Star star,
            Star orbited,
            float eccentricity,
            double semiMajorAxis,
            double periapsis,
            double apoapsis)> AddCompanionStars(int amount)
        {
            var companions = new List<(Star star, Star orbited, float eccentricity, double semiMajorAxis, double periapsis, double apoapsis)>();
            if (Stars?.Count != 1 || amount <= 0)
            {
                return companions;
            }
            var primary = Stars.First();
            var orbited = primary;

            // Most periods are about 50 years, in a log normal distribution. There is a chance of a
            // close binary, however.
            var close = false;
            var companionPeriod = 0.0;
            if (Randomizer.Static.NextDouble() <= 0.2)
            {
                close = true;
                companionPeriod = GetClosePeriod();
            }
            else
            {
                companionPeriod = Randomizer.Static.Lognormal(0, 1.5768e9);
            }
            var companion = AddCompanionStar(companions, orbited, companionPeriod);

            if (amount <= 1)
            {
                return companions;
            }
            // A third star will orbit either the initial star, or will orbit the second in a close
            // orbit, establishing a hierarchical system.

            // If the second star was given a close orbit, the third will automatically orbit the
            // original star with a long period.
            orbited = close || Randomizer.Static.NextBoolean() ? primary : companion.star;

            // Long periods are about 50 years, in a log normal distribution, shifted out to avoid
            // being too close to the 2nd star's close orbit.
            if (close)
            {
                AddCompanionStar(companions, orbited,
                    Randomizer.Static.Lognormal(0, 1.5768e9)
                    + Orbit.GetHillSphereRadius(
                        companion.star,
                        companion.orbited,
                        companion.semiMajorAxis,
                        companion.eccentricity) * 20);
            }
            else
            {
                AddCompanionStar(companions, orbited, GetClosePeriod());
            }

            if (amount <= 2)
            {
                return companions;
            }
            // A fourth star will orbit whichever star of the original two does not already have a
            // close companion, in a close orbit of its own.
            orbited = orbited == primary ? companion.star : primary;

            AddCompanionStar(companions, primary, GetClosePeriod());

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
            for (int c = 4; c <= amount; c += 2) // Step of 2 since a companion is also generated.
            {
                // Period increases geometrically.
                period = period * startingPeriod;

                companion = AddCompanionStar(companions, primary, period);

                // Add a close companion if an additional star is indicated.
                if (c < amount)
                {
                    AddCompanionStar(companions, companion.star, GetClosePeriod());
                }
            }

            return companions;
        }

        private Planemo CapturePregenPlanet(
            List<Planemo> pregenPlanets,
            out double? periapsis,
            ref int numTerrestrials, ref int numGiants, ref int numIceGiants,
            ref int totalTerrestrials)
        {
            periapsis = null;

            if ((pregenPlanets?.Count ?? 0) < 1)
            {
                return null;
            }

            var planet = pregenPlanets[0];

            periapsis = planet.Orbit?.Periapsis ?? 0;

            if (planet is IceGiant)
            {
                numIceGiants--;
            }
            else if (planet is GiantPlanet)
            {
                numGiants--;
            }
            else
            {
                numTerrestrials--;
                totalTerrestrials++;
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
        /// <returns>The chosen periapsis, or null if no valid orbit is available.</returns>
        private double? ChoosePlanetPeriapsis(
            Star star,
            Planemo planet,
            double minTerrestrialPeriapsis,
            double minGiantPeriapsis,
            double? maxApoapsis,
            Planemo innerPlanet,
            Planemo outerPlanet,
            double medianOrbit,
            int totalGiants)
        {
            double? periapsis = null;

            // If this is the first planet, the orbit is selected based on the number of giants the
            // system is to have.
            if (innerPlanet == null)
            {
                // Evaluates to ~0.3 AU if there is only 1 giant, ~5 AU if there are 4 giants (as
                // would be the case for the Solar system), and ~8 AU if there are 6 giants.
                var mean = 7.48e11 - ((4 - Math.Max(1, totalGiants)) * 2.34e11);
                var count = 0;
                while (count < 100
                    && (periapsis < (planet is GiantPlanet ? minGiantPeriapsis : minTerrestrialPeriapsis)
                    || (maxApoapsis.HasValue && periapsis > maxApoapsis)))
                {
                    periapsis = (float)Randomizer.Static.Lognormal(0, mean);
                    count++;
                }
                if (count == 100)
                {
                    periapsis = (planet is GiantPlanet ? minGiantPeriapsis : minTerrestrialPeriapsis);
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
            else if (Randomizer.Static.NextDouble() <= 0.75)
            {
                periapsis = medianOrbit / 2;
            }
            else
            {
                periapsis = medianOrbit;
            }

            if (outerPlanet != null)
            {
                if (periapsis < medianOrbit)
                {
                    // Inner orbital spacing is by an average of 21.7 mutual Hill radii, with a
                    // standard deviation of 9.5. An average planetary mass is used for the
                    // calculation since the planet hasn't been generated yet, which should produce
                    // reasonable values.
                    var spacing = innerPlanet.Orbit.GetMutualHillSphereRadius(planet is GiantPlanet ? 1.25e28 : 3.0e25)
                        * Math.Max(1, Randomizer.Static.Normal(21.7, 9.5));
                    periapsis = innerPlanet.Orbit.Periapsis - spacing;
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
                    var outerPeriod = outerPlanet.Orbit.Period;
                    if (!(planet is GiantPlanet) || outerPeriod <= 1.728e7)
                    {
                        var spacing = outerPlanet.Orbit.GetMutualHillSphereRadius(planet is GiantPlanet ? 1.25e28 : 3.0e25)
                            * Math.Max(1, Randomizer.Static.Normal(21.7, 9.5));
                        periapsis = outerPlanet.Orbit.Apoapsis + spacing;
                        if (periapsis > maxApoapsis)
                        {
                            return null;
                        }
                    }
                    // Beyond 200 days, a Gaussian distribution of mean-motion resonance with a mean
                    // of 2.2 is used to determine orbital spacing for giant planets.
                    else
                    {
                        var newPeriod = Randomizer.Static.Normal(outerPeriod * 2.2, outerPeriod);

                        // Assuming no eccentricity and an average mass, calculate a periapsis from
                        // the selected period, but set their mutual Hill sphere radius as a minimum separation.
                        periapsis = Math.Max(outerPlanet.Orbit.Apoapsis
                            + outerPlanet.Orbit.GetMutualHillSphereRadius(
                                planet is GiantPlanet ? 1.25e28 : 3.0e25),
                                Math.Pow(Math.Pow(newPeriod / Utilities.MathUtil.Constants.TwoPI, 2)
                                * Utilities.Science.Constants.G
                                * (star.Mass + (planet is GiantPlanet ? 1.25e28 : 3.0e25)),
                                1.0 / 3.0));
                    }
                }
            }

            return periapsis;
        }

        /// <summary>
        /// There is a chance of an inner-system asteroid belt inside the orbit of a giant.
        /// </summary>
        private void GenerateAsteroidBelt(Star star, GiantPlanet planet, double periapsis)
        {
            var separation = periapsis - (planet.Orbit.GetMutualHillSphereRadius(3.0e25) * Randomizer.Static.Normal(21.7, 9.5));
            new AsteroidField(this, star.Position, star, separation * 0.8, separation * 0.1);
        }

        /// <summary>
        /// Generates the planetary systems of this star system, along with all small star system bodies.
        /// </summary>
        /// <remarks>
        /// This is called automatically the first time PopulateRegion is called. Any planets already
        /// added to the star system manually will be taken into account by the automatic generation
        /// procedures when filling in the rest of the system.
        /// </remarks>
        private void GenerateChildren()
        {
            if ((Stars?.Count ?? 0) == 0)
            {
                return;
            }

            var outerApoapsis = Stars.Max(x => x.Orbit?.Apoapsis ?? 0);

            // All single and close-binary systems are presumed to have Oort clouds. Systems with
            // higher multiplicity are presumed to disrupt any Oort clouds.
            if (Stars.Count == 1 || (Stars.Count == 2 && outerApoapsis < 1.5e13))
            {
                var primary = Stars.FirstOrDefault(x => x.Orbit == null);
                new OortCloud(this, primary, outerApoapsis);
            }

            foreach (var star in Stars)
            {
                GeneratePlanetsForStar(star);
            }
        }

        /// <summary>
        /// Systems with terrestrial planets are also likely to have debris disks (Kuiper belts)
        /// outside the orbit of the most distant planet.
        /// </summary>
        private void GenerateDebrisDisc(Star star, Planemo outerPlanet, double? maxApoapsis)
        {
            var outerApoapsis = outerPlanet.Orbit.Apoapsis;
            var innerRadius = outerApoapsis + (outerPlanet.Orbit.GetMutualHillSphereRadius(3.0e25) * Randomizer.Static.Normal(21.7, 9.5));
            var width = (Stars.Count > 1 || Randomizer.Static.NextDouble() <= 0.5)
                ? Randomizer.Static.NextDouble(3.0e12, 4.5e12)
                : Randomizer.Static.Lognormal(0, 7.5e12);
            if (maxApoapsis.HasValue)
            {
                width = Math.Min(width, maxApoapsis.Value - innerRadius);
            }
            // Cannot be so wide that it overlaps the outermost planet's orbit.
            width = Math.Min(width, (innerRadius - outerApoapsis) * 0.9);
            if (width > 0)
            {
                new AsteroidField(this, star.Position, star, innerRadius + (width / 2), width);
            }
        }

        /// <summary>
        /// Determines the number of companion <see cref="Star"/>s this <see cref="StarSystem"/>
        /// will have, based on its primary star.
        /// </summary>
        /// <returns>
        /// The number of companion <see cref="Star"/>s this <see cref="StarSystem"/> will have.
        /// </returns>
        private int GenerateNumCompanions()
        {
            var primary = Stars.FirstOrDefault();
            var chance = Randomizer.Static.NextDouble();
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

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// The mass of the stellar bodies is presumed to be at least 99% of the total, so it is used
        /// as a close-enough approximation, plus a bit of extra.
        /// </remarks>
        protected override void GenerateMass() => Mass = (Stars?.Sum(s => s.Mass) ?? 0) * 1.001;

        private void GeneratePlanet(
            Star star,
            double minTerrestrialPeriapsis, double minGiantPeriapsis, double? maxApoapsis,
            ref Planemo innerPlanet, ref Planemo outerPlanet,
            ref double medianOrbit,
            ref int numTerrestrials, ref int numGiants, ref int numIceGiants,
            ref int totalTerrestrials, int totalGiants,
            List<Planemo> pregenPlanets)
        {
            Planemo planet = CapturePregenPlanet(
                pregenPlanets,
                out var periapsis,
                ref numTerrestrials, ref numGiants, ref numIceGiants,
                ref totalTerrestrials);
            if (planet == null)
            {
                GetPlanet(
                    star,
                    minTerrestrialPeriapsis, minGiantPeriapsis, maxApoapsis, ref periapsis,
                    innerPlanet, outerPlanet,
                    medianOrbit,
                    ref numTerrestrials, ref numGiants, ref numIceGiants,
                    ref totalTerrestrials, totalGiants);
            }
            if (planet == null)
            {
                return;
            }

            planet.GenerateSatellites();

            if (planet is GiantPlanet giant)
            {
                // Giants may get Trojan asteroid fields at their L4 & L5 Lagrangian points.
                if (Randomizer.Static.NextDouble() <= 0.5)
                {
                    GenerateTrojans(star, giant, periapsis.Value);
                }
                // There is a chance of an inner-system asteroid belt inside the orbit of a giant.
                if (periapsis < medianOrbit && Randomizer.Static.NextDouble() <= 0.2)
                {
                    GenerateAsteroidBelt(star, giant, periapsis.Value);
                }
            }

            if (innerPlanet == null)
            {
                innerPlanet = planet;
                outerPlanet = planet;
            }
            else if (periapsis < medianOrbit)
            {
                innerPlanet = planet;
            }
            else
            {
                outerPlanet = planet;
            }

            medianOrbit = innerPlanet.Orbit.Periapsis
                + ((outerPlanet.Orbit.Apoapsis - innerPlanet.Orbit.Periapsis) / 2);
        }

        private void GeneratePlanetsForStar(Star star)
        {
            var pregenPlanets = Children
                .Where(c => c is Planemo p && (p.Orbit == null || p.Orbit.OrbitedObject == star))
                .Cast<Planemo>().ToList();

            var (numGiants, numIceGiants, numTerrestrial) = star.GetNumPlanets();

            if (numGiants + numIceGiants + numTerrestrial == 0 && (pregenPlanets?.Count ?? 0) == 0)
            {
                return;
            }

            var (minPeriapsis, maxApoapsis) = GetApsesLimits(star);

            // The maximum mass and density are used to calculate an outer Roche limit (may not be
            // the actual Roche limit for the body which gets generated).
            var minGiantPeriapsis = Math.Max(minPeriapsis ?? 0, star.GetRocheLimit(GiantPlanet.maxDensity));
            var minTerrestialPeriapsis = Math.Max(minPeriapsis ?? 0, star.GetRocheLimit(TerrestrialPlanet.maxDensity));

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

            int totalGiants = numGiants + numIceGiants;
            int totalTerrestrials = 0;

            // Generate planets one at a time until the specified number have been generated.
            Planemo innerPlanet = null;
            Planemo outerPlanet = null;
            double medianOrbit = 0;
            while (numTerrestrial + numGiants + numIceGiants > 0 || (pregenPlanets?.Count ?? 0) > 0)
            {
                GeneratePlanet(
                    star,
                    minTerrestialPeriapsis, minGiantPeriapsis, maxApoapsis,
                    ref innerPlanet, ref outerPlanet,
                    ref medianOrbit,
                    ref numTerrestrial, ref numGiants, ref numIceGiants,
                    ref totalTerrestrials, totalGiants,
                    pregenPlanets);
            }

            // Systems with terrestrial planets are also likely to have debris disks (Kuiper belts)
            // outside the orbit of the most distant planet.
            if (totalTerrestrials > 0)
            {
                GenerateDebrisDisc(star, outerPlanet, maxApoapsis);
            }
        }

        private void GenerateStars(
            Type starType,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false)
        {
            if (starType != null && starType == typeof(Star) || starType.IsSubclassOf(typeof(Star)))
            {
                Stars.Add((Star)starType.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null,
                    new object[] { this, Vector3.Zero, spectralClass, luminosityClass, populationII }));
            }

            var numCompanions = GenerateNumCompanions();

            var companions = AddCompanionStars(numCompanions);
            // The Shape must be set before adding orbiting Stars, since it will be accessed during
            // the configuration of Orbits. However, the Shape of a StarSystem depends on the
            // configuration of the Stars, creating a circular dependency. This is resolved by
            // pre-calculating the outer radius of the mutually-orbiting Stars in order to first
            // create a Shape, then adding the companion Stars in their Orbits. The radius of the
            // system is set ~75000 AU past the outer orbit of the stars, or roughly 150% the
            // distance from the outer limit of any Oort cloud. This should give plenty of breathing
            // room for any objects with high eccentricity to stay within the system's local space
            // while not placing the objects of interest (stars, planets) too close together in the
            // center of local space.
            var radius = 1.125e16 + companions.Max(x => GetTotalApoapsis(companions, x.star, 0));
            Shape = new Sphere(radius);
            foreach (var c in companions)
            {
                Orbit.SetOrbit(
                    c.star,
                    c.orbited,
                    c.periapsis,
                    c.eccentricity,
                    (float)Math.Round(Randomizer.Static.NextDouble(Math.PI), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4));

                Stars.Add(c.star);
            }
        }

        private TerrestrialPlanet GenerateTerrestrialPlanet(Star star, double periapsis)
        {
            // Planets with very low orbits are lava planets due to tidal stress (plus a small
            // percentage of others due to impact trauma).

            // The maximum mass and density are used to calculate an outer Roche limit (may not be
            // the actual Roche limit for the body which gets generated).
            var chance = Randomizer.Static.NextDouble();
            var position = ((star.Position * LocalScale) + (Vector3.UnitX * (float)periapsis)) / LocalScale;
            if (periapsis < star.GetRocheLimit(TerrestrialPlanet.maxDensity) * 1.05 || chance <= 0.01)
            {
                return new LavaPlanet(this, position);
            }
            // Planets with close orbits may be iron planets.
            else if (periapsis < star.GetRocheLimit(TerrestrialPlanet.maxDensity) * 200 && chance <= 0.5)
            {
                return new IronPlanet(this, position);
            }
            // Late-stage stars and brown dwarfs may have carbon planets.
            else if ((star is NeutronStar && chance <= 0.2) || (star is BrownDwarf && chance <= 0.75))
            {
                return new CarbonPlanet(this, position);
            }
            // Chance of an ocean planet.
            else if (chance <= 0.25)
            {
                return new OceanPlanet(this, position);
            }
            else
            {
                return new TerrestrialPlanet(this, position);
            }
        }

        /// <summary>
        /// Giants may get Trojan asteroid fields at their L4 & L5 Lagrangian points.
        /// </summary>
        private void GenerateTrojans(Star star, GiantPlanet planet, double periapsis)
        {
            var doubleHillRadius = planet.Orbit.GetHillSphereRadius() * 2;
            var asteroids = new AsteroidField(this, (-Vector3.UnitZ * (float)periapsis) / LocalScale, star, doubleHillRadius);
            var trueAnomaly = planet.Orbit.TrueAnomaly + 1.04719755f; // +60°
            while (trueAnomaly > Utilities.MathUtil.Constants.TwoPI)
            {
                trueAnomaly -= (float)Utilities.MathUtil.Constants.TwoPI;
            }
            Orbits.Orbit.SetOrbit(
                asteroids,
                star,
                periapsis,
                planet.Orbit.Eccentricity,
                (float)Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                trueAnomaly);

            asteroids = new AsteroidField(this, (Vector3.UnitZ * (float)periapsis) / LocalScale, star, doubleHillRadius);
            trueAnomaly = planet.Orbit.TrueAnomaly - 1.04719755f; // -60°
            while (trueAnomaly < 0)
            {
                trueAnomaly += (float)Utilities.MathUtil.Constants.TwoPI;
            }
            Orbits.Orbit.SetOrbit(
                asteroids,
                star,
                periapsis,
                planet.Orbit.Eccentricity,
                (float)Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                trueAnomaly);
        }

        /// <summary>
        /// Planets can orbit stably in a multiple-star system between the stars in a range up to
        /// ~33% of an orbiting star's Hill sphere, and ~33% of the distance to an orbited star's
        /// nearest orbiting star's Hill sphere. Alternatively, planets may orbit beyond the sphere
        /// of influence of a close companion, provided they are still not beyond the limits towards
        /// further orbiting stars.
        /// </summary>
        private (double? minPeriapsis, double? maxApoapsis) GetApsesLimits(Star star)
        {
            double? maxApoapsis = null;
            double? minPeriapsis = null;
            if (star.Orbit != null)
            {
                maxApoapsis = star.Orbit.GetHillSphereRadius() * 1 / 3;
            }

            foreach (var orbiter in Stars.Where(s => s.Orbit != null && s.Orbit.OrbitedObject == star))
            {
                // If a star is orbiting within ~100 AU, it is considered too close for planets to
                // orbit in between, and orbits are only considered around them as a pair.
                if (orbiter.Orbit.Periapsis <= 1.5e13)
                {
                    minPeriapsis = orbiter.Orbit.GetHillSphereRadius() * 20;
                    // Clear the maxApoapsis if it's within this outer orbit.
                    if (maxApoapsis.HasValue && maxApoapsis < minPeriapsis)
                    {
                        maxApoapsis = null;
                    }
                }
                else
                {
                    var candidateMaxApoapsis = (orbiter.Orbit.Periapsis - orbiter.Orbit.GetHillSphereRadius()) * 1 / 3;
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

        private Planemo GetPlanet(
            Star star,
            double minTerrestrialPeriapsis, double minGiantPeriapsis, double? maxApoapsis, ref double? periapsis,
            Planemo innerPlanet, Planemo outerPlanet,
            double medianOrbit,
            ref int numTerrestrials, ref int numGiants, ref int numIceGiants,
            ref int totalTerrestrials, int totalGiants)
        {
            Planemo planet = null;

            // If this is the first planet generated, and there are to be any
            // giants, generate a giant first.
            if (innerPlanet == null && totalGiants > 0)
            {
                if (numGiants > 0)
                {
                    planet = new GiantPlanet(this);
                    numGiants--;
                }
                else
                {
                    planet = new IceGiant(this);
                    numIceGiants--;
                }
            }
            // Otherwise, select the type to generate on this pass randomly.
            else
            {
                var chance = Randomizer.Static.NextDouble();
                if (numGiants > 0 && (numTerrestrials + numIceGiants == 0 || chance <= 0.333333))
                {
                    planet = new GiantPlanet(this);
                    numGiants--;
                }
                else if (numIceGiants > 0 && (numTerrestrials == 0 || chance <= (numGiants > 0 ? 0.666666 : 0.5)))
                {
                    planet = new IceGiant(this);
                    numIceGiants--;
                }
                // If a terrestrial planet is to be generated, the exact type will be determined later.
                else
                {
                    numTerrestrials--;
                }
            }

            periapsis = ChoosePlanetPeriapsis(
                star,
                planet,
                minTerrestrialPeriapsis,
                minGiantPeriapsis,
                maxApoapsis,
                innerPlanet,
                outerPlanet,
                medianOrbit,
                totalGiants);
            // If there is no room left for outer orbits, drop this planet and try again (until there
            // are none left to assign).
            if (!periapsis.HasValue || double.IsNaN(periapsis.Value))
            {
                if (planet is GiantPlanet)
                {
                    Children.Remove(planet);
                }
                return null;
            }

            // Now that a periapsis has been chosen, assign it as the position of giants.
            // (Terrestrials get their positions set during construction, below).
            if (planet is GiantPlanet)
            {
                planet.Position = ((star.Position * LocalScale) + (Vector3.UnitX * (float)periapsis.Value)) / LocalScale;
            }
            else
            {
                planet = GenerateTerrestrialPlanet(star, periapsis.Value);
                totalTerrestrials++;
            }

            planet.GenerateOrbit(star);

            return planet;
        }

        private SpectralClass GetSpectralClassForCompanionStar(Star primary)
        {
            var chance = Randomizer.Static.NextDouble();
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

        /// <summary>
        /// Generates an appropriate population of child objects in local space, in an area around
        /// the given position.
        /// </summary>
        /// <param name="position">The location around which to generate child objects.</param>
        /// <remarks>
        /// Star systems have all their immediate children generated at once, the first time this
        /// method is called.
        /// </remarks>
        public override void PopulateRegion(Vector3 position)
        {
            if (!IsGridSpacePopulated(Vector3.Zero))
            {
                GetGridSpace(Vector3.Zero, true).Populated = true;
                GenerateChildren();
            }
        }

        /// <summary>
        /// Returns a string that represents the celestial object.
        /// </summary>
        /// <returns>A string that represents the celestial object.</returns>
        public override string ToString() => $"{TypeName} {Title}";
    }
}
