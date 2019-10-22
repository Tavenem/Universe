using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The Universe is the top-level celestial "object" in a hierarchy.
    /// </summary>
    [Serializable]
    public class Universe : CelestialLocation
    {
        private static readonly List<ChildDefinition> BaseChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GalaxySupercluster), GalaxySupercluster.Space, new Number(5.8, -26)),
        };

        private protected override string BaseTypeName => "Universe";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(BaseChildDefinitions);

        /// <summary>
        /// The <see cref="Space.Universe"/> which contains this <see cref="CelestialLocation"/>, if any.
        /// </summary>
        public override Universe? ContainingUniverse => this;

        /// <summary>
        /// The time in this universe.
        /// </summary>
        public Time Time { get; private set; }

        /// <summary>
        /// The velocity of the <see cref="CelestialLocation"/> in m/s.
        /// </summary>
        /// <remarks>
        /// The universe has no velocity. This will always return <see cref="Vector3.Zero"/>, and
        /// setting it will have no effect.
        /// </remarks>
        public override Vector3 Velocity
        {
            get => Vector3.Zero;
            set { }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Universe"/>.
        /// </summary>
        /// <param name="present">The present time in the universe.</param>
        public Universe(Duration? present = null)
        {
            Time = present.HasValue
                ? new Time(present.Value)
                : new Time();
        }

        private Universe(string id, string name, Time time)
        {
            Id = id;
            Name = name;
            Time = time;
        }

        private Universe(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string)info.GetValue(nameof(Name), typeof(string)),
            (Time)info.GetValue(nameof(Time), typeof(Time))) { }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Time), Time);
        }

        private protected override Number GetMass() => Number.PositiveInfinity;

        // A universe is modeled as a sphere with vast a radius, roughly 4 million times the size of
        // the real observable universe.
        //
        // Approximately 4e18 superclusters might be found in the modeled universe, by volume
        // (although this would require exhaustive "exploration" to populate so much space). This
        // makes the universe effectively infinite in scope, if not in linear dimensions.
        private protected override IShape GetShape() => new Sphere(new Number(1.89214, 33));

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.WarmHotIntergalacticMedium);

        private protected override double? GetTemperature() => 2.73;
    }
}
