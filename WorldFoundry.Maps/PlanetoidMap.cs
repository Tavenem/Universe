using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Time;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Maps
{
    /// <summary>
    /// Static methods to assist with producing equirectangular projections that map the surface of
    /// a planetoid.
    /// </summary>
    public static class PlanetoidMap
    {
        private const double ArcticLatitudeRange = Math.PI / 16;
        private const double ArcticLatitude = MathAndScience.Constants.Doubles.MathConstants.HalfPI - ArcticLatitudeRange;
        private const double FifthPI = Math.PI / 5;
        private const double EighthPI = Math.PI / 8;

        private static readonly double _LowTemp = (Substances.All.Water.MeltingPoint ?? 0) - 48;

        /// <summary>
        /// Gets a specific value from a range which varies over the course of a year.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="moment">The time at which the calculation is to be performed.</param>
        /// <returns>The specific value from a range which varies over the course of a
        /// year.</returns>
        public static float GetAnnualRangeValue(
            this Planetoid planet,
            FloatRange range,
            Instant moment) => SurfaceMap.GetAnnualRangeValue(range, (float)planet.GetProportionOfYearAtTime(moment));

        /// <summary>
        /// Determines whether the given <paramref name="moment"/> falls within the range indicated.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="moment">The time at which the determination is to be performed.</param>
        /// <returns><see langword="true"/> if the range indicates a positive result for the given
        /// <paramref name="moment"/>; otherwise <see langword="false"/>.</returns>
        public static bool GetAnnualRangeIsPositiveAtTime(
            this Planetoid planet,
            FloatRange range,
            Instant moment) => SurfaceMap.GetAnnualRangeIsPositiveAtTime(range, (float)planet.GetProportionOfYearAtTime(moment));

        /// <summary>
        /// Calculates the atmospheric density for the given conditions, in kg/m³.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="winterTemperatures">A winter temperature map.</param>
        /// <param name="summerTemperatures">A summer temperature map.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="latitude">The latitude of the object.</param>
        /// <param name="longitude">The longitude of the object.</param>
        /// <param name="altitude">The altitude of the object.</param>
        /// <param name="surface">
        /// If <see langword="true"/> the determination is made for a location
        /// on the surface of the planetoid at the given elevation. Otherwise, the calculation is
        /// made for an elevation above the surface.
        /// </param>
        /// <param name="options">The map projection used.</param>
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        public static double GetAtmosphericDensity(
            this Planetoid planet,
            Image<L16> winterTemperatures,
            Image<L16> summerTemperatures,
            double proportionOfYear,
            double latitude,
            double longitude,
            double altitude,
            bool surface = true,
            MapProjectionOptions? options = null)
        {
            var surfaceTemp = planet.GetSurfaceTemperature(winterTemperatures, summerTemperatures, proportionOfYear, latitude, longitude, options);
            var tempAtElevation = planet.GetTemperatureAtElevation(surfaceTemp, altitude, surface);
            return planet.Atmosphere.GetAtmosphericDensity(planet, tempAtElevation, altitude);
        }

        /// <summary>
        /// Calculates the atmospheric drag on a spherical object within the <see
        /// cref="Atmosphere"/> of this <see cref="Planetoid"/> under given conditions, in N.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="winterTemperatures">A winter temperature map.</param>
        /// <param name="summerTemperatures">A summer temperature map.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="latitude">The latitude of the object.</param>
        /// <param name="longitude">The longitude of the object.</param>
        /// <param name="altitude">The altitude of the object.</param>
        /// <param name="speed">The speed of the object, in m/s.</param>
        /// <param name="surface">
        /// If <see langword="true"/> the determination is made for a location
        /// on the surface of the planetoid at the given elevation. Otherwise, the calculation is
        /// made for an elevation above the surface.
        /// </param>
        /// <param name="options">The map projection used.</param>
        /// <returns>The atmospheric drag on the object at the specified height, in N.</returns>
        /// <remarks>
        /// 0.47 is an arbitrary drag coefficient (that of a sphere in a fluid with a Reynolds
        /// number of 10⁴), which may not reflect the actual conditions at all, but the inaccuracy
        /// is accepted since the level of detailed information needed to calculate this value
        /// accurately is not desired in this library.
        /// </remarks>
        public static double GetAtmosphericDrag(
            this Planetoid planet,
            Image<L16> winterTemperatures,
            Image<L16> summerTemperatures,
            double proportionOfYear,
            double latitude,
            double longitude,
            double altitude,
            double speed,
            bool surface = true,
            MapProjectionOptions? options = null)
        {
            var surfaceTemp = planet.GetSurfaceTemperature(winterTemperatures, summerTemperatures, proportionOfYear, latitude, longitude, options);
            var tempAtElevation = planet.GetTemperatureAtElevation(surfaceTemp, altitude, surface);
            return planet.Atmosphere.GetAtmosphericDrag(planet, tempAtElevation, altitude, speed);
        }

        /// <summary>
        /// Calculates the atmospheric pressure at a given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, at the given true anomaly of the planet's orbit, in kPa.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="winterTemperatures">A winter temperature map.</param>
        /// <param name="summerTemperatures">A summer temperature map.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="latitude">The latitude at which to determine atmospheric pressure.</param>
        /// <param name="longitude">The longitude at which to determine atmospheric
        /// pressure.</param>
        /// <param name="surface">
        /// If <see langword="true"/> the determination is made for a location
        /// on the surface of the planetoid at the given elevation. Otherwise, the calculation is
        /// made for an elevation above the surface.
        /// </param>
        /// <param name="options">The map projection used.</param>
        /// <returns>The atmospheric pressure at the specified height, in kPa.</returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the pressure lapse rate varies considerably in the
        /// different atmospheric layers, but this cannot be easily modeled for arbitrary
        /// exoplanetary atmospheres, so the simple barometric formula is used, which should be
        /// "close enough" for the purposes of this library. Also, this calculation uses the molar
        /// mass of air on Earth, which is clearly not correct for other atmospheres, but is
        /// considered "close enough" for the purposes of this library.
        /// </remarks>
        public static double GetAtmosphericPressure(
            this Planetoid planet,
            Image<L16> elevationMap,
            Image<L16> winterTemperatures,
            Image<L16> summerTemperatures,
            double proportionOfYear,
            double latitude,
            double longitude,
            bool surface = true,
            MapProjectionOptions? options = null)
        {
            var elevation = planet.GetElevationAt(elevationMap, latitude, longitude, options);
            var surfaceTemp = planet.GetSurfaceTemperature(winterTemperatures, summerTemperatures, proportionOfYear, latitude, longitude, options);
            var tempAtElevation = planet.GetTemperatureAtElevation(surfaceTemp, elevation, surface);
            return planet.Atmosphere.GetAtmosphericPressure(planet, tempAtElevation, elevation);
        }

        /// <summary>
        /// Gets the elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in meters.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="latitude">The latitude at which to determine elevation.</param>
        /// <param name="longitude">The longitude at which to determine elevation.</param>
        /// <param name="options">The map projection used.</param>
        /// <returns>
        /// The elevation at the given <paramref name="latitude"/> and <paramref name="longitude"/>,
        /// in meters.
        /// </returns>
        public static double GetElevationAt(
            this Planetoid planet,
            Image<L16> elevationMap,
            double latitude,
            double longitude,
            MapProjectionOptions? options = null) => (elevationMap.GetValueFromImage(
                latitude,
                longitude,
                options ?? MapProjectionOptions.Default,
                true)
            - planet.NormalizedSeaLevel)
            * planet.MaxElevation;

        /// <summary>
        /// Gets the elevation at the given <paramref name="position"/>, in meters.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="position">The position at which to determine elevation.</param>
        /// <param name="options">The map projection used.</param>
        /// <returns>
        /// The elevation at the given <paramref name="position"/>, in meters.
        /// </returns>
        public static double GetElevationAt(
            this Planetoid planet,
            Image<L16> elevationMap,
            Vector3 position,
            MapProjectionOptions? options = null)
            => planet.GetElevationAt(elevationMap, planet.VectorToLatitude(position), planet.VectorToLongitude(position), options);

        /// <summary>
        /// Generates an elevation map image for this planet.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="resolution">The vertical resolution of the map.</param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <returns>An elevation map image for this planet.</returns>
        public static Image<L16> GetElevationMap(
            this Planetoid planet,
            int resolution,
            MapProjectionOptions? options = null)
        {
            if (planet.MaxElevation.IsNearlyZero())
            {
                return SurfaceMapImage.GenerateZeroMapImage(resolution, options, true);
            }

            var noise1 = new FastNoise(planet.Seed1, 0.8, FastNoise.NoiseType.SimplexFractal, octaves: 6);
            var noise2 = new FastNoise(planet.Seed2, 0.6, FastNoise.NoiseType.SimplexFractal, FastNoise.FractalType.Billow, octaves: 6);
            var noise3 = new FastNoise(planet.Seed3, 1.2, FastNoise.NoiseType.Simplex);

            return SurfaceMapImage.GenerateMapImage(
                  (lat, lon) => GetElevationNoise(noise1, noise2, noise3, planet.LatitudeAndLongitudeToDoubleVector(lat, lon)),
                  resolution,
                  options,
                  true);
        }

        /// <summary>
        /// Gets the range of elevations represented by the given elevation map, in meters.
        /// </summary>
        /// <param name="planet">The planet mapped.</param>
        /// <param name="elevationMap">An elevation map image.</param>
        /// <returns>
        /// The minimum, maximum, and average elevations represented by this map image, in meters.
        /// </returns>
        public static FloatRange GetElevationRange(this Planetoid planet, Image<L16> elevationMap)
        {
            var range = elevationMap.GetRange(true);
            return new FloatRange(
                (float)((range.Min - planet.NormalizedSeaLevel) * planet.MaxElevation),
                (float)((range.Average - planet.NormalizedSeaLevel) * planet.MaxElevation),
                (float)((range.Max - planet.NormalizedSeaLevel) * planet.MaxElevation));
        }

        /// <summary>
        /// Generates a new set of precipitation and snowfall map images.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="winterTemperatures">A winter temperature map.</param>
        /// <param name="summerTemperatues">A summer temperature map.</param>
        /// <param name="resolution">The vertical resolution.</param>
        /// <param name="steps">
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere).
        /// </param>
        /// <param name="temperatureProjection">
        /// <para>
        /// The map projection of the temperature maps. They must be the same.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// assumed.
        /// </para>
        /// </param>
        /// <param name="projection">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <returns>
        /// A set of precipitation and snowfall map images. Pixel luminosity indicates
        /// precipitation in mm/hr, relative to the <see cref="Atmosphere.MaxPrecipitation"/> of
        /// this planet's <see cref="Atmosphere"/>.
        /// </returns>
        public static (Image<L16>[] precipitationMaps, Image<L16>[] snowfallMaps) GetPrecipitationAndSnowfallMaps(
            this Planetoid planet,
            Image<L16> winterTemperatures,
            Image<L16> summerTemperatues,
            int resolution,
            int steps,
            MapProjectionOptions? temperatureProjection = null,
            MapProjectionOptions? projection = null)
        {
            var options = projection ?? MapProjectionOptions.Default;
            var precipitationMaps = new Image<L16>[steps];
            var snowMaps = new Image<L16>[steps];
            if (planet.Atmosphere.MaxPrecipitation.IsNearlyZero())
            {
                var xResolution = (int)Math.Floor(resolution * options.AspectRatio);
                for (var i = 0; i < steps; i++)
                {
                    precipitationMaps[i] = new Image<L16>(xResolution, resolution);
                    snowMaps[i] = new Image<L16>(xResolution, resolution);
                }
                return (precipitationMaps, snowMaps);
            }

            var noise1 = new FastNoise(planet.Seed4, 1.0, FastNoise.NoiseType.Simplex);
            var noise2 = new FastNoise(planet.Seed5, 3.0, FastNoise.NoiseType.SimplexFractal, octaves: 3);

            var proportionOfYear = 1f / steps;
            var proportionOfYearAtMidpoint = 0f;
            var trueAnomaly = planet.WinterSolsticeTrueAnomaly;
            var trueAnomalyPerSeason = MathAndScience.Constants.Doubles.MathConstants.TwoPI / steps;

            for (var i = 0; i < steps; i++)
            {
                var solarDeclination = planet.GetSolarDeclination(trueAnomaly);
                (precipitationMaps[i], snowMaps[i]) = SurfaceMapImage.GenerateMapImages(
                    new[] { winterTemperatures, summerTemperatues },
                    (lat, lon, temperature) =>
                    {
                        var precipitation = planet.GetPrecipitationNoise(
                            noise1,
                            noise2,
                            planet.LatitudeAndLongitudeToDoubleVector(lat, lon),
                            lat,
                            Planetoid.GetSeasonalLatitudeFromDeclination(lat, solarDeclination),
                            temperature * SurfaceMapImage.TemperatureScaleFactor,
                            out var snow);
                        return (
                            precipitation / planet.Atmosphere.MaxPrecipitation,
                            snow / planet.Atmosphere.MaxSnowfall);
                    },
                    resolution,
                    proportionOfYearAtMidpoint,
                    temperatureProjection ?? MapProjectionOptions.Default,
                    options);
                proportionOfYearAtMidpoint += proportionOfYear;
                trueAnomaly += trueAnomalyPerSeason;
                if (trueAnomaly >= MathAndScience.Constants.Doubles.MathConstants.TwoPI)
                {
                    trueAnomaly -= MathAndScience.Constants.Doubles.MathConstants.TwoPI;
                }
            }

            return (precipitationMaps, snowMaps);
        }

        /// <summary>
        /// Gets the precipitation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in mm/hr.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="precipitationMap">A precipitation map.</param>
        /// <param name="latitude">The latitude at which to determine precipitation.</param>
        /// <param name="longitude">The longitude at which to determine precipitation.</param>
        /// <param name="options">The map projection used.</param>
        /// <returns>
        /// The precipitation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in mm/hr.
        /// </returns>
        public static double GetPrecipitationAt(
            this Planetoid planet,
            Image<L16> precipitationMap,
            double latitude,
            double longitude,
            MapProjectionOptions? options = null) => precipitationMap.GetValueFromImage(
                latitude,
                longitude,
                options ?? MapProjectionOptions.Default,
                true)
            * planet.Atmosphere.MaxPrecipitation;

        /// <summary>
        /// Gets the precipitation at the given <paramref name="position"/>, in mm/hr.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="precipitationMap">A precipitation map.</param>
        /// <param name="position">The longitude at which to determine precipitation.</param>
        /// <param name="options">The map projection used.</param>
        /// <returns>
        /// The precipitation at the given <paramref name="position"/>, in mm/hr.
        /// </returns>
        public static double GetPrecipitationAt(
            this Planetoid planet,
            Image<L16> precipitationMap,
            Vector3 position,
            MapProjectionOptions? options = null)
            => planet.GetPrecipitationAt(precipitationMap, planet.VectorToLatitude(position), planet.VectorToLongitude(position), options);

        /// <summary>
        /// Generates a precipitation map image for this planet at the given proportion of a year.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="precipitationMaps">A set of precipitation maps.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <returns>
        /// A precipitation map image for this planet at the given proportion of a year. Pixel
        /// luminosity indicates precipitation in mm/hr, relative to the <see
        /// cref="Atmosphere.MaxPrecipitation"/> of this planet's <see cref="Atmosphere"/>.
        /// </returns>
        public static Image<L16> GetPrecipitationMap(
#pragma warning disable IDE0060, RCS1175 // Unused this parameter: make extension.
            this Planetoid? planet,
#pragma warning restore RCS1175 // Unused this parameter.
            Image<L16>[] precipitationMaps,
            double proportionOfYear)
        {
            var proportionPerMap = 1.0 / precipitationMaps.Length;
            var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, precipitationMaps.Length - 1);
            var weight = proportionOfYear % proportionPerMap;
            if (weight.IsNearlyZero())
            {
                return precipitationMaps[season].CloneAs<L16>();
            }

            var nextSeason = season == precipitationMaps.Length - 1
                ? 0
                : season + 1;
            return SurfaceMapImage.InterpolateImages(
                precipitationMaps[season],
                precipitationMaps[nextSeason],
                weight);
        }

        /// <summary>
        /// Gets the range of precipitations represented by the given precipitation map, in mm/hr.
        /// </summary>
        /// <param name="planet">The planet mapped.</param>
        /// <param name="precipitationMap">A precipitation map image.</param>
        /// <returns>
        /// The minimum, maximum, and average precipitations represented by this map image, in
        /// mm/hr.
        /// </returns>
        public static FloatRange GetPrecipitationRange(this Planetoid planet, Image<L16> precipitationMap)
        {
            var range = precipitationMap.GetRange();
            return new FloatRange(
                (float)(range.Min * planet.Atmosphere.MaxPrecipitation),
                (float)(range.Average * planet.Atmosphere.MaxPrecipitation),
                (float)(range.Max * planet.Atmosphere.MaxPrecipitation));
        }

        /// <summary>
        /// Calculates the slope at the given coordinates, as the ratio of rise over run from the
        /// point to the point 1 arc-second away in the cardinal direction which is at the steepest
        /// angle.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="latitude">The latitude of the point.</param>
        /// <param name="longitude">The longitude of the point.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>The slope at the given coordinates.</returns>
        public static double GetSlope(
            this Planetoid planet,
            Image<L16> elevationMap,
            double latitude,
            double longitude,
            MapProjectionOptions? options = null)
        {
            var xResolution = elevationMap.Width;
            var yResolution = elevationMap.Height;
            var (x, y) = SurfaceMap.GetProjectionFromLatLong(latitude, longitude, xResolution, yResolution, options);
            return GetSlope(elevationMap, x, y, planet, xResolution, yResolution, options ?? MapProjectionOptions.Default);
        }

        /// <summary>
        /// Gets the snowfall at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in mm/hr.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="snowfallMap">A snowfall map.</param>
        /// <param name="latitude">The latitude at which to determine snowfall.</param>
        /// <param name="longitude">The longitude at which to determine snowfall.</param>
        /// <param name="options">The map projection used.</param>
        /// <returns>
        /// The snowfall at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in mm/hr.
        /// </returns>
        public static double GetSnowfallAt(
            this Planetoid planet,
            Image<L16> snowfallMap,
            double latitude,
            double longitude,
            MapProjectionOptions? options = null) => snowfallMap.GetValueFromImage(
                latitude,
                longitude,
                options ?? MapProjectionOptions.Default,
                true)
            * planet.Atmosphere.MaxSnowfall;

        /// <summary>
        /// Gets the snowfall at the given <paramref name="position"/>, in mm/hr.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="snowfallMap">A snowfall map.</param>
        /// <param name="position">The longitude at which to determine snowfall.</param>
        /// <param name="options">The map projection used.</param>
        /// <returns>
        /// The snowfall at the given <paramref name="position"/>, in mm/hr.
        /// </returns>
        public static double GetSnowfallAt(
            this Planetoid planet,
            Image<L16> snowfallMap,
            Vector3 position,
            MapProjectionOptions? options = null)
            => planet.GetSnowfallAt(snowfallMap, planet.VectorToLatitude(position), planet.VectorToLongitude(position), options);

        /// <summary>
        /// Generates a snowfall map image for this planet at the given proportion of a year.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="snowfallMaps">A set of snowfall maps.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <returns>
        /// A snowfall map image for this planet at the given proportion of a year. Pixel
        /// luminosity indicates snowfall in mm/hr, relative to the <see
        /// cref="Atmosphere.MaxPrecipitation"/> of this planet's <see cref="Atmosphere"/>.
        /// </returns>
        public static Image<L16> GetSnowfallMap(
#pragma warning disable IDE0060, RCS1175 // Unused this parameter: make extension.
            this Planetoid? planet,
#pragma warning restore RCS1175 // Unused this parameter.
            Image<L16>[] snowfallMaps,
            double proportionOfYear)
        {
            var proportionPerMap = 1.0 / snowfallMaps.Length;
            var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, snowfallMaps.Length - 1);
            var weight = proportionOfYear % proportionPerMap;
            if (weight.IsNearlyZero())
            {
                return snowfallMaps[season].CloneAs<L16>();
            }

            var nextSeason = season == snowfallMaps.Length - 1
                ? 0
                : season + 1;
            return SurfaceMapImage.InterpolateImages(
                snowfallMaps[season],
                snowfallMaps[nextSeason],
                weight);
        }

        /// <summary>
        /// Gets the range of snowfall represented by this map image, in mm/hr.
        /// </summary>
        /// <param name="planet">The planet mapped.</param>
        /// <param name="snowfallMap">A snowfall map image.</param>
        /// <returns>
        /// The minimum, maximum, and average snowfall represented by this map image, in
        /// mm/hr.
        /// </returns>
        public static FloatRange GetSnowfallRange(this Planetoid planet, Image<L16> snowfallMap)
        {
            var range = snowfallMap.GetRange();
            return new FloatRange(
                (float)(range.Min * planet.Atmosphere.MaxSnowfall),
                (float)(range.Average * planet.Atmosphere.MaxSnowfall),
                (float)(range.Max * planet.Atmosphere.MaxSnowfall));
        }

        /// <summary>
        /// Calculates the surface temperature at the given position, in K.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="winterTemperatures">A winter temperature map.</param>
        /// <param name="summerTemperatures">A summer temperature map.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="latitude">
        /// The latitude at which to calculate the temperature, in radians.
        /// </param>
        /// <param name="longitude">
        /// The latitude at which to calculate the temperature, in radians.
        /// </param>
        /// <param name="options">The map projection used.</param>
        /// <returns>The surface temperature, in K.</returns>
        public static double GetSurfaceTemperature(
#pragma warning disable IDE0060, RCS1175 // Unused this parameter: make extension.
            this Planetoid? planet,
#pragma warning restore RCS1175 // Unused this parameter.
            Image<L16> winterTemperatures,
            Image<L16> summerTemperatures,
            double proportionOfYear,
            double latitude,
            double longitude,
            MapProjectionOptions? options = null)
        {
            var (x, y) = SurfaceMap.GetProjectionFromLatLong(latitude, longitude, winterTemperatures.Width, winterTemperatures.Height, options);
            return SurfaceMapImage.InterpolateAmongImages(winterTemperatures, summerTemperatures, proportionOfYear, x, y)
                * SurfaceMapImage.TemperatureScaleFactor;
        }

        /// <summary>
        /// Calculates the range of temperatures at the given <paramref name="latitude"/> and
        /// <paramref name="longitude"/>, in K.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="winterTemperatures">A winter temperature map.</param>
        /// <param name="summerTemperatures">A summer temperature map.</param>
        /// <param name="latitude">
        /// The latitude at which to calculate the temperature range, in radians.
        /// </param>
        /// <param name="longitude">
        /// The latitude at which to calculate the temperature range, in radians.
        /// </param>
        /// <param name="options">The map projection used.</param>
        /// <returns>
        /// A <see cref="FloatRange"/> giving the range of temperatures at the given <paramref
        /// name="latitude"/> and <paramref name="longitude"/>, in K.
        /// </returns>
        public static FloatRange GetSurfaceTemperature(
#pragma warning disable IDE0060, RCS1175 // Unused this parameter: make extension.
            this Planetoid? planet,
#pragma warning restore RCS1175 // Unused this parameter.
            Image<L16> winterTemperatures,
            Image<L16> summerTemperatures,
            double latitude,
            double longitude,
            MapProjectionOptions? options = null)
        {
            var winterTemperature = winterTemperatures.GetTemperature(latitude, longitude, options ?? MapProjectionOptions.Default);
            var summerTemperature = summerTemperatures.GetTemperature(latitude, longitude, options ?? MapProjectionOptions.Default);
            if (winterTemperature <= summerTemperature)
            {
                return new FloatRange((float)winterTemperature, (float)summerTemperature);
            }
            return new FloatRange((float)summerTemperature, (float)winterTemperature);
        }

        /// <summary>
        /// Generates a temperature map image for this planet at the given proportion of a year.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="winterTemperatures">A winter temperature map.</param>
        /// <param name="summerTemperatures">A summer temperature map.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <returns>
        /// A temperature map image for this planet at the given proportion of a year.
        /// </returns>
        public static Image<L16> GetTemperatureMap(
#pragma warning disable IDE0060, RCS1175 // Unused this parameter: make extension.
            this Planetoid? planet,
#pragma warning restore RCS1175 // Unused this parameter.
            Image<L16> winterTemperatures,
            Image<L16> summerTemperatures,
            double proportionOfYear) => SurfaceMapImage.InterpolateImages(
                winterTemperatures,
                summerTemperatures,
                proportionOfYear);

        /// <summary>
        /// Generates new winter and summer temperature map images.
        /// </summary>
        /// <param name="planet">The planet to be mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="resolution">The vertical resolution.</param>
        /// <param name="elevationProjection">
        /// <para>
        /// The map projection of the elevation map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// assumed.
        /// </para>
        /// </param>
        /// <param name="projection">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <returns>Winter and summer temperature map images.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="projection"/> specifies latitudes or longitudes not included in
        /// <paramref name="elevationProjection"/>
        /// </exception>
        public static (Image<L16> winter, Image<L16> summer) GetTemperatureMaps(
            this Planetoid planet,
            Image<L16> elevationMap,
            int resolution,
            MapProjectionOptions? elevationProjection = null,
            MapProjectionOptions? projection = null)
        {
            var elevationOptions = elevationProjection ?? MapProjectionOptions.Default;
            var options = projection ?? MapProjectionOptions.Default;
            var tilt = planet.AxialTilt;
            var winterTrueAnomaly = planet.WinterSolsticeTrueAnomaly;
            var summerTrueAnomaly = planet.SummerSolsticeTrueAnomaly;
            var winterLatitudes = new Dictionary<double, double>();
            var summerLatitudes = new Dictionary<double, double>();
            var latitudeTemperatures = new Dictionary<double, double>();
            var elevationTemperatures = new Dictionary<(double, int), double>();
            var winter = SurfaceMapImage.GenerateMapImage(
                elevationMap,
                (lat, _, elevation) =>
                {
                    var roundedElevation = (int)Math.Round(Math.Max(0, (elevation - planet.NormalizedSeaLevel) * planet.MaxElevation) / 100) * 100;

                    if (!winterLatitudes.TryGetValue(lat, out var winterLat))
                    {
                        winterLat = Math.Abs((lat + Planetoid.GetSeasonalLatitudeFromDeclination(lat, tilt)) / 2);
                        winterLatitudes.Add(lat, winterLat);
                    }
                    if (!latitudeTemperatures.TryGetValue(winterLat, out var winterTemp))
                    {
                        winterTemp = planet.GetSurfaceTemperatureAtTrueAnomaly(winterTrueAnomaly, winterLat);
                        latitudeTemperatures.Add(winterLat, winterTemp);
                    }
                    if (!elevationTemperatures.TryGetValue((winterTemp, roundedElevation), out var winterTempAtElevation))
                    {
                        winterTempAtElevation = planet.GetTemperatureAtElevation(winterTemp, roundedElevation);
                        elevationTemperatures.Add((winterTemp, roundedElevation), winterTempAtElevation);
                    }

                    return winterTempAtElevation / SurfaceMapImage.TemperatureScaleFactor;
                },
                resolution,
                elevationOptions,
                options);
            var summer = SurfaceMapImage.GenerateMapImage(
                elevationMap,
                (lat, _, elevation) =>
                {
                    var roundedElevation = (int)Math.Round(Math.Max(0, (elevation - planet.NormalizedSeaLevel) * planet.MaxElevation) / 100) * 100;

                    if (!summerLatitudes.TryGetValue(lat, out var summerLat))
                    {
                        summerLat = Math.Abs((lat + Planetoid.GetSeasonalLatitudeFromDeclination(lat, -tilt)) / 2);
                        summerLatitudes.Add(lat, summerLat);
                    }
                    if (!latitudeTemperatures.TryGetValue(summerLat, out var summerTemp))
                    {
                        summerTemp = planet.GetSurfaceTemperatureAtTrueAnomaly(summerTrueAnomaly, summerLat);
                        latitudeTemperatures.Add(summerLat, summerTemp);
                    }
                    if (!elevationTemperatures.TryGetValue((summerTemp, roundedElevation), out var summerTempAtElevation))
                    {
                        summerTempAtElevation = planet.GetTemperatureAtElevation(summerTemp, roundedElevation);
                        elevationTemperatures.Add((summerTemp, roundedElevation), summerTempAtElevation);
                    }

                    return summerTempAtElevation / SurfaceMapImage.TemperatureScaleFactor;
                },
                resolution,
                elevationOptions,
                options);
            return (winter, summer);
        }

        /// <summary>
        /// Determines if the given position is mountainous (see Remarks).
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="latitude">The latitude of the position to check.</param>
        /// <param name="longitude">The longitude of the position to check.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the given position is mountainous; otherwise <see
        /// langword="false"/>.
        /// </returns>
        /// <remarks>
        /// "Mountainous" is defined as having a maximum elevation greater than 8.5% of the maximum
        /// elevation of this planet, or a maximum elevation greater than 5% of the maximum and a
        /// slope greater than 0.035, or a maximum elevation greater than 3.5% of the maximum and a
        /// slope greater than 0.0875.
        /// </remarks>
        public static bool IsMountainous(
            this Planetoid planet,
            Image<L16> elevationMap,
            double latitude,
            double longitude,
            MapProjectionOptions? options = null)
        {
            var xResolution = elevationMap.Width;
            var yResolution = elevationMap.Height;
            var (x, y) = SurfaceMap.GetProjectionFromLatLong(latitude, longitude, xResolution, yResolution, options);

            var elevation = elevationMap[x, y].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
            if (elevation < 0.035)
            {
                return false;
            }
            if (elevation > 0.085)
            {
                return true;
            }
            var slope = GetSlope(elevationMap, x, y, planet, xResolution, yResolution, options ?? MapProjectionOptions.Default);
            if (elevation > 0.05)
            {
                return slope > 0.035;
            }
            return slope > 0.0875;
        }

        private static double GetElevationNoise(FastNoise noise1, FastNoise noise2, FastNoise noise3, double x, double y, double z)
        {
            // Initial noise map: a simple fractal noise map.
            var baseNoise = noise1.GetNoise(x, y, z);

            // Mountain noise map: a more ridged map.
            var mountains = (-noise2.GetNoise(x, y, z) - 0.25) * 4 / 3;

            // Scale the base noise to the typical average height of continents, with a degree of
            // randomness borrowed from the mountain noise function.
            var scaledBaseNoise = (baseNoise * (0.25 + (mountains * 0.0625))) - 0.04;

            // Modify the mountain map to indicate mountains only in random areas, instead of
            // uniformly across the globe.
            mountains *= (noise3.GetNoise(x, y, z) + 1).Clamp(0, 1);

            // Multiply with itself to produce predominantly low values with high (and low)
            // extremes, and scale to the typical maximum height of mountains, with a degree of
            // randomness borrowed from the base noise function.
            mountains = Math.CopySign(mountains * mountains * (0.525 + (baseNoise * 0.13125)), mountains);

            // The combined value is returned, resulting in mostly broad, low-lying areas,
            // interrupted by occasional high mountain ranges and low trenches.
            return scaledBaseNoise + mountains;
        }

        private static double GetElevationNoise(FastNoise noise1, FastNoise noise2, FastNoise noise3, MathAndScience.Numerics.Doubles.Vector3 position)
            => GetElevationNoise(noise1, noise2, noise3, position.X, position.Y, position.Z);

        private static double GetPrecipitationNoise(
            this Planetoid planet,
            FastNoise noise1,
            FastNoise noise2,
            double x,
            double y,
            double z,
            double latitude,
            double seasonalLatitude,
            double temperature,
            out double snow)
        {
            snow = 0;

            // Noise map with smooth, broad areas. Random range ~0.5-2.
            var r1 = 1.25 + (noise1.GetNoise(x, y, z) * 0.75);

            // More detailed noise map. Random range of ~0-1.35.
            var r2 = 0.675 + (noise2.GetNoise(x, y, z) * 0.75);

            // Combined map is noise with broad similarity over regions, and minor local
            // diversity. Range ~0.5-3.35.
            var r = r1 * r2;

            // Hadley cells alter local conditions.
            var absLatitude = Math.Abs(latitude);
            var absSeasonalLatitude = Math.Abs((latitude + seasonalLatitude) / 2);
            var hadleyValue = 0.0;

            // The polar deserts above ~±10º result in almost no precipitation
            if (absLatitude > ArcticLatitude)
            {
                // Range ~-3-~0.
                hadleyValue = -3 * ((absLatitude - ArcticLatitude) / ArcticLatitudeRange);
            }

            // The horse latitudes create the subtropical deserts between ~±35º-30º
            if (absLatitude < FifthPI)
            {
                // Range ~-3-0.
                hadleyValue = 2 * (r1 - 2) * ((FifthPI - absLatitude) / FifthPI);

                // The ITCZ increases in intensity towards the thermal equator
                if (absSeasonalLatitude < EighthPI)
                {
                    // Range 0-~33.5.
                    hadleyValue += 10 * r * ((EighthPI - absSeasonalLatitude) / EighthPI).Cube();
                }
            }

            // Relative humidity is the Hadley cell value added to the random value. Range ~-2.5-~36.85.
            var relativeHumidity = r + hadleyValue;

            // In the range betwen 32K and 48K below freezing, the value is scaled down; below that
            // range it is cut off completely; above it is unchanged.
            relativeHumidity *= ((temperature - _LowTemp) / 16).Clamp(0, 1);

            if (relativeHumidity <= 0)
            {
                return 0;
            }

            var precipitation = planet.Atmosphere.AveragePrecipitation * relativeHumidity;

            if (temperature <= Substances.All.Water.MeltingPoint)
            {
                snow = precipitation * Atmosphere.SnowToRainRatio;
            }

            return precipitation;
        }

        private static double GetPrecipitationNoise(
            this Planetoid planet,
            FastNoise noise1,
            FastNoise noise2,
            MathAndScience.Numerics.Doubles.Vector3 position,
            double latitude,
            double seasonalLatitude,
            double temperature,
            out double snow)
            => GetPrecipitationNoise(
                planet,
                noise1,
                noise2,
                position.X,
                position.Y,
                position.Z,
                latitude,
                seasonalLatitude,
                temperature,
                out snow);

        private static double GetSlope(
            Image<L16> elevationMap,
            int x, int y,
            Planetoid planet,
            int xResolution,
            int yResolution,
            MapProjectionOptions options)
        {
            // Calculations are invalid at the top or bottom, so adjust by 1 pixel.
            if (y == 0)
            {
                y = 1;
            }
            if (y == yResolution - 1)
            {
                y = yResolution - 2;
            }
            // left: x - 1, y
            var left = x == 0
                ? 1
                : x - 1;
            // up: x, y - 1
            var up = y == 0
                ? yResolution - 1
                : y - 1;
            // right: x + 1, y
            var right = x == xResolution - 1
                ? 0
                : x + 1;
            // down: x, y + 1
            var down = y == yResolution - 1
                ? yResolution - 2
                : y + 1;

            var elevation = elevationMap[x, y].GetValueFromPixel_PosNeg();
            var distance = (double)SurfaceMap.GetSeparationOfPointFromRadiusSquared(planet.RadiusSquared, x, y, yResolution, options);

            // north
            var slope = Math.Abs(elevation - elevationMap[x, up].GetValueFromPixel_PosNeg()) * planet.MaxElevation / distance;

            // east
            slope = Math.Max(slope, Math.Abs(elevation - elevationMap[right, y].GetValueFromPixel_PosNeg()) * planet.MaxElevation / distance);

            // south
            slope = Math.Max(slope, Math.Abs(elevation - elevationMap[x, down].GetValueFromPixel_PosNeg()) * planet.MaxElevation / distance);

            // west
            return Math.Max(slope, Math.Abs(elevation - elevationMap[left, y].GetValueFromPixel_PosNeg()) * planet.MaxElevation / distance);
        }
    }
}
