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
        private const float ClimateErrorTolerance = 1.0e-4f;
        private const float SeaIcePerSecond = 1.53935e-4f;
        private const float SnowToRainRatio = 13;
        private const float PrecipitationTempScale = 0.027f;

        /// <summary>
        /// The climate of each <see cref="Edge"/> during this <see cref="Season"/>.
        /// </summary>
        public float[] EdgeRiverFlows { get; private set; }

        /// <summary>
        /// The index of this item.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The <see cref="TerrestrialPlanet"/> this <see cref="Season"/> describes.
        /// </summary>
        public TerrestrialPlanet Planet { get; private set; }

        /// <summary>
        /// The climate of each <see cref="Tile"/> during this <see cref="Season"/>.
        /// </summary>
        public TileClimate[] TileClimates { get; private set; }

        /// <summary>
        /// The latitude of the solar equator during this season, as an angle in radians from the (true) equator.
        /// </summary>
        internal float TropicalEquator { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/>.
        /// </summary>
        public Season() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/> with the given values.
        /// </summary>
        internal Season(TerrestrialPlanet planet, int index, int amount, Vector3 position, Season previous = null)
        {
            Index = index;
            Planet = planet;

            var seasonDuration = planet.Orbit.Period / amount;
            var seasonProportion = 1.0f / amount;
            var elapsedYearToDate = seasonProportion * index;

            TropicalEquator = planet.AxialTilt * (float)Math.Sin(MathConstants.TwoPI * elapsedYearToDate + MathConstants.HalfPI) * 0.6666667f;

            TileClimates = new TileClimate[planet.Topography.Tiles.Length];
            for (var j = 0; j < planet.Topography.Tiles.Length; j++)
            {
                var t = planet.Topography.Tiles[j];

                TileClimates[j] = new TileClimate();
                var seasonalLatitude = t.Latitude - TropicalEquator;
                if (seasonalLatitude > MathConstants.HalfPI)
                {
                    seasonalLatitude = (float)(MathConstants.HalfPI - (seasonalLatitude - MathConstants.HalfPI));
                }
                else if (seasonalLatitude < -MathConstants.HalfPI)
                {
                    seasonalLatitude = (float)(-MathConstants.HalfPI - (seasonalLatitude + MathConstants.HalfPI));
                }
                TileClimates[j].Latitude = seasonalLatitude;
            }
            EdgeRiverFlows = new float[planet.Topography.Edges.Length];

            SetTemperature(position);
            SetSeaIce(seasonDuration, previous);
            SetPrecipitation(seasonProportion);
            SetGroundWater(seasonDuration, previous);
            SetRiverFlow();
        }

        private static float GetSnowMelt(float temperature, double time)
            => (float)(2.44e-6 * (temperature - Chemical.Water.MeltingPoint) * time);

        private float GetCachedSaturationVaporPressure(
            Dictionary<(float, float), float> elevationTemperatures,
            Dictionary<(float, float), float> saturationVaporPressures,
            int layer,
            float elevation,
            float temperature)
        {
            var height = elevation + TileClimate.AirCellLayerHeight * layer;
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

        private void SetGroundWater(double time, Season previous)
        {
            for (var i = 0; i < Planet.Topography.Tiles.Length; i++)
            {
                var t = Planet.Topography.Tiles[i];
                if (t.TerrainType != TerrainType.Water)
                {
                    var tc = TileClimates[i];
                    var previousSnow = (previous?.TileClimates[i].SnowCover ?? 0) / SnowToRainRatio;
                    var newSnow = tc.SnowFall > 0 ? tc.Precipitation : 0;

                    var melt = 0f;
                    if (tc.Temperature > Chemical.Water.MeltingPoint
                        && (previousSnow > 0 || newSnow > 0))
                    {
                        var meltPotential = GetSnowMelt(tc.Temperature, time);

                        melt = Math.Min(meltPotential, previousSnow);
                        meltPotential -= melt;
                        previousSnow -= melt;

                        melt = Math.Min(meltPotential, newSnow);
                        newSnow -= melt;
                    }

                    tc.SnowCover = Math.Max(previousSnow, newSnow) * SnowToRainRatio;

                    // rolling average, weighted to the heavier, roughly models infiltration and seepage
                    // multiplied by a factor of 4 to roughly model groundwater flow
                    var runoff = (float)((melt * 0.004f * t.Area) / time);
                    var previousRunoff = previous?.TileClimates[i].Runoff ?? runoff;
                    tc.Runoff = (previousRunoff > runoff ? (previousRunoff * 3 + runoff) : (runoff * 3 + previousRunoff)) / 4;
                }
            }
        }

        private void SetPrecipitation(float timeRatio)
        {
            var mp = Chemical.Water.MeltingPoint;
            var hadleyLifts = new Dictionary<float, float>();

            for (var i = 0; i < Planet.Topography.Tiles.Length; i++)
            {
                var t = Planet.Topography.Tiles[i];
                var tc = TileClimates[i];

                var relTemp = PrecipitationTempScale * (tc.Temperature - mp);
                relTemp += (1 - relTemp) / 1.5f; // less impact on extrema, particularly minima
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
                    hadleyLift = (float)(Math.Cos(6 * tc.Latitude) * (1 - Math.Abs(tc.Latitude) / MathConstants.TwoPI) + 1) / 2; // less dropoff with latitude
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
                    tc.Precipitation = mixingRatio * TileClimate.GetAirCellHeight(tc) * timeRatio;

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

                var next = Planet.Topography.Corners[Planet.Topography.Edges.FirstOrDefault(x => x.RiverSource == c.Index)?.RiverDirection ?? -1];
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
                    edge.RiverDirection = next.Index;

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

        private void SetSeaIce(double time, Season previous)
        {
            var saltWaterMeltingPointOffset = Chemical.Water.MeltingPoint - Chemical.Water_Salt.MeltingPoint;
            for (var i = 0; i < Planet.Topography.Tiles.Length; i++)
            {
                var tc = TileClimates[i];
                if (Planet.Topography.Tiles[i].TerrainType.HasFlag(TerrainType.Water))
                {
                    var previousIce = previous?.TileClimates[i].SeaIce ?? 0;
                    var ice = 0f;
                    if (tc.Temperature < Chemical.Water_Salt.MeltingPoint)
                    {
                        ice = (float)(SeaIcePerSecond * time * Math.Pow((Chemical.Water_Salt.MeltingPoint - tc.Temperature), 0.58));
                    }
                    else if (previousIce > 0)
                    {
                        previousIce -= Math.Min(GetSnowMelt(tc.Temperature + saltWaterMeltingPointOffset, time), previousIce);
                    }
                    tc.SeaIce = Math.Max(previousIce, ice);
                }
            }
        }

        private void SetTemperature(Vector3 position)
        {
            var equatorialTemp = Planet.Atmosphere.GetSurfaceTemperatureAtPosition(position);
            var polarTemp = Planet.Atmosphere.GetSurfaceTemperatureAtPosition(position, true);

            var latitudeTemperatures = new Dictionary<float, float>();
            var elevationTemperatures = new Dictionary<(float, float), float>();
            var elevationPressures = new Dictionary<(float, float), float>();
            var saturationVaporPressures = new Dictionary<(float, float), float>();
            var zeroSVPIndexes = new Dictionary<(float, float), int>();

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

                var roundedElevation = (float)Math.Round(t.Elevation / 100) * 100;
                if (!elevationTemperatures.TryGetValue((surfaceTemp, roundedElevation), out var tempAtElevation))
                {
                    tempAtElevation = Planet.Atmosphere.GetTemperatureAtElevation(surfaceTemp, roundedElevation);
                    elevationTemperatures.Add((surfaceTemp, roundedElevation), tempAtElevation);
                }
                tc.Temperature = tempAtElevation;

                var roundedTemperature = (float)Math.Round(tempAtElevation / 3) * 3;
                if (!elevationPressures.TryGetValue((roundedTemperature, roundedElevation), out var pressureAtElevation))
                {
                    pressureAtElevation = Planet.Atmosphere.GetAtmosphericPressure(roundedTemperature, roundedElevation);
                    elevationPressures.Add((roundedTemperature, roundedElevation), pressureAtElevation);
                }
                tc.AtmosphericPressure = pressureAtElevation;

                var layers = (int)Math.Max(1, Math.Floor((Planet.Atmosphere.AtmosphericHeight - t.Elevation) / TileClimate.AirCellLayerHeight));
                var nearestLayerHeight = (float)Math.Round(t.Elevation / TileClimate.AirCellLayerHeight) * TileClimate.AirCellLayerHeight;
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
