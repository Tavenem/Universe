using NeverFoundry.MathAndScience.Chemistry;
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
    /// A massive gravitational singularity, found at the center of large galaxies.
    /// </summary>
    [Serializable]
    public class SupermassiveBlackHole : BlackHole
    {
        private protected override string BaseTypeName => "Supermassive Black Hole";

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/>.
        /// </summary>
        internal SupermassiveBlackHole() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="SupermassiveBlackHole"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="SupermassiveBlackHole"/>.</param>
        internal SupermassiveBlackHole(Location parent, Vector3 position) : base(parent, position) { }

        private SupermassiveBlackHole(
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

        private SupermassiveBlackHole(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3?)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        private protected override Number GetMass() => Randomizer.Instance.NextNumber(new Number(2, 35), new Number(2, 40)); // ~10e5–10e10 solar masses
    }
}
