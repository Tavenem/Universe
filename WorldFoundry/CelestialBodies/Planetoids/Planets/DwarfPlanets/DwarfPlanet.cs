using MathAndScience.MathUtil;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        private const string baseTypeName = "Dwarf Planet";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal static double densityForType = 2000;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double DensityForType => densityForType;

        internal static double maxMassForType = 2.0e22;
        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets.
        /// Within orbital systems, a calculated value for clearing the neighborhood is used instead.
        /// </remarks>
        internal override double? MaxMassForType => maxMassForType;

        internal static double minMassForType = 3.4e20;
        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>
        /// The minimum to achieve hydrostatic equilibrium and be considered a dwarf planet.
        /// </remarks>
        internal override double? MinMassForType => minMassForType;

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
        public DwarfPlanet() : base() { }

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
        /// Adds an appropriate icy crust with the given proportion.
        /// </summary>
        private protected virtual IComposition GetIcyCrust()
        {
            var dust = Math.Round(Randomizer.Static.NextDouble(), 3);
            var total = dust;

            // 50% chance of not including the following:
            var waterIce = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += waterIce;

            var n2 = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += n2;

            var ch4 = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += ch4;

            var co = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += co;

            var co2 = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += co2;

            var nh3 = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += nh3;

            var ratio = 1.0 / total;
            dust *= ratio;
            waterIce *= ratio;
            n2 *= ratio;
            ch4 *= ratio;
            co *= ratio;
            co2 *= ratio;
            nh3 *= ratio;

            var crust = new Composite((Chemical.Dust, Phase.Solid, dust));
            if (waterIce > 0)
            {
                crust.Components[(Chemical.Water, Phase.Solid)] = waterIce;
            }
            if (n2 > 0)
            {
                crust.Components[(Chemical.Nitrogen, Phase.Solid)] = n2;
            }
            if (ch4 > 0)
            {
                crust.Components[(Chemical.Methane, Phase.Solid)] = ch4;
            }
            if (co > 0)
            {
                crust.Components[(Chemical.CarbonMonoxide, Phase.Solid)] = co;
            }
            if (co2 > 0)
            {
                crust.Components[(Chemical.CarbonDioxide, Phase.Solid)] = co2;
            }
            if (nh3 > 0)
            {
                crust.Components[(Chemical.Ammonia, Phase.Solid)] = nh3;
            }
            return crust;
        }

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

            var albedo = Math.Round(Randomizer.Static.NextDouble(0.1, 0.6), 3);
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
            Atmosphere = new Atmosphere(this, atmosphere, Math.Round(Randomizer.Static.NextDouble(2.5)));
        }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            // rocky core
            var coreProportion = GetCoreProportion();
            var core = new Material(Chemical.Rock, Phase.Solid);

            var crustProportion = GetCrustProportion();

            // water-ice mantle
            var mantleProportion = 1.0 - coreProportion - crustProportion;
            var mantleIce = Math.Round(Randomizer.Static.NextDouble(0.2, 1), 3);
            var mantle = new Composite(
                (Chemical.Water, Phase.Solid, mantleIce),
                (Chemical.Water, Phase.Liquid, 1 - mantleIce));

            var crust = GetIcyCrust();

            Substance = new Substance()
            {
                Composition = new LayeredComposite(
                    (core, coreProportion),
                    (mantle, mantleProportion),
                    (crust, crustProportion)),
                Mass = GenerateMass(),
            };
            GenerateShape();
        }

        /// <summary>
        /// Generates the mass of this <see cref="DwarfPlanet"/>.
        /// </summary>
        /// <remarks>
        /// The Stern-Levison parameter for neighborhood-clearing is used to determined maximum mass
        /// at which the dwarf planet would not be able to do so at this orbital distance. We set the
        /// maximum at two orders of magnitude less than this (dwarf planets in our solar system all
        /// have masses below 5 orders of magnitude less).
        /// </remarks>
        private protected virtual double GenerateMass()
        {
            var maxMass = MaxMass;
            if (Parent != null)
            {
                maxMass = Math.Min(maxMass, GetSternLevisonLambdaMass() / 100);
                if (maxMass < MinMass)
                {
                    maxMass = MinMass; // sanity check; may result in a "dwarf" planet which *can* clear its neighborhood
                }
            }

            return Math.Round(Randomizer.Static.NextDouble(MinMass, maxMass));
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
                GetDistanceToTarget(orbitedObject),
                Eccentricity,
                Math.Round(Randomizer.Static.NextDouble(0.9), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4));
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            Planetoid satellite = null;

            // If the mass limit allows, there is an even chance that the satellite is a smaller dwarf planet.
            if (maxMass > MinMass && Randomizer.Static.NextBoolean())
            {
                satellite = new DwarfPlanet(Parent, maxMass);
            }
            else
            {
                // Otherwise, it is an asteroid, selected from the standard distribution of types.
                var chance = Randomizer.Static.NextDouble();
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
                Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4));

            return satellite;
        }

        /// <summary>
        /// Randomly determines the proportionate amount of the composition devoted to the core of a <see cref="Planemo"/>.
        /// </summary>
        /// <returns>A proportion, from 0.0 to 1.0.</returns>
        private protected override double GetCoreProportion() => Math.Round(Randomizer.Static.NextDouble(0.2, 0.55), 3);
    }
}
