using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Represents a child of a <see cref="CelestialObject"/>.
    /// </summary>
    public class ChildValue
    {
        /// <summary>
        /// The name of the of child to generate.
        /// </summary>
        public string ChildType { get; private set; }

        /// <summary>
        /// The value associated with this type of child.
        /// </summary>
        public float Value { get; internal set; }

        /// <summary>
        /// The type of child to generate.
        /// </summary>
        [NotMapped]
        public Type Type
        {
            get
            {
                try
                {
                    return Type.GetType(ChildType);
                }
                catch
                {
                    return null;
                }
            }
            internal set => ChildType = value.FullName;
        }
    }
}
