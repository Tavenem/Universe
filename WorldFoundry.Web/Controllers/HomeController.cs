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

        private Planet GetCachedPlanet(string key)
            => _cache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                var (atmosphericPressure,
                    axialTilt,
                    radius,
                    revolutionPeriod,
                    rotationalPeriod,
                    waterRatio,
                    gridSize,
                    id) = ParsePlanetKey(key);
                return Planet.FromParams(
                    atmosphericPressure,
                    axialTilt,
                    radius,
                    revolutionPeriod,
                    rotationalPeriod,
                    waterRatio,
                    gridSize,
                    id: id);
            });

        private string GetPlanetKey(
            float atmosphericPressure,
            float axialTilt,
            int radius,
            double revolutionPeriod,
            double rotationalPeriod,
            float waterRatio,
            int gridSize,
            Guid id)
            => $"{atmosphericPressure.ToString("G9")};{axialTilt.ToString("G9")};{radius.ToString("X")};{revolutionPeriod.ToString("G17")};{rotationalPeriod.ToString("G17")};{waterRatio.ToString("G9")};{gridSize.ToString("X")};{id.ToString()}";

        public PlanetData GetPlanet(
            float atmosphericPressure = Planet.defaultAtmosphericPressure,
            float axialTilt = Planet.defaultAxialTilt,
            int radius = Planet.defaultRadius,
            double revolutionPeriod = Planet.defaultRevolutionPeriod,
            double rotationalPeriod = Planet.defaultRotationalPeriod,
            float waterRatio = Planet.defaultWaterRatio,
            int gridSize = 4,
            int seasonCount = 4,
            string id = null)
        {
            if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
            {
                guid = Guid.NewGuid();
            }
            var key = GetPlanetKey(
                atmosphericPressure,
                axialTilt,
                radius,
                revolutionPeriod,
                rotationalPeriod,
                waterRatio,
                gridSize,
                guid);
            var planet = _cache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                var p = Planet.FromParams(
                    atmosphericPressure,
                    axialTilt,
                    radius,
                    revolutionPeriod,
                    rotationalPeriod,
                    waterRatio,
                    gridSize,
                    id: guid);
                p.SetClimate();
                return p;
            });
            return new PlanetData { Planet = planet, Key = key };
        }

        public Season GetSeason(string key, double? duration = null)
            => GetCachedPlanet(key).GetSeason(duration);

        private (
            float atmosphericPressure,
            float axialTilt,
            int radius,
            double revolutionPeriod,
            double rotationalPeriod,
            float waterRatio,
            int gridSize,
            Guid? id) ParsePlanetKey(string key)
        {
            var tokens = string.IsNullOrEmpty(key) ? new string[]{ } : key.Split(';');
            if (tokens.Length == 0 || !float.TryParse(tokens[0], out var atmosphericPressure))
            {
                atmosphericPressure = Planet.defaultAtmosphericPressure;
            }
            if (tokens.Length < 2 || !float.TryParse(tokens[1], out var axialTilt))
            {
                axialTilt = Planet.defaultAxialTilt;
            }
            if (tokens.Length < 3 || !int.TryParse(tokens[2], out var radius))
            {
                radius = Planet.defaultRadius;
            }
            if (tokens.Length < 4 || !double.TryParse(tokens[3], out var revolutionPeriod))
            {
                revolutionPeriod = Planet.defaultRevolutionPeriod;
            }
            if (tokens.Length < 5 || !double.TryParse(tokens[4], out var rotationalPeriod))
            {
                rotationalPeriod = Planet.defaultRotationalPeriod;
            }
            if (tokens.Length < 6 || !float.TryParse(tokens[5], out var waterRatio))
            {
                waterRatio = Planet.defaultWaterRatio;
            }
            if (tokens.Length < 7 || !int.TryParse(tokens[6], out var gridSize))
            {
                gridSize = Planet.defaultGridSize;
            }
            Guid? id = null;
            if (tokens.Length >= 8 && Guid.TryParse(tokens[7], out var parsedId))
            {
                id = parsedId;
            }
            return (
                atmosphericPressure,
                axialTilt,
                radius,
                revolutionPeriod,
                rotationalPeriod,
                waterRatio,
                gridSize,
                id);
        }
    }
}
