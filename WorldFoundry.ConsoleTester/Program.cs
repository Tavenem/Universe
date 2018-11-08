using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using WorldFoundry.Bson;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.SurfaceMaps;

namespace WorldFoundry.ConsoleTester
{
    public static class Program
    {
        private const int NumSeasons = 12;
        private const int SurfaceMapResolution = 90;

        public static void Main(string[] _)
        {
            Console.WriteLine("Starting tests...");
            BsonRegistration.Register();
            GeneratePlanets();
            Console.ReadLine();
        }

        private static void GeneratePlanets()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var planet = TerrestrialPlanet.GetPlanetForNewUniverse();
                    Console.Write($"Generated planet {planet}.");

                    var bson = planet.ToBson();
                    Console.Write($" Saved planet size: {(bson.Length / 1000).ToString("N0")} KB.");

                    var maps = planet.GetSurfaceMapSet(SurfaceMapResolution, steps: NumSeasons);

                    bson = maps.ToBson();
                    Console.WriteLine($" Saved maps size: {(bson.Length / 1000000.0).ToString("N1")} MB.");
                }
            });
        }
    }
}
