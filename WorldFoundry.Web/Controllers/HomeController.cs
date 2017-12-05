using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WorldFoundry.Climate;
using WorldFoundry.Web.ViewModels;
using System;

namespace WorldFoundry.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMemoryCache _cache;

        public HomeController(IMemoryCache cache) => _cache = cache;

        public IActionResult Index() => View();

        private Planet CreatePlanet(
            float atmosphericPressure = Planet.DefaultAtmosphericPressure,
            float axialTilt = Planet.DefaultAxialTilt,
            int radius = Planet.DefaultRadius,
            double rotationalPeriod = Planet.DefaultRotationalPeriod,
            float waterRatio = Planet.DefaultWaterRatio,
            int gridSize = 4) => throw new NotImplementedException();

        private Planet LoadPlanet(Guid id) => throw new NotImplementedException();

        private Planet GetCachedPlanet(string key)
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
            float atmosphericPressure = Planet.DefaultAtmosphericPressure,
            float axialTilt = Planet.DefaultAxialTilt,
            int radius = Planet.DefaultRadius,
            double rotationalPeriod = Planet.DefaultRotationalPeriod,
            float waterRatio = Planet.DefaultWaterRatio,
            int gridSize = 4,
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
