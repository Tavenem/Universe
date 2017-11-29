using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using WorldFoundry.Climate;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// Any non-stellar celestial body, such as a planet or asteroid.
    /// </summary>
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

        private Mixture _composition;
        /// <summary>
        /// Defines the major constituents of this <see cref="Planetoid"/>'s composition.
        /// </summary>
        public Mixture Composition
        {
            get => GetProperty(ref _composition, GenerateComposition);
            protected set => _composition = value;
        }

        private double? _density;
        /// <summary>
        /// The average density of this <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        [NotMapped]
        public double Density
        {
            get => GetProperty(ref _density, GenerateDensity) ?? TypeDensity;
            set
            {
                if (_density == value || (!_density.HasValue && value == TypeDensity))
                {
                    return;
                }

                _density = value;
            }
        }

        private bool? _hasMagnetosphere;
        /// <summary>
        /// Indicates whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        public bool HasMagnetosphere
        {
            get => GetProperty(ref _hasMagnetosphere, GenerateMagnetosphere) ?? false;
            private set => _hasMagnetosphere = value;
        }

        /// <summary>
        /// A factor which multiplies the chance of this <see cref="Planetoid"/> having a strong magnetosphere.
        /// </summary>
        public virtual float MagnetosphereChanceFactor => 1;

        private double? _maxMass;
        /// <summary>
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in
        /// kg.
        /// </summary>
        public double MaxMass
        {
            get => (_maxMass ?? MaxMass_Type) ?? 0;
            protected set
            {
                if (_maxMass == value || value == MaxMass_Type)
                {
                    return;
                }

                _maxMass = value;
            }
        }

        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>Null in the base class; subclasses are expected to override.</remarks>
        internal virtual double? MaxMass_Type => null;

        internal const int maxSatellites = 1;
        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Set to 1 on the base class; subclasses are expected to set a higher value when appropriate.
        /// </remarks>
        public virtual int MaxSatellites => maxSatellites;

        private double? _minMass;
        /// <summary>
        /// The minimum mass allowed for this <see cref="Planetoid"/> during random generation, in
        /// kg.
        /// </summary>
        public double MinMass
        {
            get => (_minMass ?? MinMass_Type) ?? 0;
            protected set
            {
                if (_minMass == value || value == MinMass_Type)
                {
                    return;
                }

                _minMass = value;
            }
        }

        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>Null in the base class; subclasses are expected to override.</remarks>
        internal virtual double? MinMass_Type => null;

        private double? _minSatellitePeriapsis;
        /// <summary>
        /// The minimum distance at which a natural satellite may orbit this <see cref="Planetoid"/>.
        /// </summary>
        protected double MinSatellitePeriapsis
        {
            get => GetProperty(ref _minSatellitePeriapsis, GenerateMinSatellitePeriapsis) ?? 0;
            set => _minSatellitePeriapsis = value;
        }

        /// <summary>
        /// The approximate rigidity of this <see cref="Planetoid"/>.
        /// </summary>
        public virtual float Rigidity => 3.0e10f;

        private double? _rotationalPeriod;
        /// <summary>
        /// The length of time it takes for this <see cref="Planetoid"/> to rotate once about its axis, in seconds.
        /// </summary>
        public double RotationalPeriod
        {
            get => GetProperty(ref _rotationalPeriod, GenerateRotationalPeriod) ?? 0;
            set => _rotationalPeriod = value;
        }

        private const int rotationalPeriod_Min = 8000;
        protected virtual int RotationalPeriod_Min => rotationalPeriod_Min;
        private const int rotationalPeriod_Max = 100000;
        protected virtual int RotationalPeriod_Max => rotationalPeriod_Max;
        private const int rotationalPeriod_MaxExtreme = 1100000;
        protected virtual int RotationalPeriod_MaxExtreme => rotationalPeriod_MaxExtreme;

        /// <summary>
        /// The collection of natural satellites around this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="CelestialObject.Children"/>, natural satellites are actually siblings
        /// in the local <see cref="CelestialObject"/> hierarchy, which merely share an orbital relationship.
        /// </remarks>
        public ICollection<Planetoid> Satellites { get; private set; }

        private const double typeDensity = 0;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal virtual double TypeDensity => typeDensity;

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/>.
        /// </summary>
        public Planetoid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        public Planetoid(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in kg.
        /// </param>
        public Planetoid(CelestialObject parent, double maxMass) : base(parent) => MaxMass = maxMass;

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        public Planetoid(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in kg.
        /// </param>
        public Planetoid(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position) => MaxMass = maxMass;

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
        /// </summary>
        /// <remarks>
        /// Provides no functionality in the base class; subclasses are expected override.
        /// </remarks>
        protected virtual void GenerateAtmosphere() { }

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Provides no functionality in the base class except initialization; subclasses are expected override.
        /// </remarks>
        protected virtual void GenerateComposition() => Composition = new Mixture();

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Does nothing in the base class (allowing <see cref="TypeDensity"/> to be used);
        /// subclasses may override if necessary.
        /// </remarks>
        protected virtual void GenerateDensity() { }

        /// <summary>
        /// Determines whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        /// <remarks>
        /// The presence of a magnetosphere is dependent on the rotational period, and on size: fast
        /// spin of a large body (with a hot interior) produces a dynamo effect. Slow spin of a small
        /// (cooled) body does not. Mass is divided by 3e24, and rotational period by the number of
        /// seconds in an Earth year, which simplifies to multiplying mass by 2.88e-19.
        /// </remarks>
        private void GenerateMagnetosphere() => HasMagnetosphere = Randomizer.Static.NextDouble() <= ((Mass * 2.88e-19) / RotationalPeriod) * MagnetosphereChanceFactor;

        /// <summary>
        /// Generates an appropriate minimum distance at which a natural satellite may orbit this <see cref="Planetoid"/>.
        /// </summary>
        protected virtual void GenerateMinSatellitePeriapsis() => _minSatellitePeriapsis = 0;

        /// <summary>
        /// Determines a rotational period for this <see cref="Planetoid"/>.
        /// </summary>
        private void GenerateRotationalPeriod()
        {
            // Check for tidal locking.
            if (Orbit != null)
            {
                // Invent an orbit age. Precision isn't important here, and some inaccuracy and
                // inconsistency between satellites is desirable. The age of the Solar system is used
                // as an arbitrary norm.
                float years = (float)Randomizer.Static.Lognormal(0, 4.6e9);
                if (GetIsTidallyLockedAfter(years))
                {
                    RotationalPeriod = Orbit.Period;
                    return;
                }
            }

            if (Randomizer.Static.NextDouble() <= 0.05) // low chance of an extreme period
            {
                RotationalPeriod = (float)Math.Round(Randomizer.Static.NextDouble(RotationalPeriod_Max, RotationalPeriod_MaxExtreme));
            }
            else
            {
                RotationalPeriod = (float)Math.Round(Randomizer.Static.NextDouble(RotationalPeriod_Min, RotationalPeriod_Max));
            }
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        /// <remarks>Returns null in the base class; subclasses are expected to override.</remarks>
        protected virtual Planetoid GenerateSatellite(double periapsis, float eccentricity, double maxMass) => null;

        /// <summary>
        /// Generates a set of natural satellites for this celestial body.
        /// </summary>
        /// <param name="max">An optional maximum number of satellites to generate.</param>
        public virtual void GenerateSatellites(int? max = null)
        {
            if (Satellites == null)
            {
                Satellites = new HashSet<Planetoid>();
            }

            if (MaxSatellites <= 0)
            {
                return;
            }

            var minPeriapsis = MinSatellitePeriapsis;
            var maxApoapsis = Orbit == null ? Radius * 100 : Orbit.GetHillSphereRadius() / 3.0;

            while (minPeriapsis <= maxApoapsis && Satellites.Count < MaxSatellites && (!max.HasValue || Satellites.Count < max.Value))
            {
                var periapsis = Math.Round(Randomizer.Static.NextDouble(minPeriapsis, maxApoapsis));

                var maxEccentricity = (float)((maxApoapsis - periapsis) / (maxApoapsis + periapsis));
                var eccentricity = (float)Math.Round(Math.Min(Math.Abs(Randomizer.Static.Normal(0, 0.05)), maxEccentricity), 4);

                var semiLatusRectum = periapsis * (1 + eccentricity);
                var semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));

                // Keep mass under the limit where the orbital barycenter would be pulled outside the boundaries of this body.
                var maxMass = Math.Max(0, Mass / ((semiMajorAxis / Radius) - 1));

                var satellite = GenerateSatellite(periapsis, eccentricity, maxMass);
                if (satellite == null)
                {
                    break;
                }

                Satellites.Add(satellite);

                minPeriapsis = satellite.Orbit.Apoapsis + satellite.Orbit.GetSphereOfInfluenceRadius();
            }
        }

        /// <summary>
        /// Calculates the escape velocity from this body, in m/s.
        /// </summary>
        /// <returns>The escape velocity from this body, in m/s.</returns>
        public float GetEscapeVelocity() => (float)Math.Sqrt((Utilities.Science.Constants.TwoG * Mass) / Radius);

        /// <summary>
        /// Calculates the heat added to this <see cref="CelestialBody"/> by insolation at the given
        /// position, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which the heat of
        /// insolation will be calculated.
        /// </param>
        /// <returns>
        /// The heat added to this <see cref="CelestialBody"/> by insolation at the given position,
        /// in K.
        /// </returns>
        protected override float GetInsolationHeat(Vector3 position)
        {
            var heat = base.GetInsolationHeat(position);

            // Rotating bodies radiate some heat as they rotate away from the source. The degree
            // depends on the speed of the rotation, but is constrained to limits (with very fast
            // rotation, every spot comes back into an insolated position quickly; and very slow
            // rotation results in long-term hot/cold hemispheres rather than continuous
            // heat-shedding). Here, we merely approximate very roughly since accurate calculations
            // depends on many factors and would involve either circular logic, or extremely
            // extensive calculus, if we attempted to calculate it accurately.
            var period = RotationalPeriod;
            if (period <= 2500)
            {
                return heat;
            }

            var areaRatio = 1.0;
            if (period <= 75000)
            {
                areaRatio = 0.25;
            }
            else if (period <= 150000)
            {
                areaRatio = 1.0 / 3.0;
            }
            else if (period <= 300000)
            {
                areaRatio = 0.5;
            }

            return (float)(heat * Math.Pow(areaRatio, 0.25));
        }

        /// <summary>
        /// Determines whether this <see cref="Planetoid"/> will have become
        /// tidally locked to its orbited object in the given timescale.
        /// </summary>
        /// <param name="years">The timescale for tidal locking to have occurred, in years. Usually the approximate age of the local system.</param>
        /// <returns>true if this body will have become tidally locked; false otherwise.</returns>
        public bool GetIsTidallyLockedAfter(float years)
            => Orbit == null
            ? false
            : Math.Pow(((years / 6.0e11) * Mass * Math.Pow(Orbit.OrbitedObject.Mass, 2)) / (Radius * Rigidity), 1.0 / 6.0) >= Orbit.SemiMajorAxis;
    }
}
