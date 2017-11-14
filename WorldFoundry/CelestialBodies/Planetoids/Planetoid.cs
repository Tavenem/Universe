using System;
using System.ComponentModel.DataAnnotations.Schema;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    public class Planetoid : CelestialBody
    {
        private float? _angleOfRotation;
        /// <summary>
        /// The angle between the Y-axis and the axis of rotation of this <see cref="Planetoid"/>.
        /// Values greater than half Pi indicate clockwise rotation.
        /// </summary>
        /// <remarks>
        /// Note that this is not the same as <see cref="AxialTilt"/>: if the <see cref="Planetoid"/>
        /// is in orbit then <see cref="AxialTilt"/> is relative to the <see cref="Planetoid"/>'s
        /// orbital plane.
        /// </remarks>
        public float AngleOfRotation
        {
            get => GetProperty(ref _angleOfRotation, GenerateAngleOfRotation) ?? 0;
            set => _angleOfRotation = value;
        }

        /// <summary>
        /// The axial tilt of the <see cref="Planetoid"/> relative to its orbital plane, in radians.
        /// Values greater than half Pi indicate clockwise rotation.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as <see cref="AngleOfRotation"/>.
        /// </remarks>
        [NotMapped]
        public float AxialTilt
        {
            get => Orbit == null ? AngleOfRotation : AngleOfRotation - Orbit.Inclination;
            set => AngleOfRotation = (Orbit == null ? value : value + Orbit.Inclination);
        }

        /// <summary>
        /// Determines an angle between the Y-axis and the axis of rotation for this <see cref="Planetoid"/>.
        /// </summary>
        protected virtual void GenerateAngleOfRotation()
        {
            if (Randomizer.Static.NextDouble() <= 0.2) // low chance of an extreme tilt
            {
                _angleOfRotation = (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.QuarterPI, Math.PI), 4);
            }
            else
            {
                _angleOfRotation = (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.QuarterPI), 4);
            }
        }
    }
}
