using MathAndScience;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents an indeterminate period of time during which the climate on a <see cref="Planet"/>
    /// is reported as an average.
    /// </summary>
    public class Season
    {
        private const double SeaIcePerSecond = 1.53935e-4;
        private const float SnowToRainRatio = 13;

        /// <summary>
        /// Hadley values are a pure function of latitude, and do not vary with any property of the
        /// planet, atmosphere, or season. Since the calculation is relatively expensive, retrieved
        /// values can be stored for the lifetime of the program for future retrieval for the same
        /// (or very similar) location.
        /// </summary>
        private static readonly Dictionary<double, double> HadleyValues = new Dictionary<double, double>();

        private static readonly double LowTemp = Chemical.Water.MeltingPoint - 8;
        private static readonly double SaltWaterMeltingPointOffset = Chemical.Water.MeltingPoint - Chemical.Water_Salt.MeltingPoint;

        private readonly double[] _airHeights;
        private readonly int[] _airCellLayers;
        private readonly double[] _latitudes;
        private readonly int _seed1;
        private readonly int _seed2;

        /// <summary>
        /// Gets the duration of this <see cref="Season"/>, in seconds.
        /// </summary>
        public double Duration { get; }

        /// <summary>
        /// The volume of water flowing in the river along each <see cref="Edge"/> during this <see
        /// cref="Season"/>, in m³/s.
        /// </summary>
        public float[] EdgeRiverFlows { get; }

        /// <summary>
        /// Indicates the proportion of the current year which has elapsed at the start of this <see
        /// cref="Season"/>.
        /// </summary>
        public double ElapsedYearToDate { get; }

        /// <summary>
        /// The index of this item.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The <see cref="TerrestrialPlanet"/> this <see cref="Season"/> describes.
        /// </summary>
        public TerrestrialPlanet Planet { get; }

        /// <summary>
        /// Indicates the proportion of one year represented by this <see cref="Season"/>.
        /// </summary>
        public double ProportionOfYear { get; }

        /// <summary>
        /// The climate of each <see cref="Tile"/> during this <see cref="Season"/>.
        /// </summary>
        public TileClimate[] TileClimates { get; }

        private FastNoise _noise1;
        private FastNoise Noise1 => _noise1 ?? (_noise1 = new FastNoise(_seed1, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 3));

        private FastNoise _noise2;
        private FastNoise Noise2 => _noise2 ?? (_noise2 = new FastNoise(_seed2, 0.004, FastNoise.NoiseType.Simplex));

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/>.
        /// </summary>
        public Season()
        {
            _seed1 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed2 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/> with the given values.
        /// </summary>
        /// <param name="planet">The <see cref="TerrestrialPlanet"/> on which this <see
        /// cref="Season"/> occurs.</param>
        /// <param name="index">The index of this <see cref="Season"/> among the collection of a
        /// year.</param>
        /// <param name="amount">The number of <see cref="Season"/>s being generated for the current
        /// year.</param>
        /// <param name="trueAnomaly">The true anomaly of the <paramref name="planet"/>'s orbit at
        /// the time of this <see cref="Season"/> (zero if not in orbit).</param>
        /// <param name="previous">The <see cref="Season"/> which immediately preceded this
        /// one.</param>
        internal Season(TerrestrialPlanet planet, int index, int amount, double trueAnomaly, Season previous = null)
        {
            _seed1 = planet._seed4;
            _seed2 = planet._seed5;

            Index = index;
            Planet = planet;

            Duration = planet.Orbit.Period / amount;
            ProportionOfYear = 1.0 / amount;
            ElapsedYearToDate = ProportionOfYear * index;

            _latitudes = new double[planet.Grid.Tiles.Length];
            var tropicalEquator = -planet.AxialTilt * Math.Cos(MathConstants.TwoPI * ElapsedYearToDate) * (2.0 / 3.0);

            TileClimates = new TileClimate[planet.Grid.Tiles.Length];
            for (var j = 0; j < planet.Grid.Tiles.Length; j++)
            {
                var t = planet.Grid.Tiles[j];

                TileClimates[j] = new TileClimate();
                var seasonalLatitude = t.Latitude - tropicalEquator;
                if (seasonalLatitude > MathConstants.HalfPI)
                {
                    seasonalLatitude = MathConstants.HalfPI - (seasonalLatitude - MathConstants.HalfPI);
                }
                else if (seasonalLatitude < -MathConstants.HalfPI)
                {
                    seasonalLatitude = -MathConstants.HalfPI - (seasonalLatitude + MathConstants.HalfPI);
                }
                _latitudes[j] = seasonalLatitude;
            }
            EdgeRiverFlows = new float[planet.Grid.Edges.Length];

            _airHeights = new double[planet.Grid.Tiles.Length];
            _airCellLayers = new int[planet.Grid.Tiles.Length];

            SetTemperature(trueAnomaly);
            SetSeaIce(previous);
            SetPrecipitation();

            _airHeights = null;
            _airCellLayers = null;
            _latitudes = null;

            SetGroundWater(previous);
            SetRiverFlow();
        }

        private static double GetSnowMelt(double temperature, double time)
            => 2.44e-6 * (temperature - Chemical.Water.MeltingPoint) * time;

        private void SetGroundWater(Season previous)
        {
            for (var i = 0; i < Planet.Grid.Tiles.Length; i++)
            {
                var t = Planet.Grid.Tiles[i];
                if (t.TerrainType != TerrainType.Water)
                {
                    var tc = TileClimates[i];
                    double previousSnow = (previous?.TileClimates[i].SnowCover ?? 0) / SnowToRainRatio;
                    double newSnow = tc.SnowFall > 0 ? tc.Precipitation : 0;

                    var melt = 0.0;
                    if (tc.Temperature > Chemical.Water.MeltingPoint
                        && (previousSnow > 0 || newSnow > 0))
                    {
                        var meltPotential = GetSnowMelt(tc.Temperature, Duration);

                        melt = Math.Min(meltPotential, previousSnow);
                        meltPotential -= melt;
                        previousSnow -= melt;

                        melt = Math.Min(meltPotential, newSnow);
                        newSnow -= melt;
                    }

                    tc.SnowCover = (float)Math.Max(previousSnow, newSnow) * SnowToRainRatio;

                    // rolling average, weighted to the heavier, roughly models infiltration and seepage
                    // multiplied by a factor of 4 to roughly model groundwater flow
                    var runoff = melt * 0.004 * t.Area / Duration;
                    var previousRunoff = previous?.TileClimates[i].Runoff ?? runoff;
                    tc.Runoff = (float)((previousRunoff > runoff ? ((previousRunoff * 3) + runoff) : ((runoff * 3) + previousRunoff)) / 4);
                }
            }
        }

        private void SetPrecipitation()
        {
            var avgPrecipitation = 990 * ProportionOfYear;

            for (var i = 0; i < Planet.Grid.Tiles.Length; i++)
            {
                var t = Planet.Grid.Tiles[i];
                var tc = TileClimates[i];

                var v = t.Vector * 100;

                // Noise map with smooth, broad areas. Random range ~-1-1.
                var r1 = Noise2.GetNoise(v.X, v.Y, v.Z);

                // More detailed noise map. Random range of ~-1-1 adjusted to ~0.8-1.
                var r2 = Math.Abs((Noise1.GetNoise(v.X, v.Y, v.Z) * 0.1) + 0.9);

                // Combined map is noise with broad similarity over regions, and minor local
                // diversity, with range of ~-1-1.
                var r = r1 * r2;

                // Hadley cells scale by 1.5 around the equator, ~0.1 ±15º lat, ~0.2 ±40º lat, and ~0
                // ±75º lat; this creates the ITCZ, the subtropical deserts, the temperate zone, and
                // the polar deserts.
                var roundedAbsLatitude = Math.Round(Math.Abs(Math.Max(0, _latitudes[i] - (Math.PI / 36))), 3);
                if (!HadleyValues.TryGetValue(roundedAbsLatitude, out var hadleyValue))
                {
                    hadleyValue = (Math.Cos(MathConstants.TwoPI * Math.Sqrt(roundedAbsLatitude)) / ((8 * roundedAbsLatitude) + 1)) - (roundedAbsLatitude / Math.PI) + 0.5;
                    HadleyValues.Add(roundedAbsLatitude, hadleyValue);
                }

                // Relative humidity is the Hadley cell value added to the random value, and cut off
                // below 0. Range 0-~2.5.
                var relativeHumidity = Math.Max(0, r + hadleyValue);

                // In a band ±8K around freezing, the value is scaled down; below that range it is
                // cut off completely; above it is unchanged.
                relativeHumidity *= ((tc.Temperature - LowTemp) / 16).Clamp(0, 1);

                if (relativeHumidity <= 0)
                {
                    continue;
                }

                // Scale by distance from target.
                var factor = (relativeHumidity * ((relativeHumidity * 0.1) - 0.15)) + 1;
                factor *= factor;

                tc.Precipitation = (float)(avgPrecipitation * relativeHumidity * factor * Planet.Atmosphere.PrecipitationFactor);
                if (tc.Temperature <= Chemical.Water.MeltingPoint)
                {
                    tc.SnowFall = tc.Precipitation / SnowToRainRatio;
                }
            }
        }

        private void SetRiverFlow()
        {
            var cornerFlows = new Dictionary<int, float>();
            var endpoints = new SortedSet<Corner>(Comparer<Corner>.Create((c1, c2) => c2.Elevation.CompareTo(c1.Elevation) * -1));

            for (var i = 0; i < Planet.Grid.Tiles.Length; i++)
            {
                if (TileClimates[i].Runoff > 0)
                {
                    var lowest = Planet.Grid.Tiles[i].GetLowestCorner(Planet.Grid);

                    cornerFlows[lowest.Index] = TileClimates[i].Runoff;

                    endpoints.Add(lowest);
                }
            }
            Corner prev = null;
            while (endpoints.Count > 0)
            {
                var c = endpoints.First();
                endpoints.Remove(c);

                var index = Array.Find(Planet.Grid.Edges, x => x.RiverSource == c.Index)?.RiverDirection ?? -1;
                var next = index == -1 ? null : Planet.Grid.Corners[index];
                if (next == null)
                {
                    next = c.GetLowestCorner(Planet.Grid, true);
                    if (next.Elevation > c.Elevation)
                    {
                        next = c.GetLowestCorner(Planet.Grid, false);
                    }
                    if (next.Elevation > c.Elevation && (prev?.LakeDepth ?? 0) == 0)
                    {
                        c.LakeDepth = Math.Min(
                            c.Corners.Min(x => Planet.Grid.Corners[x].Elevation),
                            c.Tiles.Min(x => Planet.Grid.Tiles[x].Elevation)) - c.Elevation;
                    }
                }

                if ((prev?.LakeDepth ?? 0) == 0 || c.LakeDepth + c.Elevation >= next.Elevation)
                {
                    var edgeIndex = c.Edges[c.IndexOfCorner(next.Index)];
                    var edge = Planet.Grid.Edges[edgeIndex];
                    edge.RiverSource = c.Index;

                    cornerFlows.TryGetValue(c.Index, out var flow);
                    flow += c.Edges
                        .Where(e => Planet.Grid.Edges[e].RiverDirection == c.Index)
                        .Sum(e => EdgeRiverFlows[e]);
                    EdgeRiverFlows[edgeIndex] = flow;

                    var rc = next;
                    var nextRiverEdge = -1;
                    do
                    {
                        var nextRiverEdges = rc.Edges.Where(e => Planet.Grid.Edges[e].RiverSource == rc.Index).ToList();
                        if (nextRiverEdges.Count > 0)
                        {
                            nextRiverEdge = nextRiverEdges[0];
                            EdgeRiverFlows[nextRiverEdge] += flow;
                            rc = Planet.Grid.Corners[Planet.Grid.Edges[nextRiverEdge].RiverDirection];
                        }
                        else
                        {
                            nextRiverEdge = -1;
                        }
                    } while (nextRiverEdge != -1);

                    if (next.TerrainType == TerrainType.Land
                        && next.LakeDepth == 0
                        && !endpoints.Contains(next)
                        && !next.Edges.Any(e => Planet.Grid.Edges[e].RiverSource == next.Index))
                    {
                        endpoints.Add(next);
                    }
                }

                prev = c;
            }
        }

        private void SetSeaIce(Season previous)
        {
            for (var i = 0; i < Planet.Grid.Tiles.Length; i++)
            {
                var tc = TileClimates[i];
                if (Planet.Grid.Tiles[i].TerrainType.HasFlag(TerrainType.Water))
                {
                    double previousIce = previous?.TileClimates[i].SeaIce ?? 0;
                    var ice = 0.0;
                    if (tc.Temperature < Chemical.Water_Salt.MeltingPoint)
                    {
                        ice = SeaIcePerSecond * Duration * Math.Pow(Chemical.Water_Salt.MeltingPoint - tc.Temperature, 0.58);
                    }
                    else if (previousIce > 0)
                    {
                        previousIce -= Math.Min(GetSnowMelt(tc.Temperature + SaltWaterMeltingPointOffset, Duration), previousIce);
                    }
                    tc.SeaIce = (float)Math.Max(previousIce, ice);
                }
            }
        }

        private void SetTemperature(double trueAnomaly)
        {
            var latitudeTemperatures = new Dictionary<double, double>();
            var elevationTemperatures = new Dictionary<(double, double), double>();
            var elevationPressures = new Dictionary<(double, double), double>();

            for (var i = 0; i < Planet.Grid.Tiles.Length; i++)
            {
                var t = Planet.Grid.Tiles[i];
                var tc = TileClimates[i];
                var latitude = Math.Abs(_latitudes[i]);
                if (!latitudeTemperatures.TryGetValue(latitude, out var surfaceTemp))
                {
                    surfaceTemp = Planet.GetSurfaceTemperatureAtTrueAnomaly(trueAnomaly, latitude);
                    latitudeTemperatures.Add(latitude, surfaceTemp);
                }

                var elevation = Math.Max(0, t.Elevation);
                var roundedElevation = Math.Round(elevation / 100) * 100;
                if (!elevationTemperatures.TryGetValue((surfaceTemp, roundedElevation), out var tempAtElevation))
                {
                    tempAtElevation = Planet.GetTemperatureAtElevation(surfaceTemp, roundedElevation);
                    elevationTemperatures.Add((surfaceTemp, roundedElevation), tempAtElevation);
                }
                tc.Temperature = (float)tempAtElevation;

                var roundedTemperature = Math.Round(tempAtElevation / 3) * 3;
                if (!elevationPressures.TryGetValue((roundedTemperature, roundedElevation), out var pressureAtElevation))
                {
                    pressureAtElevation = Planet.GetAtmosphericPressureFromTempAndElevation(roundedTemperature, roundedElevation);
                    elevationPressures.Add((roundedTemperature, roundedElevation), pressureAtElevation);
                }
                tc.AtmosphericPressure = (float)pressureAtElevation;
            }
        }
    }
}
