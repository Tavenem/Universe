using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeverFoundry.MathAndScience;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.SurfaceMapping;
using System;
using System.Diagnostics;

namespace NeverFoundry.WorldFoundry.Test
{
    [TestClass]
    public class MappingTests
    {
        private const int MapResolution = 360;

        [TestMethod]
        public void SurfaceMappingTest()
        {
            var planet = Planetoid.GetPlanetForSunlikeStar(out _);
            Assert.IsNotNull(planet);

            var maps = planet!.GetSurfaceMaps(MapResolution, steps: 12);

            var dirPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "TestResultImages");
            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }

            var elevationMapImage = maps.Elevation.ElevationMapToImage();
            elevationMapImage.Save(System.IO.Path.Combine(dirPath!, "ElevationImage.png"), System.Drawing.Imaging.ImageFormat.Png);

            var hillShadeMap = SurfaceMap.GetHillShadeMap(maps.Elevation, scaleFactor: 4, scaleIsRelative: true);
            var hillShadeMapImage = hillShadeMap.SurfaceMapToImage();
            hillShadeMapImage.Save(System.IO.Path.Combine(dirPath!, "HillShadeImage.png"), System.Drawing.Imaging.ImageFormat.Png);

            var tempMapImage = maps.TemperatureRangeMap.TemperatureMapToImage();
            tempMapImage.Save(System.IO.Path.Combine(dirPath!, "TemperatureImage.png"), System.Drawing.Imaging.ImageFormat.Png);

            var landTempImage = maps.TemperatureRangeMap.SurfaceMapToImage(maps.Elevation, (v, _, _, y) =>
            {
                if (y == MapResolution * 13 / 100 || y == MapResolution * 5 / 18 || y == MapResolution / 2|| y == MapResolution * 13 / 18 || y == MapResolution * 87 / 100)
                {
                    return (0, 0, 0);
                }
                return v.Average.ToTemperatureColor();
            }, (_, _, _, y) =>
            {
                if (y == MapResolution * 13 / 100 || y == MapResolution * 5 / 18 || y == MapResolution / 2 || y == MapResolution * 13 / 18 || y == MapResolution * 87 / 100)
                {
                    return (0, 0, 0);
                }
                return (255, 255, 255);
            });
            landTempImage.Save(System.IO.Path.Combine(dirPath!, "LandTemperatureImage.png"), System.Drawing.Imaging.ImageFormat.Png);

            var precipMapImage = maps.TotalPrecipitationMap.PrecipitationMapToImage(planet!.Atmosphere.MaxPrecipitation);
            precipMapImage.Save(System.IO.Path.Combine(dirPath!, "PrecipitationImage.png"), System.Drawing.Imaging.ImageFormat.Png);

            var biomeOceanImage = maps.BiomeMap.SurfaceMapToImage(maps.Elevation, (v, _) => v.ToBiomeColor(), (_, e) => e.ToElevationColor());
            biomeOceanImage.Save(System.IO.Path.Combine(dirPath!, "BiomeOceanImage.png"), System.Drawing.Imaging.ImageFormat.Png);

            //var maps2 = planet!.GetSurfaceMaps(MapResolution, equalArea: true, steps: 12);
            //var biomeOceanImage2 = maps2.BiomeMap.SurfaceMapToImage(maps2.Elevation, (v, _) => v.ToBiomeColor(), (_, e) => e.ToElevationColor());
            //biomeOceanImage2.Save(System.IO.Path.Combine(dirPath!, "BiomeOceanImage_EqualArea.png"), System.Drawing.Imaging.ImageFormat.Png);

            var biomeOceanHillShadeImage = maps.BiomeMap.SurfaceMapToImage(
                maps.Elevation,
                (v, _) => v.ToBiomeColor(),
                (_, e) => e.ToElevationColor(),
                applyHillShadingToLand: true,
                hillScaleFactor: 4,
                hillScaleIsRelative: true,
                hillShadeMultiplier: 1.25);
            biomeOceanHillShadeImage.Save(System.IO.Path.Combine(dirPath!, "BiomeOceanHillShadeImage.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
