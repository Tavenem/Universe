﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System.Text;
using WorldFoundry.Orbits;
using WorldFoundry.Space;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets
{
    /// <summary>
    /// Any planetary-mass object (massive enough to be rounded under its own gravity), such as dwarf planets, some moons, and planets.
    /// </summary>
    public class Planemo : Planetoid
    {
        private const float defaultCoreProportion = 0.15f;
        private const float ringDensity_Icy = 300.0f;
        private const float ringDensity_Rocky = 1380.0f;

        private float? _eccentricity;
        /// <summary>
        /// The eccentricity of this <see cref="Planemo"/>'s orbit.
        /// </summary>
        [NotMapped]
        protected float Eccentricity
        {
            get => GetProperty(ref _eccentricity, GenerateEccentricity) ?? 0;
            set => _eccentricity = value;
        }

        /// <summary>
        /// Indicates that ring generation has completed, whether or not any rings were actually generated.
        /// </summary>
        protected bool IsRingGenerationComplete { get; set; }

        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Set to 5 for <see cref="Planemo"/>. For reference, Pluto has 5 moons, the most of any
        /// planemo in the Solar System apart from the giants. No others are known to have more than 2.
        /// </remarks>
        public override int MaxSatellites => 5;

        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        /// <remarks>
        /// Null in the base class; subclasses may override when appropriate.
        /// </remarks>
        public virtual string PlanemoClassPrefix => null;

        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>Zero on the base class; subclasses may override when appropriate.</remarks>
        protected virtual float RingChance => 0;

        private ICollection<PlanetaryRing> _rings;
        public ICollection<PlanetaryRing> Rings
        {
            get => GetProperty(ref _rings, GenerateRings, () => !IsRingGenerationComplete);
            private set => _rings = value;
        }

        /// <summary>
        /// The name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string TypeName
        {
            get
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(PlanemoClassPrefix))
                {
                    sb.Append(PlanemoClassPrefix);
                    sb.Append(" ");
                }
                if (Orbit?.OrbitedObject is Planemo)
                {
                    sb.Append("Moon");
                }
                else if (Parent is StarSystem)
                {
                    sb.Append(BaseTypeName);
                }
                else
                {
                    sb.Insert(0, "Rogue ");
                    sb.Append(BaseTypeName);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/>.
        /// </summary>
        public Planemo() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Planemo"/> is located.
        /// </param>
        public Planemo(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Planemo"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planemo"/> during random generation, in kg.
        /// </param>
        public Planemo(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Planemo"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planemo"/>.</param>
        public Planemo(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Planemo"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planemo"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planemo"/> during random generation, in kg.
        /// </param>
        public Planemo(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Randomly determines an eccentricity for this <see cref="Planemo"/>.
        /// </summary>
        /// <remarks>
        /// High eccentricity is unusual for a <see cref="Planemo"/>. Gaussian distribution with most values between 0.0 and 0.1.
        /// </remarks>
        private void GenerateEccentricity() => Eccentricity = (float)Math.Abs(Math.Round(Randomizer.Static.Normal(0, 0.05), 4));

        /// <summary>
        /// Determines an orbit for this <see cref="Orbiter"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="Orbiter"/> which is to be orbited.</param>
        /// <remarks>
        /// In the base class, always generates a circular orbit; subclasses are expected to override.
        /// </remarks>
        public override void GenerateOrbit(Orbiter orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Orbit.SetOrbit(
                this,
                orbitedObject,
                GetDistanceToTarget(orbitedObject),
                Eccentricity,
                (float)Math.Round(Randomizer.Static.NextDouble(0.9), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4));
        }

        /// <summary>
        /// Generates the ring system around this <see cref="Planemo"/>, if any.
        /// </summary>
        protected virtual void GenerateRings()
        {
            if (_rings == null)
            {
                _rings = new HashSet<PlanetaryRing>();
            }

            IsRingGenerationComplete = true;

            var innerLimit = Atmosphere == null ? 0 : Atmosphere.AtmosphericHeight;

            var outerLimit_Icy = GetRingDistance_Icy();
            if (Orbit != null)
            {
                outerLimit_Icy = (float)Math.Min(outerLimit_Icy, Orbit.GetHillSphereRadius() / 3.0);
            }
            if (innerLimit >= outerLimit_Icy)
            {
                return;
            }

            var outerLimit_Rocky = GetRingDistance_Rocky();
            if (Orbit != null)
            {
                outerLimit_Rocky = (float)Math.Min(outerLimit_Rocky, Orbit.GetHillSphereRadius() / 3.0);
            }

            var ringChance = RingChance;
            while (Randomizer.Static.NextDouble() <= ringChance && innerLimit <= outerLimit_Icy)
            {
                if (innerLimit < outerLimit_Rocky && Randomizer.Static.NextBoolean())
                {
                    var innerRadius = (float)Math.Round(Randomizer.Static.NextDouble(innerLimit, outerLimit_Rocky));

                    Rings.Add(new PlanetaryRing(false, innerRadius, outerLimit_Rocky));

                    outerLimit_Rocky = innerRadius;
                    if (outerLimit_Rocky <= outerLimit_Icy)
                    {
                        outerLimit_Icy = innerRadius;
                    }
                }
                else
                {
                    var innerRadius = (float)Math.Round(Randomizer.Static.NextDouble(innerLimit, outerLimit_Icy));

                    Rings.Add(new PlanetaryRing(true, innerRadius, outerLimit_Icy));

                    outerLimit_Icy = innerRadius;
                    if (outerLimit_Icy <= outerLimit_Rocky)
                    {
                        outerLimit_Rocky = innerRadius;
                    }
                }

                ringChance *= 0.5f;
            }
        }

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <param name="knownRadius">
        /// A predetermined radius for the <see cref="Planemo"/>. May be left null to randomly
        /// determine an appropriate radius.
        /// </param>
        protected void GenerateShape(float? knownRadius = null)
        {
            // If no known radius is provided, an approximate radius as if the shape was a sphere is
            // determined, which is no less than the minimum required for hydrostatic equilibrium.
            var radius = knownRadius ?? (float)Math.Round(Math.Max(600000, Math.Pow((Mass / Density) / Utilities.MathUtil.Constants.FourThirdsPI, 1.0 / 3.0)));
            var flattening = Randomizer.Static.NextDouble(0.1);
            Shape = new Ellipsoid(radius, (float)Math.Round(radius * (1 - flattening)), radius);
        }

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        protected override void GenerateShape() => GenerateShape(null);

        /// <summary>
        /// Randomly determines the proportionate amount of the composition devoted to the core of a <see cref="Planemo"/>.
        /// </summary>
        /// <returns>A proportion, from 0.0 to 1.0.</returns>
        /// <remarks>The base class returns a flat ratio; subclasses are expected to override as needed.</remarks>
        public virtual float GetCoreProportion() => defaultCoreProportion;

        /// <summary>
        /// Randomly determines the proportionate amount of the composition devoted to the crust of a <see cref="Planemo"/>.
        /// </summary>
        /// <returns>A proportion, from 0.0 to 1.0.</returns>
        /// <remarks>Smaller <see cref="Planemo"/>s have thicker crusts due to faster proto-planetary cooling.</remarks>
        protected float GetCrustProportion() => (float)(400000.0 / Math.Pow(Radius, 1.6));

        /// <summary>
        /// Calculates the approximate outer distance at which rings of the given density may be found, in meters.
        /// </summary>
        /// <returns>The approximate outer distance at which rings of the given density may be found, in meters.</returns>
        private float GetRingDistance(float density) => (float)(1.26 * Radius * Math.Pow(Density / density, 1.0 / 3.0));

        /// <summary>
        /// Calculates the approximate outer distance at which ice rings may be found, in meters.
        /// </summary>
        /// <returns>The approximate outer distance at which ice rings may be found, in meters.</returns>
        protected float GetRingDistance_Icy() => GetRingDistance(ringDensity_Icy);

        /// <summary>
        /// Calculates the approximate outer distance at which rocky rings may be found, in meters.
        /// </summary>
        /// <returns>The approximate outer distance at which rocky rings may be found, in meters.</returns>
        protected float GetRingDistance_Rocky() => GetRingDistance(ringDensity_Rocky);

        /// <summary>
        /// Calculates the mass at which the Stern-Levison parameter for this <see cref="Planemo"/>
        /// will be 1, given its orbital characteristics.
        /// </summary>
        /// <remarks>
        /// Also sets <see cref="Eccentricity"/> as a side effect, if the <see cref="Planemo"/>
        /// doesn't already have a defined orbit.
        /// </remarks>
        /// <exception cref="Exception">Cannot be called if this <see cref="Planemo"/> has no Orbit or <see cref="CelestialEntity.Parent"/>.</exception>
        protected double GetSternLevisonLambdaMass()
        {
            var semiMajorAxis = 0.0;
            if (Orbit != null)
            {
                semiMajorAxis = Orbit.SemiMajorAxis;
            }
            else if (Parent == null)
            {
                throw new Exception($"{nameof(GetSternLevisonLambdaMass)} cannot be called on a {nameof(Planemo)} without either an {nameof(Orbit)} or a {nameof(Parent)}");
            }
            else
            {
                // Even if this planemo is not yet in a defined orbit, some orbital characteristics
                // must be determined early, in order to distinguish a dwarf planet from a planet,
                // which depends partially on orbital distance.
                semiMajorAxis = (GetDistanceToTarget(Parent) * (1 + Eccentricity)) / (1 - Eccentricity);
            }

            return Math.Sqrt(Math.Pow(semiMajorAxis, 3.0 / 2.0) / 2.5e-28);
        }
    }
}