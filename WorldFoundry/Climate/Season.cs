using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.Extensions;
using WorldFoundry.WorldGrid;
using WorldFoundry.Utilities;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents an indeterminate period of time during which the climate on a <see cref="Planet"/>
    /// is reported as an average.
    /// </summary>
    public class Season
    {
        /// <summary>
        /// The freezing point of water, in K.
        /// </summary>
        public const float freezingPoint = 273.15f;

        private const float errorTolerance = 1.0e-4f;
        internal const float RWater = 461.5f;
        private const float seaIceFreezingOffset = 1.8f;
        private const float seaIceFreezingPoint = freezingPoint - seaIceFreezingOffset;
        internal const int secondsPerDay = 60 * 60 * 24;
        private const float snowToRainRatio = 13;
        private const float temperatureLapseRate = 6.49e-3f;

        private double[] _edgeAirFlows;
        private float _tropicalEquator = 0;

        /// <summary>
        /// The level of river discharge along each <see cref="WorldGrid.Edge"/> during this <see
        /// cref="Season"/>, in m³/s.
        /// </summary>
        public float[] EdgeRiverFlows { get; internal set; }

        /// <summary>
        /// The climate of each <see cref="WorldGrid.Tile"/> during this <see cref="Season"/>.
        /// </summary>
        public TileClimate[] TileClimates { get; internal set; }

        /// <summary>
        /// Constructs a new instance of <see cref="Season"/>.
        /// </summary>
        public Season() { }

        internal Season(Planet planet, double seasonDuration, float elapsedYearToDate, float seasonProportion, Season previous = null)
        {
            _edgeAirFlows = new double[planet.Edges.Count];
            EdgeRiverFlows = new float[planet.Edges.Count];
            TileClimates = new TileClimate[planet.Tiles.Count];
            for (int j = 0; j < planet.Tiles.Count; j++)
            {
                TileClimates[j] = new TileClimate();
            }

            _tropicalEquator = planet.AxialTilt * (float)Math.Sin(Utilities.MathUtil.Constants.TwoPI * elapsedYearToDate) * 0.6666667f;

            SetTemperature(planet);
            SetWind(planet);
            SetSeaIce(planet, seasonDuration, previous);
            SetPrecipitation(planet, seasonProportion, previous);
            SetGroundWater(planet, seasonDuration, previous);
            SetRiverFlow(planet);
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
                    ac.RelativeHumidity = (ac.AbsoluteHumidity * RWater * ac.Temperature) / ac.SaturationVaporPressure;
                    vaporMixingRatio = ac.SaturationMixingRatio * ac.RelativeHumidity;
                }

                if (vaporMixingRatio > ac.SaturationMixingRatio)
                {
                    ac.AbsoluteHumidity = ac.SaturationHumidity;
                    ac.RelativeHumidity = 1;

                    var cloudMixingRatio = (vaporMixingRatio - ac.SaturationMixingRatio) * 16;
                    // clouds to rain, based on surface temp (snow formed above will melt as it falls)
                    if (tc.Temperature > freezingPoint)
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
            => (float)(2.44e-6 * (temperature - freezingPoint) * time);

        internal static float GetTemperatureAtElevation(float surfaceTemp, float elevation)
        {
            if (elevation <= 0)
            {
                return surfaceTemp;
            }
            else if (elevation <= 11000)
            {
                return surfaceTemp - elevation * temperatureLapseRate;
            }
            else if (elevation <= 20000)
            {
                return 216.65f;
            }
            else if (elevation <= 32000)
            {
                return 196.65f + elevation / 1000;
            }
            else if (elevation <= 47000)
            {
                return 228.65f + 2.8f * (elevation / 1000 - 32);
            }
            else if (elevation <= 51000)
            {
                return 270.65f;
            }
            else if (elevation <= 71000)
            {
                return 270.65f - 2.8f * (elevation / 1000 - 51);
            }
            else
            {
                return 241.65f - 2 * (elevation / 1000 - 71);
            }
        }

        private static float GetTemperatureAtLatitude(float latitude)
            => (float)(freezingPoint - 80 + 115 * Math.Cos(Math.Abs(latitude * 0.8)));

        private void CalculateAdvection(
            Planet planet,
            float timeRatio,
            int i,
            Queue<int> advectionTiles,
            Dictionary<int, int> visitedTiles)
        {
            var t = planet.Tiles[i];
            var tc = TileClimates[i];

            // incoming
            var inflow = new List<(double, double, double)[]>();
            for (int k = 0; k < t.Edges.Length; k++)
            {
                if (planet.Edges[t.Edges[k]].GetSign(i) * _edgeAirFlows[t.Edges[k]] > 0)
                {
                    var otherACs = TileClimates[t.Tiles[k]].AirCells;
                    var airFlow = Math.Abs(_edgeAirFlows[t.Edges[k]]);
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
            for (int k = 0; k < t.Edges.Length; k++)
            {
                if (planet.Edges[t.Edges[k]].GetSign(i) * _edgeAirFlows[t.Edges[k]] < 0)
                {
                    outflow += Math.Abs(_edgeAirFlows[t.Edges[k]]);

                    var tk = t.Tiles[k];
                    if (planet.Tiles[tk].TerrainType != TerrainType.Water)
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
            if (delta > errorTolerance)
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
            for (int i = 0; i < planet.Tiles.Count; i++)
            {
                var t = planet.Tiles[i];
                if (t.TerrainType != TerrainType.Water)
                {
                    var tc = TileClimates[i];
                    var previousSnow = (previous?.TileClimates[i].Snow ?? 0) / snowToRainRatio;

                    var melt = 0f;
                    if (tc.Temperature > freezingPoint && previousSnow > 0)
                    {
                        var meltPotential = GetSnowMelt(tc.Temperature, time);

                        melt = Math.Min(meltPotential, previousSnow);
                        previousSnow -= melt;
                    }

                    tc.Snow = Math.Max(previousSnow, tc.SnowFall) * snowToRainRatio;

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

            for (int i = 0; i < planet.Tiles.Count; i++)
            {
                var t = planet.Tiles[i];
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

        private void SetRiverFlow(Planet planet)
        {
            var cornerFlows = new Dictionary<int, float>();
            var endpoints = new SortedSet<Corner>(Comparer<Corner>.Create((c1, c2) => c2.Elevation.CompareTo(c1.Elevation) * -1));

            for (int i = 0; i < planet.Tiles.Count; i++)
            {
                if (TileClimates[i].Runoff > 0)
                {
                    var lowest = planet.Tiles[i].GetLowestCorner(planet);

                    cornerFlows[lowest.Id] = TileClimates[i].Runoff;

                    endpoints.Add(lowest);
                }
            }
            Corner prev = null;
            while (endpoints.Count > 0)
            {
                var c = endpoints.First();
                endpoints.Remove(c);

                var next = c.GetLowestCorner(planet, true);
                if (next.Elevation > c.Elevation)
                {
                    next = c.GetLowestCorner(planet, false);
                }

                if (next.Elevation > c.Elevation && (prev?.LakeDepth ?? 0) == 0)
                {
                    c.LakeDepth = Math.Min(c.Corners.Min(x => planet.Corners[x].Elevation), c.Tiles.Min(x => planet.Tiles[x].Elevation)) - c.Elevation;
                }
                if ((prev?.LakeDepth ?? 0) == 0 || c.LakeDepth + c.Elevation >= next.Elevation)
                {
                    var edgeIndex = c.Edges[c.IndexOf(next.Id)];
                    var edge = planet.Edges[edgeIndex];
                    edge.RiverSource = c.Id;
                    edge.RiverDirection = next.Id;

                    cornerFlows.TryGetValue(c.Id, out var flow);
                    flow += c.Edges
                        .Where(e => planet.Edges[e].RiverDirection == c.Id)
                        .Sum(e => EdgeRiverFlows[e]);
                    EdgeRiverFlows[edgeIndex] = flow;

                    var rc = next;
                    int nextRiverEdge = -1;
                    do
                    {
                        var nextRiverEdges = rc.Edges.Where(e => planet.Edges[e].RiverSource == rc.Id).ToList();
                        if (nextRiverEdges.Count > 0)
                        {
                            nextRiverEdge = nextRiverEdges[0];
                            EdgeRiverFlows[nextRiverEdge] += flow;
                            rc = planet.Corners[planet.Edges[nextRiverEdge].RiverDirection];
                        }
                        else
                        {
                            nextRiverEdge = -1;
                        }
                    } while (nextRiverEdge != -1);

                    if (next.TerrainType == TerrainType.Land
                        && next.LakeDepth == 0
                        && !endpoints.Contains(next)
                        && !next.Edges.Any(e => planet.Edges[e].RiverSource == next.Id))
                    {
                        endpoints.Add(next);
                    }
                }

                prev = c;
            }
        }

        private void SetSeaIce(Planet planet, double time, Season previous)
        {
            var days = time / secondsPerDay;
            for (int i = 0; i < planet.Tiles.Count; i++)
            {
                var tc = TileClimates[i];
                if (planet.Tiles[i].TerrainType.HasFlag(TerrainType.Water))
                {
                    var previousIce = previous?.TileClimates[i].SeaIce ?? 0;
                    var ice = 0f;
                    if (tc.Temperature < seaIceFreezingPoint)
                    {
                        ice = (float)(13.3 * Math.Pow((seaIceFreezingPoint - tc.Temperature) * days, 0.58));
                    }
                    else if (previousIce > 0)
                    {
                        previousIce -= Math.Min(GetSnowMelt(tc.Temperature + seaIceFreezingOffset, time), previousIce);
                    }
                    tc.SeaIce = Math.Max(previousIce, ice);
                }
            }
        }

        private void SetTemperature(Planet planet)
        {
            var temperatures = new Dictionary<float, float>();
            for (int i = 0; i < planet.Tiles.Count; i++)
            {
                var t = planet.Tiles[i];
                var tc = TileClimates[i];
                var lat = _tropicalEquator - t.Latitude;
                if (!temperatures.TryGetValue(lat, out var temperature))
                {
                    temperature = GetTemperatureAtLatitude(lat);
                    temperatures.Add(lat, temperature);
                }
                tc.Temperature = GetTemperatureAtElevation(temperature, t.Elevation);
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
            for (int i = 0; i < planet.Tiles.Count; i++)
            {
                var t = planet.Tiles[i];
                var tc = TileClimates[i];

                var coriolisCoefficient = planet.CoriolisCoefficients[t.Latitude];
                if (!pressureGradientForces.TryGetValue(t.Latitude, out var pressureGradientForce))
                {
                    var pgf = GetPressureGradientForce(t.Latitude, _tropicalEquator, planet.HalfITCZWidth);
                    pressureGradientForce = new Tuple<bool, float>(pgf < 0, Math.Abs(pgf));
                    pressureGradientForces.Add(t.Latitude, pressureGradientForce);
                }

                var v = Vector2.UnitX.Rotate((pressureGradientForce.Item1 ? Math.PI : 0) - Math.Atan2(coriolisCoefficient, t.FrictionCoefficient));
                tc.WindDirection = (float)Math.Atan2(v.Y, v.X) + t.North;

                tc.WindSpeed = pressureGradientForce.Item2 / new Vector2(coriolisCoefficient, t.FrictionCoefficient).Length();

                var corners = t.Polygon.Select(p => p.Rotate(t.North - tc.WindDirection)).ToList();
                for (int k = 0; k < t.Edges.Length; k++)
                {
                    var e = planet.Edges[t.Edges[k]];
                    var direction = e.GetSign(i);
                    if (corners[k].X + corners[(k + 1) % t.Edges.Length].X < 0)
                    {
                        direction *= -1;
                    }
                    var profileRatio =
                        Math.Abs(corners[k].Y - corners[(k + 1) % t.Edges.Length].Y)
                        / (corners[k] - corners[(k + 1) % t.Edges.Length]).Length();
                    _edgeAirFlows[t.Edges[k]] -= direction * tc.WindSpeed * e.Length * profileRatio * AirCell.LayerHeight;
                }
            }
        }
    }
}
