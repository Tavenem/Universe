using ExtensionLib;
using MathAndScience;
using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using WorldFoundry.Orbits;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A discrete region of space bound together by gravity, but not a single body; such as a galaxy or star system.
    /// </summary>
    public class CelestialRegion : Orbiter
    {
        private protected bool _isPrepopulated;

        /// <summary>
        /// The types of children found in this region.
        /// </summary>
        public virtual IEnumerable<ChildDefinition> ChildDefinitions => Enumerable.Empty<ChildDefinition>();

        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public virtual IList<(Type type, double proportion, object[] constructorParameters)> ChildPossibilities => null;

        /// <summary>
        /// The <see cref="CelestialRegion"/> children contained within this instance.
        /// </summary>
        public IEnumerable<CelestialEntity> Children => Location.Children.SelectNonNull(x => x.CelestialEntity);

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialRegion"/>.
        /// </summary>
        public CelestialRegion() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialRegion"/> is located.
        /// </param>
        public CelestialRegion(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialRegion"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialRegion"/>.</param>
        public CelestialRegion(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Enumerates all the <see cref="CelestialEntity"/> descendant children instances in this
        /// region.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of all descendant <see cref="CelestialEntity"/>
        /// child instances within this region.</returns>
        public IEnumerable<CelestialEntity> GetAllChildren()
            => Location.GetAllChildren().SelectNonNull(x => x.CelestialEntity);

        /// <summary>
        /// Enumerates all the <see cref="CelestialEntity"/> descendant children instances of the
        /// given type in this region.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="CelestialEntity"/> to retrieve.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> of all descendant <see cref="CelestialEntity"/>
        /// child instances of the given type within this region.</returns>
        public IEnumerable<T> GetAllChildren<T>() where T : CelestialEntity
            => Location.GetAllChildren().SelectNonNull(x => x.CelestialEntity).OfType<T>();

        /// <summary>
        /// Enumerates all the <see cref="CelestialEntity"/> descendant children instances of the
        /// given <paramref name="type"/> in this region.
        /// </summary>
        /// <param name="type">The type of <see cref="CelestialEntity"/> to retrieve.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of all descendant <see cref="CelestialEntity"/>
        /// child instances of the given <paramref name="type"/> within this region.</returns>
        public IEnumerable<CelestialEntity> GetAllChildren(Type type)
            => Location.GetAllChildren().SelectNonNull(x => x.CelestialEntity).Where(x => type.IsAssignableFrom(x.GetType()));

        /// <summary>
        /// Calculates the total number of children in this region. The totals are approximate,
        /// based on the defined densities of the possible children it might have.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ChildDefinition"/> instances
        /// along with the total number of children present in this region (as a double rather than
        /// an integral value, due to the potentially vast numbers involved).</returns>
        public IEnumerable<(ChildDefinition type, double total)> GetChildTotals()
            => ChildDefinitions.Select(x => (x, Shape.Volume * x.Density));

        /// <summary>
        /// Calculates the radius of a spherical region which contains at most the given amount of
        /// child entities, given the densities of the child definitions for this region.
        /// </summary>
        /// <param name="maxAmount">The maximum desired number of child entities in the
        /// region.</param>
        /// <returns>The radius of a spherical region containing at most the given amount of
        /// children, in meters.</returns>
        public double GetRadiusWithChildren(double maxAmount)
        {
            if (maxAmount.IsZero())
            {
                return 0;
            }
            var numInM3 = ChildDefinitions.Sum(x => x.Density);
            var v = maxAmount / numInM3;
            // The number in a single m³ may be so small that this goes to infinity; if so, perform
            // the calculation in reverse.
            if (double.IsInfinity(v))
            {
                var total = Shape.Volume * numInM3;
                var ratio = maxAmount / total;
                v = Shape.Volume * ratio;
            }
            return Math.Pow(3 * v / MathConstants.FourPI, 1.0 / 3.0);
        }

        /// <summary>
        /// Generates an appropriate population of child entities in the given <paramref
        /// name="region"/>.
        /// </summary>
        /// <param name="region">A <see cref="Region"/> representing an area of local space.</param>
        /// <remarks>
        /// <para>
        /// If the given <paramref name="region"/> is not within this instance's local space,
        /// nothing happens.
        /// </para>
        /// <para>
        /// If the region is small and the density of a given child is low enough that the number
        /// indicated to be present is less than one, that value is taken as the probability that
        /// one will be found instead.
        /// </para>
        /// <para>
        /// If the region is large and the density of a given child is high enough that the number
        /// of children indicated exceeds the number of actual child instances a region can maintain
        /// (<see cref="int.MaxValue"/> for all child instances), the number will be truncated at
        /// the maximum allowable value. To avoid memory issues, even this outcome should be avoided
        /// by putting constraints on calling code to ensure that regions to be populated are not so
        /// large that the number of children is excessive (<see
        /// cref="GetRadiusWithChildren(double)"/> can be used to determine an appropriate region
        /// size).
        /// </para>
        /// </remarks>
        public virtual void PopulateRegion(Region region) => PopulateLocation(region);

        /// <summary>
        /// Generates an appropriate population of child entities in this <see
        /// cref="CelestialRegion"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the region is small and the density of a given child is low enough that the number
        /// indicated to be present is less than one, that value is taken as the probability that
        /// one will be found instead.
        /// </para>
        /// <para>
        /// If the region is large and the density of a given child is high enough that the number
        /// of children indicated exceeds the number of actual child instances a region can maintain
        /// (<see cref="int.MaxValue"/> for all child instances), the number will be truncated at
        /// the maximum allowable value. To avoid memory issues, even this outcome should be avoided
        /// by putting constraints on calling code to ensure that regions to be populated are not so
        /// large that the number of children is excessive (<see
        /// cref="GetRadiusWithChildren(double)"/> and <see cref="PopulateRegion(Region)"/> can be
        /// used to populate only a sub-region of an appropriate size).
        /// </para>
        /// </remarks>
        public virtual void PopulateRegion()
        {
            if (!_isPrepopulated)
            {
                PrepopulateRegion();
            }
            foreach (var child in ChildDefinitions)
            {
                var number = Math.Min(Shape.Volume * child.Density, int.MaxValue - Children.Count() - 1);
                if (number < 1 && Randomizer.Instance.NextDouble() <= number)
                {
                    number = 1;
                }
                number -= GetAllChildren(child.Type).Count();
                for (var i = 0; i < number; i++)
                {
                    GenerateChild(child);
                }
            }
        }

        /// <summary>
        /// Removes the given child <see cref="CelestialEntity"/> from this instance. Does nothing
        /// if the given instance is <see langword="null"/> or is not a child of this one.
        /// </summary>
        /// <param name="child">A <see cref="CelestialEntity"/> to remove as a child of this
        /// one.</param>
        public void RemoveChild(CelestialEntity child) => child?.Location.SetNewParent(null);

        internal virtual void PrepopulateRegion() => _isPrepopulated = true;

        internal virtual Orbiter GenerateChild(ChildDefinition definition)
            => definition.GenerateChild(this);

        private protected override void GenerateLocation(CelestialRegion parent = null, Vector3? position = null)
            => _location = new Region(this, parent?.Location, new SinglePoint(position ?? Vector3.Zero));

        private void PopulateLocation(Location location)
        {
            if (!(Location is Region r) || location.GetCommonParent(Location) != Location)
            {
                return;
            }
            if (!_isPrepopulated)
            {
                PrepopulateRegion();
            }
            foreach (var childLocation in location.Children)
            {
                PopulateLocation(childLocation);
            }
            if (!(location is Region region))
            {
                return;
            }
            foreach (var child in ChildDefinitions)
            {
                var number = Math.Min(region.Shape.Volume * child.Density, int.MaxValue - Children.Count() - 1);
                if (number < 1 && Randomizer.Instance.NextDouble() <= number)
                {
                    number = 1;
                }
                number -= GetAllChildren(child.Type).Count(x => region.Contains(x.Location));
                for (var i = 0; i < number; i++)
                {
                    GenerateChild(child);
                }
            }
        }
    }
}
