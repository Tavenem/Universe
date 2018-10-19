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
        private const int AirCellLayerHeight = 2000;
        private const double SeaIcePerSecond = 1.53935e-4;
        private const float SnowToRainRatio = 13;
        private const double PrecipitationTempScale = 0.027;

        /// <summary>
        /// Hadley values are a pure function of latitude, and do not vary with any property of the
        /// planet, atmosphere, or season. Since the calculation is relatively expensive, retrieved
        /// values can be stored for the lifetime of the program for future retrieval for the same
        /// (or very similar) location.
        /// </summary>
        private static readonly Dictionary<double, double> HadleyValues = new Dictionary<double, double>();

        private static readonly double SaltWaterMeltingPointOffset = Chemical.Water.MeltingPoint - Chemical.Water_Salt.MeltingPoint;

        private readonly double[] _airHeights;
        private readonly int[] _airCellLayers;
        private readonly double[] _latitudes;
        private readonly int _seed;

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

        private FastNoise _noise;
        private FastNoise Noise => _noise ?? (_noise = new FastNoise(_seed, 10, FastNoise.NoiseType.CubicFractal, octaves: 4));

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/>.
        /// </summary>
        public Season() => _seed = Randomizer.Instance.NextInclusiveMaxValue();

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
        internal Season(TerrestrialPlanet planet, int index, int amount, double trueAnomaly, Season previous = null) : this()
        {
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
            var lowTemp = Chemical.Water.MeltingPoint - 8;

            // Water in atmosphere relative to Earth.
            var wetness = Planet.Atmosphere.Wetness;

            for (var i = 0; i < Planet.Grid.Tiles.Length; i++)
            {
                var t = Planet.Grid.Tiles[i];
                var tc = TileClimates[i];

                // Range of ~-0.2-0.2 adjusted to ~0-1, and cut off below 0.
                var relativeHumidity = ((Noise.GetNoise(t.Vector.X, t.Vector.Y, t.Vector.Z) * 5) + 1) / 2;

                // Hadley cells scale by ~1.3 near the equator, ~0.2 ±15º lat, ~1 ±60º lat, and ~0.2
                // ±90º lat; this creates the ITCZ, the subtropical deserts, the temperate zone, and
                // the polar deserts.
                var roundedAbsLatitude = Math.Abs(Math.Round(_latitudes[i], 3));
                if (!HadleyValues.TryGetValue(roundedAbsLatitude, out var hadleyValue))
                {
                    hadleyValue = (Math.Cos(MathConstants.TwoPI * Math.Sqrt(roundedAbsLatitude)) / 2) - (roundedAbsLatitude / Math.PI) + MathConstants.QuarterPI;
                    HadleyValues.Add(roundedAbsLatitude, hadleyValue);
                }
                relativeHumidity *= hadleyValue;

                // The strength of global winds (due to the Coriolis effect) can adjust the value up
                // by as much as the local Hadley value, to as much as 1, but does not adjust down.
                relativeHumidity += ((1 - relativeHumidity) * t.WindFactor).Clamp(0, hadleyValue);

                // In a band ±8K around freezing, the value is scaled down; below that range it is
                // cut off completely; above it is unchanged.
                relativeHumidity *= ((tc.Temperature - lowTemp) / 16).Clamp(0, 1);

                if (relativeHumidity.IsZero())
                {
                    continue;
                }

                var stdMixingRatio = Atmosphere.GetSaturationMixingRatio(
                    Atmosphere.GetSaturationVaporPressure(tc.Temperature),
                    TerrestrialPlanetParams.DefaultAtmosphericPressure);
                var mixingRatio = Atmosphere.GetSaturationMixingRatio(
                    Atmosphere.GetSaturationVaporPressure(tc.Temperature * Planet.Atmosphere.Exner(tc.AtmosphericPressure)),
                    tc.AtmosphericPressure);
                var mixingRatioRatio = mixingRatio / stdMixingRatio;

                var airMassRatio = Planet.Atmosphere.AtmosphericHeight / 103493;

                tc.Precipitation = (float)(10000 * relativeHumidity * airMassRatio * Planet.Atmosphere.DensityRatio * mixingRatioRatio * ProportionOfYear);

                if (tc.Temperature <= Chemical.Water.MeltingPoint)
                {
                    tc.SnowFall = tc.Precipitation / SnowToRainRatio;
                }
            }
        }

        private void SetPrecipitation_old()
        {
            var mp = Chemical.Water.MeltingPoint;

            for (var i = 0; i < Planet.Grid.Tiles.Length; i++)
            {
                var t = Planet.Grid.Tiles[i];
                var tc = TileClimates[i];

                var relTemp = PrecipitationTempScale * (tc.Temperature - mp);
                relTemp += (1 - relTemp) / 1.5; // less impact on extrema, particularly minima
                if (tc.SeaIce > 0)
                {
                    relTemp /= 2;
                }

                var warmAir = relTemp * t.WindFactor;
                if (TMath.IsZero(warmAir))
                {
                    continue;
                }

                var roundedLatitude = Math.Round(_latitudes[i], 3);
                if (!HadleyValues.TryGetValue(roundedLatitude, out var hadleyLift))
                {
                    hadleyLift = ((Math.Cos(6 * roundedLatitude) * (1 - (Math.Abs(roundedLatitude) / MathConstants.TwoPI))) + 1) / 2; // less dropoff with latitude
                    HadleyValues.Add(roundedLatitude, hadleyLift);
                }

                var relativeHumidity = warmAir;
                for (var j = 0; j < _airCellLayers[i]; j++)
                {
                    relativeHumidity += warmAir;
                    warmAir *= hadleyLift;
                }

                var mixingRatio = relativeHumidity > 1
                    ? relativeHumidity * Atmosphere.GetSaturationMixingRatio(
                        Atmosphere.GetSaturationVaporPressure(tc.Temperature * Planet.Atmosphere.Exner(tc.AtmosphericPressure)),
                        tc.AtmosphericPressure)
                    : 0;

                if (!TMath.IsZero(mixingRatio))
                {
                    tc.Precipitation = (float)(Atmosphere.GetAtmosphericDensity(tc.Temperature, tc.AtmosphericPressure) * AirCellLayerHeight * _airCellLayers[i] * mixingRatio * ProportionOfYear);

                    if (tc.Temperature <= Chemical.Water.MeltingPoint)
                    {
                        tc.SnowFall = tc.Precipitation / SnowToRainRatio;
                    }
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

        private void SetTemperature_old(double trueAnomaly)
        {
            var latitudeTemperatures = new Dictionary<double, double>();
            var elevationTemperatures = new Dictionary<(double, double), double>();
            var elevationPressures = new Dictionary<(double, double), double>();
            var zeroSVPIndexes = new Dictionary<(double, double), int>();
            var exners = new Dictionary<double, double>();
            var saturationVaporPressures = new Dictionary<(double, double), double>();

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

                var layers = (int)Math.Max(1, Math.Floor((Planet.Atmosphere.AtmosphericHeight - elevation) / AirCellLayerHeight));
                var nearestLayerHeight = Math.Round(elevation / AirCellLayerHeight) * AirCellLayerHeight;
                if (!elevationPressures.TryGetValue((roundedTemperature, roundedElevation), out var pressureAtLayerHeight))
                {
                    pressureAtLayerHeight = Planet.GetAtmosphericPressureFromTempAndElevation(roundedTemperature, nearestLayerHeight);
                    elevationPressures.Add((roundedTemperature, nearestLayerHeight), pressureAtLayerHeight);
                }
                if (!zeroSVPIndexes.TryGetValue((nearestLayerHeight, roundedTemperature), out var layerIndex))
                {
                    layerIndex = (int)Math.Ceiling(Planet.GetHeightForTemperature(Atmosphere.TemperatureAtNearlyZeroSaturationVaporPressure, roundedTemperature, nearestLayerHeight) / AirCellLayerHeight);
                    zeroSVPIndexes.Add((nearestLayerHeight, roundedTemperature), layerIndex);
                }
                _airCellLayers[i] = Math.Max(1, Math.Min(layers, layerIndex));

                var saturationVaporPressure = 1.0;
                do
                {
                    var height = elevation + (AirCellLayerHeight * _airCellLayers[i]);
                    if (!elevationTemperatures.TryGetValue((roundedTemperature, height), out var tempAtHeight))
                    {
                        tempAtHeight = Planet.GetTemperatureAtElevation(roundedTemperature, height);
                        elevationTemperatures.Add((roundedTemperature, height), tempAtHeight);
                    }

                    if (!saturationVaporPressures.TryGetValue((tempAtHeight, pressureAtLayerHeight), out saturationVaporPressure))
                    {
                        if (!exners.TryGetValue(pressureAtLayerHeight, out var exner))
                        {
                            exner = Planet.Atmosphere.Exner(pressureAtLayerHeight);
                            exners.Add(pressureAtLayerHeight, exner);
                        }

                        saturationVaporPressure = Atmosphere.GetSaturationVaporPressure(tempAtHeight * exner);
                        saturationVaporPressures.Add((tempAtHeight, pressureAtLayerHeight), saturationVaporPressure);
                    }

                    if (saturationVaporPressure.IsZero())
                    {
                        break;
                    }
                    else
                    {
                        _airCellLayers[i]++;
                    }
                } while (_airCellLayers[i] < layers);
            }
        }
    }
}
