using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Climate;
using WorldFoundry.ConsoleApp.Extensions;
using WorldFoundry.Space;
using WorldFoundry.Space.Galaxies;

namespace WorldFoundry.ConsoleApp
{
    class Program
    {
        private static SpiralGalaxy _galaxy = null;
        private static TerrestrialPlanet _planet = null;

        static void Main(string[] args)
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  Generate new planet: \"planet [-p <atmospheric pressure>] [-a <axial tilt>] [-g <grid size>] [-r <radius>] [-re <revolution period>] [-ro <rotational period>] [-w <water ratio>]\"");
            Console.WriteLine("  Generate seasons: \"s [-a <amount>]\"");
            Console.WriteLine("  Set Atmospheric Pressure: \"a <float, kPa>\"");
            Console.WriteLine("  Set Axial Tilt: \"a <float, radians>\"");
            Console.WriteLine("  Set Grid Size: \"g <int>\"");
            Console.WriteLine("  Set Radius: \"r <int, meters>\"");
            Console.WriteLine("  Set Revolution Period: \"re <double, seconds>\"");
            Console.WriteLine("  Set Rotational Period: \"ro <double, seconds>\"");
            Console.WriteLine("  Set Water Ratio: \"w <float>\"");
            Console.WriteLine();

            //GenerateUniverse();

            while (ReadInput())
            {
                Console.WriteLine(GetPlanetString());
            }
        }

        static T NavigateToChild<T>(CelestialObject current, Func<T, bool> condition = null) where T : CelestialObject
        {
            var position = Vector3.Zero;
            while (current.GetContainingParent(position) == current)
            {
                current.PopulateRegion(position);
                foreach (var child in current.GetNearbyChildren(position))
                {
                    if (child is T t && condition?.Invoke(t) == true)
                    {
                        return t;
                    }
                }
                var m = position.Length();
                var delta = (m + current.GridSize) / m;
                position *= delta;
                Vector3.Transform(position, Quaternion.CreateFromYawPitchRoll((float)Utilities.MathUtil.Constants.QuarterPI, (float)Utilities.MathUtil.Constants.QuarterPI, 0));
            }
            return null;
        }

        static void GenerateUniverse()
        {
            var universe = new Universe();
            var supercluster = NavigateToChild<GalaxySupercluster>(universe);
            if (supercluster == null)
            {
                return;
            }
            var cluster = NavigateToChild<GalaxyCluster>(supercluster);
            if (cluster == null)
            {
                return;
            }
            var group = NavigateToChild<GalaxyGroup>(cluster, x =>
            {
                x.PopulateRegion(Vector3.Zero);
                return x.Children.FirstOrDefault(y => y is GalaxySubgroup s && s.MainGalaxy is SpiralGalaxy) != null;
            });
            if (group == null)
            {
                return;
            }
            var subgroup = group.Children.FirstOrDefault(x => x is GalaxySubgroup s && s.MainGalaxy is SpiralGalaxy) as GalaxySubgroup;
            if (subgroup == null)
            {
                return;
            }
            _galaxy = subgroup.MainGalaxy as SpiralGalaxy;
        }

        static void GenerateNewPlanet(TerrestrialPlanetParams planetParams = null)
        {
            StarSystem system = new StarSystem(null, Vector3.Zero, typeof(Star), SpectralClass.G, LuminosityClass.V);
            //while (system == null)
            //{
            //    system = _galaxy.GenerateChildOfType(
            //    typeof(StarSystem),
            //    null,
            //    new object[] { typeof(Star), SpectralClass.G, LuminosityClass.V }) as StarSystem;
            //    if (system.Stars.Count != 1)
            //    {
            //        system = null;
            //    }
            //}
            _planet = new TerrestrialPlanet(system, Vector3.Zero, planetParams);
            _planet.GenerateOrbit(system.Stars.First());
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
            if (_planet.Topography.GetTile(0).BiomeType == BiomeType.None)
            {
                return;
            }

            sb.AppendLine("Climates:");
            sb.AppendFormat("  % Desert (land):         {0}%", _planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0f / _planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  % Rain Forest (land):    {0}%", _planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0f / _planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  Polar:                   {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Polar));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Ice:                   {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Ice));
            sb.AppendLine();
            sb.AppendFormat("  Subpolar:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar));
            sb.AppendLine();
            sb.AppendFormat("    Dry Tundra:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.DryTundra));
            sb.AppendLine();
            sb.AppendFormat("    Moist Tundra:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.MoistTundra));
            sb.AppendLine();
            sb.AppendFormat("    Wet Tundra:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.WetTundra));
            sb.AppendLine();
            sb.AppendFormat("    Rain Tundra:           {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.RainTundra));
            sb.AppendLine();
            sb.AppendFormat("  Boreal:                  {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Dry Scrub:             {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.DryScrub));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Cool Temperate:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.Steppe));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Warm Temperate:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Scrub:           {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.ThornScrub));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Subtropical:             {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Woodland:        {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.ThornWoodland));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Tropical:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Very Dry Forest:       {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.VeryDryForest));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", _planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
        }

        static void AddElevationString(StringBuilder sb)
        {
            var elevations = _planet.Topography.Tiles.Select(x => x.Elevation)
                .Concat(_planet.Topography.Corners.Select(x => x.Elevation));
            sb.AppendLine("Elevation:");
            sb.AppendFormat("  Min:                     {0} m", elevations.Min());
            sb.AppendLine();
            sb.AppendFormat("  Avg:                     {0} m", elevations.Average());
            sb.AppendLine();
            sb.AppendFormat("  Avg(> 0):                {0} m", elevations.Where(x => x > 0).Average());
            sb.AppendLine();
            sb.AppendFormat("  Max:                     {0} m", elevations.Max());
            sb.AppendLine();
        }

        static void AddPrecipitationString(StringBuilder sb, List<Season> seasons, float seasonsInYear)
        {
            sb.AppendLine("Precipitation (annual average, land):");
            var list = _planet.Topography.Tiles.Where(x => x.TerrainType != TerrainType.Water)
                .Select(x => seasons.Select(y => y.GetTileClimate(x.Index).Precipitation).Sum())
                .ToList();
            list.Sort();
            sb.AppendFormat("  Avg:                     {0} mm", list.Count == 0 ? 0 : list.Average());
            sb.AppendLine();
            sb.AppendFormat("  Avg (<=P90):             {0} mm", list.Count == 0 ? 0 : list.Take((int)Math.Floor(list.Count * 0.9)).Average());
            sb.AppendLine();
            sb.AppendFormat("  P10:                     {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First());
            sb.AppendLine();
            sb.AppendFormat("  Q1:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First());
            sb.AppendLine();
            sb.AppendFormat("  Q2:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First());
            sb.AppendLine();
            sb.AppendFormat("  Q3:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First());
            sb.AppendLine();
            sb.AppendFormat("  P90:                     {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First());
            sb.AppendLine();
            sb.AppendFormat("  Max:                     {0} mm", list.Count == 0 ? 0 : list.Last());
            sb.AppendLine();

            sb.AppendLine("  Selected Tiles:");
            sb.AppendFormat("    [100]:                 {0} mm ({1})",
                seasons.Sum(s => s.GetTileClimate(100).Precipitation),
                _planet.Topography.GetTile(100).TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [200]:                 {0} mm ({1})",
                seasons.Sum(s => s.GetTileClimate(200).Precipitation),
                _planet.Topography.GetTile(200).TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [300]:                 {0} mm ({1})",
                seasons.Sum(s => s.GetTileClimate(300).Precipitation),
                _planet.Topography.GetTile(300).TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [400]:                 {0} mm ({1})",
                seasons.Sum(s => s.GetTileClimate(400).Precipitation),
                _planet.Topography.GetTile(400).TerrainType);
            sb.AppendLine();
        }

        static void AddRiverString(StringBuilder sb, List<Season> seasons)
        {
            sb.AppendLine("Average River Flow (non-0):");
            var list = _planet.Topography.Edges.Select(x => seasons.Average(y => y.GetEdgeClimate(x.Index).RiverFlow)).ToList();
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
            var list = _planet.Topography.Tiles.Select(x => seasons
                .Select(y => y.GetTileClimate(x.Index).SeaIce).Average())
                .Where(x => x > 0)
                .ToList();
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
            list = _planet.Topography.Tiles.Select(x => seasons
                .Select(y => y.GetTileClimate(x.Index).SeaIce).Min())
                .Where(x => x > 0)
                .ToList();
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

            sb.AppendLine("  Max:");
            list = _planet.Topography.Tiles.Select(x => seasons
                .Select(y => y.GetTileClimate(x.Index).SeaIce).Max())
                .Where(x => x > 0)
                .ToList();
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

        static void AddSnowString(StringBuilder sb, List<Season> seasons, float seasonsInYear)
        {
            sb.AppendLine("Snow Cover (non-0):");
            sb.AppendLine("  Avg:");
            var list = _planet.Topography.Tiles
                .Where(x => x.TerrainType != TerrainType.Water)
                .Select(x => seasons.Select(y => y.GetTileClimate(x.Index).SnowCover).Average())
                .Where(x => x > 0)
                .ToList();
            list.Sort();
            sb.AppendFormat("    Avg:                   {0} mm", list.Count == 0 ? 0 : list.Average() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    P10:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q1:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q2:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q3:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    P90:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Max:                   {0} mm", list.Count == 0 ? 0 : list.Last() * seasonsInYear);
            sb.AppendLine();

            sb.AppendLine("  Min:");
            list = _planet.Topography.Tiles
                .Where(x => x.TerrainType != TerrainType.Water)
                .Select(x => seasons.Select(y => y.GetTileClimate(x.Index).SnowCover).Min())
                .Where(x => x > 0)
                .ToList();
            list.Sort();
            sb.AppendFormat("    Avg:                   {0} mm", list.Count == 0 ? 0 : list.Average() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    P10:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q1:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q2:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q3:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    P90:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Max:                   {0} mm", list.Count == 0 ? 0 : list.Last() * seasonsInYear);
            sb.AppendLine();

            sb.AppendLine("  Max:");
            list = _planet.Topography.Tiles
                .Where(x => x.TerrainType != TerrainType.Water)
                .Select(x => seasons.Select(y => y.GetTileClimate(x.Index).SnowCover).Max())
                .Where(x => x > 0)
                .ToList();
            list.Sort();
            sb.AppendFormat("    Avg:                   {0} mm", list.Count == 0 ? 0 : list.Average() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    P10:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q1:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q2:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Q3:                    {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    P90:                   {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First() * seasonsInYear);
            sb.AppendLine();
            sb.AppendFormat("    Max:                   {0} mm", list.Count == 0 ? 0 : list.Last() * seasonsInYear);
            sb.AppendLine();
        }

        static void AddTempString(StringBuilder sb, List<Season> seasons)
        {
            sb.AppendLine("Temp:");
            sb.AppendFormat("  Avg:                     {0} K", seasons.Average(s => s.TileClimates.Average(t => t.Temperature)));
            sb.AppendLine();


            var min = _planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => seasons.Select(y => y.GetTileClimate(x.Index).Temperature).Min()).Min();
            sb.AppendFormat("  Min Sea-Level:           {0} K", min);
            sb.AppendLine();

            var avg = _planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => seasons.Select(y => y.GetTileClimate(x.Index).Temperature).Average()).Average();
            sb.AppendFormat("  Avg Sea-Level:           {0} K", avg);
            sb.AppendLine();

            sb.AppendFormat("  Max:                     {0} K", seasons.Max(s => s.TileClimates.Max(t => t.Temperature)));
            sb.AppendLine();

            var maxAvg = _planet.Topography.Tiles
                .Select(x => seasons.Select(y => y.GetTileClimate(x.Index).Temperature).Average())
                .Max();
            sb.AppendFormat("  Max Avg:          {0} K", maxAvg);
            sb.AppendLine();

            var (minMaxTemp, minMaxIndex) = _planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => (temp: seasons.Select(y => y.GetTileClimate(x.Index).Temperature).Max(), x.Index))
                .OrderBy(x => x.temp)
                .First();
            sb.AppendFormat("  Min Max (water):  {0} K [{1}]", minMaxTemp, minMaxIndex);
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
            int? seasonsInYear = 4;
            var seasons = new List<Season>();
            if (!string.IsNullOrEmpty(cmd))
            {
                foreach (var arg in cmd.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(' ')).Where(x => x.Length > 1))
                {
                    switch (arg[0])
                    {
                        case "a":
                            seasonsInYear = arg[1].ParseNullableInt();
                            break;
                        default:
                            break;
                    }
                }
                if (seasonsInYear == null)
                {
                    seasonsInYear = 4;
                }
            }
            seasons = _planet.GetSeasons(seasonsInYear.Value).ToList();

            var sb = new StringBuilder();

            AddTempString(sb, seasons);

            //AddWindString(sb, seasons);

            //AddPrecipitationString(sb, seasons, seasonsInYear.Value);

            //AddSnowString(sb, seasons, seasonsInYear.Value);

            //AddSeaIceString(sb, seasons);

            //AddRiverString(sb, seasons);

            Console.WriteLine(sb.ToString());
        }

        static void ParsePlanetCommand(string cmd)
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();
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
                        case "re":
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
                    GenerateNewPlanet();
                }
                ParseSeasonCommand(line.Length > 2 ? line.Substring(2) : null);
            }
            else if (line.StartsWith("p"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet();
                }
                if (GetFloatArg(line, 1, out var result))
                {
                    _planet.SetAtmosphericPressure(result);
                }
            }
            else if (line.StartsWith("a"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet();
                }
                if (GetFloatArg(line, 1, out var result))
                {
                    _planet.SetAxialTilt(result);
                }
            }
            else if (line.StartsWith("g"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet();
                }
                if (GetIntArg(line, 1, out var result))
                {
                    if (result < 0)
                    {
                        result = 0;
                    }
                    if (result > WorldGrids.WorldGrid.MaxGridSize)
                    {
                        result = WorldGrids.WorldGrid.MaxGridSize;
                    }
                    _planet.SetGridSize((short)result);
                }
            }
            else if (line.StartsWith("r"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet();
                }
                if (GetIntArg(line, 1, out var result))
                {
                    _planet.SetRadius(result);
                }
            }
            else if (line.StartsWith("re"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet();
                }
                if (GetDoubleArg(line, 2, out var result))
                {
                    _planet.SetRevolutionPeriod(result);
                }
            }
            else if (line.StartsWith("ro"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet();
                }
                if (GetDoubleArg(line, 2, out var result))
                {
                    _planet.SetRotationalPeriod(result);
                }
            }
            else if (line.StartsWith("w"))
            {
                if (_planet == null)
                {
                    GenerateNewPlanet();
                }
                if (GetFloatArg(line, 1, out var result))
                {
                    _planet.SetWaterRatio(result);
                }
            }
            return true;
        }
    }
}
