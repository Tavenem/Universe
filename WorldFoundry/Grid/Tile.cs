using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.Climate;

namespace WorldFoundry.Grid
{
    /// <summary>
    /// Represents a tile on the 3D grid.
    /// </summary>
    public class Tile : IEquatable<Tile>
    {
        /// <summary>
        /// The area of this <see cref="Tile"/>, in square meters.
        /// </summary>
        public float Area { get; internal set; }

        /// <summary>
        /// The <see cref="Climate.BiomeType"/> of this <see cref="Tile"/>.
        /// </summary>
        public BiomeType BiomeType { get; internal set; }

        /// <summary>
        /// The <see cref="Climate.ClimateType"/> of this <see cref="Tile"/>.
        /// </summary>
        public ClimateType ClimateType { get; internal set; }

        /// <summary>
        /// The <see cref="Corner"/>s of this <see cref="Tile"/>.
        /// </summary>
        public int[] Corners { get; }

        /// <summary>
        /// The <see cref="Climate.EcologyType"/> of this <see cref="Tile"/>.
        /// </summary>
        public EcologyType EcologyType { get; internal set; }

        /// <summary>
        /// The <see cref="Edge"/> of this <see cref="Tile"/>.
        /// </summary>
        public int[] Edges { get; }

        /// <summary>
        /// The elevation above sea level of this <see cref="Tile"/>, in meters.
        /// </summary>
        public float Elevation { get; internal set; }

        internal float FrictionCoefficient { get; private set; }

        /// <summary>
        /// The <see cref="Climate.HumidityType"/> of this <see cref="Tile"/>.
        /// </summary>
        public HumidityType HumidityType { get; internal set; }

        /// <summary>
        /// The latitude of this <see cref="Tile"/>, as an angle in radians from the equator.
        /// </summary>
        public float Latitude { get; internal set; }

        /// <summary>
        /// The longitude of this <see cref="Tile"/>, as an angle in radians from an arbitrary meridian.
        /// </summary>
        public float Longitude { get; internal set; }

        internal List<Vector2> Polygon { get; set; }

        internal float North { get; set; }

        /// <summary>
        /// The <see cref="WorldFoundry.TerrainType"/> of this <see cref="Tile"/>.
        /// </summary>
        public TerrainType TerrainType { get; internal set; } = TerrainType.Land;

        /// <summary>
        /// The neighboring <see cref="Tile"/>s to this one.
        /// </summary>
        public int[] Tiles { get; }

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Tile"/>.
        /// </summary>
        public Vector3 Vector { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        public Tile() { }

        internal Tile(int edges)
        {
            Corners = Enumerable.Repeat(-1, edges).ToArray();
            Edges = Enumerable.Repeat(-1, edges).ToArray();
            Tiles = Enumerable.Repeat(-1, edges).ToArray();
        }

        public static bool operator ==(Tile t, object o) => ReferenceEquals(t, null) ? o == null : t.Equals(o);

        public static bool operator !=(Tile t, object o) => ReferenceEquals(t, null) ? o != null : !t.Equals(o);

        /// <summary>
        /// Returns true if this <see cref="Tile"/> is the same as the given object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (obj is Tile t)
            {
                return Equals(t);
            }
            return false;
        }

        /// <summary>
        /// Returns true if this <see cref="Tile"/> is the same as the given <see cref="Tile"/>.
        /// </summary>
        public bool Equals(Tile other) => other.Vector == Vector;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => Vector.GetHashCode();

        internal Corner GetLowestCorner(IGrid grid)
            => Corners.Select(i => grid.Corners[i]).OrderBy(c => c.Elevation).FirstOrDefault();

        internal void SetPolygon(IGrid grid, Quaternion rotation)
        {
            Polygon = new List<Vector2>();

            for (int k = 0; k < Edges.Length; k++)
            {
                var c = Vector3.Transform(grid.Corners[Corners[k]].Vector, rotation);
                Polygon.Add(new Vector2(c.X, c.Z));
            }
        }

        internal int IndexOfCorner(int cornerIndex) => Array.IndexOf(Corners, cornerIndex);

        internal int IndexOfTile(int tileIndex) => Array.IndexOf(Tiles, tileIndex);

        internal void SetClimate(float bioTemperature, float annualPrecipitation)
        {
            SetHumidityType(annualPrecipitation);

            SetClimateType(bioTemperature);

            SetEcologyType();
        }

        private void SetClimateType(float bioTemperature)
        {
            if (bioTemperature <= Season.freezingPoint + 1.5)
            {
                ClimateType = ClimateType.Polar;
            }
            else if (bioTemperature <= Season.freezingPoint + 3)
            {
                ClimateType = ClimateType.Subpolar;
            }
            else if (bioTemperature <= Season.freezingPoint + 6)
            {
                ClimateType = ClimateType.Boreal;
            }
            else if (bioTemperature <= Season.freezingPoint + 12)
            {
                ClimateType = ClimateType.CoolTemperate;
            }
            else if (bioTemperature <= Season.freezingPoint + 18)
            {
                ClimateType = ClimateType.WarmTemperate;
            }
            else if (bioTemperature <= Season.freezingPoint + 24)
            {
                ClimateType = ClimateType.Subtropical;
            }
            else if (bioTemperature <= Season.freezingPoint + 36)
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

        internal void SetFrictionCoefficient()
            => FrictionCoefficient = Elevation <= 0 ? 0.000025f : Elevation * 6.667e-9f + 0.000025f; // 0.000045 at 3000
    }
}
