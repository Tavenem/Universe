using MathAndScience.MathUtil;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Climate;
using WorldFoundry.Space;

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
            internal set
            {
                var angle = value;
                while (angle > Math.PI)
                {
                    angle -= (float)Math.PI;
                }
                while (angle < 0)
                {
                    angle += (float)Math.PI;
                }
                _angleOfRotation = angle;
                SetAxis();
            }
        }

        private double? _angularVelocity;
        /// <summary>
        /// The angular velocity of this <see cref="Planetoid"/>, in m/s.
        /// </summary>
        public double AngularVelocity
        {
            get => GetProperty(ref _angularVelocity, GenerateAngularVelocity) ?? 0;
            private set => _angularVelocity = value;
        }

        private protected Atmosphere _atmosphere;
        /// <summary>
        /// The atmosphere possessed by this <see cref="Planetoid"/>.
        /// </summary>
        public Atmosphere Atmosphere
        {
            get => GetProperty(ref _atmosphere, GenerateAtmosphere);
            protected set => _atmosphere = value;
        }

        private protected float? _axialPrecession;
        /// <summary>
        /// The angle between the X-axis and the orbital vector at which the first equinox occurs.
        /// </summary>
        public float AxialPrecession
        {
            get => GetProperty(ref _axialPrecession, GenerateAngleOfRotation) ?? 0;
            internal set
            {
                var angle = value;
                while (angle > Math.PI)
                {
                    angle -= (float)Math.PI;
                }
                while (angle < 0)
                {
                    angle += (float)Math.PI;
                }
                _axialPrecession = angle;
                SetAxis();
            }
        }

        /// <summary>
        /// The axial tilt of the <see cref="Planetoid"/> relative to its orbital plane, in radians.
        /// Values greater than half Pi indicate clockwise rotation.
        /// Read-only. To set, specify <see cref="AngleOfRotation"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as <see cref="AngleOfRotation"/>.
        /// </remarks>
        public float AxialTilt => Orbit == null ? AngleOfRotation : AngleOfRotation - Orbit.Inclination;

        private Vector3? _axis;
        /// <summary>
        /// A <see cref="Vector3"/> which represents the axis of this <see cref="Planetoid"/>.
        /// Read-only. To set, specify <see cref="AxialPrecession"/> and/or <see cref="AngleOfRotation"/>.
        /// </summary>
        public Vector3 Axis
        {
            get
            {
                if (!_axis.HasValue)
                {
                    SetAxis();
                }
                return _axis ?? Vector3.Zero;
            }
        }

        private Quaternion? _axisRotation;
        /// <summary>
        /// A <see cref="Quaternion"/> representing the rotation of this <see cref="Planetoid"/>'s
        /// <see cref="Axis"/>.
        /// </summary>
        internal Quaternion AxisRotation
        {
            get
            {
                if (!_axisRotation.HasValue)
                {
                    SetAxis();
                }
                return _axisRotation ?? Quaternion.Identity;
            }
        }

        private double? _density;
        /// <summary>
        /// The average density of this <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        public double Density
        {
            get => GetProperty(ref _density, GenerateDensity) ?? DensityForType;
            set
            {
                if (_density == value || (!_density.HasValue && value == DensityForType))
                {
                    return;
                }

                _density = value;
            }
        }

        private static readonly int extremeRotationalPeriod = 1100000;
        private protected virtual int ExtremeRotationalPeriod => extremeRotationalPeriod;

        private bool? _hasMagnetosphere;
        /// <summary>
        /// Indicates whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        public bool HasMagnetosphere
        {
            get => GetProperty(ref _hasMagnetosphere, GenerateMagnetosphere) ?? false;
            protected set => _hasMagnetosphere = value;
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
            get => (_maxMass ?? MaxMassForType) ?? 0;
            protected set
            {
                if (_maxMass == value || value == MaxMassForType)
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
        internal virtual double? MaxMassForType => null;

        private static readonly int maxRotationalPeriod = 100000;
        private protected virtual int MaxRotationalPeriod => maxRotationalPeriod;

        internal static int maxSatellites = 1;
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
            get => (_minMass ?? MinMassForType) ?? 0;
            protected set
            {
                if (_minMass == value || value == MinMassForType)
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
        internal virtual double? MinMassForType => null;

        private static readonly int minRotationalPeriod = 8000;
        private protected virtual int MinRotationalPeriod => minRotationalPeriod;

        private double? _minSatellitePeriapsis;
        /// <summary>
        /// The minimum distance at which a natural satellite may orbit this <see cref="Planetoid"/>.
        /// </summary>
        private protected double MinSatellitePeriapsis
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
            protected set => _rotationalPeriod = value;
        }

        /// <summary>
        /// The collection of natural satellites around this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="CelestialRegion.Children"/>, natural satellites are actually siblings
        /// in the local <see cref="CelestialRegion"/> hierarchy, which merely share an orbital relationship.
        /// </remarks>
        public IList<Planetoid> Satellites { get; private set; }

        private static readonly double densityForType = 0;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal virtual double DensityForType => densityForType;

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/>.
        /// </summary>
        public Planetoid() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        public Planetoid(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in kg.
        /// </param>
        public Planetoid(CelestialRegion parent, double maxMass) : base(parent) => MaxMass = maxMass;

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        public Planetoid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in kg.
        /// </param>
        public Planetoid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position) => MaxMass = maxMass;

        /// <summary>
        /// Determines an angle between the Y-axis and the axis of rotation for this <see cref="Planetoid"/>.
        /// </summary>
        private protected virtual void GenerateAngleOfRotation()
        {
            _axialPrecession = (float)Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4);
            if (Randomizer.Static.NextDouble() <= 0.2) // low chance of an extreme tilt
            {
                AngleOfRotation = (float)Math.Round(Randomizer.Static.NextDouble(MathConstants.QuarterPI, Math.PI), 4);
            }
            else
            {
                AngleOfRotation = (float)Math.Round(Randomizer.Static.NextDouble(MathConstants.QuarterPI), 4);
            }
        }

        /// <summary>
        /// Calculates the angular velocity of this <see cref="Planetoid"/>.
        /// </summary>
        private void GenerateAngularVelocity() => AngularVelocity = RotationalPeriod == 0 ? 0 : MathConstants.TwoPI / RotationalPeriod;

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Provides no functionality in the base class; subclasses are expected override.
        /// </remarks>
        private protected virtual void GenerateAtmosphere() { }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Does nothing in the base class (allowing <see cref="DensityForType"/> to be used);
        /// subclasses may override if necessary.
        /// </remarks>
        private protected virtual void GenerateDensity() { }

        /// <summary>
        /// Determines whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        /// <remarks>
        /// The presence of a magnetosphere is dependent on the rotational period, and on size: fast
        /// spin of a large body (with a hot interior) produces a dynamo effect. Slow spin of a small
        /// (cooled) body does not. Mass is divided by 3e24, and rotational period by the number of
        /// seconds in an Earth year, which simplifies to multiplying mass by 2.88e-19.
        /// </remarks>
        private protected virtual void GenerateMagnetosphere()
            => HasMagnetosphere = Randomizer.Static.NextDouble() <= ((Mass * 2.88e-19) / RotationalPeriod) * MagnetosphereChanceFactor;

        /// <summary>
        /// Generates an appropriate minimum distance at which a natural satellite may orbit this <see cref="Planetoid"/>.
        /// </summary>
        private protected virtual void GenerateMinSatellitePeriapsis() => _minSatellitePeriapsis = 0;

        /// <summary>
        /// Determines a rotational period for this <see cref="Planetoid"/>.
        /// </summary>
        private protected virtual void GenerateRotationalPeriod()
        {
            // Check for tidal locking.
            if (Orbit != null)
            {
                // Invent an orbit age. Precision isn't important here, and some inaccuracy and
                // inconsistency between satellites is desirable. The age of the Solar system is used
                // as an arbitrary norm.
                var years = (float)(Randomizer.Static.Lognormal(0, 1) * 4.6e9);
                if (GetIsTidallyLockedAfter(years))
                {
                    RotationalPeriod = Orbit.Period;
                    return;
                }
            }

            if (Randomizer.Static.NextDouble() <= 0.05) // low chance of an extreme period
            {
                RotationalPeriod = Math.Round(Randomizer.Static.NextDouble(MaxRotationalPeriod, ExtremeRotationalPeriod));
            }
            else
            {
                RotationalPeriod = Math.Round(Randomizer.Static.NextDouble(MinRotationalPeriod, MaxRotationalPeriod));
            }
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        /// <remarks>Returns null in the base class; subclasses are expected to override.</remarks>
        private protected virtual Planetoid GenerateSatellite(double periapsis, float eccentricity, double maxMass) => null;

        /// <summary>
        /// Generates a set of natural satellites for this celestial body.
        /// </summary>
        /// <param name="max">An optional maximum number of satellites to generate.</param>
        public virtual void GenerateSatellites(int? max = null)
        {
            if (Satellites == null)
            {
                Satellites = new List<Planetoid>();
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
        private protected override float GetInsolationHeat(Vector3 position)
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

        private void SetAxis()
        {
            var precession = Quaternion.CreateFromYawPitchRoll(AxialPrecession, 0, 0);
            var precessionVector = Vector3.Transform(Vector3.UnitX, precession);
            var q = Quaternion.CreateFromAxisAngle(precessionVector, AngleOfRotation);
            _axis = Vector3.Transform(Vector3.UnitY, q);
            _axisRotation = Quaternion.Conjugate(q);
        }
    }
}
