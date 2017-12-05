using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;
using WorldFoundry.Extensions;
using WorldFoundry.Substances;
using WorldFoundry.Utilities.MathUtil.Shapes;
using WorldFoundry.WorldGrids;

namespace WorldFoundry
{
    /// <summary>
    /// Represents a planet as a 3D grid of (mostly-hexagonal) tiles.
    /// </summary>
    public class Planet : TerrestrialPlanet
    {
        /// <summary>
        /// The default atmospheric pressure, used if none is specified during planet creation, in kPa.
        /// </summary>
        public const float DefaultAtmosphericPressure = 101.325f;

        /// <summary>
        /// The default axial tilt, used if none is specified during planet creation, in radians.
        /// </summary>
        public const float DefaultAxialTilt = 0.41f;

        /// <summary>
        /// The default grid size (level of detail), used if none is specified during planet creation.
        /// </summary>
        public const int DefaultGridSize = 6;

        /// <summary>
        /// The default planetary radius, used if none is specified during planet creation, in meters.
        /// </summary>
        public const int DefaultRadius = 6371000;

        /// <summary>
        /// The default period of revolution, used if none is specified during planet creation, in seconds.
        /// </summary>
        public const double DefaultRevolutionPeriod = 31558150;

        /// <summary>
        /// The default period of rotation, used if none is specified during planet creation, in seconds.
        /// </summary>
        public const double DefaultRotationalPeriod = 86164;

        /// <summary>
        /// The default ratio of water coverage, used if none is specified during planet creation.
        /// </summary>
        public const float DefaultWaterRatio = 0.65f;

        private const int DefaultDensity = 5514;

        /// <summary>
        /// The number of <see cref="Season"/> s in a year, based on the last <see cref="Season"/> set.
        /// </summary>
        public int SeasonCount { get; private set; }

        internal ICollection<Season> Seasons { get; private set; }

        /// <summary>
        /// The ratio of water coverage on the planet.
        /// </summary>
        public float WaterRatio { get; internal set; }

        /// <summary>
        /// Creates an empty <see cref="Planet"/> object. Has no useful values, and methods may not
        /// function properly. This constructor is intended for use by serialization routines, not to
        /// create a useful <see cref="Planet"/>.
        /// </summary>
        public Planet() { }

        /// <summary>
        /// Generates a planet with the given properties. In most cases, using the static <see
        /// cref="FromParams"/> method will be more useful than calling this constructor directly.
        /// </summary>
        public Planet(
            float atmosphericPressure,
            float axialTilt,
            int radius,
            double rotationalPeriod,
            float waterRatio,
            int gridSize,
            Guid? id = null)
        {
            ID = id ?? Guid.NewGuid();
            SetRadiusBase(radius);
            AxialTilt = axialTilt;
            SetRotationalPeriod(rotationalPeriod);
            WaterRatio = Math.Max(0, Math.Min(1, waterRatio));
            SetAtmosphericPressure(atmosphericPressure);

            GenerateWorldGrid(gridSize);

            SetWaterRatio(WaterRatio);
        }

        /// <summary>
        /// Generates a planet with the given properties. Default values will be used in place of any
        /// omitted parameters.
        /// </summary>
        public static Planet FromParams(
            float? atmosphericPressure = null,
            float? axialTilt = null,
            int? radius = null,
            double? rotationalPeriod = null,
            float? waterRatio = null,
            int? gridSize = null,
            Guid? id = null)
            => new Planet(
                atmosphericPressure ?? DefaultAtmosphericPressure,
                axialTilt ?? DefaultAxialTilt,
                radius ?? DefaultRadius,
                rotationalPeriod ?? DefaultRotationalPeriod,
                waterRatio ?? DefaultWaterRatio,
                gridSize ?? DefaultGridSize,
                id);

        /// <summary>
        /// Changes the atmospheric pressure of the planet.
        /// </summary>
        /// <param name="pressure">A pressure, in kPa.</param>
        public void ChangeAtmosphericPressure(float pressure)
        {
            SetAtmosphericPressure(pressure);
            Seasons.Clear();
        }

        /// <summary>
        /// Changes the axial tilt of the planet.
        /// </summary>
        /// <param name="axialTilt">An axial tilt, in radians.</param>
        public void ChangeAxialTilt(float axialTilt)
        {
            AxialTilt = axialTilt;
            Seasons.Clear();
        }

        /// <summary>
        /// Changes the radius of the planet.
        /// </summary>
        /// <param name="radius">A radius, in meters.</param>
        public void ChangeRadius(int radius)
        {
            SetRadiusBase(radius);
            Seasons.Clear();
        }

        private void ClassifyTerrain()
        {
            foreach (var t in WorldGrid.Tiles)
            {
                var land = 0;
                var water = 0;
                for (int i = 0; i < t.EdgeCount; i++)
                {
                    if (WorldGrid.GetCorner(t.GetCorner(i)).Elevation < 0)
                    {
                        water++;
                    }
                    else
                    {
                        land++;
                    }
                }
                if (t.Elevation < 0)
                {
                    water++;
                }
                else
                {
                    land++;
                }
                t.TerrainType = (land > 0 && water > 0)
                    ? TerrainType.Coast
                    : (land > 0 ? TerrainType.Land : TerrainType.Water);
            }
            foreach (var c in WorldGrid.Corners)
            {
                var land = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (WorldGrid.GetCorner(c.GetCorner(i)).Elevation >= 0)
                    {
                        land++;
                    }
                }
                c.TerrainType = c.Elevation < 0
                    ? (land > 0 ? TerrainType.Coast : TerrainType.Water)
                    : TerrainType.Land;
            }
            foreach (var e in WorldGrid.Edges)
            {
                var type = TerrainType.Land;
                for (int i = 0; i < 2; i++)
                {
                    if (WorldGrid.GetCorner(e.GetCorner(i)).TerrainType != type)
                    {
                        type = i == 0 ? WorldGrid.GetTile(e.GetTile(i)).TerrainType : TerrainType.Coast;
                    }
                }
                e.TerrainType = type;
            }
        }

        private void CreateSea()
        {
            if (TMath.IsZero(HydrosphereSurface.Proportion))
            {
                return;
            }
            var waterMass = Mass
                * Hydrosphere.Proportion
                * (Hydrosphere.Mixtures.Count > 0 ? HydrosphereSurface.Proportion : 1)
                * (HydrosphereSurface.GetProportion(Chemical.Water, Phase.Any)
                + HydrosphereSurface.GetProportion(Chemical.Water_Salt, Phase.Any));
            var oceanMass = 0.0;
            var oceanTileCount = 0;
            var seaLevel = 0f;
            while (waterMass > oceanMass)
            {
                var orderedTiles = WorldGrid.Tiles.OrderBy(t => t.Elevation);
                var landTiles = orderedTiles.Skip(oceanTileCount);
                var lowestLandElevation = landTiles.FirstOrDefault()?.Elevation;
                var nextLowestLandElevation = landTiles.SkipWhile(t => t.Elevation == lowestLandElevation).FirstOrDefault()?.Elevation;
                seaLevel = lowestLandElevation.HasValue
                    ? (nextLowestLandElevation.HasValue
                        ? (lowestLandElevation.Value + nextLowestLandElevation.Value) / 2
                        : lowestLandElevation.Value * 1.1f)
                    : WorldGrid.Tiles.Max(t => t.Elevation) * 1.1f;
                if (!nextLowestLandElevation.HasValue)
                {
                    break;
                }
                oceanTileCount = orderedTiles.TakeWhile(t => t.Elevation <= lowestLandElevation).Count();
                oceanMass = orderedTiles.Take(oceanTileCount).Sum(x => x.Area * (seaLevel - x.Elevation));
            }

            foreach (var t in WorldGrid.Tiles)
            {
                t.Elevation -= seaLevel;
            }
            foreach (var c in WorldGrid.Corners)
            {
                c.Elevation -= seaLevel;
            }
        }

        private void CreateSea(float waterRatio)
        {
            if (waterRatio == 0)
            {
                return;
            }
            var seaLevel = 0f;
            var oceanTileCount = 0;
            var oceanMass = 0.0;
            if (waterRatio == 1)
            {
                seaLevel = WorldGrid.Tiles.Max(t => t.Elevation) * 1.1f;
                oceanTileCount = WorldGrid.Tiles.Count;
                oceanMass = WorldGrid.Tiles.Sum(x => x.Area * (seaLevel - x.Elevation));
            }
            else
            {
                var targetWaterTileCount = (int)Math.Round(waterRatio * WorldGrid.Tiles.Count);
                var orderedTiles = WorldGrid.Tiles.OrderBy(t => t.Elevation);
                var landTiles = orderedTiles.Skip(targetWaterTileCount);
                var lowestLandElevation = landTiles.FirstOrDefault()?.Elevation;
                var nextLowestLandElevation = landTiles.SkipWhile(t => t.Elevation == lowestLandElevation).FirstOrDefault()?.Elevation;
                seaLevel = lowestLandElevation.HasValue
                    ? (nextLowestLandElevation.HasValue
                        ? (lowestLandElevation.Value + nextLowestLandElevation.Value) / 2
                        : lowestLandElevation.Value * 1.1f)
                    : WorldGrid.Tiles.Max(t => t.Elevation) * 1.1f;
                oceanTileCount = nextLowestLandElevation.HasValue
                    ? orderedTiles.TakeWhile(t => t.Elevation <= lowestLandElevation).Count()
                    : WorldGrid.Tiles.Count;
                oceanMass = orderedTiles.Take(oceanTileCount).Sum(x => x.Area * (seaLevel - x.Elevation));
            }

            foreach (var t in WorldGrid.Tiles)
            {
                t.Elevation -= seaLevel;
            }
            foreach (var c in WorldGrid.Corners)
            {
                c.Elevation -= seaLevel;
            }

            var hydrosphereProportion = (float)(oceanMass / Mass);
            if (Hydrosphere.Mixtures.Count > 0)
            {
                hydrosphereProportion /= Hydrosphere.Proportion;
            }
            HydrosphereSurface.Proportion = hydrosphereProportion;
        }

        /// <summary>
        /// Gets or generates a <see cref="Season"/> for the planet.
        /// </summary>
        /// <param name="amount">
        /// The number of <see cref="Season"/>s in one year. Must be greater than or equal to 1.
        /// </param>
        /// <param name="index">
        /// The 0-based index of the new <see cref="Season"/> out of one year's worth.
        /// </param>
        /// <returns>A <see cref="Season"/> for the planet.</returns>
        public Season GetSeason(int amount, int index)
        {
            if (Orbit == null)
            {
                throw new Exception("Can only generate seasons for planets in orbit.");
            }
            if (amount < 1)
            {
                throw new ArgumentException($"{nameof(amount)} must be greater than or equal to 1.", nameof(amount));
            }
            if (index < 0)
            {
                throw new ArgumentException($"{nameof(index)} must be greater than or equal to 0.", nameof(index));
            }

            Season season, previousSeason = null;
            if (amount == SeasonCount)
            {
                season = Seasons?.FirstOrDefault(x => x.Index == index);
                if (season != null)
                {
                    return season;
                }
            }
            else if (index == 0)
            {
                previousSeason = Seasons.FirstOrDefault(x => x.Index == SeasonCount - 1);
            }
            else
            {
                GetSeason(amount, 0);
            }

            if (Seasons == null)
            {
                Seasons = new HashSet<Season>();
            }
            else if (SeasonCount != amount)
            {
                Seasons.Clear();
            }
            SeasonCount = amount;

            if (previousSeason == null)
            {
                previousSeason = index == 0
                    ? Seasons.FirstOrDefault(x => x.Index == SeasonCount - 1)
                    : Seasons.FirstOrDefault(x => x.Index == index - 1);
            }
            if (previousSeason == null)
            {
                if (index == 0)
                {
                    previousSeason = SetClimate();
                }
                else
                {
                    for (int i = 0; i < index; i++)
                    {
                        previousSeason = Seasons.FirstOrDefault(x => x.Index == i);
                        if (previousSeason == null)
                        {
                            previousSeason = GetSeason(amount, i);
                        }
                    }
                }
            }

            var seasonDuration = Orbit.Period / amount;
            var seasonProportion = 1.0f / amount;
            var seasonAngle = Utilities.MathUtil.Constants.TwoPI / amount;

            var winterAngle = AxialPrecession + Utilities.MathUtil.Constants.HalfPI;
            if (winterAngle >= Utilities.MathUtil.Constants.TwoPI)
            {
                winterAngle -= Utilities.MathUtil.Constants.TwoPI;
            }
            var seasonStartAngle = winterAngle + (seasonAngle / 2);

            var r0xz = new Vector3(Orbit.R0X, 0, Orbit.R0Z);
            var r0Angle = r0xz.GetAngle(Vector3.UnitX);
            var delta = seasonStartAngle - r0Angle;
            var seasonTrueAnomaly = Orbit.TrueAnomaly + delta;
            if (seasonTrueAnomaly < 0)
            {
                seasonTrueAnomaly += Utilities.MathUtil.Constants.TwoPI;
            }

            seasonTrueAnomaly += seasonAngle * index;
            if (seasonTrueAnomaly >= Utilities.MathUtil.Constants.TwoPI)
            {
                seasonTrueAnomaly -= Utilities.MathUtil.Constants.TwoPI;
            }
            var (r, v) = Orbit.GetStateVectorsForTrueAnomaly((float)seasonTrueAnomaly);

            var elapsedYearToDate = seasonProportion * index;

            season = new Season(
                index,
                this,
                r,
                seasonDuration,
                elapsedYearToDate,
                seasonProportion,
                previousSeason);
            Seasons.Add(season);
            return season;
        }

        private void SetAtmosphericPressure(float pressure)
        {
            var atmPressure = (float)Math.Max(0, Math.Min(3774.3562, pressure));
            habitabilityRequirements = new HabitabilityRequirements
            {
                MinimumSurfacePressure = atmPressure,
                MaximumSurfacePressure = atmPressure,
            };
            GenerateAtmosphere();
        }

        private Season SetClimate()
        {
            // A year is pre-generated as a single season, and another as 12 seasons, to prime the
            // algorithms, which produce better values with historical data.

            var winterAngle = AxialPrecession + Utilities.MathUtil.Constants.HalfPI;
            if (winterAngle >= Utilities.MathUtil.Constants.TwoPI)
            {
                winterAngle -= Utilities.MathUtil.Constants.TwoPI;
            }
            var r0xz = new Vector3(Orbit.R0X, 0, Orbit.R0Z);
            var r0Angle = r0xz.GetAngle(Vector3.UnitX);
            var delta = winterAngle - r0Angle;
            var seasonTrueAnomaly = Orbit.TrueAnomaly + delta;
            if (seasonTrueAnomaly < 0)
            {
                seasonTrueAnomaly += Utilities.MathUtil.Constants.TwoPI;
            }
            if (seasonTrueAnomaly >= Utilities.MathUtil.Constants.TwoPI)
            {
                seasonTrueAnomaly -= Utilities.MathUtil.Constants.TwoPI;
            }
            var (r, v) = Orbit.GetStateVectorsForTrueAnomaly((float)seasonTrueAnomaly);

            var season = new Season(1, this, r, Orbit.Period, 0, 1);

            var seasonAngle = Utilities.MathUtil.Constants.TwoPI / 12;
            var seasonStartAngle = winterAngle + (seasonAngle / 2);
            delta = seasonStartAngle - r0Angle;
            seasonTrueAnomaly = Orbit.TrueAnomaly + delta;
            if (seasonTrueAnomaly < 0)
            {
                seasonTrueAnomaly += Utilities.MathUtil.Constants.TwoPI;
            }
            var seasonDuration = Orbit.Period / 12;
            var seasonProportion = 1.0f / 12;
            var seasons = new List<Season>(12);
            for (int i = 0; i < 12; i++)
            {
                seasonTrueAnomaly += seasonAngle * i;
                if (seasonTrueAnomaly >= Utilities.MathUtil.Constants.TwoPI)
                {
                    seasonTrueAnomaly -= Utilities.MathUtil.Constants.TwoPI;
                }
                (r, v) = Orbit.GetStateVectorsForTrueAnomaly((float)seasonTrueAnomaly);

                season = new Season(i, this, r, seasonDuration, i / 12.0f, seasonProportion, season);
                seasons.Add(season);
            }

            for (int i = 0; i < WorldGrid.Tiles.Count; i++)
            {
                WorldGrid.GetTile(i).SetClimate(
                    seasons.Average(s => s.TileClimates[i].Temperature),
                    seasons.Sum(s => s.TileClimates[i].Precipitation));
            }

            return season;
        }

        /// <summary>
        /// Changes the <see cref="WorldGrid.GridSize"/> of the <see cref="Planet"/>'s <see cref="WorldGrid"/>.
        /// </summary>
        /// <param name="gridSize">The desired <see cref="WorldGrid.GridSize"/> (level of detail).</param>
        /// <param name="preserveShape">
        /// If true, the same random seed will be used for elevation generation as before, resulting
        /// in the same height map (can be used to maintain a similar look when changing <see
        /// cref="WorldGrid.GridSize"/>, rather than an entirely new geography).
        /// </param>
        public void SetGridSize(int gridSize, bool preserveShape = true)
        {
            WorldGrid.SetGridSize(gridSize, preserveShape);

            SetWaterRatio(WaterRatio);
        }

        private void SetRadiusBase(int radius)
        {
            GenerateShape(Math.Max(600000, Math.Min(radius, GetMaxRadius())));
            Mass = Shape.GetVolume() * Density;
        }

        /// <summary>
        /// Sets the ratio of water coverage on the planet.
        /// </summary>
        /// <param name="waterRatio">A ratio: 0 indicates no water; 1 indicates complete coverage.</param>
        public void SetWaterRatio(float waterRatio)
        {
            CreateSea(Math.Max(0, Math.Min(1, waterRatio)));
            ClassifyTerrain();
            Seasons.Clear();
        }
    }
}
