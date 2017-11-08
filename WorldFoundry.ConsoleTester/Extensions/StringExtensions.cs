namespace WorldFoundry.ConsoleApp.Extensions
{
    internal static class StringExtensions
    {
        internal static double? ParseNullableDouble(this string str)
            => double.TryParse(str, out var result) ? result : (double?)null;

        internal static float? ParseNullableFloat(this string str)
            => float.TryParse(str, out var result) ? result : (float?)null;

        internal static int? ParseNullableInt(this string str)
            => int.TryParse(str, out var result) ? result : (int?)null;
    }
}
