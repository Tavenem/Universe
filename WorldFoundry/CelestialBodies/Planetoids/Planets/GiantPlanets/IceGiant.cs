using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets
{
    /// <summary>
    /// An ice giant planet, such as Neptune or Uranus.
    /// </summary>
    public class IceGiant : GiantPlanet
    {
        private const string baseTypeName = "Ice Giant";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal new static int maxSatellites = 40;
        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Set to 40 for <see cref="IceGiant"/>. For reference, Uranus has 27 moons, and Neptune has
        /// 14 moons.
        /// </remarks>
        public override int MaxSatellites => maxSatellites;

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/>.
        /// </summary>
        public IceGiant() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        public IceGiant(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IceGiant"/> during random generation, in kg.
        /// </param>
        public IceGiant(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IceGiant"/>.</param>
        public IceGiant(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IceGiant"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IceGiant"/> during random generation, in kg.
        /// </param>
        public IceGiant(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var layers = new List<(IComposition substance, double proportion)>();

            // Iron-nickel inner core.
            var coreProportion = GetCoreProportion();
            var innerCoreProportion = base.GetCoreProportion() * coreProportion;
            var coreNickel = Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            layers.Add((new Composite(
                (Chemical.Iron, Phase.Solid, 1 - coreNickel),
                (Chemical.Nickel, Phase.Solid, coreNickel)),
                innerCoreProportion));

            // Molten rock outer core.
            layers.Add((new Material(Chemical.Rock, Phase.Liquid), coreProportion - innerCoreProportion));

            var diamond = 1 - coreProportion;
            var water = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(diamond)), 4);
            diamond -= water;
            var nh4 = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(diamond)), 4);
            diamond -= nh4;
            var ch4 = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(diamond)), 4);
            diamond -= ch4;

            // Liquid diamond mantle
            if (!Troschuetz.Random.TMath.IsZero(diamond))
            {
                layers.Add((new Material(Chemical.Diamond, Phase.Liquid), diamond));
            }

            // Supercritical water-ammonia ocean layer (blends seamlessly with lower atmosphere)
            IComposition upperLayer = null;
            if (ch4 >0 || nh4 > 0)
            {
                upperLayer = new Composite((Chemical.Water, Phase.Liquid, water));
                if (ch4 > 0)
                {
                    (upperLayer as Composite).Components[(Chemical.Methane, Phase.Liquid)] = ch4;
                }
                if (nh4 > 0)
                {
                    (upperLayer as Composite).Components[(Chemical.Ammonia, Phase.Liquid)] = nh4;
                }
            }
            else
            {
                upperLayer = new Material(Chemical.Water, Phase.Liquid);
            }
            upperLayer.BalanceProportions();
            layers.Add((upperLayer, 1 - coreProportion - diamond));

            Substance = new Substance()
            {
                Composition = new LayeredComposite(layers),
                Mass = GenerateMass(),
            };
            GenerateShape();
        }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// No "puffy" ice giants.
        /// </remarks>
        private protected override void GenerateDensity() => Density = Math.Round(Randomizer.Static.NextDouble(MinDensity, MaxDensity));
    }
}
