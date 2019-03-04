using Substances;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using System.Collections.Generic;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A rocky dwarf planet without the typical subsurface ice/water mantle.
    /// </summary>
    public class RockyDwarfPlanet : DwarfPlanet
    {
        private protected override double DensityForType => 4000;

        private protected override string? PlanemoClassPrefix => "Rocky";

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/>.
        /// </summary>
        internal RockyDwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RockyDwarfPlanet"/>.</param>
        internal RockyDwarfPlanet(CelestialRegion? parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RockyDwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="RockyDwarfPlanet"/> during random generation, in kg.
        /// </param>
        internal RockyDwarfPlanet(CelestialRegion? parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            yield break;
        }
    }
}
