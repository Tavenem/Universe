import * as glM from 'gl-matrix';
import * as PlanetData from './planet-data';

const halfPI = Math.PI / 2;
const threeHalvesPI = Math.PI * 3 / 2;

export enum PlanetColorMode {
    Vegetation,
    Elevation,
    Temperature,
    Precipitation,
}

export interface Color {
    [index: number]: number;
}

export class PlanetColors {
    private static freezingPoint = 273.15;

    private static color_land_low: Color = [0.95, 0.81, 0.53, 1.0];
    private static color_land_high: Color = [0.29, 0.16, 0.11, 1.0];

    private static color_snow: Color = [1.0, 1.0, 1.0, 1.0];

    private static colors_biome: Color[][] = [[
            [0.500, 0.500, 0.500, 1.0], // undefined
        ],[
            [0.752, 0.752, 0.752, 1.0], // polar desert
            [1.000, 1.000, 1.000, 1.0], // polar ice
        ],[
            [0.501, 0.501, 0.501, 1.0], // subpolar dry tundra
            [0.203, 0.282, 0.219, 1.0], // subpolar moist tundra
            [0.078, 0.282, 0.219, 1.0], // subpolar wet tundra
            [0.000, 0.282, 0.219, 1.0], // subpolar rain tundra
        ],[
            [0.627, 0.627, 0.501, 1.0], // boreal desert
            [0.501, 0.627, 0.501, 1.0], // boreal dry scrub
            [0.156, 0.407, 0.349, 1.0], // boreal moist forest
            [0.031, 0.407, 0.349, 1.0], // boreal wet forest
            [0.000, 0.407, 0.349, 1.0], // boreal rain forest
        ],[
            [0.752, 0.752, 0.501, 1.0], // cool temperate desert
            [0.627, 0.752, 0.501, 1.0], // cool temperate desert scrub
            [0.501, 0.752, 0.501, 1.0], // cool temperate steppe
            [0.266, 0.682, 0.478, 1.0], // cool temperate moist forest
            [0.141, 0.682, 0.478, 1.0], // cool temperate wet forest
            [0.015, 0.682, 0.478, 1.0], // cool temperate rain forest
        ],[
            [0.878, 0.878, 0.501, 1.0], // warm temperate desert
            [0.752, 0.878, 0.501, 1.0], // warm temperate desert scrub
            [0.627, 0.878, 0.501, 1.0], // warm temperate thorn scrub
            [0.317, 0.733, 0.400, 1.0], // warm temperate dry forest
            [0.192, 0.733, 0.400, 1.0], // warm temperate moist forest
            [0.066, 0.733, 0.400, 1.0], // warm temperate wet forest
            [0.000, 0.733, 0.400, 1.0], // warm temperate rain forest
        ],[
            [0.941, 0.941, 0.501, 1.0], // subtropical desert
            [0.815, 0.941, 0.501, 1.0], // subtropical desert scrub
            [0.690, 0.941, 0.501, 1.0], // subtropical thorn woodland
            [0.501, 0.941, 0.501, 1.0], // subtropical dry forest
            [0.376, 0.941, 0.501, 1.0], // subtropical moist forest
            [0.250, 0.941, 0.564, 1.0], // subtropical wet forest
            [0.125, 1.000, 0.564, 1.0], // subtropical rain forest
        ],[
            [1.000, 1.000, 0.501, 1.0], // tropical desert
            [0.878, 1.000, 0.501, 1.0], // tropical desert scrub
            [0.752, 1.000, 0.501, 1.0], // tropical thorn woodland
            [0.407, 0.686, 0.109, 1.0], // tropical very dry forest
            [0.282, 0.686, 0.109, 1.0], // tropical dry forest
            [0.156, 0.686, 0.109, 1.0], // tropical moist forest
            [0.031, 0.686, 0.109, 1.0], // tropical wet forest
            [0.000, 0.686, 0.172, 1.0], // tropical rain forest
        ],
    ];

    private static colors_elevation: Color[] = [
        [1.0, 1.0, 0.0, 1.0],
        [1.0, 0.0, 0.0, 1.0],
        [0.0, 1.0, 0.0, 1.0],
        [0.0, 0.0, 1.0, 1.0],
    ];

    private static colors_precipitation: Color[] = [
        [1.0, 1.0, 0.5, 1.0],
        [0.5, 0.85, 0.25, 1.0],
        [0.0, 0.7, 0.0, 1.0],
        [0.0, 0.55, 0.0, 1.0],
        [0.0, 0.4, 0.0, 1.0],
    ];

    private static colors_temperature: Color[] = [
        [1.0, 1.0, 1.0, 1.0], // white: -50
        [1.0, 0.7, 1.0, 1.0], // light purple: -15
        [0.5, 0.3, 0.6, 1.0], // purple: -5
        [0.3, 0.3, 0.7, 1.0], // blue: 0
        [0.0, 1.0, 1.0, 1.0], // cyan: 5
        [0.3, 0.8, 0.3, 1.0], // green: 12
        [1.0, 1.0, 0.0, 1.0], // yellow: 20
        [0.8, 0.4, 0.0, 1.0], // orange: 30
        [0.6, 0.1, 0.0, 1.0], // dark red: 37
    ];
    private static temperature_limits = [-50, -15, -5, 0, 5, 12, 20, 30, 37];

    private static high_snow = 8000;

    tileColors: Color[][];

    constructor(planet: PlanetData.Planet, season: number, mode: PlanetColorMode = PlanetColorMode.Vegetation) {
        this.tileColors = Array.from({ length: planet.seasons.length },
            (v, k) => k === season ? Array.from({ length: planet.tiles.length }, v => [0.0, 0.0, 0.0, 1.0]) : undefined);

        this.addSeason(planet, season, mode);
    }

    public addSeason(planet: PlanetData.Planet, season: number, mode: PlanetColorMode = PlanetColorMode.Vegetation) {
        this.tileColors[season] = Array.from({ length: planet.tiles.length }, v => [0.0, 0.0, 0.0, 1.0]);
        switch (mode) {
            case PlanetColorMode.Elevation:
                this.getElevationColors(planet, season);
                break;
            case PlanetColorMode.Precipitation:
                this.getPrecipitationColors(planet, season);
                break;
            case PlanetColorMode.Temperature:
                this.getTemperatureColors(planet, season);
                break;
            default:
                this.getVegetationColors(planet, season);
                break;
        }
    }

    private getElevationColors(planet: PlanetData.Planet, s: number) {
        let max = planet.tiles.reduce((p, c) => c.elevation > p ? c.elevation : p, 0);
        let min = planet.tiles.reduce((p, c) => c.elevation < p ? c.elevation : p, max);
        for (let i = 0; i < planet.tiles.length; i++) {
            let t = planet.tiles[i];
            if (t.elevation > 0) {
                this.tileColors[s][i] = PlanetColors.interpolate(PlanetColors.colors_elevation[0], PlanetColors.colors_elevation[1], t.elevation / max);
            } else {
                this.tileColors[s][i] = PlanetColors.interpolate(PlanetColors.colors_elevation[2], PlanetColors.colors_elevation[3], t.elevation / min);
            }
        }
    }

    private getPrecipitationColors(planet: PlanetData.Planet, s: number) {
        let q1 = 65;
        let q2minusQ1 = 415;
        let q3minusQ2 = 1720;
        let q90minusQ3 = 800;
        for (let i = 0; i < planet.tiles.length; i++) {
            let t = planet.tiles[i];
            if (t.terrainType == PlanetData.TerrainType.Water) {
                this.tileColors[s][i] = PlanetColors.colors_precipitation[4];
            } else {
                let prec = planet.seasons[s].tileClimates[i].precipitation * planet.seasons.length;
                let color: Color;
                if (prec <= q1) {
                    color = PlanetColors.interpolate(PlanetColors.colors_precipitation[0], PlanetColors.colors_precipitation[1], Math.min(1, prec / q1));
                } else if (prec <= q2minusQ1) {
                    color = PlanetColors.interpolate(PlanetColors.colors_precipitation[1], PlanetColors.colors_precipitation[2], Math.min(1, (prec - q1) / q2minusQ1));
                } else if (prec <= q3minusQ2) {
                    color = PlanetColors.interpolate(PlanetColors.colors_precipitation[2], PlanetColors.colors_precipitation[3], Math.min(1, (prec - q2minusQ1) / q3minusQ2));
                } else {
                    color = PlanetColors.interpolate(PlanetColors.colors_precipitation[3], PlanetColors.colors_precipitation[4], Math.min(1, (prec - q3minusQ2) / q90minusQ3));
                }
                let snowFall = planet.seasons[s].tileClimates[i].snowFall * planet.seasons.length;
                if (snowFall > 0) {
                    this.tileColors[s][i] = PlanetColors.interpolate(color, PlanetColors.color_snow, Math.min(1, snowFall / PlanetColors.high_snow));
                } else {
                    this.tileColors[s][i] = color;
                }
            }
        }
    }

    private getTemperatureColors(planet: PlanetData.Planet, s: number) {
        for (let i = 0; i < planet.tiles.length; i++) {
            let t = planet.tiles[i];
            let temp = planet.seasons[s].tileClimates[i].temperature - PlanetColors.freezingPoint;
            if (temp <= PlanetColors.temperature_limits[0]) {
                this.tileColors[s][i] = PlanetColors.colors_temperature[0];
            } else if (temp >= PlanetColors.temperature_limits[8]) {
                this.tileColors[s][i] = PlanetColors.colors_temperature[8];
            } else {
                for (let k = 0; k < 8; k++) {
                    if (temp >= PlanetColors.temperature_limits[k] && temp < PlanetColors.temperature_limits[k + 1]) {
                        let d = (temp - PlanetColors.temperature_limits[k]) / (PlanetColors.temperature_limits[k + 1] - PlanetColors.temperature_limits[k]);
                        this.tileColors[s][i] = PlanetColors.interpolate(PlanetColors.colors_temperature[k], PlanetColors.colors_temperature[k + 1], d);
                        break;
                    }
                }
            }
        }
    }

    private getVegetationColors(planet: PlanetData.Planet, s: number) {
        let maxE = planet.tiles.reduce((p, c) => c.elevation > p ? c.elevation : p, 0);
        for (let i = 0; i < planet.tiles.length; i++) {
            let t = planet.tiles[i];
            let climate = planet.seasons[s].tileClimates[i];
            if (t.terrainType == PlanetData.TerrainType.Water) {
                if (climate.seaIce > 0) {
                    this.tileColors[s][i] = PlanetColors.color_snow;
                } else {
                    this.tileColors[s][i] = PlanetColors.color_land_low;
                }
            } else {
                let c = 0;
                let e = 0;
                switch (t.climateType) {
                    case PlanetData.ClimateType.Polar:
                        c = 1;
                        e = t.ecologyType - 1;
                        break;
                    case PlanetData.ClimateType.Subpolar:
                        c = 2;
                        e = t.ecologyType - 3;
                        break;
                    case PlanetData.ClimateType.Boreal:
                        c = 3;
                        if (t.ecologyType == PlanetData.EcologyType.Desert) {
                            e = 0;
                        } else if (t.ecologyType == PlanetData.EcologyType.DryScrub) {
                            e = 1;
                        } else {
                            e = t.ecologyType - 12;
                        }
                        break;
                    case PlanetData.ClimateType.CoolTemperate:
                        c = 4;
                        if (t.ecologyType == PlanetData.EcologyType.Desert) {
                            e = 0;
                        } else if (t.ecologyType == PlanetData.EcologyType.DesertScrub) {
                            e = 1;
                        } else if (t.ecologyType == PlanetData.EcologyType.Steppe) {
                            e = 2;
                        } else {
                            e = t.ecologyType - 11;
                        }
                        break;
                    case PlanetData.ClimateType.WarmTemperate:
                        c = 5;
                        if (t.ecologyType == PlanetData.EcologyType.Desert) {
                            e = 0;
                        } else if (t.ecologyType == PlanetData.EcologyType.DesertScrub) {
                            e = 1;
                        } else if (t.ecologyType == PlanetData.EcologyType.ThornScrub) {
                            e = 2;
                        } else {
                            e = t.ecologyType - 10;
                        }
                        break;
                    case PlanetData.ClimateType.Subtropical:
                        c = 6;
                        if (t.ecologyType == PlanetData.EcologyType.Desert) {
                            e = 0;
                        } else if (t.ecologyType == PlanetData.EcologyType.DesertScrub) {
                            e = 1;
                        } else if (t.ecologyType == PlanetData.EcologyType.ThornWoodland) {
                            e = 2;
                        } else {
                            e = t.ecologyType - 10;
                        }
                        break;
                    case PlanetData.ClimateType.Tropical:
                        c = 7;
                        if (t.ecologyType == PlanetData.EcologyType.Desert) {
                            e = 0;
                        } else if (t.ecologyType == PlanetData.EcologyType.DesertScrub) {
                            e = 1;
                        } else if (t.ecologyType == PlanetData.EcologyType.ThornWoodland) {
                            e = 2;
                        } else {
                            e = t.ecologyType - 9;
                        }
                        break;
                    case PlanetData.ClimateType.Supertropical:
                        c = 7;
                        e = 0;
                        break;
                    default:
                }
                let color = PlanetColors.interpolate(PlanetColors.colors_biome[c][e], PlanetColors.color_land_high, (maxE - t.elevation) / (maxE * 2));
                if (climate.snowCover > 0) {
                    color = PlanetColors.interpolate(color, PlanetColors.color_snow, Math.min(1, climate.snowCover * planet.seasons.length / PlanetColors.high_snow));
                }
                this.tileColors[s][i] = color;
            }
        }
    }

    public static interpolate(a: Color, b: Color, d: number): Color {
        return [a[0] * (1 - d) + b[0] * d, a[1] * (1 - d) + b[1] * d, a[2] * (1 - d) + b[2] * d, a[3] * (1 - d) + b[3] * d];
    }
}
