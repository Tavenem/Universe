# Changelog

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
- Add `Flags` attribute to enums

## 0.1.1-preview
### Updated
- Update dependencies

## 0.1.0-preview
### Added
- Initial preview release