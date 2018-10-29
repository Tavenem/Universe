using ExtensionLib;
using System;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.Climate;
using WorldFoundry.CelestialBodies.Planetoids;

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

        /// <summary>
        /// The <see cref="Climate.HumidityType"/> of this <see cref="Tile"/>.
        /// </summary>
        public HumidityType HumidityType { get; internal set; }

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
        /// <para>
        /// The range of proportions of the year that sea ice in present in this <see cref="Tile"/>.
        /// </para>
        /// <para>
        /// Note that the <see cref="FloatRange.Min"/> value will be larger than the <see
        /// cref="FloatRange.Max"/> value in the northern hemisphere (when the range is not a full
        /// 0-1), since ice will form towards the end of a year, and melt after the year begins. In
        /// the southern hemisphere <see cref="FloatRange.Min"/> will be somewhat below 0.5 and <see
        /// cref="FloatRange.Max"/> above 0.5, in the expected order, since ice forms towards the
        /// midpoint of a year.
        /// </para>
        /// </summary>
        public FloatRange SeaIce { get; internal set; }

        /// <summary>
        /// <para>
        /// The range of proportions of the year that snow cover in present in this <see
        /// cref="Tile"/>.
        /// </para>
        /// <para>
        /// Note that the <see cref="FloatRange.Min"/> value will be larger than the <see
        /// cref="FloatRange.Max"/> value in the northern hemisphere (when the range is not a full
        /// 0-1), since snow will fall towards the end of a year, and melt after the year begins. In
        /// the southern hemisphere <see cref="FloatRange.Min"/> will be somewhat below 0.5 and <see
        /// cref="FloatRange.Max"/> above 0.5, in the expected order, since snow falls towards the
        /// midpoint of a year.
        /// </para>
        /// </summary>
        public FloatRange SnowCover { get; internal set; }

        /// <summary>
        /// The average annual amount of snow which falls in this <see cref="Tile"/>, in mm.
        /// </summary>
        public float SnowFall { get; set; }

        /// <summary>
        /// The range of temperatures in this <see cref="Tile"/>, in K.
        /// </summary>
        public FloatRange Temperature { get; internal set; }

        /// <summary>
        /// The indexes of the <see cref="Tile"/>s to which this one is connected.
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

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        /// <param name="index">The index of the <see cref="Tile"/>.</param>
        internal Tile(int index)
        {
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

        internal int IndexOfCorner(int cornerIndex) => Array.IndexOf(Corners, cornerIndex);

        internal int IndexOfTile(int tileIndex) => Array.IndexOf(Tiles, tileIndex);
    }
}
