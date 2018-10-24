using MathAndScience;

namespace WorldFoundry
{
    /// <summary>
    /// A floating-point value which specifies an average, and optionally a range (minimum to maximum).
    /// </summary>
    public struct FloatRange
    {
        /// <summary>
        /// A <see cref="FloatRange"/> with all values set to zero.
        /// </summary>
        public static readonly FloatRange Zero = new FloatRange();

        /// <summary>
        /// A <see cref="FloatRange"/> with the minimum set to 0, the average set to 0.5, and the
        /// maximum set to 1.
        /// </summary>
        public static readonly FloatRange ZeroToOne = new FloatRange(0, 1);

        /// <summary>
        /// The average value.
        /// </summary>
        public float Average { get; }

        /// <summary>
        /// Whether this range begins and ends at zero.
        /// </summary>
        public bool IsZero => Min.IsZero() && Max.IsZero();

        /// <summary>
        /// The maximum value.
        /// </summary>
        public float Max { get; }

        /// <summary>
        /// The minimum value.
        /// </summary>
        public float Min { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="FloatRange"/> with  set to the same value.
        /// </summary>
        /// <param name="value">The value to set for all three properties (<see cref="Average"/>,
        /// <see cref="Min"/> and <see cref="Max"/>).</param>
        public FloatRange(float value)
        {
            Average = value;
            Max = value;
            Min = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="min">The value at which <see cref="Min"/> is to be set.</param>
        /// <param name="max">The value at which <see cref="Max"/> is to be set.</param>
        public FloatRange(float min, float max)
        {
            Average = min + ((max - min) / 2);
            Max = max;
            Min = min;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="min">The value at which <see cref="Min"/> is to be set.</param>
        /// <param name="average">The value at which <see cref="Average"/> is to be set.</param>
        /// <param name="max">The value at which <see cref="Max"/> is to be set.</param>
        public FloatRange(float min, float average, float max)
        {
            Average = average;
            Max = max;
            Min = min;
        }
    }
}
