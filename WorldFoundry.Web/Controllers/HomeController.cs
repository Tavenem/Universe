using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;
using WorldFoundry.Web.ViewModels;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMemoryCache _cache;

        public HomeController(IMemoryCache cache) => _cache = cache;

        public IActionResult Index() => View();

        private TerrestrialPlanet CreatePlanet(
            float atmosphericPressure = TerrestrialPlanetParams.DefaultAtmosphericPressure,
            float axialTilt = TerrestrialPlanetParams.DefaultAxialTilt,
            int radius = TerrestrialPlanetParams.DefaultRadius,
            double rotationalPeriod = TerrestrialPlanetParams.DefaultRotationalPeriod,
            float waterRatio = TerrestrialPlanetParams.DefaultWaterRatio,
            int gridSize = WorldGrid.DefaultGridSize) => throw new NotImplementedException();

        private TerrestrialPlanet LoadPlanet(Guid id) => throw new NotImplementedException();

        private TerrestrialPlanet GetCachedPlanet(string key)
        {
            if (string.IsNullOrEmpty(key) || !Guid.TryParse(key, out var guid))
            {
                guid = Guid.NewGuid();
                key = guid.ToString();
            }
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(3);
                return LoadPlanet(guid);
            });
        }

        public PlanetData GetPlanet(
            float atmosphericPressure = TerrestrialPlanetParams.DefaultAtmosphericPressure,
            float axialTilt = TerrestrialPlanetParams.DefaultAxialTilt,
            int radius = TerrestrialPlanetParams.DefaultRadius,
            double rotationalPeriod = TerrestrialPlanetParams.DefaultRotationalPeriod,
            float waterRatio = TerrestrialPlanetParams.DefaultWaterRatio,
            int gridSize = WorldGrid.DefaultGridSize,
            string id = null)
        {
            if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
            {
                guid = Guid.NewGuid();
                id = guid.ToString();
            }
            var planet = _cache.GetOrCreate(id, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(3);
                return CreatePlanet(
                    atmosphericPressure,
                    axialTilt,
                    radius,
                    rotationalPeriod,
                    waterRatio,
                    gridSize);
            });
            return new PlanetData { Planet = planet, Key = id };
        }

        public Season GetSeason(string key, int amount, int index)
            => GetCachedPlanet(key)?.GetSeason(amount, index);
    }
}
