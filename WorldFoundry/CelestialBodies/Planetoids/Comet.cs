using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WorldFoundry.Climate;
using WorldFoundry.Place;
using WorldFoundry.Space;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// Mostly ice and dust, with a large but thin atmosphere.
    /// </summary>
    [Serializable]
    public class Comet : Planetoid
    {
        internal static readonly Number Space = new Number(25000);

        private protected override string BaseTypeName => "Comet";

        private protected override Number Rigidity => new Number(4, 9);

        /// <summary>
        /// Initializes a new instance of <see cref="Comet"/>.
        /// </summary>
        internal Comet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Comet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="Comet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Comet"/>.</param>
        internal Comet(Location parent, Vector3 position) : base(parent, position) { }

        private Comet(
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
            List<Location>? children,
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
                children,
                depthMap,
                elevationMap,
                flowMap,
                precipitationMaps,
                snowfallMaps,
                temperatureMapSummer,
                temperatureMapWinter,
                maxFlow) { }

        private Comet(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (double)info.GetValue(nameof(_normalizedSeaLevel), typeof(double)),
            (int)info.GetValue(nameof(_seed1), typeof(int)),
            (int)info.GetValue(nameof(_seed2), typeof(int)),
            (int)info.GetValue(nameof(_seed3), typeof(int)),
            (int)info.GetValue(nameof(_seed4), typeof(int)),
            (int)info.GetValue(nameof(_seed5), typeof(int)),
            (double?)info.GetValue(nameof(AngleOfRotation), typeof(double?)),
            (Atmosphere?)info.GetValue(nameof(Atmosphere), typeof(Atmosphere)),
            (double?)info.GetValue(nameof(AxialPrecession), typeof(double?)),
            (bool?)info.GetValue(nameof(HasMagnetosphere), typeof(bool?)),
            (double?)info.GetValue(nameof(MaxElevation), typeof(double?)),
            (Number?)info.GetValue(nameof(RotationalOffset), typeof(Number?)),
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number?)),
            (List<Resource>?)info.GetValue(nameof(Resources), typeof(List<Resource>)),
            (List<string>?)info.GetValue(nameof(Satellites), typeof(List<string>)),
            (List<SurfaceRegion>?)info.GetValue(nameof(SurfaceRegions), typeof(List<SurfaceRegion>)),
            (Number?)info.GetValue(nameof(MaxMass), typeof(Number?)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>)),
            (byte[])info.GetValue(nameof(_depthMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_elevationMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_flowMap), typeof(byte[])),
            (byte[][])info.GetValue(nameof(_precipitationMaps), typeof(byte[][])),
            (byte[][])info.GetValue(nameof(_snowfallMaps), typeof(byte[][])),
            (byte[])info.GetValue(nameof(_temperatureMapSummer), typeof(byte[])),
            (byte[])info.GetValue(nameof(_temperatureMapWinter), typeof(byte[])),
            (double?)info.GetValue(nameof(_maxFlow), typeof(double?))) { }

        internal override void GenerateOrbit(CelestialLocation orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            // Current distance is presumed to be apoapsis for comets, which are presumed to originate in an Oort cloud,
            // and have eccentricities which may either leave them there, or send them into the inner solar system.
            var eccentricity = Randomizer.Instance.NextDouble();
            var periapsis = (1 - eccentricity) / (1 + eccentricity) * GetDistanceTo(orbitedObject);

            WorldFoundry.Space.Orbit.SetOrbit(
                this,
                orbitedObject,
                periapsis,
                eccentricity,
                Randomizer.Instance.NextDouble(Math.PI),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Math.PI);
        }

        private protected override void GenerateAlbedo()
            => Albedo = Randomizer.Instance.NextDouble(0.025, 0.055);

        private protected override void GenerateAtmosphere()
        {
            var dust = 1.0m;

            var water = Randomizer.Instance.NextDecimal(0.75m, 0.9m);
            dust -= water;

            var co = Randomizer.Instance.NextDecimal(0.05m, 0.15m);
            dust -= co;

            if (dust < 0)
            {
                water -= 0.1m;
                dust += 0.1m;
            }

            var co2 = Randomizer.Instance.NextDecimal(0.01m);
            dust -= co2;

            var nh3 = Randomizer.Instance.NextDecimal(0.01m);
            dust -= nh3;

            var ch4 = Randomizer.Instance.NextDecimal(0.01m);
            dust -= ch4;

            var h2s = Randomizer.Instance.NextDecimal(0.01m);
            dust -= h2s;

            var so2 = Randomizer.Instance.NextDecimal(0.001m);
            dust -= so2;

            _atmosphere = new Atmosphere(
                this,
                1e-8,
                (Substances.GetChemicalReference(Substances.Chemicals.Water), water),
                (Substances.GetSolutionReference(Substances.Solutions.CosmicDust), dust),
                (Substances.GetChemicalReference(Substances.Chemicals.CarbonMonoxide), co),
                (Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide), co2),
                (Substances.GetChemicalReference(Substances.Chemicals.Ammonia), nh3),
                (Substances.GetChemicalReference(Substances.Chemicals.Methane), ch4),
                (Substances.GetChemicalReference(Substances.Chemicals.HydrogenSulfide), h2s),
                (Substances.GetChemicalReference(Substances.Chemicals.SulphurDioxide), so2));
        }

        private protected override double GetDensity() => Randomizer.Instance.NextDouble(300, 700);

        private protected override (double density, Number mass, IShape shape) GetMatter()
        {
            var density = GetDensity();
            var shape = GetShape();
            return (density, shape.Volume * density, shape);
        }

        private protected override ISubstanceReference? GetSubstance() => CelestialSubstances.CometNucleus;
    }
}
