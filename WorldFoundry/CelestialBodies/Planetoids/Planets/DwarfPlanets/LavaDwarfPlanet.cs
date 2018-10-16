using Substances;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using System.Collections.Generic;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A hot, rocky dwarf planet with a molten rock mantle; usually the result of tidal stress.
    /// </summary>
    public class LavaDwarfPlanet : DwarfPlanet
    {
        internal new static double _densityForType = 4000;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double DensityForType => _densityForType;

        internal new const int _maxSatellites = 0;
        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Lava planets have no satellites; whatever forces have caused their surface trauma should
        /// also inhibit stable satellite orbits.
        /// </remarks>
        public override int MaxSatellites => _maxSatellites;

        private const string _planemoClassPrefix = "Lava";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => _planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/>.
        /// </summary>
        public LavaDwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        public LavaDwarfPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public LavaDwarfPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaDwarfPlanet"/>.</param>
        public LavaDwarfPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaDwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public LavaDwarfPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override IEnumerable<(IComposition, double)> GetCrust()
        {
            // rocky crust
            // 50% chance of dust
            var dust = Math.Round(Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5)), 3);
            if (dust > 0)
            {
                yield return (new Composite(
                    (Chemical.Rock, Phase.Solid, 1 - dust),
                    (Chemical.Dust, Phase.Solid, dust)),
                    1);
            }
            else
            {
                yield return (new Material(Chemical.Rock, Phase.Solid), 1);
            }
        }

        private protected override IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            yield return (new Material(Chemical.Rock, Phase.Liquid), 1);
        }
    }
}
