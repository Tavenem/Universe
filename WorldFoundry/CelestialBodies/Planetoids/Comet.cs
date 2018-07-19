using MathAndScience.MathUtil;
using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Climate;
using WorldFoundry.Orbits;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// Mostly ice and dust, with a large but thin atmosphere.
    /// </summary>
    public class Comet : Planetoid
    {
        private const string baseTypeName = "Comet";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// The approximate rigidity of this <see cref="Planetoid"/>.
        /// </summary>
        public override double Rigidity => 4.0e9;

        /// <summary>
        /// Initializes a new instance of <see cref="Comet"/>.
        /// </summary>
        public Comet() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Comet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Comet"/> is located.
        /// </param>
        public Comet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Comet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Comet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Comet"/>.</param>
        public Comet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        private protected override void GenerateAlbedo()
            => Albedo = Math.Round(Randomizer.Static.NextDouble(0.025, 0.055), 3);

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateAtmosphere()
        {
            var dust = 1.0;

            var water = Math.Round(Randomizer.Static.NextDouble(0.75, 0.9), 3);
            dust -= water;

            var co = Math.Round(Randomizer.Static.NextDouble(0.05, 0.15), 3);
            dust -= co;

            if (dust < 0)
            {
                water -= 0.1;
                dust += 0.1;
            }

            var co2 = Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= co2;

            var nh3 = Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= nh3;

            var ch4 = Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= ch4;

            var h2s = Math.Round(Randomizer.Static.NextDouble(0.01), 4);
            dust -= h2s;

            var so2 = Math.Round(Randomizer.Static.NextDouble(0.001), 4);
            dust -= so2;

            Atmosphere = new Atmosphere(this,
                new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
                {
                    { (Chemical.Water, Phase.Gas), water },
                    { (Chemical.Dust, Phase.Solid), dust },
                    { (Chemical.CarbonMonoxide, Phase.Gas), co },
                    { (Chemical.CarbonDioxide, Phase.Gas), co2 },
                    { (Chemical.Ammonia, Phase.Gas), nh3 },
                    { (Chemical.Methane, Phase.Gas), ch4 },
                    { (Chemical.HydrogenSulfide, Phase.Gas), h2s },
                    { (Chemical.SulphurDioxide, Phase.Gas), so2 },
                }),
                1e-8);
        }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var dust = 1.0;

            var water = Math.Round(Randomizer.Static.NextDouble(0.35, 0.45), 3);
            dust -= water;

            var co = Math.Round(Randomizer.Static.NextDouble(0.04, 0.1), 3);
            dust -= co;

            if (dust >= 0.5)
            {
                water -= 0.08;
                dust += 0.08;
            }

            var co2 = Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= co2;

            var nh3 = Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= nh3;

            var ch4 = Math.Round(Randomizer.Static.NextDouble(0.01), 3);
            dust -= ch4;

            var rock = Math.Round(Randomizer.Static.NextDouble(0.05), 3);
            dust -= rock;

            Substance = new Substance
            {
                Composition = new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
                {
                    { (Chemical.Water, Phase.Solid), water },
                    { (Chemical.Dust, Phase.Solid), dust },
                    { (Chemical.CarbonMonoxide, Phase.Solid), co },
                    { (Chemical.CarbonDioxide, Phase.Solid), co2 },
                    { (Chemical.Ammonia, Phase.Solid), nh3 },
                    { (Chemical.Methane, Phase.Solid), ch4 },
                })
            };

            // Gaussian distribution with most values between 1km and 19km.
            var axis = Math.Round(10000 + Math.Abs(Randomizer.Static.Normal(0, 4500)));
            var irregularity = Math.Round(Randomizer.Static.NextDouble(0.5, 1), 2);
            var shape = new Ellipsoid(axis, irregularity);

            Substance.Mass = shape.Volume * Density;

            SetShape(shape);
        }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateDensity()
            => Density = Math.Round(Randomizer.Static.NextDouble(300, 700), 2);

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
            var eccentricity = Math.Round(Randomizer.Static.NextDouble(), 2);
            var periapsis = ((1 - eccentricity) / (1 + eccentricity)) * GetDistanceToTarget(orbitedObject);

            Orbit.SetOrbit(
                this,
                orbitedObject,
                periapsis,
                eccentricity,
                Math.Round(Randomizer.Static.NextDouble(Math.PI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.PI);
        }
    }
}
