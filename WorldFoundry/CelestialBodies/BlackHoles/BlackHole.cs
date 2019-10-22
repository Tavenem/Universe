using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WorldFoundry.Place;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.BlackHoles
{
    /// <summary>
    /// A gravitational singularity.
    /// </summary>
    [Serializable]
    public class BlackHole : CelestialLocation
    {
        internal static readonly Number Space = new Number(60000);

        private protected override string BaseTypeName => "Black Hole";

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/>.
        /// </summary>
        internal BlackHole()  { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="BlackHole"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BlackHole"/>.</param>
        internal BlackHole(Location parent, Vector3 position) : base(parent, position) { }

        private protected BlackHole(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                children) { }

        private BlackHole(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        private protected override Number GetMass() => Randomizer.Instance.NextNumber(new Number(6, 30), new Number(4, 31)); // ~3–20 solar masses

        private protected override (double density, Number mass, IShape shape) GetMatter()
        {
            var mass = GetMass();

            // The shape given is presumed to refer to the shape of the event horizon.
            var shape = new Sphere(ScienceConstants.TwoG * mass / ScienceConstants.SpeedOfLightSquared, Position);

            return ((double)(mass / shape.Volume), mass, shape);
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetHomogeneousSubstanceReference(Substances.HomogeneousSubstances.Fuzzball);
    }
}
