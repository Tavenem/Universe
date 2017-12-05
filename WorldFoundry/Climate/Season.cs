using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.CelestialBodies;
using WorldFoundry.Extensions;
using WorldFoundry.Substances;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents an indeterminate period of time during which the climate on a <see cref="Planet"/>
    /// is reported as an average.
    /// </summary>
    public class Season : IIndexedItem
    {
        private const float AdvectionErrorTolerance = 1.0e-4f;
        private const float SeaIcePerSecond = 1.53935e-4f;
        private const float SnowToRainRatio = 13;

        internal double[] EdgeAirFlows { get; private set; }

        /// <summary>
        /// The level of river discharge along each <see cref="WorldGrids.Edge"/> during this <see
        /// cref="Season"/>, in m³/s.
        /// </summary>
        public float[] EdgeRiverFlows { get; internal set; }

        /// <summary>
        /// The index of this item.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The climate of each <see cref="WorldGrids.Tile"/> during this <see cref="Season"/>.
        /// </summary>
        public TileClimate[] TileClimates { get; internal set; }

        /// <summary>
        /// The latitude of the solar equator during this season, as an angle in radians from the (true) equator.
        /// </summary>
        internal float TropicalEquator { get; private set; }

        /// <summary>
        /// Constructs a new instance of <see cref="Season"/>.
        /// </summary>
        public Season() { }

        internal Season(int index, Planet planet, Vector3 position, double seasonDuration, float elapsedYearToDate, float seasonProportion, Season previous = null)
        {
            Index = index;
            EdgeAirFlows = new double[planet.WorldGrid.Edges.Count];
            EdgeRiverFlows = new float[planet.WorldGrid.Edges.Count];
            TileClimates = new TileClimate[planet.WorldGrid.Tiles.Count];
            for (int j = 0; j < planet.WorldGrid.Tiles.Count; j++)
            {
                TileClimates[j] = new TileClimate();
            }

            TropicalEquator = planet.AxialTilt * (float)Math.Sin(Utilities.MathUtil.Constants.TwoPI * elapsedYearToDate + Utilities.MathUtil.Constants.HalfPI) * 0.6666667f;

            SetTemperature(planet, position);
            SetWind(planet);
            SetSeaIce(planet, seasonDuration, previous);
            SetPrecipitation(planet, seasonProportion, previous);
            SetGroundWater(planet, seasonDuration, previous);
            SetRiverFlow(planet.WorldGrid);
        }

        private static void CalculatePrecipitation(Tile t, TileClimate tc, float timeRatio)
        {
            var rainMixingRatios = new float[tc.AirCells.Count];
            var snowMixingRatios = new float[tc.AirCells.Count];
            for (int i = 0; i < tc.AirCells.Count; i++)
            {
                var ac = tc.AirCells[i];
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

            tc.SnowFall = 0;
            if (snowMixingRatios.Any(s => s > Utilities.MathUtil.Constants.NearlyZero)) // snow
            {
                tc.SnowFall = snowMixingRatios.Select((s, i) => s * tc.AirCells[i].Density).Sum() * AirCell.LayerHeight * timeRatio;
            }

            tc.Precipitation = tc.SnowFall;
            if (rainMixingRatios.Any(r => r > Utilities.MathUtil.Constants.NearlyZero)) // rain
            {
                tc.Precipitation += rainMixingRatios.Select((r, i) => r * tc.AirCells[i].Density).Sum() * AirCell.LayerHeight * timeRatio;
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

        private static float GetPressureGradientForce(float latitude, float tropicalEquator, float halfITCZWidth)
        {
            var c = Utilities.MathUtil.Constants.ThreePI / (Utilities.MathUtil.Constants.HalfPI + tropicalEquator * (latitude > tropicalEquator ? -1 : 1));
            var pressure_derivate = -0.001f * (float)Math.Sin(c * (latitude - tropicalEquator)) + 3.333333e-4f;

            if (latitude < tropicalEquator + halfITCZWidth
                && latitude > tropicalEquator - halfITCZWidth)
            {
                pressure_derivate *= 1.25f;
            }

            return pressure_derivate;
        }

        private static float GetSnowMelt(float temperature, double time)
            => (float)(2.44e-6 * (temperature - Chemical.Water.MeltingPoint) * time);

        private void CalculateAdvection(
            Planet planet,
            float timeRatio,
            int i,
            Queue<int> advectionTiles,
            Dictionary<int, int> visitedTiles)
        {
            var t = planet.WorldGrid.GetTile(i);
            var tc = TileClimates[i];

            // incoming
            var inflow = new List<(double, double, double)[]>();
            for (int k = 0; k < t.EdgeCount; k++)
            {
                if (planet.WorldGrid.GetEdge(t.GetEdge(k)).GetSign(i) * EdgeAirFlows[t.GetEdge(k)] > 0)
                {
                    var otherACs = TileClimates[t.GetTile(k)].AirCells;
                    var airFlow = Math.Abs(EdgeAirFlows[t.GetEdge(k)]);
                    var edgeInflow = new(double, double)[tc.AirCells.Count];
                    var minCount = Math.Min(tc.AirCells.Count, otherACs.Count);
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
                for (int j = 0; j < tc.AirCells.Count; j++)
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
            else if (tc.AirCells.Sum(ac => ac.AbsoluteHumidity) == 0)
            {
                return;
            }

            // outgoing
            var destinationTiles = new List<int>();
            var outflow = 0d;
            for (int k = 0; k < t.EdgeCount; k++)
            {
                if (planet.WorldGrid.GetEdge(t.GetEdge(k)).GetSign(i) * EdgeAirFlows[t.GetEdge(k)] < 0)
                {
                    outflow += Math.Abs(EdgeAirFlows[t.GetEdge(k)]);

                    var tk = t.GetTile(k);
                    if (planet.WorldGrid.GetTile(tk).TerrainType != TerrainType.Water)
                    {
                        destinationTiles.Add(tk);
                    }
                }
            }

            var originalAbsHumidities = tc.AirCells.Select(ac => ac.AbsoluteHumidity).ToArray();
            for (int j = 0; j < tc.AirCells.Count; j++)
            {
                var ac = tc.AirCells[j];
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
            for (int j = 0; j < tc.AirCells.Count; j++)
            {
                var ac = tc.AirCells[j];
                delta = Math.Max(delta,
                    originalAbsHumidities[j] > ac.AbsoluteHumidity
                    ? 0
                    : GetHumidityChange(originalAbsHumidities[j], ac.AbsoluteHumidity));
            }
            if (delta > AdvectionErrorTolerance)
            {
                foreach (var k in destinationTiles.Where(k => !advectionTiles.Contains(k)
                    && (!visitedTiles.ContainsKey(k) || visitedTiles[k] < 5)))
                {
                    advectionTiles.Enqueue(k);
                }
            }
        }

        private void SetGroundWater(Planet planet, double time, Season previous)
        {
            for (int i = 0; i < planet.WorldGrid.Tiles.Count; i++)
            {
                var t = planet.WorldGrid.GetTile(i);
                if (t.TerrainType != TerrainType.Water)
                {
                    var tc = TileClimates[i];
                    var previousSnow = (previous?.TileClimates[i].Snow ?? 0) / SnowToRainRatio;

                    var melt = 0f;
                    if (tc.Temperature > Chemical.Water.MeltingPoint && previousSnow > 0)
                    {
                        var meltPotential = GetSnowMelt(tc.Temperature, time);

                        melt = Math.Min(meltPotential, previousSnow);
                        previousSnow -= melt;
                    }

                    tc.Snow = Math.Max(previousSnow, tc.SnowFall) * SnowToRainRatio;

                    // rolling average, weighted to the heavier, roughly models infiltration and seepage
                    // multiplied by a factor of 4 to roughly model groundwater flow
                    var runoff = (float)(((tc.Precipitation - tc.SnowFall + melt) * 0.004f * t.Area) / time);
                    var previousRunoff = previous?.TileClimates[i].Runoff ?? runoff;
                    tc.Runoff = (previousRunoff > runoff ? (previousRunoff * 3 + runoff) : (runoff * 3 + previousRunoff)) / 4;
                }
            }
        }

        private void SetPrecipitation(Planet planet, float timeRatio, Season previous)
        {
            var advectionTiles = new Queue<int>();

            for (int i = 0; i < planet.WorldGrid.Tiles.Count; i++)
            {
                var t = planet.WorldGrid.GetTile(i);
                var tc = TileClimates[i];
                if (t.TerrainType == TerrainType.Water)
                {
                    // ocean surface cells are supersaturated
                    foreach (var ac in tc.AirCells)
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
                        var prevACs = previous.TileClimates[i].AirCells;
                        var minCount = Math.Min(tc.AirCells.Count, prevACs.Count);
                        for (int j = 0; j < minCount; j++)
                        {
                            tc.AirCells[j].AbsoluteHumidity = prevACs[j].AbsoluteHumidity;
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
                CalculateAdvection(planet, timeRatio, i, advectionTiles, visitedTiles);
            }
        }

        private void SetRiverFlow(WorldGrid worldGrid)
        {
            var cornerFlows = new Dictionary<int, float>();
            var endpoints = new SortedSet<Corner>(Comparer<Corner>.Create((c1, c2) => c2.Elevation.CompareTo(c1.Elevation) * -1));

            for (int i = 0; i < worldGrid.Tiles.Count; i++)
            {
                if (TileClimates[i].Runoff > 0)
                {
                    var lowest = worldGrid.GetTile(i).GetLowestCorner(worldGrid);

                    cornerFlows[lowest.Index] = TileClimates[i].Runoff;

                    endpoints.Add(lowest);
                }
            }
            Corner prev = null;
            while (endpoints.Count > 0)
            {
                var c = endpoints.First();
                endpoints.Remove(c);

                var next = c.GetLowestCorner(worldGrid, true);
                if (next.Elevation > c.Elevation)
                {
                    next = c.GetLowestCorner(worldGrid, false);
                }

                if (next.Elevation > c.Elevation && (prev?.LakeDepth ?? 0) == 0)
                {
                    c.LakeDepth = Math.Min(c.GetCorners().Min(x => worldGrid.GetCorner(x).Elevation), c.GetTiles().Min(x => worldGrid.GetTile(x).Elevation)) - c.Elevation;
                }
                if ((prev?.LakeDepth ?? 0) == 0 || c.LakeDepth + c.Elevation >= next.Elevation)
                {
                    var edgeIndex = c.GetEdge(c.IndexOfCorner(next.Index));
                    var edge = worldGrid.GetEdge(edgeIndex);
                    edge.RiverSource = c.Index;
                    edge.RiverDirection = next.Index;

                    cornerFlows.TryGetValue(c.Index, out var flow);
                    flow += c.GetEdges()
                        .Where(e => worldGrid.GetEdge(e).RiverDirection == c.Index)
                        .Sum(e => EdgeRiverFlows[e]);
                    EdgeRiverFlows[edgeIndex] = flow;

                    var rc = next;
                    int nextRiverEdge = -1;
                    do
                    {
                        var nextRiverEdges = rc.GetEdges().Where(e => worldGrid.GetEdge(e).RiverSource == rc.Index).ToList();
                        if (nextRiverEdges.Count > 0)
                        {
                            nextRiverEdge = nextRiverEdges[0];
                            EdgeRiverFlows[nextRiverEdge] += flow;
                            rc = worldGrid.GetCorner(worldGrid.GetEdge(nextRiverEdge).RiverDirection);
                        }
                        else
                        {
                            nextRiverEdge = -1;
                        }
                    } while (nextRiverEdge != -1);

                    if (next.TerrainType == TerrainType.Land
                        && next.LakeDepth == 0
                        && !endpoints.Contains(next)
                        && !next.GetEdges().Any(e => worldGrid.GetEdge(e).RiverSource == next.Index))
                    {
                        endpoints.Add(next);
                    }
                }

                prev = c;
            }
        }

        private void SetSeaIce(Planet planet, double time, Season previous)
        {
            var saltWaterMeltingPointOffset = Chemical.Water.MeltingPoint - Chemical.Water_Salt.MeltingPoint;
            for (int i = 0; i < planet.WorldGrid.Tiles.Count; i++)
            {
                var tc = TileClimates[i];
                if (planet.WorldGrid.GetTile(i).TerrainType.HasFlag(TerrainType.Water))
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

        private void SetTemperature(Planet planet, Vector3 position)
        {
            var temperatures = new Dictionary<float, float>();

            var equatorialTemp = planet.Atmosphere.GetSurfaceTemperatureAtPosition(position);
            var polarTemp = planet.Atmosphere.GetSurfaceTemperatureAtPosition(position, true);

            for (int i = 0; i < planet.WorldGrid.Tiles.Count; i++)
            {
                var t = planet.WorldGrid.GetTile(i);
                var tc = TileClimates[i];
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
                tc.Temperature = planet.Atmosphere.GetTemperatureAtElevation(surfaceTemp, t.Elevation);
                var c = 0;
                while (c < 12)
                {
                    var ac = new AirCell(planet, t, tc, c);
                    tc.AirCells.Add(ac);
                    if (ac.SaturationVaporPressure == 0)
                    {
                        c = 12; // skip to the end, since no higher cells will provide any value
                    }
                    c++;
                }
                tc.AtmosphericPressure = tc.AirCells[0].Pressure;
            }
        }

        private void SetWind(Planet planet)
        {
            var pressureGradientForces = new Dictionary<float, Tuple<bool, float>>();
            for (int i = 0; i < planet.WorldGrid.Tiles.Count; i++)
            {
                var t = planet.WorldGrid.GetTile(i);
                var tc = TileClimates[i];

                if (!pressureGradientForces.TryGetValue(t.Latitude, out var pressureGradientForce))
                {
                    var pgf = GetPressureGradientForce(t.Latitude, TropicalEquator, planet.HalfITCZWidth);
                    pressureGradientForce = new Tuple<bool, float>(pgf < 0, Math.Abs(pgf));
                    pressureGradientForces.Add(t.Latitude, pressureGradientForce);
                }

                var v = Vector2.UnitX.Rotate((pressureGradientForce.Item1 ? Math.PI : 0) - Math.Atan2(t.CoriolisCoefficient, t.FrictionCoefficient));
                tc.WindDirection = (float)Math.Atan2(v.Y, v.X) + t.North;

                tc.WindSpeed = pressureGradientForce.Item2 / new Vector2(t.CoriolisCoefficient, t.FrictionCoefficient).Length();

                var corners = t.Polygon.Select(p => p.Rotate(t.North - tc.WindDirection)).ToList();
                for (int k = 0; k < t.EdgeCount; k++)
                {
                    var e = planet.WorldGrid.GetEdge(t.GetEdge(k));
                    var direction = e.GetSign(i);
                    if (corners[k].X + corners[(k + 1) % t.EdgeCount].X < 0)
                    {
                        direction *= -1;
                    }
                    var profileRatio =
                        Math.Abs(corners[k].Y - corners[(k + 1) % t.EdgeCount].Y)
                        / (corners[k] - corners[(k + 1) % t.EdgeCount]).Length();
                    EdgeAirFlows[t.GetEdge(k)] -= direction * tc.WindSpeed * e.Length * profileRatio * AirCell.LayerHeight;
                }
            }
        }
    }
}
