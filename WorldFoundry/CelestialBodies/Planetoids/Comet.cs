﻿using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Climate;
using WorldFoundry.Orbits;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// Mostly ice and dust, with a large but thin atmosphere.
    /// </summary>
    public class Comet : Planetoid
    {
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public new static string BaseTypeName => "Comet";

        /// <summary>
        /// The approximate rigidity of this <see cref="Planetoid"/>.
        /// </summary>
        public override float Rigidity => 4.0e9f;

        /// <summary>
        /// Initializes a new instance of <see cref="Comet"/>.
        /// </summary>
        public Comet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Comet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Comet"/> is located.
        /// </param>
        public Comet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Comet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Comet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Comet"/>.</param>
        public Comet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        protected override void GenerateAlbedo() => Albedo = (float)Math.Round(Randomizer.Static.NextDouble(0.025, 0.055), 3);

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateAtmosphere()
        {
            var dust = 1.0f;

            var water = (float)Math.Round(Randomizer.Static.NextDouble(0.75, 0.9), 3);
            dust -= water;

            var co = (float)Math.Round(Randomizer.Static.NextDouble(0.05, 0.15), 3);
            dust -= co;

            if (dust < 0)
            {
                water -= 0.1f;
                dust += 0.1f;
            }

            var co2 = (float)Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= co2;

            var nh3 = (float)Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= nh3;

            var ch4 = (float)Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= ch4;

            var h2s = (float)Math.Round(Randomizer.Static.NextDouble(0.01), 4);
            dust -= h2s;

            var so2 = (float)Math.Round(Randomizer.Static.NextDouble(0.001), 4);
            dust -= so2;

            Atmosphere = new Atmosphere(this)
            {
                Mixtures = new HashSet<Mixture>()
            };
            Atmosphere.Mixtures.Add(new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Gas),
                    Proportion = water,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Dust, Phase.Solid),
                    Proportion = dust,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.CarbonMonoxide, Phase.Gas),
                    Proportion = co,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.CarbonDioxide, Phase.Gas),
                    Proportion = co2,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ammonia, Phase.Gas),
                    Proportion = nh3,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Methane, Phase.Gas),
                    Proportion = ch4,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.HydrogenSulfide, Phase.Gas),
                    Proportion = h2s,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.SulphurDioxide, Phase.Gas),
                    Proportion = so2,
                },
            }));
        }

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateComposition()
        {
            var dust = 1.0f;

            var water = (float)Math.Round(Randomizer.Static.NextDouble(0.35, 0.45), 3);
            dust -= water;

            var co = (float)Math.Round(Randomizer.Static.NextDouble(0.04, 0.1), 3);
            dust -= co;

            if (dust >= 0.5)
            {
                water -= 0.08f;
                dust += 0.08f;
            }

            var co2 = (float)Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= co2;

            var nh3 = (float)Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= nh3;

            var ch4 = (float)Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= ch4;

            var rock = (float)Math.Round(Randomizer.Static.NextDouble(0.05), 3);
            dust -= rock;

            Composition = new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Solid),
                    Proportion = water,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Dust, Phase.Solid),
                    Proportion = dust,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.CarbonMonoxide, Phase.Solid),
                    Proportion = co,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.CarbonDioxide, Phase.Solid),
                    Proportion = co2,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ammonia, Phase.Solid),
                    Proportion = nh3,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Methane, Phase.Solid),
                    Proportion = ch4,
                },
            });
        }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        private void GenerateDensity() => Density = (float)Math.Round(Randomizer.Static.NextDouble(300, 700), 2);

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        protected override void GenerateMass() => Mass = Shape.GetVolume() * Density;

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

            // Current distance is presumed to be apoapsis for comets, which are presumed to originate in an Oort cloud,
            // and have eccentricities which may either leave them there, or send them into the inner solar system.
            var eccentricity = (float)Math.Round(Randomizer.Static.NextDouble(), 2);
            var periapsis = ((1 - eccentricity) / (1 + eccentricity)) * GetDistanceToTarget(orbitedObject);

            Orbit.SetOrbit(
                this,
                orbitedObject,
                periapsis,
                eccentricity,
                (float)Math.Round(Randomizer.Static.NextDouble(Math.PI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.PI);
        }

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        protected override void GenerateShape()
        {
            // Gaussian distribution with most values between 1km and 19km.
            var axisA = (float)Math.Round(10000 + Math.Abs(Randomizer.Static.Normal(0, 4500)));
            var irregularity = (float)Math.Round(Randomizer.Static.NextDouble(0.5, 1), 2);
            Shape = new Ellipsoid(axisA, axisA * irregularity, axisA / irregularity);
        }
    }
}