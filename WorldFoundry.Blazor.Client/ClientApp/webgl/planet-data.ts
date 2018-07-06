import * as glM from 'gl-matrix';
import * as PlanetColor from './planet-color';
import * as WebGLUtil from './util';

export enum ClimateType {
    None,
    Polar,
    Subpolar,
    Boreal,
    CoolTemperate,
    WarmTemperate,
    Subtropical,
    Tropical,
    Supertropical
}

export enum EcologyType {
    None,
    Desert,
    Ice,
    DryTundra,
    MoistTundra,
    WetTundra,
    RainTundra,
    DesertScrub,
    DryScrub,
    Steppe,
    ThornScrub,
    ThornWoodland,
    VeryDryForest,
    DryForest,
    MoistForest,
    WetForest,
    RainForest
}

export enum TerrainType {
    None = 0,
    Land = 1,
    Water = 2,
    Coast = 3
}

interface PlanetVector {
    x: number;
    y: number;
    z: number;
}

export interface Corner {
    corners: number[];
    elevation: number;
    lakeDepth: number;
    latitude: number;
    longitude: number;
    tiles: number[];
    vector: glM.vec3;
    vectorScaled: glM.vec3;
}

export interface CornerData {
    corners: number[];
    elevation: number;
    lakeDepth: number;
    latitude: number;
    longitude: number;
    tiles: number[];
    vector: PlanetVector;
}

export interface Edge {
    tiles: number[];
}

export interface HammerTile {
    center: glM.vec3;
    corners: glM.vec3[];
}

export interface Tile {
    climateType: ClimateType;
    corners: number[];
    ecologyType: EcologyType;
    edges: number[];
    elevation: number;
    latitude: number;
    longitude: number;
    terrainType: TerrainType;
    vector: glM.vec3;
    vectorScaled: glM.vec3;
}

export interface TileData {
    climateType: ClimateType;
    corners: number[];
    ecologyType: EcologyType;
    edges: number[];
    elevation: number;
    latitude: number;
    longitude: number;
    terrainType: TerrainType;
    vector: PlanetVector;
}

export interface TileClimate {
    precipitation: number;
    seaIce: number;
    snowCover: number;
    snowFall: number;
    temperature: number;
}

export interface Season {
    edgeRiverFlows: number[];
    index: number;
    tileClimates: TileClimate[];
}

export interface Planet {
    axis: glM.vec3;
    buffers: WebGLUtil.WebGLBuffers[];
    colors?: PlanetColor.PlanetColors;
    corners: Corner[];
    edges: Edge[];
    hammerTiles?: HammerTile[];
    lakeBuffers?: WebGLUtil.BufferData;
    oceanBuffers?: WebGLUtil.BufferData[];
    seasons: Season[];
    elevationSeed: number;
    tileBuffers?: WebGLUtil.BufferData;
    tiles: Tile[];
}

export interface Topography {
    corners: CornerData[];
    edges: Edge[];
    elevationSeed: number;
    tiles: TileData[];
}

export interface PlanetData {
    axis: PlanetVector;
    topography: Topography;
    seasons: Season[];
}

function glMVec3FromPlanetVector(v: PlanetVector) {
    return glM.vec3.fromValues(v.x, v.y, v.z);
}

export function scaleForElevation(vec: glM.vec3, elevation: number) {
    let sv = glM.vec3.create();
    let scaleFactor = 150000;
    glM.vec3.scale(sv, vec, (scaleFactor + elevation) / scaleFactor);
    return sv;
}

function cornersFromData(corners: CornerData[]) {
    return corners.map<Corner>(c => {
        let v = glMVec3FromPlanetVector(c.vector);
        return {
            corners: c.corners,
            elevation: c.elevation,
            lakeDepth: c.lakeDepth,
            latitude: c.latitude,
            longitude: c.longitude,
            tiles: c.tiles,
            vector: v,
            vectorScaled: scaleForElevation(v, c.elevation),
        };
    });
}

function tilesFromData(tiles: TileData[]) {
    return tiles.map<Tile>(t => {
        let v = glMVec3FromPlanetVector(t.vector);
        return {
            climateType: t.climateType,
            corners: t.corners,
            ecologyType: t.ecologyType,
            edges: t.edges,
            elevation: t.elevation,
            latitude: t.latitude,
            longitude: t.longitude,
            terrainType: t.terrainType,
            vector: v,
            vectorScaled: scaleForElevation(v, t.elevation),
        };
    });
}

export function planetFromData(data: PlanetData): Planet {
    var planet = {
        axis: glMVec3FromPlanetVector(data.axis),
        buffers: [],
        corners: cornersFromData(data.topography.corners),
        edges: data.topography.edges,
        seasons: data.seasons,
        elevationSeed: data.topography.elevationSeed,
        tiles: tilesFromData(data.topography.tiles),
    };
    planet.seasons.sort((a, b) => a.index - b.index);
    return planet;
}
