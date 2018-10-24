using MathAndScience;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents an indeterminate period of time during which the climate on a <see
    /// cref="TerrestrialPlanet"/> is reported as an average.
    /// </summary>
    public class Season
    {
        /// <summary>
        /// The index of this item.
        /// </summary>
        public uint Index { get; }

        /// <summary>
        /// Indicates the proportion of one year represented by this <see cref="Season"/>.
        /// </summary>
        public double ProportionOfYear { get; }

        /// <summary>
        /// The climate of each <see cref="Tile"/> during this <see cref="Season"/>.
        /// </summary>
        public TileClimate[] TileClimates { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/>.
        /// </summary>
        private Season() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Season"/>.
        /// </summary>
        /// <param name="planet">The <see cref="TerrestrialPlanet"/> on which this season occurs.</param>
        /// <param name="index">The index of this season among the collection of a year.</param>
        /// <param name="amount">The number of seasons being generated for the current year.</param>
        /// <param name="trueAnomaly">The true anomaly of the <paramref name="planet"/>'s orbit at
        /// the time of this season (zero if not in orbit).</param>
        internal Season(TerrestrialPlanet planet, uint index, uint amount, double trueAnomaly)
        {
            Index = index;

            ProportionOfYear = 1.0 / amount;

            TileClimates = new TileClimate[planet.Grid.Tiles.Length];

            var solarEquator = -planet.AxialTilt * Math.Cos(MathConstants.TwoPI * ProportionOfYear * index) * (2.0 / 3.0);
            var latitudes = new double[planet.Grid.Tiles.Length];
            for (var j = 0; j < planet.Grid.Tiles.Length; j++)
            {
                var t = planet.Grid.Tiles[j];

                TileClimates[j] = new TileClimate();
                var seasonalLatitude = t.Latitude - solarEquator;
                if (seasonalLatitude > MathConstants.HalfPI)
                {
                    seasonalLatitude = Math.PI - seasonalLatitude;
                }
                else if (seasonalLatitude < -MathConstants.HalfPI)
                {
                    seasonalLatitude = -seasonalLatitude - Math.PI;
                }
                latitudes[j] = Math.Abs(seasonalLatitude);
            }

            SetTemperature(planet, trueAnomaly, latitudes);
            SetPrecipitation(planet, latitudes);
            latitudes = null;

            SetSnowAndIce(planet.Grid);
        }

        private void SetPrecipitation(TerrestrialPlanet planet, double[] latitudes)
        {
            for (var i = 0; i < planet.Grid.Tiles.Length; i++)
            {
                var tc = TileClimates[i];

                tc.Precipitation = (float)planet.GetPrecipitation(planet.Grid.Tiles[i].Vector, latitudes[i], tc.Temperature, ProportionOfYear, out var snow);
                tc.SnowFall = (float)snow;
            }
        }

        private void SetSnowAndIce(WorldGrid grid)
        {
            for (var i = 0; i < grid.Tiles.Length; i++)
            {
                var t = grid.Tiles[i];
                var tc = TileClimates[i];

                if (t.TerrainType == TerrainType.Water && !t.SeaIce.IsZero)
                {
                    if (t.SeaIce.Min > t.SeaIce.Max)
                    {
                        tc.SeaIce = ProportionOfYear >= t.SeaIce.Min || ProportionOfYear <= t.SeaIce.Max;
                    }
                    else
                    {
                        tc.SeaIce = ProportionOfYear >= t.SeaIce.Min && ProportionOfYear <= t.SeaIce.Max;
                    }
                }

                if (t.TerrainType != TerrainType.Water && !t.SnowCover.IsZero)
                {
                    if (tc.SnowFall > 0)
                    {
                        tc.SnowCover = true;
                    }
                    else if (t.SnowCover.Min > t.SnowCover.Max)
                    {
                        tc.SnowCover = ProportionOfYear >= t.SnowCover.Min || ProportionOfYear <= t.SnowCover.Max;
                    }
                    else
                    {
                        tc.SnowCover = ProportionOfYear >= t.SnowCover.Min && ProportionOfYear <= t.SnowCover.Max;
                    }
                }
            }
        }

        private void SetTemperature(Planetoid planet, double? trueAnomaly, double[] latitudes)
        {
            var latitudeTemperatures = new Dictionary<double, double>();
            var elevationTemperatures = new Dictionary<(double, double), double>();

            for (var i = 0; i < planet.Grid.Tiles.Length; i++)
            {
                var t = planet.Grid.Tiles[i];
                var tc = TileClimates[i];
                if (!latitudeTemperatures.TryGetValue(latitudes[i], out var surfaceTemp))
                {
                    surfaceTemp = trueAnomaly.HasValue
                        ? planet.GetSurfaceTemperatureAtTrueAnomaly(trueAnomaly.Value, latitudes[i])
                        : planet.GetSurfaceTemperature(latitudes[i]);
                    latitudeTemperatures.Add(latitudes[i], surfaceTemp);
                }

                var roundedElevation = Math.Round(Math.Max(0, t.Elevation) / 100) * 100;
                if (!elevationTemperatures.TryGetValue((surfaceTemp, roundedElevation), out var tempAtElevation))
                {
                    tempAtElevation = planet.GetTemperatureAtElevation(surfaceTemp, roundedElevation);
                    elevationTemperatures.Add((surfaceTemp, roundedElevation), tempAtElevation);
                }
                tc.Temperature = (float)tempAtElevation;
            }
        }
    }
}
