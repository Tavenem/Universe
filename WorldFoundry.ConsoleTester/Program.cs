﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldFoundry.Climate;
using WorldFoundry.ConsoleApp.Extensions;

namespace WorldFoundry.ConsoleApp
{
    class Program
    {
        private static Planet _planet = null;
        private static PlanetParams _planetParams = null;

        static void Main(string[] args)
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  Generate new planet: \"planet [-p <atmospheric pressure>] [-a <axial tilt>] [-r <radius>] [-rp <revolution period>] [-ro <rotational period>] [-w <water ratio>] [-g <grid count>] [-es <elevation size>] [-seed <string>]\"");
            Console.WriteLine("  Generate new season: \"s [-d <duration>] [-n <number of seasons>]\"");
            Console.WriteLine("  Set Atmospheric Pressure: \"a <float, kPa>\"");
            Console.WriteLine("  Set Axial Tilt: \"a <float, radians>\"");
            Console.WriteLine("  Set Radius: \"r <int, meters>\"");
            Console.WriteLine("  Set Revolution Period: \"rp <double, seconds>\"");
            Console.WriteLine("  Set Rotational Period: \"ro <double, seconds>\"");
            Console.WriteLine("  Set Water Ratio: \"w <float>\"");
            Console.WriteLine("  Set Grid Count: \"g <int>\"");
            Console.WriteLine("  Set Elevation Size: \"es <int>\"");
            Console.WriteLine();
            while (ReadInput())
            {
                Console.WriteLine(GetPlanetString());
            }
        }

        static void GenerateNewPlanet(PlanetParams planetParams)
        {
            _planetParams = planetParams;
            _planet = Planet.FromParams(
                  planetParams.AtmosphericPressure,
                  planetParams.AxialTilt,
                  planetParams.Radius,
                  planetParams.RevolutionPeriod,
                  planetParams.RotationalPeriod,
                  planetParams.WaterRatio,
                  planetParams.GridSize,
                  planetParams.ElevationSize,
                  planetParams.Seed);
        }

        static bool GetDoubleArg(string cmd, int cmdLength, out double result)
        {
            result = 0;
            if (cmd.Length < cmdLength + 1)
            {
                return false;
            }
            if (double.TryParse(cmd.Substring(cmdLength + 1), out var r))
            {
                result = r;
                return true;
            }
            return false;
        }

        static bool GetFloatArg(string cmd, int cmdLength, out float result)
        {
            result = 0;
            if (cmd.Length < cmdLength + 1)
            {
                return false;
            }
            if (float.TryParse(cmd.Substring(cmdLength + 1), out var r))
            {
                result = r;
                return true;
            }
            return false;
        }

        static bool GetIntArg(string cmd, int cmdLength, out int result)
        {
            result = 0;
            if (cmd.Length < cmdLength + 1)
            {
                return false;
            }
            if (int.TryParse(cmd.Substring(cmdLength + 1), out var r))
            {
                result = r;
                return true;
            }
            return false;
        }

        static void AddClimateString(StringBuilder sb)
        {
            if (_planet.Tiles[0].BiomeType == BiomeType.None)
            {
                return;
            }

            sb.AppendLine("Climates:");
            sb.AppendFormat("  % Desert (land):         {0}%", _planet.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0f / _planet.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  % Rain Forest (land):    {0}%", _planet.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0f / _planet.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  Polar:                   {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Polar));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Ice:                   {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Ice));
            sb.AppendLine();
            sb.AppendFormat("  Subpolar:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar));
            sb.AppendLine();
            sb.AppendFormat("    Dry Tundra:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.DryTundra));
            sb.AppendLine();
            sb.AppendFormat("    Moist Tundra:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.MoistTundra));
            sb.AppendLine();
            sb.AppendFormat("    Wet Tundra:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.WetTundra));
            sb.AppendLine();
            sb.AppendFormat("    Rain Tundra:           {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.RainTundra));
            sb.AppendLine();
            sb.AppendFormat("  Boreal:                  {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Boreal));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Dry Scrub:             {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.DryScrub));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Cool Temperate:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.Steppe));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Warm Temperate:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Scrub:           {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.ThornScrub));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Subtropical:             {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Woodland:        {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.ThornWoodland));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Tropical:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Tropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Very Dry Forest:       {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.VeryDryForest));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
        }

        static void AddElevationString(StringBuilder sb)
        {
            sb.AppendLine("Elevation:");
            sb.AppendFormat("  Min:                     {0} m", Math.Min(_planet.Tiles.Min(t => t.Elevation), _planet.Corners.Min(c => c.Elevation)));
            sb.AppendLine();
            sb.AppendFormat("  Avg:                     {0} m", (_planet.Tiles.Sum(t => t.Elevation) + _planet.Corners.Sum(c => c.Elevation)) / (_planet.Tiles.Count + _planet.Corners.Count));
            sb.AppendLine();
            var sum = 0f;
            var count = 0;
            for (int i = 0; i < _planet.Tiles.Count; i++)
            {
                if (_planet.Tiles[i].Elevation > 0)
                {
                    sum += _planet.Tiles[i].Elevation;
                    count++;
                }
            }
            for (int i = 0; i < _planet.Corners.Count; i++)
            {
                if (_planet.Corners[i].Elevation > 0)
                {
                    sum += _planet.Corners[i].Elevation;
                    count++;
                }
            }
            sb.AppendFormat("  Avg(> 0):                {0} m", sum / count);
            sb.AppendLine();
            sb.AppendFormat("  Max:                     {0} m", Math.Max(_planet.Tiles.Max(t => t.Elevation), _planet.Corners.Max(c => c.Elevation)));
            sb.AppendLine();
        }

        static void AddPrecipitationString(StringBuilder sb, List<Season> seasons, float seasonToYearRatio)
        {
            sb.AppendLine("Precipitation (average, land):");
            var list = new List<float>();
            for (int j = 0; j < seasons.Count; j++)
            {
                for (int i = 0; i < _planet.Tiles.Count; i++)
                {
                    if (_planet.Tiles[i].TerrainType != TerrainType.Water)
                    {
                        list.Add(seasons[j].TileClimates[i].Precipitation);
                    }
                }
            }
            list.Sort();
            sb.AppendFormat("  Avg:                     {0} mm", list.Count == 0 ? 0 : list.Average() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("  Avg (<=P90):             {0} mm", list.Count == 0 ? 0 : list.Take((int)Math.Floor(list.Count * 0.9)).Average() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("  P10:                     {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("  Q1:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("  Q2:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("  Q3:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("  P90:                     {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("  Max:                     {0} mm", list.Count == 0 ? 0 : list.Last() * seasonToYearRatio);
            sb.AppendLine();

            sb.AppendLine("  Selected Tiles:");
            sb.AppendFormat("    [100]:                 {0} mm ({1})",
                seasons.Average(s => s.TileClimates[100].Precipitation) * seasonToYearRatio,
                _planet.Tiles[100].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [200]:                 {0} mm ({1})",
                seasons.Average(s => s.TileClimates[200].Precipitation) * seasonToYearRatio,
                _planet.Tiles[200].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [300]:                 {0} mm ({1})",
                seasons.Average(s => s.TileClimates[300].Precipitation) * seasonToYearRatio,
                _planet.Tiles[300].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [400]:                 {0} mm ({1})",
                seasons.Average(s => s.TileClimates[400].Precipitation) * seasonToYearRatio,
                _planet.Tiles[400].TerrainType);
            sb.AppendLine();
        }

        static void AddRiverString(StringBuilder sb, List<Season> seasons)
        {
            sb.AppendLine("Average River Flow (non-0):");
            var list = new List<float>();
            for (int i = 0; i < _planet.Edges.Count; i++)
            {
                var flow = seasons.Average(s => s.EdgeRiverFlows[i]);
                if (flow > 0)
                {
                    list.Add(flow);
                }
            }
            list.Sort();
            sb.AppendFormat("    Avg:                   {0} m³/s", list.Count == 0 ? 0 : list.Average());
            sb.AppendLine();
            sb.AppendFormat("    P10:                   {0} m³/s", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q1:                    {0} m³/s", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q2:                    {0} m³/s", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q3:                    {0} m³/s", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First());
            sb.AppendLine();
            sb.AppendFormat("    P90:                   {0} m³/s", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First());
            sb.AppendLine();
            sb.AppendFormat("    Max:                   {0} m³/s", list.Count == 0 ? 0 : list.Last());
            sb.AppendLine();
        }

        static void AddSeaIceString(StringBuilder sb, List<Season> seasons)
        {
            sb.AppendLine("Sea Ice Depth (non-0):");
            sb.AppendLine("  Avg:");
            var list = new List<float>();
            for (int j = 0; j < seasons.Count; j++)
            {
                for (int i = 0; i < _planet.Tiles.Count; i++)
                {
                    if (seasons[j].TileClimates[i].SeaIce > 0)
                    {
                        list.Add(seasons[j].TileClimates[i].SeaIce);
                    }
                }
            }
            list.Sort();
            sb.AppendFormat("    Avg:                   {0} mm", list.Count == 0 ? 0 : list.Average());
            sb.AppendLine();
            sb.AppendFormat("    P10:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q1:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q2:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q3:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First());
            sb.AppendLine();
            sb.AppendFormat("    P90:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First());
            sb.AppendLine();
            sb.AppendFormat("    Max:                   {0} mm", list.Count == 0 ? 0 : list.Last());
            sb.AppendLine();

            sb.AppendLine("  Min:");
            list = new List<float>();
            for (int i = 0; i < _planet.Tiles.Count; i++)
            {
                var min = 1000000f;
                for (int j = 0; j < seasons.Count; j++)
                {
                    if (seasons[j].TileClimates[i].SeaIce > 0)
                    {
                        min = Math.Min(min, seasons[j].TileClimates[i].SeaIce);
                    }
                }
                if (min > 0 && min < 1000000f)
                {
                    list.Add(min);
                }
            }
            list.Sort();
            sb.AppendFormat("    Avg:                   {0} mm", list.Count == 0 ? 0 : list.Average());
            sb.AppendLine();
            sb.AppendFormat("    P10:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q1:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q2:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First());
            sb.AppendLine();
            sb.AppendFormat("    Q3:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First());
            sb.AppendLine();
            sb.AppendFormat("    P90:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First());
            sb.AppendLine();
            sb.AppendFormat("    Max:                   {0} mm", list.Count == 0 ? 0 : list.Last());
            sb.AppendLine();
        }

        static void AddSnowString(StringBuilder sb, List<Season> seasons, float seasonToYearRatio)
        {
            sb.AppendLine("Snow Cover (non-0):");
            sb.AppendLine("  Avg:");
            var list = new List<float>();
            for (int j = 0; j < seasons.Count; j++)
            {
                for (int i = 0; i < _planet.Tiles.Count; i++)
                {
                    if (_planet.Tiles[i].TerrainType != TerrainType.Water
                        && seasons[j].TileClimates[i].Snow > 0)
                    {
                        list.Add(seasons[j].TileClimates[i].Snow);
                    }
                }
            }
            list.Sort();
            sb.AppendFormat("    Avg:                   {0} mm", list.Count == 0 ? 0 : list.Average() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    P10:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    Q1:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    Q2:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    Q3:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    P90:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    Max:                   {0} mm", list.Count == 0 ? 0 : list.Last() * seasonToYearRatio);
            sb.AppendLine();

            sb.AppendLine("  Max:");
            list = new List<float>();
            for (int i = 0; i < _planet.Tiles.Count; i++)
            {
                if (_planet.Tiles[i].TerrainType != TerrainType.Water)
                {
                    var max = 0f;
                    for (int j = 0; j < seasons.Count; j++)
                    {
                        if (seasons[j].TileClimates[i].Snow > 0)
                        {
                            max = Math.Max(max, seasons[j].TileClimates[i].Snow);
                        }
                    }
                    if (max > 0)
                    {
                        list.Add(max);
                    }
                }
            }
            list.Sort();
            sb.AppendFormat("    Avg:                   {0} mm", list.Count == 0 ? 0 : list.Average() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    P10:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    Q1:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    Q2:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    Q3:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    P90:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First() * seasonToYearRatio);
            sb.AppendLine();
            sb.AppendFormat("    Max:                   {0} mm", list.Count == 0 ? 0 : list.Last() * seasonToYearRatio);
            sb.AppendLine();
        }

        static void AddTempString(StringBuilder sb, List<Season> seasons)
        {
            sb.AppendLine("Temp:");
            sb.AppendFormat("  Avg:                     {0} K", seasons.Average(s => s.TileClimates.Average(t => t.Temperature)));
            sb.AppendLine();

            var sum = 0f;
            var count = 0;
            var min = 10000f;
            for (int i = 0; i < _planet.Tiles.Count; i++)
            {
                if (_planet.Tiles[i].TerrainType == TerrainType.Water)
                {
                    var avg = seasons.Average(s => s.TileClimates[i].Temperature);
                    min = Math.Min(min, avg);
                    sum += avg;
                    count++;
                }
            }
            sb.AppendFormat("  Min Sea-Level:           {0} K", min);
            sb.AppendLine();
            sb.AppendFormat("  Avg Sea-Level:           {0} K", sum / count);
            sb.AppendLine();

            sb.AppendFormat("  Max:                     {0} K", seasons.Max(s => s.TileClimates.Max(t => t.Temperature)));
            sb.AppendLine();

            min = 10000f;
            var max = 0f;
            var minIndex = -1;
            for (int i = 0; i < _planet.Tiles.Count; i++)
            {
                max = Math.Max(max, seasons.Average(s => s.TileClimates[i].Temperature));
                if (_planet.Tiles[i].TerrainType.HasFlag(TerrainType.Water))
                {
                    var sMax = 0f;
                    for (int j = 0; j < seasons.Count; j++)
                    {
                        sMax = Math.Max(sMax, seasons[j].TileClimates[i].Temperature);
                    }
                    if (sMax < min)
                    {
                        min = sMax;
                        minIndex = i;
                    }
                }
            }
            sb.AppendFormat("  Max Avg:          {0} K", max);
            sb.AppendLine();
            sb.AppendFormat("  Min Max (water):  {0} K [{1}]", min, minIndex);
            sb.AppendLine();

            sb.AppendFormat("Avg Surface Pressure:      {0} kPa", seasons.Average(s => s.TileClimates.Average(t => t.AtmosphericPressure)));
            sb.AppendLine();
        }

        static void AddWindString(StringBuilder sb, List<Season> seasons)
        {
            sb.AppendLine("Wind Speed:");
            sb.AppendFormat("  Min:                     {0} m/s", seasons.Min(s => s.TileClimates.Min(t => t.WindSpeed)));
            sb.AppendLine();
            sb.AppendFormat("  Avg:                     {0} m/s", seasons.Average(s => s.TileClimates.Average(t => t.WindSpeed)));
            sb.AppendLine();
            sb.AppendFormat("  Max:                     {0} m/s", seasons.Max(s => s.TileClimates.Max(t => t.WindSpeed)));
            sb.AppendLine();
        }

        static string GetPlanetString()
        {
            if (_planet == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Planet:");

            //sb.AppendFormat("Angular Velocity:          {0} rad/s", _planet.AngularVelocity);
            //sb.AppendLine();

            //sb.AppendFormat("Axis:                      {0}", _planet.Axis);
            //sb.AppendLine();

            //AddElevationString(sb);

            //AddClimateString(sb);

            return sb.ToString();
        }

        static void ParseSeasonCommand(string cmd)
        {
            double? duration = null;
            var seasonToYearRatio = 1f;
            var seasons = new List<Season>();
            if (string.IsNullOrEmpty(cmd))
            {
                seasonToYearRatio = 4;
                seasons.Add(_planet.GetSeason(duration));
            }
            else
            {
                int? seasonCount = null;
                foreach (var arg in cmd.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(' ')).Where(x => x.Length > 1))
                {
                    switch (arg[0])
                    {
                        case "d":
                            duration = arg[1].ParseNullableDouble();
                            break;
                        case "n":
                            seasonCount = arg[1].ParseNullableInt();
                            break;
                        default:
                            break;
                    }
                }
                if (seasonCount == null)
                {
                    seasonCount = 1;
                }
                seasonToYearRatio = duration.HasValue ? (float)(_planet.RotationalPeriod / duration.Value) : 4;
                for (int i = 0; i < seasonCount; i++)
                {
                    seasons.Add(_planet.GetSeason(duration));
                }
            }

            var sb = new StringBuilder();

            AddTempString(sb, seasons);

            //AddWindString(sb, seasons);

            //AddPrecipitationString(sb, seasons, seasonToYearRatio);

            //AddSnowString(sb, seasons, seasonToYearRatio);

            //AddSeaIceString(sb, seasons);

            //AddRiverString(sb, seasons);

            Console.WriteLine(sb.ToString());
        }

        static void ParsePlanetCommand(string cmd)
        {
            var planetParams = new PlanetParams();
            if (!string.IsNullOrWhiteSpace(cmd))
            {
                foreach (var arg in cmd.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(' ')).Where(x => x.Length > 1))
                {
                    switch (arg[0])
                    {
                        case "p":
                            planetParams.AtmosphericPressure = arg[1].ParseNullableFloat();
                            break;
                        case "a":
                            planetParams.AxialTilt = arg[1].ParseNullableFloat();
                            break;
                        case "r":
                            planetParams.Radius = arg[1].ParseNullableInt();
                            break;
                        case "rp":
                            planetParams.RevolutionPeriod = arg[1].ParseNullableDouble();
                            break;
                        case "ro":
                            planetParams.RotationalPeriod = arg[1].ParseNullableDouble();
                            break;
                        case "w":
                            planetParams.WaterRatio = arg[1].ParseNullableFloat();
                            break;
                        case "g":
                            planetParams.GridSize = arg[1].ParseNullableInt();
                            break;
                        case "es":
                            planetParams.ElevationSize = arg[1].ParseNullableInt();
                            break;
                        case "seed":
                            planetParams.Seed = arg[1];
                            break;
                        default:
                            break;
                    }
                }
            }
            GenerateNewPlanet(planetParams);
        }

        static bool ReadInput()
        {
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line) || line.Trim() == "q")
            {
                return false;
            }
            else if (line.StartsWith("planet"))
            {
                ParsePlanetCommand(line.Length > 7 ? line.Substring(7) : null);
            }
            else if (line.StartsWith("s"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                ParseSeasonCommand(line.Length > 2 ? line.Substring(2) : null);
            }
            else if (line.StartsWith("p"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                if (GetFloatArg(line, 1, out var result))
                {
                    _planet.ChangeAtmosphericPressure(result);
                }
            }
            else if (line.StartsWith("a"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                if (GetFloatArg(line, 1, out var result))
                {
                    _planet.ChangeAxialTilt(result);
                }
            }
            else if (line.StartsWith("r"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                if (GetIntArg(line, 1, out var result))
                {
                    _planet.ChangeRadius(result);
                }
            }
            else if (line.StartsWith("rp"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                if (GetDoubleArg(line, 2, out var result))
                {
                    _planet.ChangeRevolutionPeriod(result);
                }
            }
            else if (line.StartsWith("ro"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                if (GetDoubleArg(line, 2, out var result))
                {
                    _planet.ChangeRotationalPeriod(result);
                }
            }
            else if (line.StartsWith("w"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                if (GetFloatArg(line, 1, out var result))
                {
                    _planet.SetWaterRatio(result);
                }
            }
            else if (line.StartsWith("g"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                if (GetIntArg(line, 1, out var result))
                {
                    _planet.ChangeGridSize(result);
                }
            }
            else if (line.StartsWith("es"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet(new PlanetParams());
                }
                if (GetIntArg(line, 2, out var result))
                {
                    _planet.ChangeElevationSize(result);
                }
            }
            return true;
        }
    }
}
