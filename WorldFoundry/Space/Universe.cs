using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// The Universe is the top-level celestial "object" in a hierarchy.
    /// </summary>
    [Serializable]
    public class Universe : CelestialLocation
    {
        private static readonly List<IChildDefinition> _BaseChildDefinitions = new List<IChildDefinition>
        {
            new ChildDefinition<GalaxySupercluster>(GalaxySupercluster.Space, new Number(5.8, -26)),
        };

        private protected override string BaseTypeName => "Universe";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _BaseChildDefinitions;

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

        private Universe(
            string id,
            string? name,
            IMaterial? material,
            Time time) : base(id, name, false, 0, Vector3.Zero, null, material, null)
            => Time = time;

        private Universe(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string)info.GetValue(nameof(Name), typeof(string)),
            (IMaterial)info.GetValue(nameof(_material), typeof(IMaterial)),
            (Time)info.GetValue(nameof(Time), typeof(Time))) { }

        /// <summary>
        /// Gets a new <see cref="CelestialLocation"/> instance.
        /// </summary>
        /// <returns>A new instance of the indicated <see cref="CelestialLocation"/> type, or <see
        /// langword="null"/> if no instance could be generated with the given parameters.</returns>
        public static async Task<Universe?> GetNewInstanceAsync()
        {
            var instance = new Universe();
            await instance.InitializeBaseAsync((string?)null).ConfigureAwait(false);
            return instance;
        }

        /// <summary>
        /// Gets the <see cref="Universe"/> which contains this <see cref="CelestialLocation"/>, if
        /// any.
        /// </summary>
        /// <returns>The <see cref="Universe"/> which contains this <see cref="CelestialLocation"/>,
        /// or <see langword="null"/> if this location is not contained within a universe.</returns>
        public override Task<Universe?> GetContainingUniverseAsync() => Task.FromResult<Universe?>(this);

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
            info.AddValue(nameof(_material), _material);
            info.AddValue(nameof(Time), Time);
        }

        private protected override ValueTask<Number> GetMassAsync() => new ValueTask<Number>(Number.PositiveInfinity);

        // A universe is modeled as a sphere with vast a radius, roughly 4 million times the size of
        // the real observable universe.
        //
        // Approximately 4e18 superclusters might be found in the modeled universe, by volume
        // (although this would require exhaustive "exploration" to populate so much space). This
        // makes the universe effectively infinite in scope, if not in linear dimensions.
        private protected override ValueTask<IShape> GetShapeAsync() => new ValueTask<IShape>(new Sphere(new Number(1.89214, 33)));

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.WarmHotIntergalacticMedium);

        private protected override double? GetTemperature() => 2.73;
    }
}
