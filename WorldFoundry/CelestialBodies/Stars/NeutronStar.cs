using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WorldFoundry.Place;
using WorldFoundry.Space;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A neutron star.
    /// </summary>
    [Serializable]
    public class NeutronStar : Star
    {
        // False for neutron stars, due to their excessive ionizing radiation, which makes the
        // development of life nearby highly unlikely.
        internal override bool IsHospitable => false;

        private protected override string BaseTypeName => "Neutron Star";

        private protected override string DesignatorPrefix => "X";

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/>.
        /// </summary>
        internal NeutronStar() { }

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="NeutronStar"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="NeutronStar"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="NeutronStar"/>.</param>
        internal NeutronStar(Location parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        private protected NeutronStar(
            string id,
            string? name,
            bool isPrepopulated,
            double? luminosity,
            LuminosityClass? luminosityClass,
            bool isPopulationII,
            SpectralClass? spectralClass,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children)
            : base(
                id,
                name,
                isPrepopulated,
                luminosity,
                luminosityClass,
                isPopulationII,
                spectralClass,
                albedo,
                velocity,
                orbit,
                material,
                children) { }

        private NeutronStar(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Luminosity), typeof(double?)),
            (LuminosityClass?)info.GetValue(nameof(LuminosityClass), typeof(LuminosityClass?)),
            (bool)info.GetValue(nameof(IsPopulationII), typeof(bool)),
            (SpectralClass?)info.GetValue(nameof(SpectralClass), typeof(SpectralClass?)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        private protected override double GetLuminosity(Number? temperature = null) => GetLuminosityFromRadius();

        private protected override LuminosityClass GetLuminosityClass() => LuminosityClass.Other;

        private protected override Number GetMass()
            => Randomizer.Instance.NormalDistributionSample(4.4178e30, 5.174e29); // between 1.44 and 3 times solar mass

        private protected override IMaterial GetMaterial()
        {
            var shape = GetShape();
            var mass = GetMass();
            return GetComposition((double)(mass / shape.Volume), mass, shape, GetTemperature());
        }

        private protected override IShape GetShape()
        {
            var radius = Randomizer.Instance.NextNumber(1000, 2000);
            var flattening = (Number)Randomizer.Instance.NormalDistributionSample(0.15, 0.05, minimum: 0);
            return new Ellipsoid(radius, radius * (1 - flattening), Position);
        }

        private protected override SpectralClass GetSpectralClass() => SpectralClass.Other;

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetHomogeneousSubstanceReference(Substances.HomogeneousSubstances.NeutronDegenerateMatter);

        private protected override double? GetTemperature()
            => Randomizer.Instance.NormalDistributionSample(600000, 133333);
    }
}
