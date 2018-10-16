using MathAndScience;
using Substances;
using System;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.Climate;
using WorldFoundry.Orbits;
using WorldFoundry.Space;
using WorldFoundry.Substances;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A dwarf planet: a body large enough to form a roughly spherical shape under its own gravity,
    /// but not large enough to clear its orbital "neighborhood" of smaller bodies.
    /// </summary>
    public class DwarfPlanet : Planemo
    {
        /// <summary>
        /// The radius of the maximum space required by this type of <see cref="CelestialEntity"/>,
        /// in meters.
        /// </summary>
        public const double Space = 1.5e6;

        private const string _baseTypeName = "Dwarf Planet";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        internal static double _densityForType = 2000;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double DensityForType => _densityForType;

        internal static double _maxMassForType = 2.0e22;
        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets.
        /// Within orbital systems, a calculated value for clearing the neighborhood is used instead.
        /// </remarks>
        internal override double? MaxMassForType => _maxMassForType;

        internal static double _minMassForType = 3.4e20;
        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>
        /// The minimum to achieve hydrostatic equilibrium and be considered a dwarf planet.
        /// </remarks>
        internal override double? MinMassForType => _minMassForType;

        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>
        /// There is a low chance of most planets having substantial rings; 10 for <see
        /// cref="DwarfPlanet"/>s.
        /// </remarks>
        protected new static double RingChance => 10;

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/>.
        /// </summary>
        public DwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        public DwarfPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="DwarfPlanet"/> during random generation, in kg.
        /// </param>
        public DwarfPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfPlanet"/>.</param>
        public DwarfPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="DwarfPlanet"/> during random generation, in kg.
        /// </param>
        public DwarfPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        private protected override void GenerateAlbedo()
        {
            var surface = Substance.Composition.GetSurface();
            var surfaceIce = 0.0;
            if (surface != null)
            {
                surfaceIce = surface.GetChemicals(Phase.Solid).Sum(x => x.proportion);
            }

            var albedo = Randomizer.Instance.NextDouble(0.1, 0.6);
            Albedo = (albedo * (1.0 - surfaceIce)) + (0.9 * surfaceIce);
        }

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateAtmosphere()
        {
            // Atmosphere is based solely on the volatile ices present.

            var crust = Substance.Composition.GetSurface();

            var water = crust.GetProportion(Chemical.Water, Phase.Solid);
            var anyIces = water > 0;

            var n2 = crust.GetProportion(Chemical.Nitrogen, Phase.Solid);
            anyIces &= n2 > 0;

            var ch4 = crust.GetProportion(Chemical.Methane, Phase.Solid);
            anyIces &= ch4 > 0;

            var co = crust.GetProportion(Chemical.CarbonMonoxide, Phase.Solid);
            anyIces &= co > 0;

            var co2 = crust.GetProportion(Chemical.CarbonDioxide, Phase.Solid);
            anyIces &= co2 > 0;

            var nh3 = crust.GetProportion(Chemical.Ammonia, Phase.Solid);
            anyIces &= nh3 > 0;

            if (!anyIces)
            {
                Atmosphere = new Atmosphere(this, Material.Empty(), 0);
                return;
            }

            var atmosphere = new Composite();
            if (water > 0)
            {
                atmosphere.Components[(Chemical.Water, Phase.Gas)] = water;
            }
            if (n2 > 0)
            {
                atmosphere.Components[(Chemical.Nitrogen, Phase.Gas)] = n2;
            }
            if (ch4 > 0)
            {
                atmosphere.Components[(Chemical.Methane, Phase.Gas)] = ch4;
            }
            if (co > 0)
            {
                atmosphere.Components[(Chemical.CarbonMonoxide, Phase.Gas)] = co;
            }
            if (co2 > 0)
            {
                atmosphere.Components[(Chemical.CarbonDioxide, Phase.Gas)] = co2;
            }
            if (nh3 > 0)
            {
                atmosphere.Components[(Chemical.Ammonia, Phase.Gas)] = nh3;
            }
            Atmosphere = new Atmosphere(this, atmosphere, Math.Round(Randomizer.Instance.NextDouble(2.5)));
        }

        /// <summary>
        /// Determines an orbit for this <see cref="Orbiter"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="Orbiter"/> which is to be orbited.</param>
        public override void GenerateOrbit(Orbiter orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Orbit.SetOrbit(
                this,
                orbitedObject,
                Location.GetDistanceTo(orbitedObject),
                Eccentricity,
                Math.Round(Randomizer.Instance.NextDouble(0.9), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4));
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <param name="periapsis">The periapsis of the satellite's orbit.</param>
        /// <param name="eccentricity">The eccentricity of the satellite's orbit.</param>
        /// <param name="maxMass">The maximum mass of the satellite.</param>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            Planetoid satellite = null;

            // If the mass limit allows, there is an even chance that the satellite is a smaller dwarf planet.
            if (maxMass > MinMass && Randomizer.Instance.NextBoolean())
            {
                satellite = new DwarfPlanet(Parent, maxMass);
            }
            else
            {
                // Otherwise, it is an asteroid, selected from the standard distribution of types.
                var chance = Randomizer.Instance.NextDouble();
                if (chance <= 0.75)
                {
                    satellite = new CTypeAsteroid(Parent, maxMass);
                }
                else if (chance <= 0.9)
                {
                    satellite = new STypeAsteroid(Parent, maxMass);
                }
                else
                {
                    satellite = new MTypeAsteroid(Parent, maxMass);
                }
            }

            Orbit.SetOrbit(
                satellite,
                this,
                periapsis,
                eccentricity,
                Math.Round(Randomizer.Instance.NextDouble(0.5), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4));

            return satellite;
        }

        /// <summary>
        /// Randomly determines the proportionate amount of the composition devoted to the core of a <see cref="Planemo"/>.
        /// </summary>
        /// <param name="mass">The mass of the <see cref="Planemo"/>.</param>
        /// <returns>A proportion, from 0.0 to 1.0.</returns>
        private protected override double GetCoreProportion(double mass) => Math.Round(Randomizer.Instance.NextDouble(0.2, 0.55), 3);
    }
}
