using ExtensionLib;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.Climate;

namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents a tile on a <see cref="WorldGrids.WorldGrid"/>.
    /// </summary>
    public class Tile
    {
        /// <summary>
        /// The area of this <see cref="Tile"/>, in square meters.
        /// </summary>
        public double Area { get; internal set; }

        /// <summary>
        /// The average atmospheric pressure in this <see cref="Tile"/>, in kPa.
        /// </summary>
        public FloatRange AtmosphericPressure { get; internal set; }

        /// <summary>
        /// The <see cref="Climate.BiomeType"/> of this <see cref="Tile"/>.
        /// </summary>
        public BiomeType BiomeType { get; internal set; }

        /// <summary>
        /// The <see cref="Climate.ClimateType"/> of this <see cref="Tile"/>.
        /// </summary>
        public ClimateType ClimateType { get; internal set; }

        /// <summary>
        /// The indexes of the <see cref="Corner"/>s to which this <see cref="Tile"/> is connected.
        /// </summary>
        public int[] Corners { get; }

        /// <summary>
        /// The <see cref="Climate.EcologyType"/> of this <see cref="Tile"/>.
        /// </summary>
        public EcologyType EcologyType { get; internal set; }

        /// <summary>
        /// The number of sides possessed by this <see cref="Tile"/>.
        /// </summary>
        public int EdgeCount { get; }

        /// <summary>
        /// The indexes of the <see cref="Edge"/>s to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int[] Edges { get; }

        /// <summary>
        /// The elevation above sea level of this <see cref="Tile"/>, in meters.
        /// </summary>
        public float Elevation { get; internal set; }

        internal float FrictionCoefficient { get; set; }

        /// <summary>
        /// The <see cref="Climate.HumidityType"/> of this <see cref="Tile"/>.
        /// </summary>
        public HumidityType HumidityType { get; internal set; }

        /// <summary>
        /// The index of this <see cref="Tile"/>.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The latitude of this <see cref="Tile"/>, as an angle in radians from the equator.
        /// </summary>
        public float Latitude { get; internal set; }

        /// <summary>
        /// The longitude of this <see cref="Tile"/>, as an angle in radians from the X-axis at 0 rotation.
        /// </summary>
        public float Longitude { get; internal set; }

        /// <summary>
        /// The average annual precipitation in this <see cref="Tile"/>, in mm. Counts all forms of
        /// precipitation, including the water-equivalent amount of snowfall (even though snow is
        /// also reported separately).
        /// </summary>
        public float Precipitation { get; internal set; }

        /// <summary>
        /// The distance between this <see cref="Tile"/>'s center and any of its <see
        /// cref="Corners"/>.
        /// </summary>
        public double Radius { get; internal set; }

        /// <summary>
        /// The resources which can be found in this <see cref="Tile"/>, along with a value from 0 to
        /// 1 (inclusive) indicating the relative richness of the resource in that location.
        /// </summary>
        public Dictionary<Chemical, float> Resources { get; internal set; }

        /// <summary>
        /// The average thickness of sea ice in this <see cref="Tile"/>, in meters.
        /// </summary>
        public FloatRange SeaIce { get; internal set; }

        /// <summary>
        /// The average depth of persistent snow cover in this <see cref="Tile"/>, in mm. Assumes a
        /// typical ratio of 1mm water-equivalent = 13mm snow.
        /// </summary>
        public FloatRange SnowCover { get; internal set; }

        /// <summary>
        /// The average annual amount of snow which falls in this <see cref="Tile"/>, in mm.
        /// </summary>
        public float SnowFall { get; set; }

        /// <summary>
        /// The average temperature in this <see cref="Tile"/>, in K.
        /// </summary>
        public FloatRange Temperature { get; internal set; }

        /// <summary>
        /// The <see cref="WorldFoundry.TerrainType"/> of this <see cref="Tile"/>.
        /// </summary>
        public TerrainType TerrainType { get; internal set; } = TerrainType.Land;

        /// <summary>
        /// The indexes of the <see cref="Tile"/>s to which this one is connected.
        /// </summary>
        public int[] Tiles { get; }

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Tile"/>.
        /// </summary>
        public Vector3 Vector { get; internal set; }

        internal float WindFactor { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        public Tile() { }

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        /// <param name="index">The <see cref="Index"/> of the <see cref="Tile"/>.</param>
        internal Tile(int index)
        {
            Index = index;
            EdgeCount = index < 12 ? 5 : 6;
            Corners = new int[EdgeCount];
            Edges = new int[EdgeCount];
            Tiles = new int[EdgeCount];
            for (var i = 0; i < EdgeCount; i++)
            {
                Corners[i] = -1;
                Edges[i] = -1;
                Tiles[i] = -1;
            }
        }

        /// <summary>
        /// Determines if this <see cref="Tile"/> instance is mountainous (see Remarks).
        /// </summary>
        /// <param name="grid">The <see cref="WorldGrid"/> of which this <see cref="Tile"/> is a
        /// part.</param>
        /// <returns><see langword="true"/> if this <see cref="Tile"/> is mountainous; otherwise
        /// <see langword="false"/>.</returns>
        /// <remarks>
        /// "Mountainous" is defined as having a maximum elevation greater than 8.5% of the maximum
        /// elevation of this world, or a maximum elevation greater than 5% of the maximum and a
        /// slope greater than 0.035, or a maximum elevation greater than 3.5% of the maximum and a
        /// slope greater than 0.0875.
        /// </remarks>
        public bool GetIsMountainous(WorldGrid grid)
        {
            var maxElevation = Math.Max(Elevation, Corners.Select(x => grid.Corners[x]).Max(x => x.Elevation));
            var maxWorldElevation = Math.Max(grid.Tiles.Max(x => x.Elevation), grid.Corners.Max(x => x.Elevation));
            if (maxElevation < maxWorldElevation * 0.035)
            {
                return false;
            }
            if (maxElevation > maxWorldElevation * 0.085)
            {
                return true;
            }
            var slope = GetSlope(grid);
            if (maxElevation > maxWorldElevation * 0.05)
            {
                return slope > 0.035;
            }
            return slope > 0.0875;
        }

        /// <summary>
        /// Calculates the slope of this <see cref="Tile"/>, as the ratio of rise over run from the
        /// lowest point to highest point among its center and corners.
        /// </summary>
        /// <param name="grid">The <see cref="WorldGrid"/> of which this <see cref="Tile"/> is a
        /// part.</param>
        /// <returns>The slope of this <see cref="Tile"/>.</returns>
        public double GetSlope(WorldGrid grid)
        {
            var minCorner = grid.Corners[Corners[Corners.Select(x => grid.Corners[x].Elevation).IndexOfMin()]];
            var maxCorner = grid.Corners[Corners[Corners.Select(x => grid.Corners[x].Elevation).IndexOfMax()]];
            var centerMin = Elevation < minCorner.Elevation;
            var centerMax = Elevation > maxCorner.Elevation;
            var diff = 0.0;
            var dist = 0.0;
            if (centerMin)
            {
                diff = maxCorner.Elevation - Elevation;
                dist = (Vector - maxCorner.Vector).Length();
            }
            else if (centerMax)
            {
                diff = Elevation - minCorner.Elevation;
                dist = (Vector - minCorner.Vector).Length();
            }
            else
            {
                diff = maxCorner.Elevation - minCorner.Elevation;
                dist = (maxCorner.Vector - minCorner.Vector).Length();
            }
            return diff / dist;
        }

        internal Corner GetLowestCorner(WorldGrid grid)
            => Corners.Select(i => grid.Corners[i]).OrderBy(c => c.Elevation).FirstOrDefault();

        internal int IndexOfCorner(int cornerIndex) => Array.IndexOf(Corners, cornerIndex);

        internal int IndexOfTile(int tileIndex) => Array.IndexOf(Tiles, tileIndex);

        internal void SetClimate(IEnumerable<Season> seasons)
        {
            AtmosphericPressure = new FloatRange(
                seasons.Min(s => s.TileClimates[Index].AtmosphericPressure),
                seasons.Average(s => s.TileClimates[Index].AtmosphericPressure),
                seasons.Max(s => s.TileClimates[Index].AtmosphericPressure));
            Temperature = new FloatRange(
                seasons.Min(s => s.TileClimates[Index].Temperature),
                seasons.Average(s => s.TileClimates[Index].Temperature),
                seasons.Max(s => s.TileClimates[Index].Temperature));
            Precipitation = seasons.Sum(s => s.TileClimates[Index].Precipitation);
            SeaIce = new FloatRange(
                seasons.Min(s => s.TileClimates[Index].SeaIce),
                seasons.Average(s => s.TileClimates[Index].SeaIce),
                seasons.Max(s => s.TileClimates[Index].SeaIce));
            SnowCover = new FloatRange(
                seasons.Min(s => s.TileClimates[Index].SnowCover),
                seasons.Average(s => s.TileClimates[Index].SnowCover),
                seasons.Max(s => s.TileClimates[Index].SnowCover));
            SnowFall = seasons.Sum(s => s.TileClimates[Index].SnowFall);

            SetHumidityType(Precipitation);

            SetClimateType(Temperature.Avg);

            SetEcologyType();
        }

        private void SetClimateType(float temperature)
        {
            if (temperature <= Chemical.Water.MeltingPoint + 1.5)
            {
                ClimateType = ClimateType.Polar;
            }
            else if (temperature <= Chemical.Water.MeltingPoint + 3)
            {
                ClimateType = ClimateType.Subpolar;
            }
            else if (temperature <= Chemical.Water.MeltingPoint + 6)
            {
                ClimateType = ClimateType.Boreal;
            }
            else if (temperature <= Chemical.Water.MeltingPoint + 12)
            {
                ClimateType = ClimateType.CoolTemperate;
            }
            else if (temperature <= Chemical.Water.MeltingPoint + 18)
            {
                ClimateType = ClimateType.WarmTemperate;
            }
            else if (temperature <= Chemical.Water.MeltingPoint + 24)
            {
                ClimateType = ClimateType.Subtropical;
            }
            else if (temperature <= Chemical.Water.MeltingPoint + 36)
            {
                ClimateType = ClimateType.Tropical;
            }
            else
            {
                ClimateType = ClimateType.Supertropical;
            }
        }

        private void SetEcologyType()
        {
            if (TerrainType == TerrainType.Water)
            {
                BiomeType = BiomeType.Sea;
                EcologyType = EcologyType.Sea;
                return;
            }

            switch (ClimateType)
            {
                case ClimateType.Polar:
                    BiomeType = BiomeType.Polar;
                    if (HumidityType <= HumidityType.Perarid)
                    {
                        EcologyType = EcologyType.Desert;
                    }
                    else
                    {
                        EcologyType = EcologyType.Ice;
                    }
                    break;
                case ClimateType.Subpolar:
                    BiomeType = BiomeType.Tundra;
                    if (HumidityType == HumidityType.Superarid)
                    {
                        EcologyType = EcologyType.DryTundra;
                    }
                    else if (HumidityType == HumidityType.Perarid)
                    {
                        EcologyType = EcologyType.MoistTundra;
                    }
                    else if (HumidityType == HumidityType.Arid)
                    {
                        EcologyType = EcologyType.WetTundra;
                    }
                    else
                    {
                        EcologyType = EcologyType.RainTundra;
                    }
                    break;
                case ClimateType.Boreal:
                    if (HumidityType <= HumidityType.Perarid)
                    {
                        BiomeType = BiomeType.LichenWoodland;
                        if (HumidityType == HumidityType.Superarid)
                        {
                            EcologyType = EcologyType.Desert;
                        }
                        else
                        {
                            EcologyType = EcologyType.DryScrub;
                        }
                    }
                    else
                    {
                        BiomeType = BiomeType.ConiferousForest;
                        if (HumidityType == HumidityType.Arid)
                        {
                            EcologyType = EcologyType.MoistForest;
                        }
                        else if (HumidityType == HumidityType.Semiarid)
                        {
                            EcologyType = EcologyType.WetForest;
                        }
                        else
                        {
                            EcologyType = EcologyType.RainForest;
                        }
                    }
                    break;
                case ClimateType.CoolTemperate:
                    if (HumidityType <= HumidityType.Perarid)
                    {
                        BiomeType = BiomeType.ColdDesert;
                        if (HumidityType == HumidityType.Superarid)
                        {
                            EcologyType = EcologyType.Desert;
                        }
                        else
                        {
                            EcologyType = EcologyType.DesertScrub;
                        }
                    }
                    else if (HumidityType == HumidityType.Arid)
                    {
                        BiomeType = BiomeType.Steppe;
                        EcologyType = EcologyType.Steppe;
                    }
                    else
                    {
                        BiomeType = BiomeType.MixedForest;
                        if (HumidityType == HumidityType.Semiarid)
                        {
                            EcologyType = EcologyType.MoistForest;
                        }
                        else if (HumidityType == HumidityType.Subhumid)
                        {
                            EcologyType = EcologyType.WetForest;
                        }
                        else
                        {
                            EcologyType = EcologyType.RainForest;
                        }
                    }
                    break;
                case ClimateType.WarmTemperate:
                    if (HumidityType <= HumidityType.Perarid)
                    {
                        BiomeType = BiomeType.HotDesert;
                        if (HumidityType == HumidityType.Superarid)
                        {
                            EcologyType = EcologyType.Desert;
                        }
                        else
                        {
                            EcologyType = EcologyType.DesertScrub;
                        }
                    }
                    else if (HumidityType <= HumidityType.Semiarid)
                    {
                        BiomeType = BiomeType.Shrubland;
                        if (HumidityType == HumidityType.Arid)
                        {
                            EcologyType = EcologyType.ThornScrub;
                        }
                        else
                        {
                            EcologyType = EcologyType.DryForest;
                        }
                    }
                    else
                    {
                        BiomeType = BiomeType.DeciduousForest;
                        if (HumidityType == HumidityType.Subhumid)
                        {
                            EcologyType = EcologyType.MoistForest;
                        }
                        else if (HumidityType == HumidityType.Humid)
                        {
                            EcologyType = EcologyType.WetForest;
                        }
                        else
                        {
                            EcologyType = EcologyType.RainForest;
                        }
                    }
                    break;
                case ClimateType.Subtropical:
                    if (HumidityType <= HumidityType.Perarid)
                    {
                        BiomeType = BiomeType.HotDesert;
                        if (HumidityType == HumidityType.Superarid)
                        {
                            EcologyType = EcologyType.Desert;
                        }
                        else
                        {
                            EcologyType = EcologyType.DesertScrub;
                        }
                    }
                    else if (HumidityType == HumidityType.Arid)
                    {
                        BiomeType = BiomeType.Savanna;
                        EcologyType = EcologyType.ThornWoodland;
                    }
                    else if (HumidityType <= HumidityType.Subhumid)
                    {
                        BiomeType = BiomeType.MonsoonForest;
                        if (HumidityType == HumidityType.Semiarid)
                        {
                            EcologyType = EcologyType.DryForest;
                        }
                        else if (HumidityType == HumidityType.Subhumid)
                        {
                            EcologyType = EcologyType.MoistForest;
                        }
                    }
                    else
                    {
                        BiomeType = BiomeType.RainForest;
                        if (HumidityType == HumidityType.Humid)
                        {
                            EcologyType = EcologyType.WetForest;
                        }
                        else
                        {
                            EcologyType = EcologyType.RainForest;
                        }
                    }
                    break;
                case ClimateType.Tropical:
                    if (HumidityType <= HumidityType.Perarid)
                    {
                        BiomeType = BiomeType.HotDesert;
                        if (HumidityType == HumidityType.Superarid)
                        {
                            EcologyType = EcologyType.Desert;
                        }
                        else if (HumidityType == HumidityType.Perarid)
                        {
                            EcologyType = EcologyType.DesertScrub;
                        }
                    }
                    else if (HumidityType <= HumidityType.Semiarid)
                    {
                        BiomeType = BiomeType.Savanna;
                        if (HumidityType == HumidityType.Arid)
                        {
                            EcologyType = EcologyType.ThornWoodland;
                        }
                        else if (HumidityType == HumidityType.Semiarid)
                        {
                            EcologyType = EcologyType.VeryDryForest;
                        }
                    }
                    else if (HumidityType == HumidityType.Subhumid)
                    {
                        BiomeType = BiomeType.MonsoonForest;
                        EcologyType = EcologyType.DryForest;
                    }
                    else
                    {
                        BiomeType = BiomeType.RainForest;
                        if (HumidityType == HumidityType.Humid)
                        {
                            EcologyType = EcologyType.MoistForest;
                        }
                        else if (HumidityType == HumidityType.Perhumid)
                        {
                            EcologyType = EcologyType.WetForest;
                        }
                        else
                        {
                            EcologyType = EcologyType.RainForest;
                        }
                    }
                    break;
                case ClimateType.Supertropical:
                    BiomeType = BiomeType.HotDesert;
                    EcologyType = EcologyType.Desert;
                    break;
                default:
                    break;
            }
        }

        private void SetHumidityType(float annualPrecipitation)
        {
            if (TerrainType == TerrainType.Water)
            {
                HumidityType = HumidityType.Superhumid;
            }
            else if (annualPrecipitation < 125)
            {
                HumidityType = HumidityType.Superarid;
            }
            else if (annualPrecipitation < 250)
            {
                HumidityType = HumidityType.Perarid;
            }
            else if (annualPrecipitation < 500)
            {
                HumidityType = HumidityType.Arid;
            }
            else if (annualPrecipitation < 1000)
            {
                HumidityType = HumidityType.Semiarid;
            }
            else if (annualPrecipitation < 2000)
            {
                HumidityType = HumidityType.Subhumid;
            }
            else if (annualPrecipitation < 4000)
            {
                HumidityType = HumidityType.Humid;
            }
            else if (annualPrecipitation < 8000)
            {
                HumidityType = HumidityType.Perhumid;
            }
            else
            {
                HumidityType = HumidityType.Superhumid;
            }
        }
    }
}
