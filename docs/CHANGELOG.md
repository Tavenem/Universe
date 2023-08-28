# Changelog

## 0.6.2-preview
### Added
- `AddPlanetAsync` to `StarSystem`
### Changed
- `SetPopulationII` to `SetPopulationIIAsync` on `Star`

## 0.6.1-preview
### Added
- Allow setting orbit by eccentricity and period

## 0.6.0-preview
### Added
- `SetAveragePrecipitation` method to `Atmopshere`
- `orbited` parameter (`CosmicLocation`) to `CosmicLocation.NewAsteroidField` static method
- `OrbitedId` (`string`) and `Barycenter` (`Vector3<HugeNumber>`) properties to `Orbit`
- `orbitedId` (`string`) parameter to `Orbit` constructors and static factories
- `GetStateVectorsAfterDuration` method to `Orbit`
- `planetType` parameter (`PlanetType`) to `Planetoid.GetPlanetForStar` static method
- `AddResource`, `GenerateRings`, `RemoveResource`, `SetAlbedo`, `SetAngleOfRotation`, `SetAxialPrecession`, `SetHasBiosphere`, `SetHasMagnetosphere`, `SetNormalizedSeaLevelAsync`, `SetPlanetTypeAsync`, `SetSeaLevelAsync` methods to `Planetoid`
- `SetLuminosityAsync`, `SetLuminosityClassAsync`, `SetPopulationII`, `SetSpectralClassAsync`, `SetStarTypeAsync` methods to `Star`
- `RemoveLocationsAsync` methods to `Territory`
### Changed
- `Orbit.R0` is now relative to the orbit's `Barycenter` rather than its `OrbitedPosition`
- `Planetoid` public constructor `satellite` parameter (`bool`) replaced with `satelliteOf` parameter (`Planetoid`)
- `Planetoid.GetPositionAtTime` replaced with `GetPositionAtTimeAsync`, now takes star orbit into account in the case of multi-star systems
- `Planetoid.GetIllumination` replaced with `GetIlluminationAsync` (depended on `GetPositionAtTime`)
- `CosmicLocation.GetPositionAfterDuration` replaced with `GetPositionAfterDurationAsync`, which takes nested orbits into account (a body in orbit around another, which is also in orbit around something else)
- `CosmicLocation.GetPositionAtTime` replaced with `GetPositionAtTimeAsync`, which takes nested orbits into account
- `StarSystem.RemoveStar` replaced with `RemoveStarAsync`, which applies all relevant side effects rather than simply removing the ID
### Removed
- `HasBiosphere` public setter
- `GetLocalSunriseAndSunset`; must use `GetLocalSunriseAndSunsetAsync` (depended on `GetPositionAtTime`)
- `GetLocalTimeOfDay`; must use `GetLocalTimeOfDayAsync` (depended on `GetPositionAtTime`)
- `GetSatellitePhase`; must use `GetSatellitePhaseAsync` (depended on `GetPositionAtTime`)
- `position` and `orbit` parameters from `CosmicLocation.NewOortCloud` static method

## 0.5.5-preview
### Added
- `RemoveSatellite` method for `Planetoid`

## 0.5.4-preview
### Added
- Public `GenerateSatellite` methods for `Planetoid`

## 0.5.3-preview
### Changed
- `FastNoise` changes

## 0.5.2-preview
### Changed
- Improve seed generation algorithm

## 0.5.1-preview
### Changed
- Made `Orbit` a record

## 0.5.0-preview
### Added
- Source generated (de)serialization support
### Changed
- Change `Planetoid.Rings` type to `IReadOnlyList<PlanetaryRing>`
- Change `Planetoid.Resources` type to `IReadOnlyList<Resource>`
- Replace most read-only structs with records
### Updated
- Update to .NET 8 preview

## 0.4.6-preview
### Updated
- Update dependencies

## 0.4.5-preview
### Changed
- Update to .NET 7

## 0.4.4-preview
### Changed
- Update to .NET 7 preview

## 0.4.1-preview - 0.4.3-preview
### Updated
- Update dependencies

## 0.4.0-preview
### Changed
- Update to .NET 6 preview
- Update to C# 10 preview
### Removed
- Support for non-JSON serialization

## 0.3.2-preview
### Changed
- Allow rings in `PlanetParams` for Earthlike constructor to be determined randomly

## 0.3.1-preview
### Fixed
- Correct default value for rings in `PlanetParams` for Earthlike constructor

## 0.3.0-preview
### Changed
- Allow specifying ring system in `PlanetParams`

## 0.2.1-preview
### Updated
- Update dependencies

## 0.2.0-preview
### Added
- Added alternative, synchronous methods to `Planetoid` which assume a sun-like star at
  `Vector3.Zero`, which can be used in place of the asynchronous methods that attempt to load and
  utilize local system data.

## 0.1.3-preview - 0.1.5-preview
### Updated
- Update dependencies

## 0.1.2-preview
### Fixed
- Add `Flags` attribute to enumerations

## 0.1.1-preview
### Updated
- Update dependencies

## 0.1.0-preview
### Added
- Initial preview release