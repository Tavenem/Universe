namespace WorldFoundry
{
    /// <summary>
    /// A float value which specifies an average, and optionally a range (minimum to maximum), and a total (sum).
    /// </summary>
    public struct FloatRange
    {
        /// <summary>
        /// The average value.
        /// </summary>
        public float Avg { get; set; }

        private float? _max;
        /// <summary>
        /// The maximum value. Defaults to <see cref="Avg"/> if unset.
        /// </summary>
        public float Max
        {
            get => _max ?? Avg;
            set => _max = value;
        }

        private float? _min;
        /// <summary>
        /// The minimum value. Defaults to <see cref="Avg"/> if unset.
        /// </summary>
        public float Min
        {
            get => _min ?? Avg;
            set => _min = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FloatRange"/> with an <see cref="Avg"/> but no
        /// <see cref="Min"/> or <see cref="Max"/>.
        /// </summary>
        public FloatRange(float value)
        {
            Avg = value;
            _max = null;
            _min = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FloatRange"/> with the specified values.
        /// </summary>
        public FloatRange(float min, float avg, float max)
        {
            Avg = avg;
            _max = max;
            _min = min;
        }
    }
}
