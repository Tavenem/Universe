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
    /// A brown dwarf star.
    /// </summary>
    [Serializable]
    public class BrownDwarf : Star
    {
        // False for brown dwarfs; their habitable zones, if any, are moving targets due to rapid
        // cooling, and intersect soon with severe tidal forces, making it unlikely that life could
        // develop before a planet becomes uninhabitable.
        internal override bool IsHospitable => false;

        private protected override string BaseTypeName => "Brown Dwarf";

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/>.
        /// </summary>
        internal BrownDwarf() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="BrownDwarf"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BrownDwarf"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="BrownDwarf"/>.</param>
        internal BrownDwarf(Location parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        private protected BrownDwarf(
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

        private BrownDwarf(SerializationInfo info, StreamingContext context) : this(
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

        private protected override Number GetMass() => Randomizer.Instance.NextNumber(new Number(2.468, 28), new Number(1.7088, 29));

        private protected override IMaterial GetMaterial()
        {
            var temperature = GetTemperature();
            var shape = GetShape(temperature);
            var mass = GetMass();
            return GetComposition((double)(mass / shape.Volume), mass, shape, temperature);
        }

        private protected override IShape GetShape()
        {
            var radius = (Number)Randomizer.Instance.NormalDistributionSample(69911000, 3495550);
            var flattening = Randomizer.Instance.NextNumber(Number.Deci);
            return new Ellipsoid(radius, radius * (1 - flattening), Position);
        }

        private protected override SpectralClass GetSpectralClass()
        {
            var chance = Randomizer.Instance.NextDouble();
            if (chance <= 0.29)
            {
                return SpectralClass.M; // 29%
            }
            else if (chance <= 0.79)
            {
                return SpectralClass.L; // 50%
            }
            else if (chance <= 0.99)
            {
                return SpectralClass.T; // 20%
            }
            else
            {
                return SpectralClass.Y; // 1%
            }
        }
    }
}
