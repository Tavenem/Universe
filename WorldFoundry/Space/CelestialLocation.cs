using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using NeverFoundry.WorldFoundry.CelestialBodies.Stars;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;
using System.Threading.Tasks;
using System.Reflection;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A place in a universe, with a location that defines its position, and a shape that defines
    /// its extent, as well as a mass, density, and temperature. It may or may not also have a
    /// particular chemical composition.
    /// </summary>
    /// <remarks>
    /// Locations can exist in a hierarchy. Any location may contain other locations, and be
    /// contained by a location in turn. The relative positions of locations within the same
    /// hierarchy can be analyzed using the methods available on this class.
    /// </remarks>
    [Serializable]
    public class CelestialLocation : Location
    {
        private protected double? _albedo;
        private double? _averageBlackbodyTemperature;
        private double? _blackbodyTemperature;
        private protected bool _isPrepopulated;
        private double? _surfaceTemperatureAtApoapsis;
        private double? _surfaceTemperatureAtPeriapsis;

        /// <summary>
        /// <para>
        /// The average density of this material, in kg/m³.
        /// </para>
        /// <para>
        /// A material may have either uniform or uneven density (e.g. contained voids or an
        /// irregular shape contained within its overall dimensions). This value represents the
        /// average throughout the full volume of its <see cref="IMaterial.Shape"/>.
        /// </para>
        /// </summary>
        public double Density
        {
            get => Material.Density;
            set
            {
                Material.Density = value;
                _surfaceGravity = null;
            }
        }

        /// <summary>
        /// A string that uniquely identifies this <see cref="CelestialLocation"/> for display
        /// purposes.
        /// </summary>
        public string Designation
            => string.IsNullOrEmpty(DesignatorPrefix)
                ? Id
                : $"{DesignatorPrefix} {Id}";

        /// <summary>
        /// The mass of this material, in kg.
        /// </summary>
        public Number Mass
        {
            get => Material.Mass;
            set
            {
                Material.Mass = value;
                _surfaceGravity = null;
            }
        }

        private protected IMaterial? _material;
        /// <summary>
        /// The physical material which comprises this location.
        /// </summary>
        public virtual IMaterial Material
        {
            get => _material ??= new Material(0, SinglePoint.Origin);
            set => _material = value;
        }

        /// <summary>
        /// An optional name for this <see cref="CelestialLocation"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="CelestialLocation"/> must have a name. They may be uniquely identified
        /// by their <see cref="Designation"/>, instead.
        /// </remarks>
        public virtual string? Name { get; private protected set; }

        private protected Orbit? _orbit;
        /// <summary>
        /// The orbit occupied by this <see cref="CelestialLocation"/> (may be null).
        /// </summary>
        public virtual Orbit? Orbit => _orbit;

        private Vector3 _position;
        /// <summary>
        /// The position of this location relative to the center of its parent.
        /// </summary>
        public override Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                _blackbodyTemperature = null;
                if (!(_material is null))
                {
                    _material.Shape = _material.Shape.GetCloneAtPosition(value);
                }
            }
        }

        /// <summary>
        /// The shape of this location.
        /// </summary>
        public override IShape Shape
        {
            get => Material.Shape;
            set
            {
                Material.Shape = value.GetCloneAtPosition(Position);
                _radiusSquared = null;
                _surfaceGravity = null;
            }
        }

        private protected Number? _surfaceGravity;
        /// <summary>
        /// The average force of gravity at the surface of this object, in N.
        /// </summary>
        public Number SurfaceGravity => _surfaceGravity ??= Material.GetSurfaceGravity();

        /// <summary>
        /// The <see cref="CelestialLocation"/>'s <see cref="Name"/>, if it has one; otherwise its
        /// <see cref="Designation"/>.
        /// </summary>
        public string Title => Name ?? Designation;

        /// <summary>
        /// The name for this type of <see cref="CelestialLocation"/>.
        /// </summary>
        public virtual string TypeName => BaseTypeName;

        /// <summary>
        /// The velocity of the <see cref="CelestialLocation"/> in m/s.
        /// </summary>
        public virtual Vector3 Velocity { get; set; }

        private Number? _radiusSquared;
        internal Number RadiusSquared => _radiusSquared ??= Shape.ContainingRadius.Square();

        private protected virtual string BaseTypeName => "Celestial Location";

        private protected virtual IEnumerable<IChildDefinition> ChildDefinitions => Enumerable.Empty<IChildDefinition>();

        private protected virtual string DesignatorPrefix => string.Empty;

        private protected override bool HasDefinedShape => !(_material is null);

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialLocation"/>.
        /// </summary>
        internal CelestialLocation() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialLocation"/>.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The position of the location relative to the center of its
        /// parent.</param>
        public CelestialLocation(string? parentId, Vector3 position) : base(parentId) => Position = position;

        private protected CelestialLocation(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            string? parentId) : base(id, parentId)
        {
            Id = id;
            Name = name;
            _isPrepopulated = isPrepopulated;
            _albedo = albedo;
            _orbit = orbit;
            _material = material;
            Velocity = velocity;
        }

        private CelestialLocation(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        /// <summary>
        /// Gets a new instance of the indicated <see cref="CelestialLocation"/> type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="CelestialLocation"/> to generate.</typeparam>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// <paramref name="parent"/>.</param>
        /// <param name="orbit">The orbit to set for the new <see cref="CelestialLocation"/>, if
        /// any.</param>
        /// <returns>A new instance of the indicated <see cref="CelestialLocation"/> type, or <see
        /// langword="null"/> if no instance could be generated with the given parameters.</returns>
        public static async Task<T?> GetNewInstanceAsync<T>(Location? parent, Vector3 position, OrbitalParameters? orbit = null) where T : CelestialLocation
        {
            var instance = typeof(T).InvokeMember(
                null,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                new object?[] { parent?.Id, position }) as T;
            if (instance != null)
            {
                await instance.GenerateMaterialAsync().ConfigureAwait(false);
                if (orbit.HasValue)
                {
                    await Space.Orbit.SetOrbitAsync(instance, orbit.Value).ConfigureAwait(false);
                }
                await instance.InitializeBaseAsync(parent).ConfigureAwait(false);
            }
            return instance;
        }

        /// <summary>
        /// Gets a new instance of the indicated <see cref="CelestialLocation"/> type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="CelestialLocation"/> to generate.</typeparam>
        /// <param name="parentId">The id of the location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// parent.</param>
        /// <param name="orbit">The orbit to set for the new <see cref="CelestialLocation"/>, if
        /// any.</param>
        /// <returns>A new instance of the indicated <see cref="CelestialLocation"/> type, or <see
        /// langword="null"/> if no instance could be generated with the given parameters.</returns>
        public static async Task<T?> GetNewInstanceAsync<T>(string? parentId, Vector3 position, OrbitalParameters? orbit = null) where T : CelestialLocation
        {
            var instance = typeof(T).InvokeMember(
                null,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                new object?[] { parentId, position }) as T;
            if (instance != null)
            {
                await instance.GenerateMaterialAsync().ConfigureAwait(false);
                if (orbit.HasValue)
                {
                    await Space.Orbit.SetOrbitAsync(instance, orbit.Value).ConfigureAwait(false);
                }
                await instance.InitializeBaseAsync(parentId).ConfigureAwait(false);
            }
            return instance;
        }

        /// <summary>
        /// <para>
        /// Generates and returns new child entities within this <see cref="CelestialLocation"/>.
        /// </para>
        /// <para>
        /// CAUTION: this enumeration may not terminate on its own. Large, dense regions may
        /// potentially contains billions or trillions of children. Take care that calling code
        /// restricts the number of iterations performed.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// NOTE: Generated children are not automatically saved to the data source. This permits
        /// calling code to optionally save or discard individual results.
        /// </para>
        /// <para>
        /// If the region is small and the density of a given child is low enough that the number
        /// indicated to be present is less than one, that value is taken as the probability that
        /// one will be found instead.
        /// </para>
        /// <para>
        /// If the region is large and the density of children is high, there is a real possibility
        /// that the number of children enumerated can result in overflows or other errors. Calling
        /// code should ensure that only a restricted number of iterations are performed, and avoid
        /// methods such as <c>Last</c> or <c>Count</c> which would result in an attempt to
        /// enumerate more results than are possible.
        /// </para>
        /// <para>
        /// See <see cref="GetRadiusWithChildren(Number)"/> and <see
        /// cref="GenerateChildrenAsync(Location)"/>, which can be used in combination to get
        /// children only in a sub-region of specific size, and potentially avoid the possibility of
        /// dangerous enumerations.
        /// </para>
        /// </remarks>
        public async IAsyncEnumerable<CelestialLocation> GenerateChildrenAsync()
        {
            if (!_isPrepopulated)
            {
                await PrepopulateRegionAsync().ConfigureAwait(false);
            }
            var childAmounts = new List<(IChildDefinition def, Number rem)>();
            foreach (var (totalType, totalAmount) in GetChildTotals())
            {
                var count = await GetMatchingChildrenAsync(totalType).CountAsync().ConfigureAwait(false);
                var rem = totalAmount - count;
                childAmounts.Add((totalType, rem));
            }
            await foreach (var child in GenerateChildrenAsync(childAmounts))
            {
                yield return child;
            }
        }

        /// <summary>
        /// <para>
        /// Generates and returns new child entities within this <see cref="CelestialLocation"/>,
        /// inside the boundaries of the given <paramref name="location"/>.
        /// </para>
        /// <para>
        /// CAUTION: this enumeration may not terminate on its own. Large, dense regions may
        /// potentially contains billions or trillions of children. Take care that calling code
        /// restricts the number of iterations performed.
        /// </para>
        /// </summary>
        /// <param name="location">The location within which children will be generated.</param>
        /// <remarks>
        /// <para>
        /// NOTE: Generated children are not automatically saved to the data source. This permits
        /// calling code to optionally save or discard individual results.
        /// </para>
        /// <para>
        /// If the region is small and the density of a given child is low enough that the number
        /// indicated to be present is less than one, that value is taken as the probability that
        /// one will be found instead.
        /// </para>
        /// <para>
        /// If the region is large and the density of children is high, there is a real possibility
        /// that the number of children enumerated can result in overflows or other errors. Calling
        /// code should ensure that only a restricted number of iterations are performed, and avoid
        /// methods such as <c>Last</c> or <c>Count</c> which would result in an attempt to
        /// enumerate more results than are possible.
        /// </para>
        /// <para>
        /// See <see cref="GetRadiusWithChildren(Number)"/>, which can be used in combination with
        /// this method to get children only in a sub-region of specific size, and potentially avoid
        /// the possibility of dangerous enumerations.
        /// </para>
        /// </remarks>
        public async IAsyncEnumerable<CelestialLocation> GenerateChildrenAsync(Location location)
        {
            if (await location.GetCommonParentAsync(this).ConfigureAwait(false) != this)
            {
                yield break;
            }
            if (!_isPrepopulated)
            {
                await PrepopulateRegionAsync().ConfigureAwait(false);
            }
            var childAmounts = new List<(IChildDefinition def, Number rem)>();
            foreach (var (totalType, totalAmount) in ChildDefinitions.Select(x => (x, Shape.Volume * x.Density)))
            {
                var rem = totalAmount - await GetMatchingChildrenAsync(totalType).CountAwaitAsync(x => location.ContainsAsync(x)).ConfigureAwait(false);
                childAmounts.Add((totalType, rem));
            }
            await foreach (var child in GenerateChildrenAsync(childAmounts))
            {
                yield return child;
            }
        }

        /// <summary>
        /// <para>
        /// Generates and returns new child entities of the given type within this <see
        /// cref="CelestialLocation"/>.
        /// </para>
        /// <para>
        /// CAUTION: this enumeration may not terminate on its own. Large, dense regions may
        /// potentially contains billions or trillions of children. Take care that calling code
        /// restricts the number of iterations performed.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of children to generate.</typeparam>
        /// <param name="condition">An <see cref="IChildDefinition"/> the children must
        /// match.</param>
        /// <remarks>
        /// <para>
        /// NOTE: Generated children are not automatically saved to the data source. This permits
        /// calling code to optionally save or discard individual results.
        /// </para>
        /// <para>
        /// If the region is small and the density of the given child type is low enough that the
        /// number indicated to be present is less than one, that value is taken as the probability
        /// that one will be found instead.
        /// </para>
        /// <para>
        /// If the region is large and the density of the given child type is high, there is a real
        /// possibility that the number of children enumerated can result in overflows or other
        /// errors. Calling code should ensure that only a restricted number of iterations are
        /// performed, and avoid methods such as <c>Last</c> or <c>Count</c> which would result in
        /// an attempt to enumerate more results than are possible.
        /// </para>
        /// <para>
        /// See <see cref="GetRadiusWithChildren{T}(Number, IChildDefinition)"/> and <see
        /// cref="GenerateChildrenAsync{T}(Location, IChildDefinition)"/>, which can be used in
        /// combination to get children only in a sub-region of specific size, and potentially avoid
        /// the possibility of dangerous enumerations.
        /// </para>
        /// </remarks>
        public async IAsyncEnumerable<T> GenerateChildrenAsync<T>(IChildDefinition? condition = null)
        {
            var definitions = ChildDefinitions.Where(x => x.IsSatisfiedBy(typeof(T)) && condition?.IsSatisfiedBy(x) != false).ToList();
            if (definitions.Count == 0)
            {
                yield break;
            }
            if (!_isPrepopulated)
            {
                await PrepopulateRegionAsync().ConfigureAwait(false);
            }
            var childAmounts = new List<(IChildDefinition def, Number rem)>();
            foreach (var (totalType, totalAmount) in definitions.Select(x => (x, Shape.Volume * x.Density)))
            {
                var count = await GetMatchingChildrenAsync(totalType).CountAsync().ConfigureAwait(false);
                var rem = totalAmount - count;
                childAmounts.Add((totalType, rem));
            }
            await foreach (var child in GenerateChildrenAsync(childAmounts).OfType<T>())
            {
                yield return child;
            }
        }

        /// <summary>
        /// <para>
        /// Generates and returns new child entities of the given type within this <see
        /// cref="CelestialLocation"/>, inside the boundaries of the given <paramref
        /// name="location"/>.
        /// </para>
        /// <para>
        /// CAUTION: this enumeration may not terminate on its own. Large, dense regions may
        /// potentially contains billions or trillions of children. Take care that calling code
        /// restricts the number of iterations performed.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of children to generate.</typeparam>
        /// <param name="location">The location within which children will be generated.</param>
        /// <param name="condition">An <see cref="IChildDefinition"/> the children must
        /// match.</param>
        /// <remarks>
        /// <para>
        /// NOTE: Generated children are not automatically saved to the data source. This permits
        /// calling code to optionally save or discard individual results.
        /// </para>
        /// <para>
        /// If the region is small and the density of the given child type is low enough that the
        /// number indicated to be present is less than one, that value is taken as the probability
        /// that one will be found instead.
        /// </para>
        /// <para>
        /// If the region is large and the density of the given child is high, there is a real
        /// possibility that the number of children enumerated can result in overflows or other
        /// errors. Calling code should ensure that only a restricted number of iterations are
        /// performed, and avoid methods such as <c>Last</c> or <c>Count</c> which would result in
        /// an attempt to enumerate more results than are possible.
        /// </para>
        /// <para>
        /// See <see cref="GetRadiusWithChildren{T}(Number, IChildDefinition)"/>, which can be used
        /// in combination with this method to get children only in a sub-region of specific size,
        /// and potentially avoid the possibility of dangerous enumerations.
        /// </para>
        /// </remarks>
        public async IAsyncEnumerable<T> GenerateChildrenAsync<T>(Location location, IChildDefinition? condition = null)
        {
            var definitions = ChildDefinitions.Where(x => x.IsSatisfiedBy(typeof(T)) && condition?.IsSatisfiedBy(x) != false);
            if (!definitions.Any())
            {
                yield break;
            }
            if (await location.GetCommonParentAsync(this).ConfigureAwait(false) != this)
            {
                yield break;
            }
            if (!_isPrepopulated)
            {
                await PrepopulateRegionAsync().ConfigureAwait(false);
            }
            var childAmounts = new List<(IChildDefinition def, Number rem)>();
            foreach (var (totalType, totalAmount) in definitions.Select(x => (x, Shape.Volume * x.Density)))
            {
                var rem = totalAmount - await GetMatchingChildrenAsync(totalType).CountAwaitAsync(x => location.ContainsAsync(x)).ConfigureAwait(false);
                childAmounts.Add((totalType, rem));
            }
            await foreach (var child in GenerateChildrenAsync(childAmounts).OfType<T>())
            {
                yield return child;
            }
        }

        /// <summary>
        /// Gets the average albedo of this celestial entity (a value between 0 and 1).
        /// </summary>
        /// <returns>
        /// The average albedo of this celestial entity (a value between 0 and 1).
        /// </returns>
        /// <remarks>
        /// This refers to the total albedo of the body, including any atmosphere, not just the
        /// surface albedo of the main body.
        /// </remarks>
        public async Task<double> GetAlbedoAsync()
        {
            if (!_albedo.HasValue)
            {
                await GenerateAlbedoAsync().ConfigureAwait(false);
            }
            return _albedo ?? 0;
        }

        /// <summary>
        /// Gets the total temperature of this location, averaged over its orbit, in K.
        /// </summary>
        public async Task<double> GetAverageBlackbodyTemperatureAsync()
        {
            _averageBlackbodyTemperature ??= Orbit.HasValue
                ? ((await GetTemperatureAtPeriapsisAsync().ConfigureAwait(false) * (1 + Orbit.Value.Eccentricity)) + (await GetTemperatureAtApoapsisAsync().ConfigureAwait(false) * (1 - Orbit.Value.Eccentricity))) / 2
                : await GetTemperatureAtPositionAsync(Position).ConfigureAwait(false);
            return _averageBlackbodyTemperature.Value;
        }

        /// <summary>
        /// The average surface temperature of the location near its equator throughout its orbit
        /// (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public virtual Task<double> GetAverageSurfaceTemperatureAsync() => GetAverageBlackbodyTemperatureAsync();

        /// <summary>
        /// Gets the total temperature of this location.
        /// </summary>
        public async Task<double> GetBlackbodyTemperatureAsync()
        {
            _blackbodyTemperature ??= await GetTemperatureAtPositionAsync(Position).ConfigureAwait(false);
            return _blackbodyTemperature.Value;
        }

        /// <summary>
        /// Gets the parent <see cref="CelestialLocation"/> which contains this one, if any.
        /// </summary>
        /// <returns>The parent <see cref="CelestialLocation"/> which contains this one, if
        /// any.</returns>
        public Task<CelestialLocation?> GetCelestialParentAsync() => DataStore.GetItemAsync<CelestialLocation>(ParentId);

        /// <summary>
        /// Gets a random child of the specified type: either an existing child, or a newly
        /// generated one if there are no current children which match the given criteria.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="CelestialLocation"/> child entity to
        /// get.</typeparam>
        /// <param name="condition">A <see cref="IChildDefinition"/> the child must match.</param>
        /// <returns>A child of the given type, or <see langword="null"/> if no child of the given
        /// type could be retrieved. This might occur if no children of the given type occur in this
        /// location, or if insufficient free space remains to generate a new one.</returns>
        /// <remarks>
        /// Any newly generated child is not automatically saved to the data source. This permits
        /// calling code to optionally save or discard the result.
        /// </remarks>
        public async Task<T?> GetChildAsync<T>(IChildDefinition? condition = null) where T : CelestialLocation
        {
            if (!_isPrepopulated)
            {
                await PrepopulateRegionAsync().ConfigureAwait(false);
            }
            await foreach (var child in GetChildrenAsync()
                .OfType<T>()
                .Where(x => typeof(T).IsAssignableFrom(x.GetType())))
            {
                if (condition is null)
                {
                    return child;
                }
                var match = await condition.IsSatisfiedByAsync(child).ConfigureAwait(false);
                if (match)
                {
                    return child;
                }
            }
            return await GenerateChildrenAsync<T>(condition).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerates the children of this instance.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of child <see cref="Location"/> instances of
        /// this one.</returns>
        public override async IAsyncEnumerable<Location> GetChildrenAsync()
        {
            if (!_isPrepopulated)
            {
                await PrepopulateRegionAsync().ConfigureAwait(false);
            }
            await foreach (var child in base.GetChildrenAsync())
            {
                yield return child;
            }
        }

        /// <summary>
        /// Calculates the total number of children in this region. The totals are approximate,
        /// based on the defined densities of the possible children it might have.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IChildDefinition"/> instances
        /// along with the total number of children present in this region (as a <see
        /// cref="Number"/> due to the potentially vast numbers
        /// involved).</returns>
        public IEnumerable<(IChildDefinition type, Number total)> GetChildTotals()
            => ChildDefinitions.Select(x => (x, Shape.Volume * x.Density));

        /// <summary>
        /// Gets the <see cref="Universe"/> which contains this <see cref="CelestialLocation"/>, if
        /// any.
        /// </summary>
        /// <returns>The <see cref="Universe"/> which contains this <see cref="CelestialLocation"/>,
        /// or <see langword="null"/> if this location is not contained within a universe.</returns>
        public virtual async Task<Universe?> GetContainingUniverseAsync()
        {
            var parent = await GetParentAsync().ConfigureAwait(false);
            if (parent is Universe u)
            {
                return u;
            }
            if (parent is CelestialLocation cl)
            {
                return await cl.GetContainingUniverseAsync().ConfigureAwait(false);
            }
            return null;
        }

        /// <summary>
        /// Calculates the escape velocity from this location, in m/s.
        /// </summary>
        /// <returns>The escape velocity from this location, in m/s.</returns>
        public Number GetEscapeVelocity() => Number.Sqrt(ScienceConstants.TwoG * Mass / Shape.ContainingRadius);

        /// <summary>
        /// Calculates the force of gravity on this <see cref="CelestialLocation"/> from another as
        /// a vector, in N.
        /// </summary>
        /// <param name="other">A <see cref="CelestialLocation"/> from which the force gravity will
        /// be calculated. If <see langword="null"/>, or if the two do not share a common parent,
        /// the result will be zero.</param>
        /// <returns>
        /// The force of gravity from this <see cref="CelestialLocation"/> to the other, in N, as a
        /// vector.
        /// </returns>
        /// <exception cref="Exception">
        /// An exception will be thrown if the two <see cref="CelestialLocation"/> instances do not
        /// share a <see cref="Location"/> parent at some point.
        /// </exception>
        /// <remarks>
        /// Newton's law is used. General relativity would be more accurate in certain
        /// circumstances, but is considered unnecessarily intensive work for the simple simulations
        /// expected to make use of this library. If you are an astronomer performing scientifically
        /// rigorous calculations or simulations, this is not the library for you ;)
        /// </remarks>
        public async Task<Vector3> GetGravityFromObjectAsync(CelestialLocation? other)
        {
            if (other is null)
            {
                return Vector3.Zero;
            }

            var distance = await GetDistanceToAsync(other).ConfigureAwait(false);

            if (distance.IsFinite)
            {
                return Vector3.Zero;
            }

            var scale = -ScienceConstants.G * (Mass * other.Mass / (distance * distance));

            // Get the normalized vector
            var normalized = (other.Position - Position) / distance;

            return normalized * scale;
        }

        /// <summary>
        /// Calculates the total luminous flux incident on this body from nearby sources of light
        /// (stars in the same system), in lumens.
        /// </summary>
        /// <returns>The total illumination on the body, in lumens.</returns>
        /// <remarks>
        /// A conversion of 0.0079 W/m² per lumen is used, which is roughly accurate for the sun,
        /// but may not be as precise for other stellar bodies.
        /// </remarks>
        public async Task<double> GetLuminousFluxAsync()
        {
            var parent = await GetCelestialParentAsync().ConfigureAwait(false);
            if (parent is null)
            {
                return 0;
            }
            return await parent.GetAllChildrenAsync<Star>()
                .SumAwaitAsync(async x => (double)(x.Luminosity / (MathConstants.FourPI * await GetDistanceSquaredToAsync(x).ConfigureAwait(false))) / 0.0079)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates a random child of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="CelestialLocation"/> child entity to
        /// generate.</typeparam>
        /// <param name="condition">A <see cref="IChildDefinition"/> the child must match.</param>
        /// <returns>A randomly generated child of the given type, or <see langword="null"/> if no
        /// child of the given type could be generated. This might occur if no children of the given
        /// type occur in this location, or if insufficient free space remains.</returns>
        /// <remarks>
        /// The generated child is not automatically saved to the data source. This permits calling
        /// code to optionally save or discard the result.
        /// </remarks>
        public async Task<T?> GetNewChildAsync<T>(IChildDefinition? condition = null) where T : CelestialLocation
            => await GenerateChildrenAsync<T>(condition).FirstOrDefaultAsync().ConfigureAwait(false);

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
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(_albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(_material), _material);
        }

        /// <summary>
        /// Calculates the position of this <see cref="CelestialLocation"/> at the given time,
        /// taking its orbit or velocity into account, without actually updating its current
        /// <see cref="Position"/>. Does not perform integration over time of gravitational
        /// influences not reflected by <see cref="Orbit"/>.
        /// </summary>
        /// <param name="time">The time at which to get a position.</param>
        /// <returns>A <see cref="Vector3"/> representing position relative to the center of the
        /// parent.</returns>
        public async ValueTask<Vector3> GetPositionAtTimeAsync(Duration time)
        {
            if (Orbit.HasValue)
            {
                var (position, _) = Orbit.Value.GetStateVectorsAtTime(time);

                var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                if (orbited?.ParentId != ParentId)
                {
                    if (string.IsNullOrEmpty(ParentId))
                    {
                        return position;
                    }
                    else
                    {
                        var parent = await GetParentAsync().ConfigureAwait(false);
                        if (parent is null || orbited is null)
                        {
                            return position;
                        }
                        else
                        {
                            return await parent.GetLocalizedPositionAsync(orbited).ConfigureAwait(false) + position;
                        }
                    }
                }
                else
                {
                    return (orbited?.Position ?? Vector3.Zero) + position;
                }
            }
            else
            {
                return Position + (Velocity * time.ToSeconds());
            }
        }

        /// <summary>
        /// Calculates the radius of a spherical region which contains at most the given amount of
        /// child entities, given the densities of the child definitions for this region.
        /// </summary>
        /// <param name="maxAmount">The maximum desired number of child entities in the
        /// region.</param>
        /// <returns>The radius of a spherical region containing at most the given amount of
        /// children, in meters.</returns>
        public Number GetRadiusWithChildren(Number maxAmount)
        {
            if (maxAmount.IsZero)
            {
                return 0;
            }
            var numInM3 = ChildDefinitions.Sum(x => x.Density);
            var v = maxAmount / numInM3;
            // The number in a single m³ may be so small that this goes to infinity; if so, perform
            // the calculation in reverse.
            if (v.IsInfinite)
            {
                var total = Shape.Volume * numInM3;
                var ratio = maxAmount / total;
                v = Shape.Volume * ratio;
            }
            return (3 * v / MathConstants.FourPI).CubeRoot();
        }

        /// <summary>
        /// Calculates the radius of a spherical region which contains at most the given amount of
        /// child entities of the given type, given the densities of the child definitions for this
        /// region.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="CelestialLocation"/> child entities to
        /// check.</typeparam>
        /// <param name="maxAmount">The maximum desired number of child entities of the given type
        /// in the region.</param>
        /// <param name="condition">A <see cref="IChildDefinition"/> the children must match.</param>
        /// <returns>The radius of a spherical region containing at most the given amount of
        /// children, in meters. May be zero, if this location does not contain children of the
        /// given type.</returns>
        public Number GetRadiusWithChildren<T>(Number maxAmount, IChildDefinition? condition = null) where T : CelestialLocation
        {
            if (maxAmount.IsZero)
            {
                return 0;
            }
            var numInM3 = ChildDefinitions
                .Where(x => x.IsSatisfiedBy(typeof(T)) && condition?.IsSatisfiedBy(x) != false)
                .Sum(x => x.Density);
            var v = maxAmount / numInM3;
            // The number in a single m³ may be so small that this goes to infinity; if so, perform
            // the calculation in reverse.
            if (v.IsInfinite)
            {
                var total = Shape.Volume * numInM3;
                var ratio = maxAmount / total;
                v = Shape.Volume * ratio;
            }
            return (3 * v / MathConstants.FourPI).CubeRoot();
        }

        /// <summary>
        /// Gets the average temperature of this material, in K. May be <see langword="null"/>,
        /// indicating that it is at the ambient temperature of its environment.
        /// </summary>
        /// <returns>
        /// The average temperature of this material, in K; or <see langword="null"/>, indicating
        /// that it is at the ambient temperature of its environment.
        /// </returns>
        /// <remarks>
        /// No less than the ambient temperature of its parent, if any.
        /// </remarks>
        public async Task<double?> GetTemperatureAsync()
        {
            if (!(await GetParentAsync().ConfigureAwait(false) is CelestialLocation parent))
            {
                return Material.Temperature;
            }
            var parentTemp = await parent.GetTemperatureAsync().ConfigureAwait(false);
            if (parentTemp.HasValue)
            {
                if (Material.Temperature.HasValue)
                {
                    return Math.Max(Material.Temperature.Value, parentTemp.Value);
                }
                else
                {
                    return parentTemp;
                }
            }
            return Material.Temperature;
        }

        /// <summary>
        /// Calculates the total force of gravity on this <see cref="CelestialLocation"/>, in N, as
        /// a vector. Note that results may be highly inaccurate if the parent region has not been
        /// populated thoroughly enough in the vicinity of this entity (with the scale of "vicinity"
        /// depending strongly on the mass of the region's potential children).
        /// </summary>
        /// <returns>
        /// The total force of gravity on this <see cref="CelestialLocation"/> from all
        /// currently-generated children, in N, as a vector.
        /// </returns>
        /// <remarks>
        /// Newton's law is used. Children of sibling objects are not counted individually; instead
        /// the entire sibling is treated as a single entity, with total mass including all its
        /// children. Objects outside the parent are ignored entirely, assuming they are either too
        /// far to be of significance, or operate in a larger frame of reference (e.g. the Earth
        /// moves within the gravity of the Milky Way, but when determining its movement within the
        /// solar system, the effects of the greater galaxy are not relevant).
        /// </remarks>
        public async Task<Vector3> GetTotalLocalGravityAsync()
        {
            var totalGravity = Vector3.Zero;

            var parent = await GetCelestialParentAsync().ConfigureAwait(false);
            // No gravity for a parent-less object
            if (parent is null)
            {
                return totalGravity;
            }

            await foreach (var sibling in parent.GetAllChildrenAsync<CelestialLocation>())
            {
                totalGravity += await GetGravityFromObjectAsync(sibling).ConfigureAwait(false);
            }

            return totalGravity;
        }

        /// <summary>
        /// Sets the average albedo of this celestial entity (a value between 0 and 1).
        /// </summary>
        /// <param name="value">A value between 0 and 1.</param>
        public async Task SetAlbedoAsync(double value)
        {
            _albedo = value.Clamp(0, 1);
            await ResetCachedTemperaturesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets this location's name.
        /// </summary>
        /// <param name="value">The name. May be <see langword="null"/>.</param>
        /// <remarks>
        /// If the name is set to <see langword="null"/>, a generic designation will be used.
        /// </remarks>
        public virtual Task SetNameAsync(string? value)
        {
            Name = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the occupied by this <see cref="CelestialLocation"/> (may be null).
        /// </summary>
        /// <param name="value">An <see cref="Orbit"/>.</param>
        public async Task SetOrbitAsync(Orbit? value)
        {
            _orbit = value;
            await ResetCachedTemperaturesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the average temperature of this material, in K. May be set to <see
        /// langword="null"/> to indicate that it is at the ambient temperature of its environment.
        /// </summary>
        /// <param name="value">A temperature in K; or <see langword="null"/> to indicate the
        /// ambient temperature of its environment.</param>
        public void SetTemperature(double? value) => Material.Temperature = value;

        /// <summary>
        /// Returns a string that represents the celestial object.
        /// </summary>
        /// <returns>A string that represents the celestial object.</returns>
        public override string ToString() => $"{TypeName} {Title}";

        /// <summary>
        /// Updates the position and velocity of this object to correspond with the state predicted
        /// by its <see cref="Orbit"/> at the current time of its containing <see
        /// cref="Universe"/>, assuming no influences on the body's motion have occurred aside
        /// from its orbit. Has no effect if the body is not in orbit.
        /// </summary>
        public async Task UpdateOrbitAsync()
        {
            if (!Orbit.HasValue)
            {
                return;
            }
            var universe = await GetContainingUniverseAsync().ConfigureAwait(false);
            if (universe is null)
            {
                return;
            }

            var (position, velocity) = Orbit.Value.GetStateVectorsAtTime(universe.Time.Now);

            await UpdateOrbitAsync(position, velocity).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the position and velocity of this object to correspond with the state predicted
        /// by its <see cref="Orbit"/> after the specified number of seconds since its orbit's epoch
        /// (initial time of pericenter), assuming no influences on the body's motion have occurred
        /// aside from its orbit. Has no effect if the body is not in orbit.
        /// </summary>
        /// <param name="elapsedSeconds">
        /// The number of seconds which have elapsed since the orbit's defining epoch (time of
        /// pericenter).
        /// </param>
        public async Task UpdateOrbitAsync(Number elapsedSeconds)
        {
            if (!Orbit.HasValue)
            {
                return;
            }

            var (position, velocity) = Orbit.Value.GetStateVectorsAtTime(elapsedSeconds);

            await UpdateOrbitAsync(position, velocity).ConfigureAwait(false);
        }

        internal virtual Task GenerateOrbitAsync(CelestialLocation orbitedObject) => Space.Orbit.SetCircularOrbitAsync(this, orbitedObject);

        internal async Task<Number> GetHillSphereRadiusAsync() => Orbit.HasValue
            ? await Orbit.Value.GetHillSphereRadiusAsync(this).ConfigureAwait(false)
            : Number.Zero;

        internal virtual async Task<bool> GetIsHospitableAsync()
        {
            var parent = await GetCelestialParentAsync().ConfigureAwait(false);
            if (parent != null)
            {
                return await parent.GetIsHospitableAsync().ConfigureAwait(false);
            }
            return true;
        }

        /// <summary>
        /// Approximates the radius of the orbiting body's mutual Hill sphere with another orbiting
        /// body in orbit around the same primary, in meters.
        /// </summary>
        /// <remarks>
        /// Assumes the semimajor axis of both orbits is identical for the purposes of the
        /// calculation, which obviously would not be the case, but generates reasonably close
        /// estimates in the absence of actual values.
        /// </remarks>
        /// <param name="otherMass">
        /// The mass of another celestial body presumed to be orbiting the same primary as this one.
        /// </param>
        /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
        internal async Task<Number> GetMutualHillSphereRadiusAsync(Number otherMass) => Orbit.HasValue
            ? await Orbit.Value.GetMutualHillSphereRadiusAsync(this, otherMass).ConfigureAwait(false)
            : Number.Zero;

        internal Number GetRocheLimit(Number orbitingDensity)
            => new Number(8947, -4) * (Mass / orbitingDensity).CubeRoot();

        internal async Task<Number> GetSphereOfInfluenceRadiusAsync() => Orbit.HasValue
            ? await Orbit.Value.GetSphereOfInfluenceRadiusAsync(this).ConfigureAwait(false)
            : Number.Zero;

        internal virtual Task ResetCachedTemperaturesAsync()
        {
            _averageBlackbodyTemperature = null;
            _blackbodyTemperature = null;
            _surfaceTemperatureAtApoapsis = null;
            _surfaceTemperatureAtPeriapsis = null;
            return Task.CompletedTask;
        }

        private protected virtual Task GenerateAlbedoAsync()
        {
            _albedo = 0;
            return Task.CompletedTask;
        }

        private async Task<CelestialLocation?> GenerateChildAsync(IChildDefinition definition)
        {
            var position = await GetOpenSpaceAsync(definition.Space).ConfigureAwait(false);
            if (!position.HasValue)
            {
                return null;
            }
            if (definition is IStarSystemChildDefinition sscd)
            {
                return await sscd.GetStarSystemAsync(this, position.Value).ConfigureAwait(false);
            }
            return await definition.GetChildAsync(this, position.Value).ConfigureAwait(false);
        }

        private async IAsyncEnumerable<CelestialLocation> GenerateChildrenAsync(List<(IChildDefinition def, Number rem)> childAmounts)
        {
            var total = childAmounts.Sum(x => x.rem);
            var defs = childAmounts.Select(x => (x.def, ratio: x.rem / total, x.rem)).ToList();
            var nullCount = 0;
            while (!defs.Sum(x => x.ratio).IsZero)
            {
                var def = Randomizer.Instance.Next(defs, x => (double)x.ratio);

                var child = await GenerateChildAsync(def.def).ConfigureAwait(false);
                if (child != null)
                {
                    nullCount = 0;
                    yield return child;
                }
                else
                {
                    nullCount++;
                    if (nullCount >= 10)
                    {
                        break;
                    }
                }

                def.rem = Number.Max(Number.Zero, def.rem - Number.One);
                total--;
                def.ratio = def.rem.IsZero ? 0 : (double)(def.rem / total);
            }
        }

        private protected virtual async Task GenerateMaterialAsync()
        {
            if (_material is null)
            {
                var (density, mass, shape) = await GetMatterAsync().ConfigureAwait(false);
                Material = GetComposition(density, mass, shape, GetTemperature());
            }
        }

        private protected virtual IMaterial GetComposition(double density, Number mass, IShape shape, double? temperature)
        {
            var substance = GetSubstance();
            return !(substance is null)
                ? new Material(substance, density, mass, shape, temperature)
                : new Material(density, mass, shape, temperature);
        }

        /// <summary>
        /// Calculates the heat added to this location by insolation at the given position, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this location at which the heat of insolation will be
        /// calculated.
        /// </param>
        /// <returns>
        /// The heat added to this location by insolation at the given position, in K.
        /// </returns>
        private async Task<double> GetInsolationHeatAsync(Vector3 position)
        {
            var parent = await GetCelestialParentAsync().ConfigureAwait(false);
            if (parent is null)
            {
                return 0;
            }
            else
            {
                var relativePosition = await GetLocalizedPositionAsync(parent, position).ConfigureAwait(false);
                var albedo = await GetAlbedoAsync().ConfigureAwait(false);
                return Math.Pow(1 - albedo, 0.25) * (await parent
                  .GetAllChildrenAsync<Star>()
                  .Where(x => x != this)
                  .SumAwaitAsync(async x => (await x.GetTemperatureAsync().ConfigureAwait(false) ?? 0) * (double)Number.Sqrt(x.Shape.ContainingRadius / (2 * await GetDistanceFromPositionToAsync(relativePosition, x).ConfigureAwait(false))))
                  .ConfigureAwait(false));
            }
        }

        private async IAsyncEnumerable<Location> GetMatchingChildrenAsync(IChildDefinition childDefinition)
        {
            await foreach (var child in GetChildrenAsync())
            {
                if (await childDefinition.IsSatisfiedByAsync(child).ConfigureAwait(false))
                {
                    yield return child;
                }
            }
        }

        private protected virtual async ValueTask<(double density, Number mass, IShape shape)> GetMatterAsync()
        {
            var mass = await GetMassAsync().ConfigureAwait(false);
            var shape = await GetShapeAsync().ConfigureAwait(false);
            return ((double)(mass / shape.Volume), mass, shape);
        }

        /// <summary>
        /// Calculates the total average temperature of the location as if this object was at the
        /// apoapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit, or if its apoapsis is infinite.
        /// </remarks>
        private protected async Task<double> GetTemperatureAtApoapsisAsync()
        {
            if (!_surfaceTemperatureAtApoapsis.HasValue)
            {
                // Actual position doesn't matter for temperature, only distance.
                if (!Orbit.HasValue || Orbit.Value.Apoapsis.IsInfinite)
                {
                    _surfaceTemperatureAtApoapsis = await GetTemperatureAtPositionAsync(Position).ConfigureAwait(false);
                }
                else
                {
                    var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                    if (orbited is null)
                    {
                        _surfaceTemperatureAtApoapsis = await GetTemperatureAtPositionAsync(Position).ConfigureAwait(false);
                    }
                    else
                    {
                        _surfaceTemperatureAtApoapsis = await GetTemperatureAtPositionAsync(orbited.Position + (Vector3.UnitX * Orbit.Value.Apoapsis)).ConfigureAwait(false);
                    }
                }
            }
            return _surfaceTemperatureAtApoapsis.Value;
        }

        /// <summary>
        /// Calculates the total average temperature of the location as if this object was at the
        /// periapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit.
        /// </remarks>
        private protected async Task<double> GetTemperatureAtPeriapsisAsync()
        {
            if (!_surfaceTemperatureAtPeriapsis.HasValue)
            {
                // Actual position doesn't matter for temperature, only distance.
                if (!Orbit.HasValue || Orbit.Value.Apoapsis.IsInfinite)
                {
                    _surfaceTemperatureAtPeriapsis = await GetTemperatureAtPositionAsync(Position).ConfigureAwait(false);
                }
                else
                {
                    var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                    if (orbited is null)
                    {
                        _surfaceTemperatureAtPeriapsis = await GetTemperatureAtPositionAsync(Position).ConfigureAwait(false);
                    }
                    else
                    {
                        _surfaceTemperatureAtPeriapsis = await GetTemperatureAtPositionAsync(orbited.Position + (Vector3.UnitX * Orbit.Value.Periapsis)).ConfigureAwait(false);
                    }
                }
            }
            return _surfaceTemperatureAtPeriapsis.Value;
        }

        /// <summary>
        /// Calculates the total average temperature of the location as if this object was at the
        /// specified position, including ambient heat of its parent and radiated heat from all
        /// sibling objects, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this location at which its temperature will be calculated.
        /// </param>
        /// <returns>
        /// The total average temperature of the location at the given position, in K.
        /// </returns>
        private protected async Task<double> GetTemperatureAtPositionAsync(Vector3 position)
            => (await GetTemperatureAsync().ConfigureAwait(false) ?? 0) + (await GetInsolationHeatAsync(position).ConfigureAwait(false));

        /// <summary>
        /// Estimates the total average temperature of the location as if this object was at the
        /// specified true anomaly in its orbit, including ambient heat of its parent and radiated
        /// heat from all sibling objects, in K. If the body is not in orbit, returns the
        /// temperature at its current position.
        /// </summary>
        /// <param name="trueAnomaly">
        /// A true anomaly at which its temperature will be calculated.
        /// </param>
        /// <returns>
        /// The total average temperature of the location at the given position, in K.
        /// </returns>
        /// <remarks>
        /// The estimation is performed by linear interpolation between the temperature at periapsis
        /// and apoapsis, which is not necessarily accurate for highly elliptical orbits, or bodies
        /// with multiple significant nearby heat sources, but it should be fairly accurate for
        /// bodies in fairly circular orbits around heat sources which are all close to the center
        /// of the orbit, and much faster for successive calls than calculating the temperature at
        /// specific positions precisely.
        /// </remarks>
        private protected async Task<double> GetTemperatureAtTrueAnomalyAsync(double trueAnomaly)
        {
            var tempApoapsis = await GetTemperatureAtApoapsisAsync().ConfigureAwait(false);
            var tempPeriapsis = await GetTemperatureAtPeriapsisAsync().ConfigureAwait(false);
            return tempPeriapsis.Lerp(tempApoapsis, trueAnomaly <= Math.PI ? trueAnomaly / Math.PI : 2 - (trueAnomaly / Math.PI));
        }

        private protected virtual double GetDensity() => 0;

        private protected virtual ValueTask<Number> GetMassAsync() => new ValueTask<Number>(Number.Zero);

        private protected virtual ValueTask<IShape> GetShapeAsync() => new ValueTask<IShape>(new SinglePoint(Position));

        private protected virtual ISubstanceReference? GetSubstance() => null;

        private protected virtual double? GetTemperature() => null;

        private protected virtual Task InitializeAsync() => Task.CompletedTask;

        private protected async Task InitializeBaseAsync(Location? parent)
        {
            if (parent is CelestialLocation celestialParent)
            {
                await celestialParent.InitializeChildAsync(this).ConfigureAwait(false);
            }
            await InitializeAsync().ConfigureAwait(false);
        }

        private protected async Task InitializeBaseAsync(string? parentId)
        {
            var parent = await DataStore.GetItemAsync<Location>(parentId).ConfigureAwait(false);
            await InitializeBaseAsync(parent).ConfigureAwait(false);
        }

        private protected virtual Task InitializeChildAsync(CelestialLocation child) => Task.CompletedTask;

        private protected virtual Task PrepopulateRegionAsync()
        {
            _isPrepopulated = true;
            return Task.CompletedTask;
        }

        private async Task UpdateOrbitAsync(Vector3 position, Vector3 velocity)
        {
            if (!Orbit.HasValue)
            {
                return;
            }
            var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
            if (orbited?.ParentId != ParentId)
            {
                if (string.IsNullOrEmpty(ParentId))
                {
                    Position = position;
                }
                else
                {
                    var parent = await GetParentAsync().ConfigureAwait(false);
                    if (parent is null || orbited is null)
                    {
                        Position = position;
                    }
                    else
                    {
                        Position = await parent.GetLocalizedPositionAsync(orbited).ConfigureAwait(false) + position;
                    }
                }
            }
            else
            {
                Position = orbited!.Position + position;
            }

            Velocity = velocity;
        }
    }
}
