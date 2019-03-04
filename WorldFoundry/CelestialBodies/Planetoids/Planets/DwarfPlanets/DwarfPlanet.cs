using MathAndScience;
using Substances;
using System;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.Climate;
using WorldFoundry.Space;
using System.Collections.Generic;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A dwarf planet: a body large enough to form a roughly spherical shape under its own gravity,
    /// but not large enough to clear its orbital "neighborhood" of smaller bodies.
    /// </summary>
    public class DwarfPlanet : Planemo
    {
        internal const double Space = 1.5e6;

        private protected override string BaseTypeName => "Dwarf Planet";

        internal const double BaseDensityForType = 2000;
        private protected override double DensityForType => BaseDensityForType;

        // An arbitrary limit separating rogue dwarf planets from rogue planets.
        // Within orbital systems, a calculated value for clearing the neighborhood is used instead.
        private protected override double? MaxMassForType => 2.0e22;

        internal const double BaseMinMassForType = 3.4e20;
        // The minimum to achieve hydrostatic equilibrium and be considered a dwarf planet.
        private protected override double? MinMassForType => BaseMinMassForType;

        private protected override double RingChance => 10;

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/>.
        /// </summary>
        internal DwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfPlanet"/>.</param>
        internal DwarfPlanet(CelestialRegion? parent, Vector3 position) : base(parent, position) { }

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
        internal DwarfPlanet(CelestialRegion? parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        internal override void GenerateOrbit(ICelestialLocation orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            WorldFoundry.Space.Orbit.SetOrbit(
                this,
                orbitedObject,
                GetDistanceTo(orbitedObject),
                Eccentricity,
                Math.Round(Randomizer.Instance.NextDouble(0.9), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4));
        }

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

        private protected override void GenerateAtmosphere()
        {
            // Atmosphere is based solely on the volatile ices present.

            var crust = Substance.Composition.GetSurface();

            var water = crust?.GetProportion(Chemical.Water, Phase.Solid) ?? 0;
            var anyIces = water > 0;

            var n2 = crust?.GetProportion(Chemical.Nitrogen, Phase.Solid) ?? 0;
            anyIces &= n2 > 0;

            var ch4 = crust?.GetProportion(Chemical.Methane, Phase.Solid) ?? 0;
            anyIces &= ch4 > 0;

            var co = crust?.GetProportion(Chemical.CarbonMonoxide, Phase.Solid) ?? 0;
            anyIces &= co > 0;

            var co2 = crust?.GetProportion(Chemical.CarbonDioxide, Phase.Solid) ?? 0;
            anyIces &= co2 > 0;

            var nh3 = crust?.GetProportion(Chemical.Ammonia, Phase.Solid) ?? 0;
            anyIces &= nh3 > 0;

            if (!anyIces)
            {
                _atmosphere = new Atmosphere(this, Material.Empty, 0);
                return;
            }

            var components = new Dictionary<Material, double>();
            if (water > 0)
            {
                components[new Material(Chemical.Water, Phase.Gas)] = water;
            }
            if (n2 > 0)
            {
                components[new Material(Chemical.Nitrogen, Phase.Gas)] = n2;
            }
            if (ch4 > 0)
            {
                components[new Material(Chemical.Methane, Phase.Gas)] = ch4;
            }
            if (co > 0)
            {
                components[new Material(Chemical.CarbonMonoxide, Phase.Gas)] = co;
            }
            if (co2 > 0)
            {
                components[new Material(Chemical.CarbonDioxide, Phase.Gas)] = co2;
            }
            if (nh3 > 0)
            {
                components[new Material(Chemical.Ammonia, Phase.Gas)] = nh3;
            }
            _atmosphere = new Atmosphere(this, new Composite(components), Math.Round(Randomizer.Instance.NextDouble(2.5)));
        }

        private protected override Planetoid? GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            Planetoid satellite;

            // If the mass limit allows, there is an even chance that the satellite is a smaller dwarf planet.
            if (maxMass > MinMass && Randomizer.Instance.NextBoolean())
            {
                satellite = new DwarfPlanet(ContainingCelestialRegion, Vector3.Zero, maxMass);
            }
            else
            {
                // Otherwise, it is an asteroid, selected from the standard distribution of types.
                var chance = Randomizer.Instance.NextDouble();
                if (chance <= 0.75)
                {
                    satellite = new CTypeAsteroid(ContainingCelestialRegion, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.9)
                {
                    satellite = new STypeAsteroid(ContainingCelestialRegion, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new MTypeAsteroid(ContainingCelestialRegion, Vector3.Zero, maxMass);
                }
            }

            WorldFoundry.Space.Orbit.SetOrbit(
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

        private protected override double GetCoreProportion(double mass) => Math.Round(Randomizer.Instance.NextDouble(0.2, 0.55), 3);
    }
}
