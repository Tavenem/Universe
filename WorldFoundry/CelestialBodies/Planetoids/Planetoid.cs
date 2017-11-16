﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using WorldFoundry.Climate;
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
            internal set => _angleOfRotation = value;
        }

        private Atmosphere _atmosphere;
        /// <summary>
        /// The atmosphere possessed by this <see cref="Planetoid"/>.
        /// </summary>
        public Atmosphere Atmosphere
        {
            get => GetProperty(ref _atmosphere, GenerateAtmosphere);
            protected set => _atmosphere = value;
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
            internal set => AngleOfRotation = (Orbit == null ? value : value + Orbit.Inclination);
        }

        /// <summary>
        /// Determines an angle between the Y-axis and the axis of rotation for this <see cref="Planetoid"/>.
        /// </summary>
        protected void GenerateAngleOfRotation()
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

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// Provides no functionality in the base class; subclasses are expected override.
        /// </summary>
        protected virtual void GenerateAtmosphere() { }

        /// <summary>
        /// Calculates the escape velocity from this body, in m/s.
        /// </summary>
        /// <returns>The escape velocity from this body, in m/s.</returns>
        public float GetEscapeVelocity() => (float)Math.Sqrt((Utilities.Science.Constants.TwoG * Mass) / Radius);
    }
}
