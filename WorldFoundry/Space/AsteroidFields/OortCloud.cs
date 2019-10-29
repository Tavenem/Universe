using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.Place;

namespace WorldFoundry.Space.AsteroidFields
{
    /// <summary>
    /// A shell surrounding a star with a high concentration of cometary bodies.
    /// </summary>
    [Serializable]
    public class OortCloud : AsteroidField
    {
        private static readonly Number _ChildDensity = new Number(8.31, -38);

        private static readonly List<IChildDefinition> _ChildDefinitions = new List<IChildDefinition>
        {
            new ChildDefinition<Comet>(Comet.Space, _ChildDensity * new Number(85, -2)),
            new ChildDefinition<CTypeAsteroid>(Asteroid.Space, _ChildDensity * new Number(11, -2)),
            new ChildDefinition<STypeAsteroid>(Asteroid.Space, _ChildDensity * new Number(25, -3)),
            new ChildDefinition<MTypeAsteroid>(Asteroid.Space, _ChildDensity * new Number(15, -3)),
        };

        private protected override string BaseTypeName => "Oort Cloud";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _ChildDefinitions;

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/>.
        /// </summary>
        internal OortCloud() { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="OortCloud"/>.</param>
        internal OortCloud(string? parentId, Vector3 position) : base(parentId, position) { }

        private OortCloud(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            Number? majorRadius,
            Number? minorRadius,
            string? parentId)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                majorRadius,
                minorRadius,
                parentId) { }

        private OortCloud(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (Number?)info.GetValue(nameof(_majorRadius), typeof(Number?)),
            (Number?)info.GetValue(nameof(_minorRadius), typeof(Number?)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        /// <summary>
        /// Gets a new <see cref="AsteroidField"/> instance.
        /// </summary>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// <paramref name="parent"/>.</param>
        /// <param name="starSystemRadius">
        /// The outer radius of the <see cref="StarSystem"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        /// <param name="orbit">The orbit to set for the new <see cref="AsteroidField"/>, if
        /// any.</param>
        /// <returns>A new <see cref="AsteroidField"/> instance, or <see langword="null"/> if no
        /// instance could be generated with the given parameters.</returns>
        public static async Task<OortCloud?> GetNewInstanceAsync(
            Location? parent,
            Number starSystemRadius,
            OrbitalParameters? orbit = null)
        {
            var instance = new OortCloud(parent?.Id, Vector3.Zero);
            if (instance != null)
            {
                instance._majorRadius = new Number(7.5, 15) + starSystemRadius;
                instance._minorRadius = new Number(3, 15) + starSystemRadius;
                if (orbit.HasValue)
                {
                    await Space.Orbit.SetOrbitAsync(instance, orbit.Value).ConfigureAwait(false);
                }
                await instance.InitializeBaseAsync(parent).ConfigureAwait(false);
            }
            return instance;
        }

        private protected override ValueTask<Number> GetMassAsync() => new ValueTask<Number>(new Number(3, 25));

        private protected override async ValueTask<(double density, Number mass, IShape shape)> GetMatterAsync()
        {
            var mass = await GetMassAsync().ConfigureAwait(false);
            var shape = await GetShapeAsync().ConfigureAwait(false);
            return ((double)(mass / shape.Volume), mass, shape);
        }

        private protected override ValueTask<IShape> GetShapeAsync()
        {
            var shape = new HollowSphere(_minorRadius ?? new Number(3, 15), _majorRadius ?? new Number(7.5, 15), Position);
            _majorRadius = null;
            _minorRadius = null;
            return new ValueTask<IShape>(shape);
        }

        private protected override async Task InitializeChildAsync(CelestialLocation child)
        {
            if (Orbit.HasValue)
            {
                var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                if (orbited != null)
                {
                    await child.GenerateOrbitAsync(orbited).ConfigureAwait(false);
                }
            }
        }
    }
}
