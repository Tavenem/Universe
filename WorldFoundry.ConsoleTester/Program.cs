using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using NeverFoundry.WorldFoundry.SurfaceMapping;
using System;
using System.Collections.Generic;
using System.Linq;
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
                //var avgs = new List<double>();
                //var maxes = new List<double>();
                //var mins = new List<double>();
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

                        var maps = planet.GetSurfaceMaps(SurfaceMapResolution, steps: NumSeasons);
                        //var map = planet.GetElevationMap(360, out var max, equalArea: true);
                        //var avg = 0.0;
                        //if (planet.Hydrosphere?.IsEmpty == false)
                        //{
                        //    var landCoords = new List<(int x, int y)>();
                        //    for (var x = 0; x < map.Length; x++)
                        //    {
                        //        for (var y = 0; y < map[0].Length; y++)
                        //        {
                        //            if (map[x][y] > 0)
                        //            {
                        //                landCoords.Add((x, y));
                        //            }
                        //        }
                        //    }
                        //    avg = landCoords.Average(x => map[x.x][x.y]) * max;
                        //}
                        //else
                        //{
                        //    var sum = 0.0;
                        //    for (var x = 0; x < map.Length; x++)
                        //    {
                        //        for (var y = 0; y < map[0].Length; y++)
                        //        {
                        //            sum += map[x][y];
                        //        }
                        //    }
                        //    avg = sum / (map.Length * map[0].Length);
                        //}
                        //var min = 0.0;
                        //for (var x = 0; x < map.Length; x++)
                        //{
                        //    for (var y = 0; y < map[0].Length; y++)
                        //    {
                        //        min = Math.Min(min, map[x][y] * max);
                        //    }
                        //}
                        //avgs.Add(avg);
                        //maxes.Add(max);
                        //mins.Add(min);
                        //Console.Write($" Min elevation: {min:N0}m <{mins.Average():N0}m>");
                        //Console.Write($" Avg elevation: {avg:N0}m <{avgs.Average():N0}m>");
                        //Console.WriteLine($" Max elevation: {max:N0}m <{maxes.Average():N0}m> / {planet.MaxElevation:N0}m");

                        //avgs.Add(maps.TemperatureRange.Average);
                        //var avg = avgs.Average();
                        //Console.WriteLine($" Avg temp: {maps.TemperatureRange.Average:N0}K ({Math.Round(maps.TemperatureRange.Average - PlanetParams.EarthSurfaceTemperature, 2):+0.##;-0.##;on-targ\\et}) <{avg:N0}K ({Math.Round(avg - PlanetParams.EarthSurfaceTemperature, 2):+0.##;-0.##;on-targ\\et})>");

                        var bytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(planet)).Length;
                        Console.Write($" Size: {BytesToString(bytes)}.");

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
