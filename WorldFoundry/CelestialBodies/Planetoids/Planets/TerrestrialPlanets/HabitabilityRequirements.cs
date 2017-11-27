using System.Collections.Generic;
using WorldFoundry.Substances;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A collection of parameters for habitability by a given form of life which a <see
    /// cref="TerrestrialPlanet"/> must meet in order for such life to survive unaided.
    /// </summary>
    public struct HabitabilityRequirements
    {
        public List<ComponentRequirement> AtmosphericRequirements { get; set; }
        public float? MinimumSurfaceTemperature { get; set; }
        public float? MaximumSurfaceTemperature { get; set; }
        public float? MinimumSurfacePressure { get; set; }
        public float? MaximumSurfacePressure { get; set; }
        public float? MinimumSurfaceGravity { get; set; }
        public float? MaximumSurfaceGravity { get; set; }

        public HabitabilityRequirements(
            List<ComponentRequirement> atmosphericRequirements,
            float? minimumSurfaceTemperature, float? maximumSurfaceTemperature,
            float? minimumSurfacePressure, float? maximumSurfacePressure,
            float? minimumSurfaceGravity, float? maximumSurfaceGravity)
        {
            AtmosphericRequirements = atmosphericRequirements;
            MinimumSurfaceTemperature = minimumSurfaceTemperature;
            MaximumSurfaceTemperature = maximumSurfaceTemperature;
            MinimumSurfacePressure = minimumSurfacePressure;
            MaximumSurfacePressure = maximumSurfacePressure;
            MinimumSurfaceGravity = minimumSurfaceGravity;
            MaximumSurfaceGravity = maximumSurfaceGravity;
        }
    }
}
