using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The remnants of a red giant, which have left behind an ionized gas cloud surrounding a white
    /// dwarf star.
    /// </summary>
    /// <remarks>Not related to a planet in any way. Gets its name from a quirk of
    /// history.</remarks>
    [Serializable]
    public class PlanetaryNebula : CelestialLocation
    {
        internal static readonly Number Space = new Number(9.5, 15);

        private protected override string BaseTypeName => "Planetary Nebula";

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryNebula"/>.
        /// </summary>
        internal PlanetaryNebula() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryNebula"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="PlanetaryNebula"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="PlanetaryNebula"/>.</param>
        internal PlanetaryNebula(Location parent, Vector3 position) : base(parent, position) { }

        private PlanetaryNebula(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children = null)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                children) { }

        private PlanetaryNebula(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        internal override void PrepopulateRegion()
        {
            if (_isPrepopulated)
            {
                return;
            }
            base.PrepopulateRegion();

            new StarSystem(this, Vector3.Zero, typeof(WhiteDwarf));
        }

        private protected override Number GetMass() => Randomizer.Instance.NextNumber(new Number(1.99, 29), new Number(1.99, 30)); // ~0.1–1 solar mass.

        // Actual planetary nebulae are spherical only 20% of the time, but the shapes are irregular
        // and not considered critical to model precisely, especially given their extremely
        // attenuated nature. Instead, a ~1 ly sphere is used.
        private protected override IShape GetShape() => new Sphere(Space, Position);

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IonizedCloud);
    }
}
