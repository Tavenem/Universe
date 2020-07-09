using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.SurfaceMapping;
using System;
using System.Text;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.ConsoleTester
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
            Task.Run(() =>
            {
                while (true)
                {
                    var planet = Planetoid.GetPlanetForSunlikeStar(out _);
                    if (planet is null)
                    {
                        Console.WriteLine("Failed to generate planet.");
                    }
                    else
                    {
                        Console.Write($"Generated planet {planet}.");

                        var bytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(planet)).Length;
                        Console.Write($" Size: {BytesToString(bytes)}.");

                        var maps = planet.GetSurfaceMaps(SurfaceMapResolution, steps: NumSeasons);

                        bytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(maps)).Length;
                        Console.WriteLine($" Maps size: {BytesToString(bytes)}.");
                    }
                }
            });
        }

        private static string BytesToString(int numBytes)
        {
            if (numBytes < 1000)
            {
                return $"{numBytes} bytes";
            }
            if (numBytes < 1000000)
            {
                return $"{numBytes / 1000.0:N2} KB";
            }
            if (numBytes < 1000000000)
            {
                return $"{numBytes / 1000000.0:N2} MB";
            }
            return $"{numBytes / 1000000000.0:N2} GB";
        }
    }
}
