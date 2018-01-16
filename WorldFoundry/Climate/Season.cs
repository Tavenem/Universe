using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Extensions;
using WorldFoundry.Substances;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents an indeterminate period of time during which the climate on a <see cref="Planet"/>
    /// is reported as an average.
    /// </summary>
    public class Season : DataItem, IIndexedItem
    {
        internal const float ClimateErrorTolerance = 1.0e-4f;
        private const float SeaIcePerSecond = 1.53935e-4f;
        private const float SnowToRainRatio = 13;

        [NotMapped]
        internal EdgeClimate[] edgeClimateArray;

        /// <summary>
        /// The climate of each <see cref="Edge"/> during this <see cref="Season"/>.
        /// </summary>
        public ICollection<EdgeClimate> EdgeClimates { get; private set; }

        /// <summary>
        /// The index of this item.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The <see cref="TerrestrialPlanet"/> this <see cref="Season"/> describes.
        /// </summary>
        public TerrestrialPlanet Planet { get; private set; }

        [NotMapped]
        internal TileClimate[] tileClimateArray;

        /// <summary>
        /// The climate of each <see cref="Tile"/> during this <see cref="Season"/>.
        /// </summary>
        public ICollection<TileClimate> TileClimates { get; private set; }

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
            tileClimateArray = new TileClimate[planet.Topography.Tiles.Count];
            for (int j = 0; j < planet.Topography.Tiles.Count; j++)
            {
                tileClimateArray[j] = new TileClimate(j);
            }
            edgeClimateArray = new EdgeClimate[planet.Topography.Edges.Count];
            for (int j = 0; j < planet.Topography.Edges.Count; j++)
            {
                edgeClimateArray[j] = new EdgeClimate(j);
            }

            var seasonDuration = planet.Orbit.Period / amount;
            var seasonProportion = 1.0f / amount;
            var elapsedYearToDate = seasonProportion * index;

            TropicalEquator = planet.AxialTilt * (float)Math.Sin(Utilities.MathUtil.Constants.TwoPI * elapsedYearToDate + Utilities.MathUtil.Constants.HalfPI) * 0.6666667f;

            if (previous != null)
            {
                previous.TileClimates.SetIndexedArray(ref previous.tileClimateArray);
            }

            SetTemperature(position);
            var edgeAirFlows = SetWind();
            SetSeaIce(seasonDuration, previous);
            SetPrecipitation(seasonProportion, previous, edgeAirFlows);
            SetGroundWater(seasonDuration, previous);
            SetRiverFlow();

            foreach (var tc in tileClimateArray)
            {
                tc.AirCells = new HashSet<AirCell>(tc.airCellList);
            }
            TileClimates = new HashSet<TileClimate>(tileClimateArray);
            EdgeClimates = new HashSet<EdgeClimate>(edgeClimateArray);

            Planet.Topography.UpdateCollectionsFromArrays();
        }

        private static void CalculatePrecipitation(Tile t, TileClimate tc, float timeRatio)
        {
            var rainMixingRatios = new float[tc.airCellList.Count];
            var snowMixingRatios = new float[tc.airCellList.Count];
            for (int i = 0; i < tc.airCellList.Count; i++)
            {
                var ac = tc.airCellList[i];
                var vaporMixingRatio = 0f;
                if (ac.SaturationVaporPressure == 0)
                {
                    vaporMixingRatio = ac.AbsoluteHumidity / (ac.Density - ac.AbsoluteHumidity);
                }
                else
                {
                    ac.RelativeHumidity = (ac.AbsoluteHumidity * Utilities.Science.Constants.SpecificGasConstantOfWater * ac.Temperature) / ac.SaturationVaporPressure;
                    vaporMixingRatio = ac.SaturationMixingRatio * ac.RelativeHumidity;
                }

                if (vaporMixingRatio > ac.SaturationMixingRatio)
                {
                    ac.AbsoluteHumidity = ac.SaturationHumidity;
                    ac.RelativeHumidity = 1;

                    var cloudMixingRatio = (vaporMixingRatio - ac.SaturationMixingRatio) * 16;
                    // clouds to rain, based on surface temp (snow formed above will melt as it falls)
                    if (tc.Temperature > Chemical.Water.MeltingPoint)
                    {
                        rainMixingRatios[i] = cloudMixingRatio;
                    }
                    else // clouds to snow
                    {
                        snowMixingRatios[i] = cloudMixingRatio;
                    }
                }
            }

            // snow
            tc.SnowFall = 0;
            tc.SnowFallWaterEquivalent = 0;
            if (snowMixingRatios.Any(s => s > Utilities.MathUtil.Constants.NearlyZero))
            {
                tc.SnowFallWaterEquivalent = snowMixingRatios.Select((s, i) => s * tc.airCellList[i].Density).Sum() * AirCell.LayerHeight * timeRatio;
                tc.SnowFall = tc.SnowFallWaterEquivalent / SnowToRainRatio;
            }

            // rain
            tc.Precipitation = tc.SnowFallWaterEquivalent;
            if (rainMixingRatios.Any(r => r > Utilities.MathUtil.Constants.NearlyZero))
            {
                tc.Precipitation += rainMixingRatios.Select((r, i) => r * tc.airCellList[i].Density).Sum() * AirCell.LayerHeight * timeRatio;
            }
        }

        private static float GetHumidityChange(double first, double second)
        {
            if (first < Utilities.MathUtil.Constants.NearlyZero)
            {
                return second > Utilities.MathUtil.Constants.NearlyZero ? 1 : 0;
            }
            else
            {
                return (float)(1 - (second == 0 ? 0 : first / second));
            }
        }

        private static float GetSnowMelt(float temperature, double time)
            => (float)(2.44e-6 * (temperature - Chemical.Water.MeltingPoint) * time);

        private void CalculateAdvection(
            float timeRatio,
            int i,
            Queue<int> advectionTiles,
            Dictionary<int, int> visitedTiles,
            double[] edgeAirFlows)
        {
            var t = Planet.Topography.TileArray[i];
            var tc = tileClimateArray[i];

            // incoming
            var inflow = new List<(double, double, double)[]>();
            for (int k = 0; k < t.EdgeCount; k++)
            {
                if (Planet.Topography.EdgeArray[t.GetEdge(k)].GetSign(i) * edgeAirFlows[t.GetEdge(k)] > 0)
                {
                    var otherACs = tileClimateArray[t.GetTile(k)].airCellList;
                    var airFlow = Math.Abs(edgeAirFlows[t.GetEdge(k)]);
                    var edgeInflow = new(double, double)[tc.airCellList.Count];
                    var minCount = Math.Min(tc.airCellList.Count, otherACs.Count);
                    for (int j = 0; j < minCount; j++)
                    {
                        edgeInflow[j] = (airFlow, otherACs[j].AbsoluteHumidity);
                    }
                    if (edgeInflow.Any(f => f.Item2 > 0))
                    {
                        inflow.Add(edgeInflow.Select(f => (f.Item1, f.Item2 * f.Item1, 1d)).ToArray());
                    }
                }
            }
            if (inflow.Count > 0)
            {
                for (int j = 0; j < tc.airCellList.Count; j++)
                {
                    var inflowTotal = inflow.Sum(f => f[j].Item2);
                    for (int f = 0; f < inflow.Count; f++)
                    {
                        inflow[f][j] = (
                            inflow[f][j].Item1,
                            inflow[f][j].Item2,
                            inflow[f][j].Item2 == 0 ? 0 : inflow[f][j].Item2 / inflowTotal
                        );
                    }
                }
            }
            else if (tc.airCellList.Sum(ac => ac.AbsoluteHumidity) == 0)
            {
                return;
            }

            // outgoing
            var destinationTiles = new List<int>();
            var outflow = 0d;
            for (int k = 0; k < t.EdgeCount; k++)
            {
                if (Planet.Topography.EdgeArray[t.GetEdge(k)].GetSign(i) * edgeAirFlows[t.GetEdge(k)] < 0)
                {
                    outflow += Math.Abs(edgeAirFlows[t.GetEdge(k)]);

                    var tk = t.GetTile(k);
                    if (Planet.Topography.TileArray[tk].TerrainType != TerrainType.Water)
                    {
                        destinationTiles.Add(tk);
                    }
                }
            }

            var originalAbsHumidities = tc.airCellList.Select(ac => ac.AbsoluteHumidity).ToArray();
            for (int j = 0; j < tc.airCellList.Count; j++)
            {
                var ac = tc.airCellList[j];
                var convection = outflow - inflow.Sum(f => f[j].Item1);

                var density = inflow.Sum(f => f[j].Item2 == 0 ? 0 : (f[j].Item2 * f[j].Item3) / ((f[j].Item1 + (convection > 0 ? convection : 0)) * f[j].Item3));
                if (convection < 0)
                {
                    density += density * (-convection / inflow.Sum(f => f[j].Item1));
                }

                ac.AbsoluteHumidity = (float)density;
            }

            CalculatePrecipitation(t, tc, timeRatio);

            var delta = 0f;
            for (int j = 0; j < tc.airCellList.Count; j++)
            {
                var ac = tc.airCellList[j];
                delta = Math.Max(delta,
                    originalAbsHumidities[j] > ac.AbsoluteHumidity
                    ? 0
                    : GetHumidityChange(originalAbsHumidities[j], ac.AbsoluteHumidity));
            }
            if (delta > ClimateErrorTolerance)
            {
                foreach (var k in destinationTiles.Where(k => !advectionTiles.Contains(k)
                    && (!visitedTiles.ContainsKey(k) || visitedTiles[k] < 5)))
                {
                    advectionTiles.Enqueue(k);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="EdgeClimate"/> for this <see cref="Season"/> with the given index.
        /// </summary>
        /// <param name="index">An index to a <see cref="EdgeClimate"/>.</param>
        /// <returns>The <see cref="EdgeClimate"/> with the given index.</returns>
        public EdgeClimate GetEdgeClimate(int index) => index == -1 ? null : EdgeClimates.FirstOrDefault(x => x.Index == index);

        private float GetPressureGradientForce(float latitude)
        {
            var c = Utilities.MathUtil.Constants.ThreePI / (Utilities.MathUtil.Constants.HalfPI + TropicalEquator * (latitude > TropicalEquator ? -1 : 1));
            var pressure_derivate = (float)(-0.001 * Math.Sin(c * (latitude - TropicalEquator)) + (1.0 / 3000));

            if (latitude < TropicalEquator + Planet.HalfITCZWidth
                && latitude > TropicalEquator - Planet.HalfITCZWidth)
            {
                pressure_derivate *= 1.25f;
            }

            return pressure_derivate;
        }

        /// <summary>
        /// Gets the <see cref="TileClimate"/> for this <see cref="Season"/> with the given index.
        /// </summary>
        /// <param name="index">An index to a <see cref="TileClimate"/>.</param>
        /// <returns>The <see cref="TileClimate"/> with the given index.</returns>
        public TileClimate GetTileClimate(int index) => index == -1 ? null : TileClimates.FirstOrDefault(x => x.Index == index);

        private void SetGroundWater(double time, Season previous)
        {
            for (int i = 0; i < Planet.Topography.TileArray.Length; i++)
            {
                var t = Planet.Topography.TileArray[i];
                if (t.TerrainType != TerrainType.Water)
                {
                    var tc = tileClimateArray[i];
                    var previousSnow = (previous?.tileClimateArray[i].SnowCover ?? 0) / SnowToRainRatio;
                    var newSnow = tc.SnowFallWaterEquivalent;

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
                    var runoff = (float)(((tc.Precipitation - tc.SnowFallWaterEquivalent + melt) * 0.004f * t.Area) / time);
                    var previousRunoff = previous?.tileClimateArray[i].Runoff ?? runoff;
                    tc.Runoff = (previousRunoff > runoff ? (previousRunoff * 3 + runoff) : (runoff * 3 + previousRunoff)) / 4;
                }
            }
        }

        private void SetPrecipitation(float timeRatio, Season previous, double[] edgeAirFlows)
        {
            var advectionTiles = new Queue<int>();

            for (int i = 0; i < Planet.Topography.TileArray.Length; i++)
            {
                var t = Planet.Topography.TileArray[i];
                var tc = tileClimateArray[i];
                if (t.TerrainType == TerrainType.Water)
                {
                    // ocean surface cells are supersaturated
                    foreach (var ac in tc.airCellList)
                    {
                        ac.AbsoluteHumidity = ac.SaturationHumidity * 1.1f;

                        // adjust for ice cover
                        if (tc.SeaIce > 0)
                        {
                            ac.AbsoluteHumidity *= 0.5f;
                        }
                    }
                }
                else
                {
                    advectionTiles.Enqueue(i);

                    if (previous != null)
                    {
                        var prevTC = previous.tileClimateArray[i];
                        var minCount = Math.Min(tc.airCellList.Count, prevTC.AirCells.Count);
                        for (int j = 0; j < minCount; j++)
                        {
                            tc.airCellList[j].AbsoluteHumidity = prevTC.GetAirCell(j).AbsoluteHumidity;
                        }
                    }
                }
            }

            var visitedTiles = new Dictionary<int, int>();
            while (advectionTiles.Count > 0)
            {
                var i = advectionTiles.Dequeue();
                if (visitedTiles.ContainsKey(i))
                {
                    visitedTiles[i]++;
                }
                else
                {
                    visitedTiles.Add(i, 0);
                }
                CalculateAdvection(timeRatio, i, advectionTiles, visitedTiles, edgeAirFlows);
            }
        }

        private void SetRiverFlow()
        {
            var cornerFlows = new Dictionary<int, float>();
            var endpoints = new SortedSet<Corner>(Comparer<Corner>.Create((c1, c2) => c2.Elevation.CompareTo(c1.Elevation) * -1));

            for (int i = 0; i < Planet.Topography.TileArray.Length; i++)
            {
                if (tileClimateArray[i].Runoff > 0)
                {
                    var lowest = Planet.Topography.TileArray[i].GetLowestCorner(Planet.Topography);

                    cornerFlows[lowest.Index] = tileClimateArray[i].Runoff;

                    endpoints.Add(lowest);
                }
            }
            Corner prev = null;
            while (endpoints.Count > 0)
            {
                var c = endpoints.First();
                endpoints.Remove(c);

                var next = Planet.Topography.GetCorner(Planet.Topography.EdgeArray.FirstOrDefault(x => x.RiverSource == c.Index)?.RiverDirection ?? -1);
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
                            c.GetCorners().Min(x => Planet.Topography.CornerArray[x].Elevation),
                            c.GetTiles().Min(x => Planet.Topography.TileArray[x].Elevation)) - c.Elevation;
                    }
                }

                if ((prev?.LakeDepth ?? 0) == 0 || c.LakeDepth + c.Elevation >= next.Elevation)
                {
                    var edgeIndex = c.GetEdge(c.IndexOfCorner(next.Index));
                    var edge = Planet.Topography.EdgeArray[edgeIndex];
                    edge.RiverSource = c.Index;
                    edge.RiverDirection = next.Index;

                    cornerFlows.TryGetValue(c.Index, out var flow);
                    flow += c.GetEdges()
                        .Where(e => Planet.Topography.EdgeArray[e].RiverDirection == c.Index)
                        .Sum(e => edgeClimateArray[e].RiverFlow);
                    edgeClimateArray[edgeIndex].RiverFlow = flow;

                    var rc = next;
                    int nextRiverEdge = -1;
                    do
                    {
                        var nextRiverEdges = rc.GetEdges().Where(e => Planet.Topography.EdgeArray[e].RiverSource == rc.Index).ToList();
                        if (nextRiverEdges.Count > 0)
                        {
                            nextRiverEdge = nextRiverEdges[0];
                            edgeClimateArray[nextRiverEdge].RiverFlow += flow;
                            rc = Planet.Topography.CornerArray[Planet.Topography.EdgeArray[nextRiverEdge].RiverDirection];
                        }
                        else
                        {
                            nextRiverEdge = -1;
                        }
                    } while (nextRiverEdge != -1);

                    if (next.TerrainType == TerrainType.Land
                        && next.LakeDepth == 0
                        && !endpoints.Contains(next)
                        && !next.GetEdges().Any(e => Planet.Topography.EdgeArray[e].RiverSource == next.Index))
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
            for (int i = 0; i < Planet.Topography.TileArray.Length; i++)
            {
                var tc = tileClimateArray[i];
                if (Planet.Topography.TileArray[i].TerrainType.HasFlag(TerrainType.Water))
                {
                    var previousIce = previous?.tileClimateArray[i].SeaIce ?? 0;
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

            var temperatures = new Dictionary<float, float>();
            for (int i = 0; i < Planet.Topography.TileArray.Length; i++)
            {
                var t = Planet.Topography.TileArray[i];
                var tc = tileClimateArray[i];
                var lat = Math.Abs(t.Latitude - TropicalEquator);
                if (lat > Utilities.MathUtil.Constants.HalfPI)
                {
                    lat -= (float)Utilities.MathUtil.Constants.HalfPI;
                }
                if (!temperatures.TryGetValue(lat, out var surfaceTemp))
                {
                    surfaceTemp = CelestialBody.GetTemperatureAtLatitude(equatorialTemp, polarTemp, lat);
                    temperatures.Add(lat, surfaceTemp);
                }
                tc.Temperature = Planet.Atmosphere.GetTemperatureAtElevation(surfaceTemp, t.Elevation);
                var c = 0;
                var layers = (int)Math.Max(1, Math.Floor(Planet.Atmosphere.AtmosphericHeight / AirCell.LayerHeight));
                tc.airCellList = new List<AirCell>();
                while (c < layers)
                {
                    var ac = new AirCell(c, Planet, t, tc);
                    tc.airCellList.Add(ac);
                    if (TMath.IsZero(ac.SaturationVaporPressure))
                    {
                        break; // no higher cells will provide any value
                    }
                    c++;
                }
                tc.AtmosphericPressure = tc.airCellList[0].Pressure;
            }
        }

        private double[] SetWind()
        {
            var edgeAirFlows = new double[Planet.Topography.EdgeArray.Length];
            var pressureGradientForces = new Dictionary<float, Tuple<bool, float>>();
            for (int i = 0; i < Planet.Topography.TileArray.Length; i++)
            {
                var t = Planet.Topography.TileArray[i];
                var tc = tileClimateArray[i];

                if (!pressureGradientForces.TryGetValue(t.Latitude, out var pressureGradientForce))
                {
                    var pgf = GetPressureGradientForce(t.Latitude);
                    pressureGradientForce = new Tuple<bool, float>(pgf < 0, Math.Abs(pgf));
                    pressureGradientForces.Add(t.Latitude, pressureGradientForce);
                }

                var v = Vector2.UnitX.Rotate((pressureGradientForce.Item1 ? Math.PI : 0) - Math.Atan2(t.CoriolisCoefficient, t.FrictionCoefficient));
                tc.WindDirection = (float)Math.Atan2(v.Y, v.X) + t.North;

                tc.WindSpeed = pressureGradientForce.Item2 / new Vector2(t.CoriolisCoefficient, t.FrictionCoefficient).Length();

                var corners = t.Polygon.Select(p => p.Rotate(t.North - tc.WindDirection)).ToList();
                for (int k = 0; k < t.EdgeCount; k++)
                {
                    var e = Planet.Topography.EdgeArray[t.GetEdge(k)];
                    var direction = e.GetSign(i);
                    if (corners[k].X + corners[(k + 1) % t.EdgeCount].X < 0)
                    {
                        direction *= -1;
                    }
                    var profileRatio =
                        Math.Abs(corners[k].Y - corners[(k + 1) % t.EdgeCount].Y)
                        / (corners[k] - corners[(k + 1) % t.EdgeCount]).Length();
                    edgeAirFlows[t.GetEdge(k)] -= direction * tc.WindSpeed * e.Length * profileRatio * AirCell.LayerHeight;
                }
            }
            return edgeAirFlows;
        }
    }
}
