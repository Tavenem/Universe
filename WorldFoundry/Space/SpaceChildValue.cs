using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Represents a child of a <see cref="SpaceRegion"/>.
    /// </summary>
    public class SpaceChildValue
    {
        /// <summary>
        /// The name of the of child to generate.
        /// </summary>
        public string ChildType { get; set; }

        /// <summary>
        /// The value associated with this type of child.
        /// </summary>
        public float Value { get; set; }

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
            set => ChildType = value.FullName;
        }
    }
}
