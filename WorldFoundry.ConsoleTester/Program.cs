using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.SurfaceMapping;

namespace WorldFoundry.ConsoleTester
{
    public static class Program
    {
        private const int NumSeasons = 12;
        private const int SurfaceMapResolution = 90;

        public static void Main(string[] _)
        {
            Console.WriteLine("Starting tests...");
            GeneratePlanets();
            Console.ReadLine();
        }

        private static void GeneratePlanets()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var planet = await TerrestrialPlanet.GetPlanetForNewUniverseAsync().ConfigureAwait(false);
                    if (planet == null)
                    {
                        Console.Write("Failed to generate planet.");
                    }
                    else
                    {
                        Console.Write($"Generated planet {planet}.");

                        var json = JsonConvert.SerializeObject(planet);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        Console.Write($" Saved planet size: {(bytes.Length / 1000).ToString("N0")} KB.");

                        var maps = await planet.GetSurfaceMapsAsync(SurfaceMapResolution, steps: NumSeasons).ConfigureAwait(false);

                        json = JsonConvert.SerializeObject(maps);
                        bytes = Encoding.UTF8.GetBytes(json);
                        Console.WriteLine($" Saved maps size: {(bytes.Length / 1000000.0).ToString("N1")} MB.");
                    }
                }
            });
        }
    }
}
