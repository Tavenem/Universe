using System;

namespace WorldFoundry.ConsoleApp
{
    internal class PlanetParams
    {
        internal float? AtmosphericPressure { get; set; }
        internal float? AxialTilt { get; set; }
        internal int? Radius { get; set; }
        internal double? RevolutionPeriod { get; set; }
        internal double? RotationalPeriod { get; set; }
        internal float? WaterRatio { get; set; }
        internal int? GridSize { get; set; }
        internal int? ElevationSize { get; set; }
        internal Guid? ID { get; set; }

        internal PlanetParams() { }
    }
}
