using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using WorldFoundry.Climate;
using WorldFoundry.Extensions;

namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents a tile on a <see cref="WorldGrids.WorldGrid"/>.
    /// </summary>
    public class Tile : IIndexedItem
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
        /// The Coriolis coefficient at this <see cref="Tile"/>'s <see cref="Latitude"/>.
        /// </summary>
        internal float CoriolisCoefficient { get; set; }

        /// <summary>
        /// The index of the first <see cref="Corner"/> to which this <see cref="Tile"/> is connected.
        /// </summary>
        public int Corner0 { get; private set; } = -1;

        /// <summary>
        /// The index of the second <see cref="Corner"/> to which this <see cref="Tile"/> is connected.
        /// </summary>
        public int Corner1 { get; private set; } = -1;

        /// <summary>
        /// The index of the third <see cref="Corner"/> to which this <see cref="Tile"/> is connected.
        /// </summary>
        public int Corner2 { get; private set; } = -1;

        /// <summary>
        /// The index of the fourth <see cref="Corner"/> to which this <see cref="Tile"/> is connected.
        /// </summary>
        public int Corner3 { get; private set; } = -1;

        /// <summary>
        /// The index of the fifth <see cref="Corner"/> to which this <see cref="Tile"/> is connected.
        /// </summary>
        public int Corner4 { get; private set; } = -1;

        /// <summary>
        /// The index of the sixth <see cref="Corner"/> to which this <see cref="Tile"/> is connected (if it has six sides).
        /// </summary>
        public int Corner5 { get; private set; } = -1;

        /// <summary>
        /// The <see cref="Climate.EcologyType"/> of this <see cref="Tile"/>.
        /// </summary>
        public EcologyType EcologyType { get; internal set; }

        /// <summary>
        /// The number of sides possessed by this <see cref="Tile"/>.
        /// </summary>
        public int EdgeCount { get; }

        /// <summary>
        /// The index of the first <see cref="Edge"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Edge0 { get; private set; } = -1;

        /// <summary>
        /// The index of the second <see cref="Edge"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Edge1 { get; private set; } = -1;

        /// <summary>
        /// The index of the third <see cref="Edge"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Edge2 { get; private set; } = -1;

        /// <summary>
        /// The index of the fourth <see cref="Edge"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Edge3 { get; private set; } = -1;

        /// <summary>
        /// The index of the fifth <see cref="Edge"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Edge4 { get; private set; } = -1;

        /// <summary>
        /// The index of the sixth <see cref="Edge"/> to which this <see cref="Corner"/> is connected (if it has six sides).
        /// </summary>
        public int Edge5 { get; private set; } = -1;

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
        /// The longitude of this <see cref="Tile"/>, as an angle in radians from an arbitrary meridian.
        /// </summary>
        public float Longitude { get; internal set; }

        internal float North { get; set; }

        private List<Vector2> _polygon;
        [NotMapped]
        internal List<Vector2> Polygon
        {
            get
            {
                if (_polygon == null)
                {
                    SetPolygonList();
                }
                return _polygon;
            }
            set => _polygon = value;
        }

        internal float Polygon0X { get; private set; }

        internal float Polygon0Y { get; private set; }

        internal float Polygon1X { get; private set; }

        internal float Polygon1Y { get; private set; }

        internal float Polygon2X { get; private set; }

        internal float Polygon2Y { get; private set; }

        internal float Polygon3X { get; private set; }

        internal float Polygon3Y { get; private set; }

        internal float Polygon4X { get; private set; }

        internal float Polygon4Y { get; private set; }

        internal float Polygon5X { get; private set; }

        internal float Polygon5Y { get; private set; }

        /// <summary>
        /// The <see cref="WorldFoundry.TerrainType"/> of this <see cref="Tile"/>.
        /// </summary>
        public TerrainType TerrainType { get; internal set; } = TerrainType.Land;

        /// <summary>
        /// The index of the first <see cref="Tile"/> to which this one is connected.
        /// </summary>
        public int Tile0 { get; private set; } = -1;

        /// <summary>
        /// The index of the second <see cref="Tile"/> to which this one is connected.
        /// </summary>
        public int Tile1 { get; private set; } = -1;

        /// <summary>
        /// The index of the third <see cref="Tile"/> to which this one is connected.
        /// </summary>
        public int Tile2 { get; private set; } = -1;

        /// <summary>
        /// The index of the fourth <see cref="Tile"/> to which this one is connected.
        /// </summary>
        public int Tile3 { get; private set; } = -1;

        /// <summary>
        /// The index of the fifth <see cref="Tile"/> to which this one is connected.
        /// </summary>
        public int Tile4 { get; private set; } = -1;

        /// <summary>
        /// The index of the sixth <see cref="Tile"/> to which this one is connected (if it has six sides).
        /// </summary>
        public int Tile5 { get; private set; } = -1;

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Tile"/>.
        /// </summary>
        [NotMapped]
        public Vector3 Vector
        {
            get => new Vector3(VectorX, VectorY, VectorZ);
            set
            {
                VectorX = value.X;
                VectorY = value.Y;
                VectorZ = value.Z;
            }
        }

        /// <summary>
        /// The X component of the vector which defines the position of this <see cref="Tile"/>.
        /// </summary>
        protected float VectorX { get; private set; }

        /// <summary>
        /// The Y component of the vector which defines the position of this <see cref="Tile"/>.
        /// </summary>
        protected float VectorY { get; private set; }

        /// <summary>
        /// The Z component of the vector which defines the position of this <see cref="Tile"/>.
        /// </summary>
        protected float VectorZ { get; private set; }

        /// <summary>
        /// The <see cref="WorldGrids.WorldGrid"/> of which this <see cref="Tile"/> forms a part.
        /// </summary>
        internal WorldGrid WorldGrid { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        private Tile() { }

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        internal Tile(WorldGrid grid, int id, int edgeCount)
        {
            Index = id;
            EdgeCount = edgeCount;
        }

        /// <summary>
        /// Gets the index of the <see cref="Corner"/> at the given index in this <see
        /// cref="Tile"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// An index to this <see cref="Tile"/>'s collection of <see cref="Corner"/>s.
        /// </param>
        /// <returns>The index of the <see cref="Corner"/> at the given index.</returns>
        public int GetCorner(int index)
        {
            if (index == 0)
            {
                return Corner0;
            }
            if (index == 1)
            {
                return Corner1;
            }
            if (index == 2)
            {
                return Corner2;
            }
            if (index == 3)
            {
                return Corner3;
            }
            if (index == 4)
            {
                return Corner4;
            }
            if (EdgeCount == 6 && index == 5)
            {
                return Corner5;
            }
            return -1;
        }

        /// <summary>
        /// Enumerates all the <see cref="Corner"/>s to which this <see cref="Tile"/> is connected.
        /// </summary>
        public IEnumerable<int> GetCorners()
        {
            var corners = new List<int> { Corner0, Corner1, Corner2, Corner3, Corner4 };
            if (EdgeCount == 6)
            {
                corners.Add(Corner5);
            }
            return corners.AsEnumerable();
        }

        /// <summary>
        /// Gets the index of the <see cref="Edge"/> at the given index in this <see
        /// cref="Tile"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// An index to this <see cref="Tile"/>'s collection of <see cref="Edge"/>s.
        /// </param>
        /// <returns>The index of the <see cref="Edge"/> at the given index.</returns>
        public int GetEdge(int index)
        {
            if (index == 0)
            {
                return Edge0;
            }
            if (index == 1)
            {
                return Edge1;
            }
            if (index == 2)
            {
                return Edge2;
            }
            if (index == 3)
            {
                return Edge3;
            }
            if (index == 4)
            {
                return Edge4;
            }
            if (EdgeCount == 6 && index == 5)
            {
                return Edge5;
            }
            return -1;
        }

        /// <summary>
        /// Enumerates all the <see cref="Edge"/>s to which this <see cref="Tile"/> is connected.
        /// </summary>
        public IEnumerable<int> GetEdges()
        {
            var edges = new List<int> { Edge0, Edge1, Edge2, Edge3, Edge4 };
            if (EdgeCount == 6)
            {
                edges.Add(Edge5);
            }
            return edges.AsEnumerable();
        }

        internal Corner GetLowestCorner(WorldGrid grid)
            => GetCorners().Select(i => grid.GetCorner(i)).OrderBy(c => c.Elevation).FirstOrDefault();

        /// <summary>
        /// Gets the index of the <see cref="Tile"/> at the given index in this <see
        /// cref="Tile"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// An index to this <see cref="Tile"/>'s collection of <see cref="Tile"/>s.
        /// </param>
        /// <returns>The index of the <see cref="Edge"/> at the given index.</returns>
        public int GetTile(int index)
        {
            if (index == 0)
            {
                return Tile0;
            }
            if (index == 1)
            {
                return Tile1;
            }
            if (index == 2)
            {
                return Tile2;
            }
            if (index == 3)
            {
                return Tile3;
            }
            if (index == 4)
            {
                return Tile4;
            }
            if (EdgeCount == 6 && index == 5)
            {
                return Tile5;
            }
            return -1;
        }

        /// <summary>
        /// Enumerates all the <see cref="Tile"/>s to which this one is connected.
        /// </summary>
        public IEnumerable<int> GetTiles()
        {
            var corners = new List<int> { Tile0, Tile1, Tile2, Tile3, Tile4 };
            if (EdgeCount == 6)
            {
                corners.Add(Tile5);
            }
            return corners.AsEnumerable();
        }

        internal void SetPolygon(Quaternion rotation)
        {
            _polygon = new List<Vector2>();

            for (int k = 0; k < EdgeCount; k++)
            {
                var c = Vector3.Transform(WorldGrid.GetCorner(GetCorner(k)).Vector, rotation);
                _polygon.Add(new Vector2(c.X, c.Z));
            }

            Polygon0X = Polygon[0].X;
            Polygon0Y = Polygon[0].Y;
            Polygon1X = Polygon[1].X;
            Polygon1Y = Polygon[1].Y;
            Polygon2X = Polygon[2].X;
            Polygon2Y = Polygon[2].Y;
            Polygon3X = Polygon[3].X;
            Polygon3Y = Polygon[3].Y;
            Polygon4X = Polygon[4].X;
            Polygon4Y = Polygon[4].Y;
            if (EdgeCount == 6)
            {
                Polygon5X = Polygon[5].X;
                Polygon5Y = Polygon[5].Y;
            }
        }

        internal void SetPolygonList()
        {
            _polygon = new List<Vector2>
            {
                new Vector2(Polygon0X, Polygon0Y),
                new Vector2(Polygon1X, Polygon1Y),
                new Vector2(Polygon2X, Polygon2Y),
                new Vector2(Polygon3X, Polygon3Y),
                new Vector2(Polygon4X, Polygon4Y),
            };
            if (EdgeCount == 6)
            {
                _polygon.Add(new Vector2(Polygon5X, Polygon5Y));
            }
        }

        internal int IndexOfCorner(int cornerIndex)
        {
            if (Corner0 == cornerIndex)
            {
                return 0;
            }
            if (Corner1 == cornerIndex)
            {
                return 1;
            }
            if (Corner2 == cornerIndex)
            {
                return 2;
            }
            if (Corner3 == cornerIndex)
            {
                return 3;
            }
            if (Corner4 == cornerIndex)
            {
                return 4;
            }
            if (EdgeCount == 6 && Corner5 == cornerIndex)
            {
                return 5;
            }
            return -1;
        }

        internal int IndexOfTile(int tileIndex)
        {
            if (Tile0 == tileIndex)
            {
                return 0;
            }
            if (Tile1 == tileIndex)
            {
                return 1;
            }
            if (Tile2 == tileIndex)
            {
                return 2;
            }
            if (Tile3 == tileIndex)
            {
                return 3;
            }
            if (Tile4 == tileIndex)
            {
                return 4;
            }
            if (EdgeCount == 6 && Tile5 == tileIndex)
            {
                return 5;
            }
            return -1;
        }

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

        /// <summary>
        /// Sets the value of the <see cref="Corner"/> index at the given index to this <see
        /// cref="Tile"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// The index to this <see cref="Tile"/>'s collection of <see cref="Corner"/>s to set.
        /// </param>
        /// <param name="value">The value to store in the given index.</param>
        public void SetCorner(int index, int value)
        {
            if (index == 0)
            {
                Corner0 = value;
            }
            if (index == 1)
            {
                Corner1 = value;
            }
            if (index == 2)
            {
                Corner2 = value;
            }
            if (index == 3)
            {
                Corner3 = value;
            }
            if (index == 4)
            {
                Corner4 = value;
            }
            if (EdgeCount == 6 && index == 5)
            {
                Corner5 = value;
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

        /// <summary>
        /// Sets the value of the <see cref="Edge"/> index at the given index to this <see
        /// cref="Tile"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// The index to this <see cref="Tile"/>'s collection of <see cref="Edge"/>s to set.
        /// </param>
        /// <param name="value">The value to store in the given index.</param>
        public void SetEdge(int index, int value)
        {
            if (index == 0)
            {
                Edge0 = value;
            }
            if (index == 1)
            {
                Edge1 = value;
            }
            if (index == 2)
            {
                Edge2 = value;
            }
            if (index == 3)
            {
                Edge3 = value;
            }
            if (index == 4)
            {
                Edge4 = value;
            }
            if (EdgeCount == 6 && index == 5)
            {
                Edge5 = value;
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

        /// <summary>
        /// Sets the value of the <see cref="Tile"/> index at the given index to this <see
        /// cref="Tile"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// The index to this <see cref="Tile"/>'s collection of <see cref="Tile"/>s to set.
        /// </param>
        /// <param name="value">The value to store in the given index.</param>
        public void SetTile(int index, int value)
        {
            if (index == 0)
            {
                Tile0 = value;
            }
            if (index == 1)
            {
                Tile1 = value;
            }
            if (index == 2)
            {
                Tile2 = value;
            }
            if (index == 3)
            {
                Tile3 = value;
            }
            if (index == 4)
            {
                Tile4 = value;
            }
            if (EdgeCount == 6 && index == 5)
            {
                Tile5 = value;
            }
        }
    }
}
