using System.Numerics;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Place"/> which refers to an individual location (as opposed to a region).
    /// </summary>
    public class Location : Place
    {
        /// <summary>
        /// The exact position within or on the <see cref="Place.Entity"/> represented by this <see cref="Location"/>.
        /// </summary>
        public virtual Vector3 Position { get; set; }

        /// <summary>
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public override Place GetDeepClone() => new Location
        {
            Entity = Entity,
            Position = Position,
        };

        /// <summary>
        /// Gets a deep clone of this <see cref="Location"/>.
        /// </summary>
        public Location GetDeepCopy() => GetDeepClone() as Location;

        /// <summary>
        /// Determines whether the specified <see cref="Place"/> is equivalent to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Place"/> is equivalent to the
        /// current object; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Matches(Place obj) => obj is Location location && Matches(location);

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is equivalent to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is equivalent to the
        /// current object; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool Matches(Location obj) => base.Matches(obj) && Position == obj.Position;

        /// <summary>
        /// Gets a <see cref="Territory"/> which is equivalent to this <see cref="Location"/>.
        /// </summary>
        public virtual Territory ToTerritory() => new Territory { Entity = Entity };
    }
}
