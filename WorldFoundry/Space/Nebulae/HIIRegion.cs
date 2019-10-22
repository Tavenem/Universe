using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A charged cloud of interstellar gas and dust.
    /// </summary>
    [Serializable]
    public class HIIRegion : Nebula
    {
        private static readonly Number _ChildDensity = new Number(6, -50);

        private static readonly List<ChildDefinition> _BaseChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(9998, -4), typeof(Star), SpectralClass.B, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(2, -4), typeof(Star), SpectralClass.O, LuminosityClass.V),
        };

        private protected override string BaseTypeName => "HII Region";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_BaseChildDefinitions);

        internal HIIRegion() { }

        internal HIIRegion(Location parent, Vector3 position) : base(parent, position) { }

        private HIIRegion(
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

        private HIIRegion(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        private protected override Number GetMass() => Randomizer.Instance.NextNumber(new Number(1.99, 33), new Number(1.99, 37)); // ~10e3–10e7 solar masses

        // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
        // which the dust clouds and filaments roughly fit. The radius follows a log-normal
        // distribution, with  ~20 ly as the mode, starting at ~10 ly, and cutting off around ~600
        // ly.
        private protected override IShape GetShape()
        {
            Number axis;
            do
            {
                axis = new Number(1, 17) + (Randomizer.Instance.LogNormalDistributionSample(0, 1) * new Number(1, 17));
            } while (axis > Space);
            return new Ellipsoid(
                axis,
                axis * Randomizer.Instance.NextNumber(Number.Half, new Number(15, -1)),
                axis * Randomizer.Instance.NextNumber(Number.Half, new Number(15, -1)),
                Position);
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IonizedCloud);

        private protected override double? GetTemperature() => 10000;
    }
}
