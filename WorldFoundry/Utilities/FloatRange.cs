﻿using NeverFoundry.MathAndScience;
using NeverFoundry.WorldFoundry.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text.Json.Serialization;

namespace NeverFoundry.WorldFoundry
{
    /// <summary>
    /// A floating-point value which specifies an average, and optionally a range (minimum to maximum).
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(FloatRangeConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(Utilities.NewtonsoftJson.FloatRangeConverter))]
    public struct FloatRange : ISerializable, IEquatable<FloatRange>
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
        public bool IsZero => Min.IsNearlyZero() && Max.IsNearlyZero();

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
        public FloatRange(float value) : this(value, value, value) { }

        /// <summary>
        /// Initializes a new instance of <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="min">The value at which <see cref="Min"/> is to be set.</param>
        /// <param name="max">The value at which <see cref="Max"/> is to be set.</param>
        public FloatRange(float min, float max) : this(min, min + ((max - min) / 2), max) { }

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

        private FloatRange(SerializationInfo info, StreamingContext context) : this(
            (float?)info.GetValue(nameof(Min), typeof(float)) ?? default,
            (float?)info.GetValue(nameof(Max), typeof(float)) ?? default)
        { }

        /// <summary>
        /// Attempts to parse the given string as a <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="s">A string.</param>
        /// <returns>
        /// The resulting <see cref="FloatRange"/> value.
        /// </returns>
        /// <exception cref="FormatException">
        /// The provided value is not a valid <see cref="FloatRange"/>.
        /// </exception>
        public static FloatRange Parse(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new FormatException();
            }
            return Parse(s.AsSpan(), CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Parses the given span as a <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="s">A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.</param>
        /// <returns>
        /// The resulting <see cref="FloatRange"/> value.
        /// </returns>
        /// <exception cref="FormatException">
        /// The provided value is not a valid <see cref="FloatRange"/>.
        /// </exception>
        public static FloatRange Parse(in ReadOnlySpan<char> s)
            => Parse(s, CultureInfo.CurrentCulture);

        /// <summary>
        /// Attempts to parse the given string as a <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="s">A string.</param>
        /// <param name="provider">
        /// An object that supplies culture-specific formatting information.
        /// </param>
        /// <returns>
        /// The resulting <see cref="FloatRange"/> value.
        /// </returns>
        /// <exception cref="FormatException">
        /// The provided value is not a valid <see cref="FloatRange"/>.
        /// </exception>
        public static FloatRange Parse(string? s, IFormatProvider? provider)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new FormatException();
            }
            return Parse(s.AsSpan(), provider);
        }

        /// <summary>
        /// Parses the given span as a <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="s">A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.</param>
        /// <param name="provider">
        /// An object that supplies culture-specific formatting information.
        /// </param>
        /// <returns>
        /// The resulting <see cref="FloatRange"/> value.
        /// </returns>
        /// <exception cref="FormatException">
        /// The provided value is not a valid <see cref="FloatRange"/>.
        /// </exception>
        public static FloatRange Parse(in ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (!TryParse(s, provider, out var value))
            {
                throw new FormatException();
            }
            return value;
        }

        /// <summary>
        /// Attempts to parse the given string as a <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="s">A string.</param>
        /// <param name="value">
        /// If this method returns <see langword="true"/>, will be set to the resulting <see
        /// cref="FloatRange"/> value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the span could be successfully parsed; otherwise <see
        /// langword="false"/>.
        /// </returns>
        public static bool TryParse(string? s, out FloatRange value)
        {
            value = Zero;
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }
            return TryParse(s.AsSpan(), CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Attempts to parse the given span as a <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="s">A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.</param>
        /// <param name="value">
        /// If this method returns <see langword="true"/>, will be set to the resulting <see
        /// cref="FloatRange"/> value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the span could be successfully parsed; otherwise <see
        /// langword="false"/>.
        /// </returns>
        public static bool TryParse(in ReadOnlySpan<char> s, out FloatRange value)
            => TryParse(s, CultureInfo.CurrentCulture, out value);

        /// <summary>
        /// Attempts to parse the given string as a <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="s">A string.</param>
        /// <param name="provider">
        /// An object that supplies culture-specific formatting information.
        /// </param>
        /// <param name="value">
        /// If this method returns <see langword="true"/>, will be set to the resulting <see
        /// cref="FloatRange"/> value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the span could be successfully parsed; otherwise <see
        /// langword="false"/>.
        /// </returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out FloatRange value)
        {
            value = Zero;
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }
            return TryParse(s.AsSpan(), provider, out value);
        }

        /// <summary>
        /// Attempts to parse the given span as a <see cref="FloatRange"/>.
        /// </summary>
        /// <param name="s">A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.</param>
        /// <param name="provider">
        /// An object that supplies culture-specific formatting information.
        /// </param>
        /// <param name="value">
        /// If this method returns <see langword="true"/>, will be set to the resulting <see
        /// cref="FloatRange"/> value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the span could be successfully parsed; otherwise <see
        /// langword="false"/>.
        /// </returns>
        public static bool TryParse(in ReadOnlySpan<char> s, IFormatProvider? provider, out FloatRange value)
        {
            value = Zero;
            if (s.IsEmpty || s.IsWhiteSpace()
                || s.Length < 5
                || s[0] != '<'
                || s[^1] != '>')
            {
                return false;
            }
            var separatorIndex = s.IndexOf(';');
            if (separatorIndex == -1)
            {
                return false;
            }
            if (!float.TryParse(s[1..separatorIndex], NumberStyles.Float | NumberStyles.AllowThousands, provider, out var min))
            {
                return false;
            }
            if (!float.TryParse(s[(separatorIndex + 1)..^1], NumberStyles.Float | NumberStyles.AllowThousands, provider, out var max))
            {
                return false;
            }
            value = new FloatRange(min, max);
            return true;
        }

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type
        /// and represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public override bool Equals(object? obj) => obj is FloatRange range && Equals(range);

        /// <summary>Indicates whether the current object is equal to another object of the same
        /// type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <see langword="true" /> if the current object is equal to the <paramref name="other" />
        /// parameter; otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals([AllowNull] FloatRange other) => Max == other.Max && Min == other.Min;

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode() => HashCode.Combine(Max, Min);

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Min), Min);
            info.AddValue(nameof(Max), Max);
        }

        /// <summary>Returns this instance as a <see cref="string"/>.</summary>
        /// <returns>The <see cref="string"/> equivalent of this instance.</returns>
        public override string ToString() => $"<{Min:G9};{Max:G9}>";

        /// <summary>Returns this instance as a <see cref="string"/>.</summary>
        /// <param name="format">A numeric format string.</param>
        /// <returns>The <see cref="string"/> equivalent of this instance.</returns>
        public string ToString(string? format) => $"<{Min.ToString(format)};{Max.ToString(format)}>";

        /// <summary>Returns this instance as a <see cref="string"/>.</summary>
        /// <param name="provider">
        /// An object that supplies culture-specific formatting information.
        /// </param>
        /// <returns>The <see cref="string"/> equivalent of this instance.</returns>
        public string ToString(IFormatProvider? provider) => $"<{Min.ToString("G9", provider)};{Max.ToString("G9", provider)}>";

        /// <summary>Returns this instance as a <see cref="string"/>.</summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="provider">
        /// An object that supplies culture-specific formatting information.
        /// </param>
        /// <returns>The <see cref="string"/> equivalent of this instance.</returns>
        public string ToString(string? format, IFormatProvider? provider) => $"<{Min.ToString(format, provider)};{Max.ToString(format, provider)}>";

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="left" /> and <paramref name="right" />
        /// represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator ==(FloatRange left, FloatRange right) => left.Equals(right);

        /// <summary>Indicates whether this instance and a specified object are unequal.</summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="left" /> and <paramref name="right" />
        /// represent different values; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator !=(FloatRange left, FloatRange right) => !(left == right);
    }
}
