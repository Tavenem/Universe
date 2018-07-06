using Microsoft.JSInterop;
using System.Threading.Tasks;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;

namespace WorldFoundry.Blazor.Client
{
    public static class PlanetDisplay
    {
        public static Task<bool> AutoRotate(string planetId, bool value) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.autoRotate", planetId, value);

        public static Task<bool> AutoSeason(string planetId, bool value) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.autoSeason", planetId, value);

        public static Task<bool> ChangeColorMode(string planetId, PlanetColorMode value) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.changeColorMode", planetId, value);

        public static Task<bool> ChangeRenderMode(string planetId, RenderMode value) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.changeRenderMode", planetId, value);

        public static Task<bool> ChangeSeason(string planetId, int value) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.changeSeason", planetId, value);

        public static Task<bool> Create(string planetId, string canvasId) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.create", planetId, canvasId);

        public static Task<bool> Scale(string planetId, float value) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.scale", planetId, value);

        public static Task<bool> SetPlanet(string planetId, TerrestrialPlanet planet) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.setPlanet", planetId, planet);

        public static Task<bool> Smooth(string planetId, bool value) => JSRuntime.Current.InvokeAsync<bool>("planetBlazorFunctions.smoth", planetId, value);
    }
}
