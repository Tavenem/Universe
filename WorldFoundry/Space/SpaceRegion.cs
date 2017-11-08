using System;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A region of space.
    /// </summary>
    public class SpaceRegion : SpaceGrid
    {
        /// <summary>
        /// The shape of the <see cref="SpaceRegion"/>.
        /// </summary>
        public override Shape Shape
        {
            get => GetProperty(ref _shape, GenerateShape);
            set => base.Shape = value;
        }

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="SpaceRegion"/>.
        /// </summary>
        /// <remarks>Generates an empty sphere in the base class.</remarks>
        protected void GenerateShape() => Shape = new Sphere();

        protected T GetProperty<T>(ref T storage, Action generator = null, Func<bool> condition = null)
        {
            if ((storage == null || (storage is string s && string.IsNullOrEmpty(s)))
                && (condition == null || condition.Invoke()))
            {
                if (storage == null && typeof(T) != typeof(string))
                {
                    storage = (T)Activator.CreateInstance(typeof(T));
                }
                if (generator != null)
                {
                    generator.Invoke();
                }
            }
            return storage;
        }
    }
}
