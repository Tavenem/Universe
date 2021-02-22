using System;

namespace NeverFoundry.WorldFoundry.Planet.SurfaceMapping
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member. Can't specify XML comments for record members yet: https://github.com/dotnet/roslyn/issues/44571.
    /// <summary>
    /// Options for specifying hill shading to a surface map.
    /// </summary>
    public record HillShadingOptions(
        bool ApplyToLand,
        bool ApplyToOcean,
        double ScaleFactor = 5,
        bool ScaleIsRelative = true,
        double ShadeMultiplier = 1.25)
    {
        private double _scaleFactor = ScaleFactor;
        public double ScaleFactor
        {
            get => _scaleFactor;
            init => _scaleFactor = Math.Max(0, value);
        }

        private double _shadeMultiplier = ShadeMultiplier;
        public double ShadeMultiplier
        {
            get => _shadeMultiplier;
            init => _shadeMultiplier = Math.Max(0, value);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
