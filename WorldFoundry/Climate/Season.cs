using MathAndScience.MathUtil;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies;
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
        private const double PrecipitationTempScale = 0.027;

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

        /// <summary>
        /// The latitude of the solar equator during this season, as an angle in radians from the (true) equator.
        /// </summary>
        internal double TropicalEquator { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/>.
        /// </summary>
        public Season() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/> with the given values.
        /// </summary>
        /// <param name="planet">The <see cref="TerrestrialPlanet"/> on which this <see
        /// cref="Season"/> occurs.</param>
        /// <param name="index">The index of this <see cref="Season"/> among the collection of a
        /// year.</param>
        /// <param name="amount">The number of <see cref="Season"/>s being generated for the current
        /// year.</param>
        /// <param name="position">The position of <paramref name="planet"/> at the time of this
        /// <see cref="Season"/>.</param>
        /// <param name="previous">The <see cref="Season"/> which immediately preceded this
        /// one.</param>
        internal Season(TerrestrialPlanet planet, int index, int amount, Vector3 position, Season previous = null)
        {
            Index = index;
            Planet = planet;

            Duration = planet.Orbit.Period / amount;
            ProportionOfYear = 1.0 / amount;
            ElapsedYearToDate = ProportionOfYear * index;

            TropicalEquator = planet.AxialTilt * Math.Sin((MathConstants.TwoPI * ElapsedYearToDate) + MathConstants.HalfPI) * (2.0 / 3.0);

            TileClimates = new TileClimate[planet.Topography.Tiles.Length];
            for (var j = 0; j < planet.Topography.Tiles.Length; j++)
            {
                var t = planet.Topography.Tiles[j];

                TileClimates[j] = new TileClimate();
                var seasonalLatitude = t.Latitude - TropicalEquator;
                if (seasonalLatitude > MathConstants.HalfPI)
                {
                    seasonalLatitude = MathConstants.HalfPI - (seasonalLatitude - MathConstants.HalfPI);
                }
                else if (seasonalLatitude < -MathConstants.HalfPI)
                {
                    seasonalLatitude = -MathConstants.HalfPI - (seasonalLatitude + MathConstants.HalfPI);
                }
                TileClimates[j].Latitude = (float)seasonalLatitude;
            }
            EdgeRiverFlows = new float[planet.Topography.Edges.Length];

            SetTemperature(position);
            SetSeaIce(previous);
            SetPrecipitation();
            SetGroundWater(previous);
            SetRiverFlow();
        }

        private static double GetSnowMelt(double temperature, double time)
            => 2.44e-6 * (temperature - Chemical.Water.MeltingPoint) * time;

        private double GetCachedSaturationVaporPressure(
            Dictionary<(double, double), double> elevationTemperatures,
            Dictionary<(double, double), double> saturationVaporPressures,
            int layer,
            double elevation,
            double temperature)
        {
            var height = elevation + (TileClimate.AirCellLayerHeight * layer);
            if (!elevationTemperatures.TryGetValue((temperature, height), out var tempAtElevation))
            {
                tempAtElevation = Planet.Atmosphere.GetTemperatureAtElevation(temperature, height);
                elevationTemperatures.Add((temperature, height), tempAtElevation);
            }

            if (!saturationVaporPressures.TryGetValue((tempAtElevation, height), out var saturationVaporPressure))
            {
                saturationVaporPressure = TileClimate.GetSaturationVaporPressure(Planet, height, tempAtElevation);
                saturationVaporPressures.Add((tempAtElevation, height), saturationVaporPressure);
            }

            return saturationVaporPressure;
        }

        private void SetGroundWater(Season previous)
        {
            for (var i = 0; i < Planet.Topography.Tiles.Length; i++)
            {
                var t = Planet.Topography.Tiles[i];
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
                    var runoff = (melt * 0.004 * t.Area) / Duration;
                    var previousRunoff = previous?.TileClimates[i].Runoff ?? runoff;
                    tc.Runoff = (float)((previousRunoff > runoff ? ((previousRunoff * 3) + runoff) : ((runoff * 3) + previousRunoff)) / 4);
                }
            }
        }

        private void SetPrecipitation()
        {
            var mp = Chemical.Water.MeltingPoint;
            var hadleyLifts = new Dictionary<double, double>();

            for (var i = 0; i < Planet.Topography.Tiles.Length; i++)
            {
                var t = Planet.Topography.Tiles[i];
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

                if (!hadleyLifts.TryGetValue(tc.Latitude, out var hadleyLift))
                {
                    hadleyLift = ((Math.Cos(6 * tc.Latitude) * (1 - (Math.Abs(tc.Latitude) / MathConstants.TwoPI))) + 1) / 2; // less dropoff with latitude
                    hadleyLifts.Add(tc.Latitude, hadleyLift);
                }

                var relativeHumidity = warmAir;
                for (var j = 0; j < tc.AirCellLayers; j++)
                {
                    relativeHumidity += warmAir;
                    warmAir *= hadleyLift;
                }

                var mixingRatio = relativeHumidity > 1
                    ? TileClimate.GetSaturationMixingRatio(Planet, tc, t.Elevation) * relativeHumidity
                    : 0;

                if (!TMath.IsZero(mixingRatio))
                {
                    tc.Precipitation = (float)(mixingRatio * TileClimate.GetAirCellHeight(tc) * ProportionOfYear);

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

            for (var i = 0; i < Planet.Topography.Tiles.Length; i++)
            {
                if (TileClimates[i].Runoff > 0)
                {
                    var lowest = Planet.Topography.Tiles[i].GetLowestCorner(Planet.Topography);

                    cornerFlows[lowest.Index] = TileClimates[i].Runoff;

                    endpoints.Add(lowest);
                }
            }
            Corner prev = null;
            while (endpoints.Count > 0)
            {
                var c = endpoints.First();
                endpoints.Remove(c);

                var index = Array.Find(Planet.Topography.Edges, x => x.RiverSource == c.Index)?.RiverDirection ?? -1;
                var next = index == -1 ? null : Planet.Topography.Corners[index];
                if (next == null)
                {
                    next = c.GetLowestCorner(Planet.Topography, true);
                    if (next.Elevation > c.Elevation)
                    {
                        next = c.GetLowestCorner(Planet.Topography, false);
                    }
                    if (next.Elevation > c.Elevation && (prev?.LakeDepth ?? 0) == 0)
                    {
                        c.LakeDepth = Math.Min(
                            c.Corners.Min(x => Planet.Topography.Corners[x].Elevation),
                            c.Tiles.Min(x => Planet.Topography.Tiles[x].Elevation)) - c.Elevation;
                    }
                }

                if ((prev?.LakeDepth ?? 0) == 0 || c.LakeDepth + c.Elevation >= next.Elevation)
                {
                    var edgeIndex = c.Edges[c.IndexOfCorner(next.Index)];
                    var edge = Planet.Topography.Edges[edgeIndex];
                    edge.RiverSource = c.Index;

                    cornerFlows.TryGetValue(c.Index, out var flow);
                    flow += c.Edges
                        .Where(e => Planet.Topography.Edges[e].RiverDirection == c.Index)
                        .Sum(e => EdgeRiverFlows[e]);
                    EdgeRiverFlows[edgeIndex] = flow;

                    var rc = next;
                    var nextRiverEdge = -1;
                    do
                    {
                        var nextRiverEdges = rc.Edges.Where(e => Planet.Topography.Edges[e].RiverSource == rc.Index).ToList();
                        if (nextRiverEdges.Count > 0)
                        {
                            nextRiverEdge = nextRiverEdges[0];
                            EdgeRiverFlows[nextRiverEdge] += flow;
                            rc = Planet.Topography.Corners[Planet.Topography.Edges[nextRiverEdge].RiverDirection];
                        }
                        else
                        {
                            nextRiverEdge = -1;
                        }
                    } while (nextRiverEdge != -1);

                    if (next.TerrainType == TerrainType.Land
                        && next.LakeDepth == 0
                        && !endpoints.Contains(next)
                        && !next.Edges.Any(e => Planet.Topography.Edges[e].RiverSource == next.Index))
                    {
                        endpoints.Add(next);
                    }
                }

                prev = c;
            }
        }

        private void SetSeaIce(Season previous)
        {
            var saltWaterMeltingPointOffset = Chemical.Water.MeltingPoint - Chemical.Water_Salt.MeltingPoint;
            for (var i = 0; i < Planet.Topography.Tiles.Length; i++)
            {
                var tc = TileClimates[i];
                if (Planet.Topography.Tiles[i].TerrainType.HasFlag(TerrainType.Water))
                {
                    double previousIce = previous?.TileClimates[i].SeaIce ?? 0;
                    var ice = 0.0;
                    if (tc.Temperature < Chemical.Water_Salt.MeltingPoint)
                    {
                        ice = SeaIcePerSecond * Duration * Math.Pow(Chemical.Water_Salt.MeltingPoint - tc.Temperature, 0.58);
                    }
                    else if (previousIce > 0)
                    {
                        previousIce -= Math.Min(GetSnowMelt(tc.Temperature + saltWaterMeltingPointOffset, Duration), previousIce);
                    }
                    tc.SeaIce = (float)Math.Max(previousIce, ice);
                }
            }
        }

        private void SetTemperature(Vector3 position)
        {
            var equatorialTemp = Planet.Atmosphere.GetSurfaceTemperatureAtPosition(position);
            var polarTemp = Planet.Atmosphere.GetSurfaceTemperatureAtPosition(position, true);

            var latitudeTemperatures = new Dictionary<double, double>();
            var elevationTemperatures = new Dictionary<(double, double), double>();
            var elevationPressures = new Dictionary<(double, double), double>();
            var saturationVaporPressures = new Dictionary<(double, double), double>();
            var zeroSVPIndexes = new Dictionary<(double, double), int>();

            for (var i = 0; i < Planet.Topography.Tiles.Length; i++)
            {
                var t = Planet.Topography.Tiles[i];
                var tc = TileClimates[i];
                var lat = Math.Abs(tc.Latitude);
                if (!latitudeTemperatures.TryGetValue(lat, out var surfaceTemp))
                {
                    surfaceTemp = CelestialBody.GetTemperatureAtLatitude(equatorialTemp, polarTemp, lat);
                    latitudeTemperatures.Add(lat, surfaceTemp);
                }

                var roundedElevation = Math.Round(t.Elevation / 100) * 100;
                if (!elevationTemperatures.TryGetValue((surfaceTemp, roundedElevation), out var tempAtElevation))
                {
                    tempAtElevation = Planet.Atmosphere.GetTemperatureAtElevation(surfaceTemp, roundedElevation);
                    elevationTemperatures.Add((surfaceTemp, roundedElevation), tempAtElevation);
                }
                tc.Temperature = (float)tempAtElevation;

                var roundedTemperature = Math.Round(tempAtElevation / 3) * 3;
                if (!elevationPressures.TryGetValue((roundedTemperature, roundedElevation), out var pressureAtElevation))
                {
                    pressureAtElevation = Planet.Atmosphere.GetAtmosphericPressure(roundedTemperature, roundedElevation);
                    elevationPressures.Add((roundedTemperature, roundedElevation), pressureAtElevation);
                }
                tc.AtmosphericPressure = (float)pressureAtElevation;

                var layers = (int)Math.Max(1, Math.Floor((Planet.Atmosphere.AtmosphericHeight - t.Elevation) / TileClimate.AirCellLayerHeight));
                var nearestLayerHeight = Math.Round(t.Elevation / TileClimate.AirCellLayerHeight) * TileClimate.AirCellLayerHeight;
                if (!zeroSVPIndexes.TryGetValue((nearestLayerHeight, roundedTemperature), out var layerIndex))
                {
                    layerIndex = TileClimate.GetAirCellIndexOfNearlyZeroSaturationVaporPressure(Planet, nearestLayerHeight, roundedTemperature);
                    zeroSVPIndexes.Add((nearestLayerHeight, roundedTemperature), layerIndex);
                }
                tc.AirCellLayers = Math.Max(1, Math.Min(layers, layerIndex));

                while (tc.AirCellLayers < layers
                    && !TMath.IsZero(GetCachedSaturationVaporPressure(elevationTemperatures, saturationVaporPressures, tc.AirCellLayers - 1, nearestLayerHeight, roundedTemperature)))
                {
                    tc.AirCellLayers++;
                }
            }
        }
    }
}
