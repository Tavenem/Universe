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
        /// Indicates whether this <see cref="Location"/> refers to the same <see cref="Place"/> as
        /// the given one.
        /// </summary>
        public virtual bool Matches(Location other) => Entity == other?.Entity && Position == other?.Position;

        /// <summary>
        /// Gets a <see cref="Territory"/> which is equivalent to this <see cref="Location"/>.
        /// </summary>
        public virtual Territory ToTerritory() => new Territory { Entity = Entity };
    }
}
