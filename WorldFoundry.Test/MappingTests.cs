using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeverFoundry.MathAndScience;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using NeverFoundry.WorldFoundry.SurfaceMapping;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Test
{
    [TestClass]
    public class MappingTests
    {
        private const int ElevationMapResolution = 960;
        private const int ElevationMapResolution_Pregen = 2700;
        private const int PrecipitationMapResolution = 960;
        private const int TemperatureMapResolution = 960;
        private const int ProjectionResolution = 640;
        private const int Seasons = 12;

        private static readonly HillShadingOptions _HillShading = new (true, false, 8);

        private static string _OutputPath = string.Empty;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            _OutputPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "TestResultImages");
            if (!System.IO.Directory.Exists(_OutputPath))
            {
                System.IO.Directory.CreateDirectory(_OutputPath);
            }
        }

        [TestMethod]
        public async Task RegionMappingPregenTestAsync()
        {
            using var planet = await GetPlanetPregenAsync().ConfigureAwait(false);

            using var elevationMap = await planet
                .GetElevationMapProjectionAsync(ElevationMapResolution_Pregen, MapProjectionOptions.Default)
                .ConfigureAwait(false);

            var (winterTemperatureMap, summerTemperatureMap) = planet.GenerateTemperatureMaps(elevationMap, TemperatureMapResolution);
            await planet.AssignTemperatureMapWinterAsync(winterTemperatureMap)
                .ConfigureAwait(false);
            await planet.AssignTemperatureMapSummerAsync(summerTemperatureMap)
                .ConfigureAwait(false);

            var region = new SurfaceRegion(planet, -0.9162975, -0.174533, 0.610865);
            const bool regionEqualArea = true;
            var regionProjection = region.GetProjection(planet, regionEqualArea);

            using var regionElevationMap = await region
                .GetElevationMapAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false);
            //using (var img = regionElevationMap
            //    .ElevationMapToImage(planet))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_ElevationImage_Regional.png"));
            //}
            using (var img = regionElevationMap
                .ElevationMapToImage(planet, _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_HillShadeImage_Pregen_Regional.png"));
            }

            using var regionTemperatureMap = await region
                .GetTemperatureMapAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false);
            //using (var img = regionTemperatureMap.TemperatureMapToImage(
            //    planet,
            //    regionElevationMap,
            //    false))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_LandTemperatureImage_Regional.png"));
            //}

            using var regionPrecipitationMap = await region
                .GetPrecipitationMapAsync(planet, ProjectionResolution, Seasons, regionEqualArea)
                .ConfigureAwait(false);
            //using (var img = regionPrecipitationMap.PrecipitationMapToImage(
            //    planet,
            //    regionElevationMap,
            //    false))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_LandPrecipitationImage_Regional.png"));
            //}

            WeatherMaps regionClimateMaps;
            using (var regionWinterTemperatureMap = await region
                .GetTemperatureMapWinterAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false))
            {
                using var regionSummerTemperatureMap = await region
                    .GetTemperatureMapSummerAsync(planet, ProjectionResolution, regionEqualArea)
                    .ConfigureAwait(false);
                regionClimateMaps = new WeatherMaps(
                    planet,
                    regionElevationMap,
                    regionWinterTemperatureMap,
                    regionSummerTemperatureMap,
                    regionPrecipitationMap,
                    ProjectionResolution,
                    regionProjection);
            }

            using (var img = regionClimateMaps.BiomeMap.BiomeMapToImage(
                planet,
                regionElevationMap,
                true,
                regionProjection,
                regionProjection,
                _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_BiomeOceanHillShadeImage_Regional.png"));
            }

            using (var img = regionClimateMaps.GetSatelliteImage(
                planet,
                regionElevationMap,
                regionTemperatureMap,
                regionProjection,
                regionProjection))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_SatelliteImage_Regional.png"));
            }

            winterTemperatureMap.Dispose();
            summerTemperatureMap.Dispose();
        }

        [TestMethod]
        public async Task RegionMappingTestAsync()
        {
            var planet = GetPlanet();

            using var elevationMap = planet.GenerateElevationMap(ElevationMapResolution);
            await planet.AssignElevationMapAsync(elevationMap)
                .ConfigureAwait(false);

            var (winterTemperatureMap, summerTemperatureMap) = planet.GenerateTemperatureMaps(elevationMap, TemperatureMapResolution);
            await planet.AssignTemperatureMapWinterAsync(winterTemperatureMap)
                .ConfigureAwait(false);
            await planet.AssignTemperatureMapSummerAsync(summerTemperatureMap)
                .ConfigureAwait(false);

            var region = new SurfaceRegion(planet, -0.9162975, -0.174533, 0.610865);
            const bool regionEqualArea = true;
            var regionProjection = region.GetProjection(planet, regionEqualArea);

            using var regionElevationMap = await region
                .GetElevationMapAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false);
            //using (var img = regionElevationMap
            //    .ElevationMapToImage(planet))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "ElevationImage_Regional.png"));
            //}
            using (var img = regionElevationMap
                .ElevationMapToImage(planet, _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "HillShadeImage_Regional.png"));
            }

            using var regionTemperatureMap = await region
                .GetTemperatureMapAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false);
            //using (var img = regionTemperatureMap.TemperatureMapToImage(
            //    planet,
            //    regionElevationMap,
            //    false))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "LandTemperatureImage_Regional.png"));
            //}

            using var regionPrecipitationMap = await region
                .GetPrecipitationMapAsync(planet, ProjectionResolution, Seasons, regionEqualArea)
                .ConfigureAwait(false);
            //using (var img = regionPrecipitationMap.PrecipitationMapToImage(
            //    planet,
            //    regionElevationMap,
            //    false))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "LandPrecipitationImage_Regional.png"));
            //}

            WeatherMaps regionClimateMaps;
            using (var regionWinterTemperatureMap = await region
                .GetTemperatureMapWinterAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false))
            {
                using var regionSummerTemperatureMap = await region
                    .GetTemperatureMapSummerAsync(planet, ProjectionResolution, regionEqualArea)
                    .ConfigureAwait(false);
                regionClimateMaps = new WeatherMaps(
                    planet,
                    regionElevationMap,
                    regionWinterTemperatureMap,
                    regionSummerTemperatureMap,
                    regionPrecipitationMap,
                    ProjectionResolution,
                    regionProjection);
            }

            using (var img = regionClimateMaps.BiomeMap.BiomeMapToImage(
                planet,
                regionElevationMap,
                true,
                regionProjection,
                regionProjection,
                _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "BiomeOceanHillShadeImage_Regional.png"));
            }

            using (var img = regionClimateMaps.GetSatelliteImage(
                planet,
                regionElevationMap,
                regionTemperatureMap,
                regionProjection,
                regionProjection))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "SatelliteImage_Regional.png"));
            }

            winterTemperatureMap.Dispose();
            summerTemperatureMap.Dispose();
        }

        [TestMethod]
        public async Task SurfaceMappingPregenTestAsync()
        {
            var projection = MapProjectionOptions.Default;

            using var planet = await GetPlanetPregenAsync().ConfigureAwait(false);

            using var elevationMap = await planet
                .GetElevationMapProjectionAsync(ElevationMapResolution_Pregen, projection)
                .ConfigureAwait(false);
            using var elevationProjection = await planet
                .GetElevationMapProjectionAsync(ProjectionResolution, projection)
                .ConfigureAwait(false);
            //using (var img = elevationProjection.ElevationMapToImage(planet))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_ElevationImage.png"));
            //}
            using (var img = elevationProjection.ElevationMapToImage(planet, _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_HillShadeImage.png"));
            }

            var (winterTemperatureMap, summerTemperatureMap) = planet.GenerateTemperatureMaps(elevationMap, TemperatureMapResolution);
            await planet.AssignTemperatureMapWinterAsync(winterTemperatureMap)
                .ConfigureAwait(false);
            await planet.AssignTemperatureMapSummerAsync(summerTemperatureMap)
                .ConfigureAwait(false);
            using var temperatureMap = await planet
                .GetTemperatureMapProjectionAsync(TemperatureMapResolution, projection)
                .ConfigureAwait(false);
            //using (var tMap = await planet
            //    .GetTemperatureMapProjectionAsync(ProjectionResolution, projection)
            //    .ConfigureAwait(false))
            //{
            //    using (var img = tMap.TemperatureMapToImage())
            //    {
            //        img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_TemperatureImage.png"));
            //    }
            //    using var tImg = tMap.TemperatureMapToImage(planet, elevationMap, false, projection, projection);
            //    tImg.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_LandTemperatureImage.png"));
            //}

            using var precipitationMap = await planet
                .GetPrecipitationMapProjectionAsync(PrecipitationMapResolution, Seasons, projection)
                .ConfigureAwait(false);
            //using (var img = precipitationMap.PrecipitationMapToImage())
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_PrecipitationImage.png"));
            //}
            //using (var img = precipitationMap.PrecipitationMapToImage(planet, elevationMap, false))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_LandPrecipitationImage.png"));
            //}

            var climateMaps = new WeatherMaps(
                planet,
                elevationMap,
                winterTemperatureMap,
                summerTemperatureMap,
                precipitationMap,
                ProjectionResolution,
                projection);

            //var seasonsPath = System.IO.Path.Combine(_OutputPath, "Seasons");
            //if (!System.IO.Directory.Exists(seasonsPath))
            //{
            //    System.IO.Directory.CreateDirectory(seasonsPath);
            //}
            //var precipitationMaps = await planet
            //    .GetPrecipitationMapsProjectionAsync(PrecipitationMapResolution, Seasons, projection)
            //    .ConfigureAwait(false);

            //for (var i = 0; i < Seasons; i++)
            //{
            //    var proportionOfYear = i / (float)Seasons;
            //    using (var map = await planet
            //        .GetTemperatureMapProjectionAsync(ProjectionResolution, proportionOfYear, projection)
            //        .ConfigureAwait(false))
            //    {
            //        using var img = map.TemperatureMapToImage();
            //        img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, @$"Seasons\Pregen_TemperatureImage_{i:00}.png"));
            //    }

            //    precipitationMaps[i]
            //        .SaveAsPng(System.IO.Path.Combine(_OutputPath!, @$"Seasons\Pregen_PrecipitationImage_{i:00}.png"));

            //    precipitationMaps[i].Dispose();
            //}

            //using (var img = climateMaps.SeaIceRangeMap.SurfaceMapToImage(
            //    planet,
            //    elevationMap,
            //    (_, _) => new Rgba32(0, 0, 0),
            //    (v, _) => new Rgba32(
            //        (byte)(v.IsZero ? 2 : Math.Round(198 * v.Range) + 2),
            //        (byte)(v.IsZero ? 5 : Math.Round(235 * v.Range) + 5),
            //        (byte)(v.IsZero ? 20 : Math.Round(230 * v.Range) + 20)),
            //    projection,
            //    projection))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_SeaIceImage.png"));
            //}

            //using (var img = climateMaps.ClimateMap.SurfaceMapToImage(
            //    planet,
            //    elevationMap,
            //    (v, _) => v switch
            //    {
            //        ClimateType.Polar => new Rgba32(255, 255, 255),
            //        ClimateType.Subpolar => new Rgba32(100, 255, 200),
            //        ClimateType.Boreal => new Rgba32(0, 50, 0),
            //        ClimateType.CoolTemperate => new Rgba32(0, 200, 100),
            //        ClimateType.WarmTemperate => new Rgba32(0, 200, 0),
            //        ClimateType.Subtropical => new Rgba32(200, 200, 0),
            //        ClimateType.Tropical => new Rgba32(200, 0, 0),
            //        ClimateType.Supertropical => new Rgba32(50, 0, 0),
            //        _ => new Rgba32(127, 127, 127),
            //    },
            //    (_, _) => new Rgba32(2, 5, 20),
            //    projection,
            //    projection,
            //    _HillShading))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_ClimateImage.png"));
            //}

            using (var img = climateMaps.BiomeMap.BiomeMapToImage(
                planet,
                elevationMap,
                true,
                projection,
                projection,
                _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_BiomeOceanHillShadeImage.png"));
            }

            //using (var snowfallMap = await planet
            //    .GetSnowfallMapProjectionAsync(PrecipitationMapResolution, Seasons)
            //    .ConfigureAwait(false))
            //{
            //    using var snowfallImg = snowfallMap.SurfaceMapToImage(
            //        planet,
            //        elevationMap,
            //        (v, _) =>
            //        {
            //            var value = ((double)v.PackedValue / ushort.MaxValue).Clamp(0, 1);
            //            return new Rgba32((byte)Math.Round(245 * value), (byte)Math.Round(250 * value), (byte)Math.Round(255 * value));
            //        },
            //        (_, _) => new Rgba32(2, 5, 20),
            //        projection,
            //        projection);
            //    snowfallImg.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_SnowfallImage.png"));
            //}

            using (var satelliteImg = climateMaps.GetSatelliteImage(
                planet,
                elevationMap,
                temperatureMap,
                projection,
                projection))
            {
                satelliteImg.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_SatelliteImage.png"));
            }

            winterTemperatureMap.Dispose();
            summerTemperatureMap.Dispose();
        }

        [TestMethod]
        public async Task SurfaceMappingTestAsync()
        {
            var projection = MapProjectionOptions.Default;

            var planet = GetPlanet();

            using var elevationMap = planet.GenerateElevationMap(ElevationMapResolution);
            await planet.AssignElevationMapAsync(elevationMap)
                .ConfigureAwait(false);
            using var elevationProjection = await planet
                .GetElevationMapProjectionAsync(ProjectionResolution, projection)
                .ConfigureAwait(false);
            //using (var img = elevationProjection.ElevationMapToImage(planet))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "ElevationImage.png"));
            //}
            using (var img = elevationProjection.ElevationMapToImage(planet, _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "HillShadeImage.png"));
            }

            var (winterTemperatureMap, summerTemperatureMap) = planet.GenerateTemperatureMaps(elevationMap, TemperatureMapResolution);
            await planet.AssignTemperatureMapWinterAsync(winterTemperatureMap)
                .ConfigureAwait(false);
            await planet.AssignTemperatureMapSummerAsync(summerTemperatureMap)
                .ConfigureAwait(false);
            using var temperatureMap = await planet
                .GetTemperatureMapProjectionAsync(TemperatureMapResolution, projection)
                .ConfigureAwait(false);
            //using (var tMap = await planet
            //    .GetTemperatureMapProjectionAsync(ProjectionResolution, projection)
            //    .ConfigureAwait(false))
            //{
            //    using (var img = tMap.TemperatureMapToImage())
            //    {
            //        img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "TemperatureImage.png"));
            //    }
            //    using var tImg = tMap.TemperatureMapToImage(planet, elevationMap, false, projection, projection);
            //    tImg.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "LandTemperatureImage.png"));
            //}

            using var precipitationMap = await planet
                .GetPrecipitationMapProjectionAsync(PrecipitationMapResolution, Seasons, projection)
                .ConfigureAwait(false);
            //using (var img = precipitationMap.PrecipitationMapToImage())
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "PrecipitationImage.png"));
            //}
            //using (var img = precipitationMap.PrecipitationMapToImage(planet, elevationMap, false))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "LandPrecipitationImage.png"));
            //}

            var climateMaps = new WeatherMaps(
                planet,
                elevationMap,
                winterTemperatureMap,
                summerTemperatureMap,
                precipitationMap,
                ProjectionResolution,
                projection);

            //var seasonsPath = System.IO.Path.Combine(_OutputPath, "Seasons");
            //if (!System.IO.Directory.Exists(seasonsPath))
            //{
            //    System.IO.Directory.CreateDirectory(seasonsPath);
            //}
            //var precipitationMaps = await planet
            //    .GetPrecipitationMapsProjectionAsync(PrecipitationMapResolution, Seasons, projection)
            //    .ConfigureAwait(false);

            //for (var i = 0; i < Seasons; i++)
            //{
            //    var proportionOfYear = i / (float)Seasons;
            //    using (var map = await planet
            //        .GetTemperatureMapProjectionAsync(ProjectionResolution, proportionOfYear, projection)
            //        .ConfigureAwait(false))
            //    {
            //        using var img = map.TemperatureMapToImage();
            //        img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, @$"Seasons\TemperatureImage_{i:00}.png"));
            //    }

            //    precipitationMaps[i]
            //        .SaveAsPng(System.IO.Path.Combine(_OutputPath!, @$"Seasons\PrecipitationImage_{i:00}.png"));

            //    precipitationMaps[i].Dispose();
            //}

            //using (var img = climateMaps.SeaIceRangeMap.SurfaceMapToImage(
            //    planet,
            //    elevationMap,
            //    (_, _) => new Rgba32(0, 0, 0),
            //    (v, _) => new Rgba32(
            //        (byte)(v.IsZero ? 2 : Math.Round(198 * v.Range) + 2),
            //        (byte)(v.IsZero ? 5 : Math.Round(235 * v.Range) + 5),
            //        (byte)(v.IsZero ? 20 : Math.Round(230 * v.Range) + 20)),
            //    projection,
            //    projection))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "SeaIceImage.png"));
            //}

            //using (var img = climateMaps.ClimateMap.SurfaceMapToImage(
            //    planet,
            //    elevationMap,
            //    (v, _) => v switch
            //    {
            //        ClimateType.Polar => new Rgba32(255, 255, 255),
            //        ClimateType.Subpolar => new Rgba32(100, 255, 200),
            //        ClimateType.Boreal => new Rgba32(0, 50, 0),
            //        ClimateType.CoolTemperate => new Rgba32(0, 200, 100),
            //        ClimateType.WarmTemperate => new Rgba32(0, 200, 0),
            //        ClimateType.Subtropical => new Rgba32(200, 200, 0),
            //        ClimateType.Tropical => new Rgba32(200, 0, 0),
            //        ClimateType.Supertropical => new Rgba32(50, 0, 0),
            //        _ => new Rgba32(127, 127, 127),
            //    },
            //    (_, _) => new Rgba32(2, 5, 20),
            //    projection,
            //    projection,
            //    _HillShading))
            //{
            //    img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "ClimateImage.png"));
            //}

            using (var img = climateMaps.BiomeMap.BiomeMapToImage(
                planet,
                elevationMap,
                true,
                projection,
                projection,
                _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "BiomeOceanHillShadeImage.png"));
            }

            //using (var snowfallMap = await planet
            //    .GetSnowfallMapProjectionAsync(PrecipitationMapResolution, Seasons)
            //    .ConfigureAwait(false))
            //{
            //    using var snowfallImg = snowfallMap.SurfaceMapToImage(
            //        planet,
            //        elevationMap,
            //        (v, _) =>
            //        {
            //            var value = ((double)v.PackedValue / ushort.MaxValue).Clamp(0, 1);
            //            return new Rgba32((byte)Math.Round(245 * value), (byte)Math.Round(250 * value), (byte)Math.Round(255 * value));
            //        },
            //        (_, _) => new Rgba32(2, 5, 20),
            //        projection,
            //        projection);
            //    snowfallImg.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "SnowfallImage.png"));
            //}

            using (var satelliteImg = climateMaps.GetSatelliteImage(
                planet,
                elevationMap,
                temperatureMap,
                projection,
                projection))
            {
                satelliteImg.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "SatelliteImage.png"));
            }

            winterTemperatureMap.Dispose();
            summerTemperatureMap.Dispose();
        }

        [TestMethod]
        public async Task SurfaceMappingEqualAreaPregenTestAsync()
        {
            var projection = new MapProjectionOptions(equalArea: true);

            using var planet = await GetPlanetPregenAsync().ConfigureAwait(false);

            using var stdElevationMap = await planet
                .GetElevationMapProjectionAsync(ElevationMapResolution_Pregen, MapProjectionOptions.Default)
                .ConfigureAwait(false);
            using var elevationMap = await planet
                .GetElevationMapProjectionAsync(ElevationMapResolution_Pregen, projection)
                .ConfigureAwait(false);
            using var elevationProjection = await planet
                .GetElevationMapProjectionAsync(ProjectionResolution, projection)
                .ConfigureAwait(false);
            using (var img = elevationProjection.ElevationMapToImage(planet, _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_HillShadeImage_EA.png"));
            }

            var (stdWinterTemperatureMap, stdSummerTemperatureMap) = planet.GenerateTemperatureMaps(stdElevationMap, TemperatureMapResolution);
            await planet.AssignTemperatureMapWinterAsync(stdWinterTemperatureMap)
                .ConfigureAwait(false);
            await planet.AssignTemperatureMapSummerAsync(stdSummerTemperatureMap)
                .ConfigureAwait(false);
            using var temperatureMap = await planet
                .GetTemperatureMapProjectionAsync(TemperatureMapResolution, projection)
                .ConfigureAwait(false);

            using var precipitationMap = await planet
                .GetPrecipitationMapProjectionAsync(PrecipitationMapResolution, Seasons, projection)
                .ConfigureAwait(false);

            WeatherMaps climateMaps;
            using (var winterTemperatureMap = await planet.GetTemperatureMapProjectionWinterAsync(TemperatureMapResolution, projection)
                .ConfigureAwait(false))
            {
                using var summerTemperatureMap = await planet.GetTemperatureMapProjectionSummerAsync(TemperatureMapResolution, projection)
                    .ConfigureAwait(false);
                climateMaps = new WeatherMaps(
                    planet,
                    elevationMap,
                    winterTemperatureMap,
                    summerTemperatureMap,
                    precipitationMap,
                    ProjectionResolution,
                    projection);
            }

            using (var img = climateMaps.BiomeMap.BiomeMapToImage(
                planet,
                elevationMap,
                true,
                projection,
                projection,
                _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_BiomeOceanHillShadeImage_EA.png"));
            }

            using (var satelliteImg = climateMaps.GetSatelliteImage(
                planet,
                elevationMap,
                temperatureMap,
                projection,
                projection))
            {
                satelliteImg.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "Pregen_SatelliteImage_EA.png"));
            }

            var region = new SurfaceRegion(planet, -0.9162975, -0.174533, 0.610865);
            const bool regionEqualArea = true;
            var regionProjection = region.GetProjection(planet, regionEqualArea);

            using var regionElevationMap = await region
                .GetElevationMapAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false);

            using var regionTemperatureMap = await region
                .GetTemperatureMapAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false);

            using var regionPrecipitationMap = await region
                .GetPrecipitationMapAsync(planet, ProjectionResolution, Seasons, regionEqualArea)
                .ConfigureAwait(false);

            WeatherMaps regionClimateMaps;
            using (var regionWinterTemperatureMap = await region
                .GetTemperatureMapWinterAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false))
            {
                using var regionSummerTemperatureMap = await region
                    .GetTemperatureMapSummerAsync(planet, ProjectionResolution, regionEqualArea)
                    .ConfigureAwait(false);
                regionClimateMaps = new WeatherMaps(
                    planet,
                    regionElevationMap,
                    regionWinterTemperatureMap,
                    regionSummerTemperatureMap,
                    regionPrecipitationMap,
                    ProjectionResolution,
                    regionProjection);
            }

            stdWinterTemperatureMap.Dispose();
            stdSummerTemperatureMap.Dispose();

            var normalizedSeaLevel = planet.SeaLevel / planet.MaxElevation;
            var elevationRange = elevationProjection.GetElevationRange(planet);
            var landCoords = 0;
            if (planet.Hydrosphere?.IsEmpty == false)
            {
                for (var x = 0; x < elevationProjection.Width; x++)
                {
                    for (var y = 0; y < elevationProjection.Height; y++)
                    {
                        var value = (2.0 * elevationProjection[x, y].PackedValue / ushort.MaxValue) - 1;
                        if (value - normalizedSeaLevel > 0)
                        {
                            landCoords++;
                        }
                    }
                }
            }
            var sb = new StringBuilder();
            AddTempString(sb, temperatureMap);
            sb.AppendLine();
            AddTerrainString(sb, planet, elevationProjection, regionElevationMap, landCoords);
            sb.AppendLine();
            AddClimateString(sb, elevationProjection, normalizedSeaLevel, landCoords, climateMaps);
            sb.AppendLine();
            AddPrecipitationString(sb, planet, elevationProjection, precipitationMap, normalizedSeaLevel, landCoords, climateMaps, projection);
            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public async Task SurfaceMappingEqualAreaTestAsync()
        {
            var projection = new MapProjectionOptions(equalArea: true);

            var planet = GetPlanet();

            using var stdElevationMap = planet.GenerateElevationMap(ElevationMapResolution);
            await planet.AssignElevationMapAsync(stdElevationMap)
                .ConfigureAwait(false);
            using var elevationMap = await planet
                .GetElevationMapProjectionAsync(ElevationMapResolution_Pregen, projection)
                .ConfigureAwait(false);
            using var elevationProjection = await planet
                .GetElevationMapProjectionAsync(ProjectionResolution, projection)
                .ConfigureAwait(false);
            using (var img = elevationProjection.ElevationMapToImage(planet, _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "HillShadeImage_EA.png"));
            }

            var (stdWinterTemperatureMap, stdSummerTemperatureMap) = planet.GenerateTemperatureMaps(stdElevationMap, TemperatureMapResolution);
            await planet.AssignTemperatureMapWinterAsync(stdWinterTemperatureMap)
                .ConfigureAwait(false);
            await planet.AssignTemperatureMapSummerAsync(stdSummerTemperatureMap)
                .ConfigureAwait(false);
            using var temperatureMap = await planet
                .GetTemperatureMapProjectionAsync(TemperatureMapResolution, projection)
                .ConfigureAwait(false);

            using var precipitationMap = await planet
                .GetPrecipitationMapProjectionAsync(PrecipitationMapResolution, Seasons, projection)
                .ConfigureAwait(false);

            WeatherMaps climateMaps;
            using (var winterTemperatureMap = await planet.GetTemperatureMapProjectionWinterAsync(TemperatureMapResolution, projection)
                .ConfigureAwait(false))
            {
                using var summerTemperatureMap = await planet.GetTemperatureMapProjectionSummerAsync(TemperatureMapResolution, projection)
                    .ConfigureAwait(false);
                climateMaps = new WeatherMaps(
                    planet,
                    elevationMap,
                    winterTemperatureMap,
                    summerTemperatureMap,
                    precipitationMap,
                    ProjectionResolution,
                    projection);
            }

            using (var img = climateMaps.BiomeMap.BiomeMapToImage(
                planet,
                elevationMap,
                true,
                projection,
                projection,
                _HillShading))
            {
                img.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "BiomeOceanHillShadeImage_EA.png"));
            }

            using (var satelliteImg = climateMaps.GetSatelliteImage(
                planet,
                elevationMap,
                temperatureMap,
                projection,
                projection))
            {
                satelliteImg.SaveAsPng(System.IO.Path.Combine(_OutputPath!, "SatelliteImage_EA.png"));
            }

            var region = new SurfaceRegion(planet, -0.9162975, -0.174533, 0.610865);
            const bool regionEqualArea = true;
            var regionProjection = region.GetProjection(planet, regionEqualArea);

            using var regionElevationMap = await region
                .GetElevationMapAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false);

            using var regionTemperatureMap = await region
                .GetTemperatureMapAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false);

            using var regionPrecipitationMap = await region
                .GetPrecipitationMapAsync(planet, ProjectionResolution, Seasons, regionEqualArea)
                .ConfigureAwait(false);

            WeatherMaps regionClimateMaps;
            using (var regionWinterTemperatureMap = await region
                .GetTemperatureMapWinterAsync(planet, ProjectionResolution, regionEqualArea)
                .ConfigureAwait(false))
            {
                using var regionSummerTemperatureMap = await region
                    .GetTemperatureMapSummerAsync(planet, ProjectionResolution, regionEqualArea)
                    .ConfigureAwait(false);
                regionClimateMaps = new WeatherMaps(
                    planet,
                    regionElevationMap,
                    regionWinterTemperatureMap,
                    regionSummerTemperatureMap,
                    regionPrecipitationMap,
                    ProjectionResolution,
                    regionProjection);
            }

            stdWinterTemperatureMap.Dispose();
            stdSummerTemperatureMap.Dispose();

            var normalizedSeaLevel = planet.SeaLevel / planet.MaxElevation;
            var elevationRange = elevationProjection.GetElevationRange(planet);
            var landCoords = 0;
            if (planet.Hydrosphere?.IsEmpty == false)
            {
                for (var x = 0; x < elevationProjection.Width; x++)
                {
                    for (var y = 0; y < elevationProjection.Height; y++)
                    {
                        var value = (2.0 * elevationProjection[x, y].PackedValue / ushort.MaxValue) - 1;
                        if (value - normalizedSeaLevel > 0)
                        {
                            landCoords++;
                        }
                    }
                }
            }
            var sb = new StringBuilder();
            AddTempString(sb, temperatureMap);
            sb.AppendLine();
            AddTerrainString(sb, planet, elevationProjection, regionElevationMap, landCoords);
            sb.AppendLine();
            AddClimateString(sb, elevationProjection, normalizedSeaLevel, landCoords, climateMaps);
            sb.AppendLine();
            AddPrecipitationString(sb, planet, elevationProjection, precipitationMap, normalizedSeaLevel, landCoords, climateMaps, projection);
            Console.WriteLine(sb.ToString());
        }

        private static void AddClimateString(
            StringBuilder sb,
            Image<L16> elevationMap,
            double normalizedSeaLevel,
            int landCoords,
            WeatherMaps maps)
        {
            if (maps.BiomeMap[0][0] == BiomeType.None)
            {
                return;
            }

            var biomes = new Dictionary<BiomeType, int>();
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (biomes.ContainsKey(maps.BiomeMap[x][y]))
                    {
                        biomes[maps.BiomeMap[x][y]]++;
                    }
                    else
                    {
                        biomes[maps.BiomeMap[x][y]] = 1;
                    }
                }
            }

            var deserts = 0;
            var warmDeserts = 0;
            var tropicalDeserts = 0;
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (maps.BiomeMap[x][y] == BiomeType.HotDesert
                        || maps.BiomeMap[x][y] == BiomeType.ColdDesert)
                    {
                        deserts++;
                        if (maps.BiomeMap[x][y] == BiomeType.HotDesert)
                        {
                            if (maps.ClimateMap[x][y] == ClimateType.WarmTemperate)
                            {
                                warmDeserts++;
                            }
                            else
                            {
                                tropicalDeserts++;
                            }
                        }
                    }
                }
            }

            var climates = new Dictionary<ClimateType, int>();
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if ((2.0 * elevationMap[x, y].PackedValue / ushort.MaxValue) - 1 - normalizedSeaLevel <= 0)
                    {
                        continue;
                    }
                    if (climates.ContainsKey(maps.ClimateMap[x][y]))
                    {
                        climates[maps.ClimateMap[x][y]]++;
                    }
                    else
                    {
                        climates[maps.ClimateMap[x][y]] = 1;
                    }
                }
            }

            sb.AppendLine("Climates:");
            var desert = deserts * 100.0 / landCoords;
            sb.AppendFormat("  Desert:                  {0}% ({1:+0.##;-0.##;on-targ\\et})", Math.Round(desert, 2), Math.Round(desert - 14, 2));
            sb.AppendLine();
            var polar = (climates.TryGetValue(ClimateType.Polar, out var value) ? value : 0) * 100.0 / landCoords;
            sb.AppendFormat("  Polar:                   {0}% ({1:+0.##;-0.##;on-targ\\et})", Math.Round(polar, 2), Math.Round(polar - 20, 2));
            sb.AppendLine();
            sb.AppendFormat("  Tundra:                  {0}%", Math.Round((biomes.TryGetValue(BiomeType.Tundra, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var alpine = (biomes.TryGetValue(BiomeType.Alpine, out value) ? value : 0) * 100.0 / landCoords;
            sb.AppendFormat("  Alpine:                  {0}% ({1:+0.##;-0.##;on-targ\\et})", Math.Round(alpine, 2), Math.Round(alpine - 3, 2));
            sb.AppendLine();
            sb.AppendFormat("  Subalpine:               {0}%", Math.Round((biomes.TryGetValue(BiomeType.Subalpine, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var boreal = climates.TryGetValue(ClimateType.Boreal, out value) ? value : 0;
            sb.AppendFormat("  Boreal:                  {0}%", Math.Round(boreal * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Lichen Woodland:       {0}% ({1}%)",
                boreal == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.LichenWoodland, out value) ? value : 0) * 100.0 / boreal, 2),
                Math.Round((biomes.TryGetValue(BiomeType.LichenWoodland, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Coniferous Forest:     {0}% ({1}%)",
                boreal == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.ConiferousForest, out value) ? value : 0) * 100.0 / boreal, 2),
                Math.Round((biomes.TryGetValue(BiomeType.ConiferousForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var coolTemperate = climates.TryGetValue(ClimateType.CoolTemperate, out value) ? value : 0;
            sb.AppendFormat("  Cool Temperate:          {0}%", Math.Round(coolTemperate * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Cold Desert:           {0}% ({1}%)",
                coolTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.ColdDesert, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.ColdDesert, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}% ({1}%)",
                coolTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.Steppe, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Steppe, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Mixed Forest:          {0}% ({1}%)",
                coolTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.MixedForest, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.MixedForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var warmTemperate = climates.TryGetValue(ClimateType.WarmTemperate, out value) ? value : 0;
            sb.AppendFormat("  Warm Temperate:          {0}%", Math.Round(warmTemperate * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}% ({1}%)",
                warmTemperate == 0 ? 0 : Math.Round(warmDeserts * 100.0 / warmTemperate, 2),
                Math.Round(warmDeserts * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Shrubland:             {0}% ({1}%)",
                warmTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.Shrubland, out value) ? value : 0) * 100.0 / warmTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Shrubland, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Deciduous Forest:      {0}% ({1}%)",
                warmTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.DeciduousForest, out value) ? value : 0) * 100.0 / warmTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.DeciduousForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var tropical = 0;
            tropical += climates.TryGetValue(ClimateType.Subtropical, out value) ? value : 0;
            tropical += climates.TryGetValue(ClimateType.Tropical, out value) ? value : 0;
            tropical += climates.TryGetValue(ClimateType.Supertropical, out value) ? value : 0;
            sb.AppendFormat("  Tropical:                {0}%", Math.Round(tropical * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}% ({1}%)",
                tropical == 0 ? 0 : Math.Round(tropicalDeserts * 100.0 / tropical, 2),
                Math.Round(tropicalDeserts * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Savanna:               {0}% ({1}%)",
                tropical == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.Savanna, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Savanna, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Monsoon Forest:        {0}% ({1}%)",
                tropical == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.MonsoonForest, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round((biomes.TryGetValue(BiomeType.MonsoonForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var rainforest = (biomes.TryGetValue(BiomeType.RainForest, out value) ? value : 0) * 100.0 / landCoords;
            sb.AppendFormat("    Rain Forest:           {0}% ({1}%) ({2:+0.##;-0.##;on-targ\\et})",
                Math.Round(rainforest, 2),
                tropical == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.RainForest, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round(rainforest - 6, 2));
            sb.AppendLine();
        }

        private static void AddPrecipitationString(
            StringBuilder sb,
            Planetoid planet,
            Image<L16> elevationMap,
            Image<L16> precipitationMap,
            double normalizedSeaLevel,
            int landCoords,
            WeatherMaps maps,
            MapProjectionOptions options)
        {
            sb.Append("Max precip: ")
                .Append(Math.Round(planet.Atmosphere.MaxPrecipitation, 3))
                .AppendLine("mm/hr");

            sb.AppendLine("Precipitation (average, land):");
            if (landCoords == 0)
            {
                sb.AppendLine("  No land.");
                return;
            }

            var n = 0;
            var temperate = 0.0;
            var list = new List<double>();
            var scale = options.EqualArea
                ? MathAndScience.Constants.Doubles.MathConstants.TwoPI / maps.YLength
                : Math.PI / maps.YLength;
            var elevationScale = options.EqualArea
                ? MathAndScience.Constants.Doubles.MathConstants.TwoPI / elevationMap.Height
                : Math.PI / elevationMap.Height;
            var precipitationScale = options.EqualArea
                ? MathAndScience.Constants.Doubles.MathConstants.TwoPI / precipitationMap.Height
                : Math.PI / precipitationMap.Height;
            var halfResolution = maps.YLength / 2;
            var halfElevationResolution = elevationMap.Height / 2;
            var halfPrecipitationResolution = precipitationMap.Height / 2;
            var halfXResolution = maps.XLength / 2;
            var halfXElevationResolution = elevationMap.Width / 2;
            var halfXPrecipitationResolution = precipitationMap.Width / 2;
            for (var x = 0; x < maps.XLength; x++)
            {
                var lon = options.EqualArea
                    ? (x - halfXResolution) * scale / Math.PI
                    : (x - maps.XLength) * scale;
                var elevationX = options.EqualArea
                    ? (int)Math.Round((lon * Math.PI / elevationScale) + halfXElevationResolution).Clamp(0, elevationMap.Width - 1)
                    : (int)Math.Round((lon / elevationScale) + elevationMap.Width).Clamp(0, elevationMap.Width - 1);
                var precipitationX = options.EqualArea
                    ? (int)Math.Round((lon * Math.PI / precipitationScale) + halfXPrecipitationResolution).Clamp(0, precipitationMap.Width - 1)
                    : (int)Math.Round((lon / precipitationScale) + precipitationMap.Width).Clamp(0, precipitationMap.Width - 1);
                for (var y = 0; y < maps.YLength; y++)
                {
                    var lat = (y - halfResolution) * scale;
                    var elevationY = options.EqualArea
                        ? (int)Math.Round(halfElevationResolution + (Math.Sin(lat) * Math.PI / elevationScale)).Clamp(0, elevationMap.Height - 1)
                        : (int)Math.Round((lat / elevationScale) + halfElevationResolution).Clamp(0, elevationMap.Height - 1);
                    if (((double)elevationMap[elevationX, elevationY].PackedValue / ushort.MaxValue) - normalizedSeaLevel < 0)
                    {
                        continue;
                    }

                    var precipitationY = options.EqualArea
                        ? (int)Math.Round(halfPrecipitationResolution + (Math.Sin(lat) * Math.PI / precipitationScale)).Clamp(0, precipitationMap.Height - 1)
                        : (int)Math.Round((lat / precipitationScale) + halfPrecipitationResolution).Clamp(0, precipitationMap.Height - 1);
                    var precipitation = (double)precipitationMap[precipitationX, precipitationY].PackedValue / ushort.MaxValue * planet.Atmosphere.MaxPrecipitation;

                    list.Add(precipitation);

                    if (maps.ClimateMap[x][y] == ClimateType.CoolTemperate
                        || maps.ClimateMap[x][y] == ClimateType.WarmTemperate)
                    {
                        temperate += precipitation;
                        n++;
                    }
                }
            }
            list.Sort();
            if (n == 0)
            {
                temperate = 0;
            }
            else
            {
                temperate /= n;
            }

            var avg = list.Average();
            sb.AppendFormat("  Avg:                     {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(avg, 3), Math.Round(avg - 0.11293634496919917864476386036961, 3));
            sb.AppendLine();
            var avg90 = list.Take((int)Math.Floor(list.Count * 0.9)).Average();
            sb.AppendFormat("  Avg (<=P90):             {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(avg90, 3), Math.Round(avg90 - 0.11293634496919917864476386036961, 3));
            sb.AppendLine();
            var avgList = planet.Atmosphere.AveragePrecipitation;
            sb.AppendFormat("  Avg (listed):            {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(avgList, 3), Math.Round(avgList - 0.11293634496919917864476386036961, 3));
            sb.AppendLine();
            sb.AppendFormat("  Avg (Temperate):         {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(temperate, 3), Math.Round(temperate - 0.12548482774355464293862651152179, 3));
            sb.AppendLine();

            sb.AppendFormat("  Min:                     {0}mm/hr", Math.Round(list[0], 3));
            sb.AppendLine();
            sb.AppendFormat("  P10:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.1)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P20:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.2)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P30:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.3)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P40:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.4)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P50:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.5)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P60:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.6)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P70:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.7)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P80:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.8)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P90:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.9)).First(), 3));
            sb.AppendLine();
            var max = list.Last();
            sb.AppendFormat("  Max:                     {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(max), Math.Round(max - 1.3542094455852156057494866529774, 3));

            sb.AppendLine();
        }

        private static void AddTempString(StringBuilder sb, Image<L16> temperatureMap)
        {
            sb.AppendLine("Temp:");
            var range = temperatureMap.GetTemperatureRange();
            sb.AppendFormat("  Avg:                     {0} K ({1:+0.##;-0.##;on-targ\\et})", Math.Round(range.Average), Math.Round(range.Average - (float)PlanetParams.EarthSurfaceTemperature, 2));
            sb.AppendLine();
            sb.AppendFormat("  Min:                     {0} K", Math.Round(range.Min));
            sb.AppendLine();
            sb.AppendFormat("  Max:                     {0} K", Math.Round(range.Max));
            sb.AppendLine();
        }

        private static void AddTerrainString(StringBuilder sb, Planetoid planet, Image<L16> elevationMap, Image<L16> regionElevationMap, int landCoords)
        {
            sb.AppendFormat("Sea Level:                 {0}m", Math.Round(planet.SeaLevel));
            sb.AppendLine();

            var elevationRange = elevationMap.GetElevationRange(planet);
            if (planet.Hydrosphere?.IsEmpty == false)
            {
                var totalCoords = (decimal)(elevationMap.Width * elevationMap.Height);
                var landProportion = landCoords / totalCoords;
                sb.AppendFormat("Land proportion:           {0}", Math.Round(landProportion, 2));
                sb.AppendLine();
                sb.AppendFormat("Water proportion:          {0}", Math.Round(1 - landProportion, 2));
                sb.AppendLine();
            }

            sb.AppendFormat("Avg Elevation:             {0}m", Math.Round(elevationRange.Average));
            sb.AppendLine();
            sb.AppendFormat("Min Elevation:             {0}m / {1}m", Math.Round(elevationRange.Min), Math.Round(planet.MaxElevation));
            sb.AppendLine();
            sb.AppendFormat("Max Elevation:             {0}m / {1}m", Math.Round(elevationRange.Max), Math.Round(planet.MaxElevation));
            sb.AppendLine();

            var regionElevationRange = regionElevationMap.GetElevationRange(planet);
            sb.AppendFormat("Region Avg Elevation:      {0}m", Math.Round(regionElevationRange.Average));
            sb.AppendLine();
            sb.AppendFormat("Region Min Elevation:      {0}m / {1}m", Math.Round(regionElevationRange.Min), Math.Round(planet.MaxElevation));
            sb.AppendLine();
            sb.AppendFormat("Region Max Elevation:      {0}m / {1}m", Math.Round(regionElevationRange.Max), Math.Round(planet.MaxElevation));
            sb.AppendLine();
        }

        private static Planetoid GetPlanet()
        {
            var planet = Planetoid.GetPlanetForSunlikeStar(out _);
            Assert.IsNotNull(planet);
            Console.WriteLine($"Planet seed: {planet!.Seed}");
            return planet;
        }

        private static async Task<Planetoid> GetPlanetPregenAsync()
        {
            var planet = Planetoid.GetPlanetForSunlikeStar(out _, seed: 3246055358);
            //using var planet = Planetoid.GetPlanetForSunlikeStar(out _);
            Assert.IsNotNull(planet);
            //Console.WriteLine($"Planet seed: {planet!.Seed}");

            await planet!
                .AssignElevationMapAsync(Image.Load(@"D:\Documents\Programming\Projects\WorldFoundry\WorldFoundry.Test\avora_elev_5400x2700.png"))
                .ConfigureAwait(false);

            for (var i = 1; i <= 12; i++)
            {
                await planet
                    .AssignPrecipitationMapAsync(Image.Load($@"D:\Documents\Programming\Projects\WorldFoundry\WorldFoundry.Test\Precipitation\{i:00}.png"))
                    .ConfigureAwait(false);
            }

            return planet;
        }
    }
}
