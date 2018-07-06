import * as glM from 'gl-matrix';
import * as PlanetData from './planet-data';
import * as PlanetColor from './planet-color';
import * as DotNetInterop from '../dotnet_interop';

export interface AttribLocations {
    vertexPosition: number;
    vertexColor: number;
    vertexNormal?: number;
    [index: string]: number;
}

export interface BufferData {
    positions: number[];
    colors?: number[];
    indices: number[];
    c: number;
}

export interface ParamInitData {
    autorotate: boolean;
    latitude?: number;
    longitude?: number;
    season: number;
    scale: number;
    smooth: boolean;
}

export interface RendererParams {
    autorotate: boolean;
    latitude: number;
    longitude: number;
    planet: PlanetData.Planet;
    season: number;
    scale: number;
    smooth: boolean;
}
export interface InitParams {
    (planet: PlanetData.Planet, data: ParamInitData): RendererParams;
}
export let initParams: InitParams;
initParams = function (planet: PlanetData.Planet, data: ParamInitData): RendererParams {
    return {
        autorotate: data.autorotate,
        latitude: data.latitude || 0,
        longitude: data.longitude || 0,
        planet: planet,
        season: data.season,
        scale: data.scale,
        smooth: data.smooth,
    };
}

export interface ProgramInfo {
    program: WebGLProgram,
    attribLocations: AttribLocations,
    uniformLocations: UniformLocations,
}

export interface Renderer {
    (
        gl: WebGLRenderingContext,
        sceneInfo: SceneInfo,
        params: RendererParams,
        deltaTime: number
    ): void;
}

interface InitBuffers {
    (gl: WebGLRenderingContext, ...args: any[]): WebGLBuffers;
}

export interface RenderInfo {
    params: RendererParams;
    renderer: Renderer;
    sceneInfo: SceneInfo;
    updateBuffers: InitBuffers;
}

export interface RenderInfoInit { (gl: WebGLRenderingContext, planet: PlanetData.Planet, data: ParamInitData): RenderInfo; }

export interface SceneInfo {
    programInfo: ProgramInfo;
    buffers: WebGLBuffers;
}

export interface UniformLocations {
    projectionMatrix: WebGLUniformLocation;
    modelViewMatrix: WebGLUniformLocation;
    normalMatrix?: WebGLUniformLocation;
    [index: string]: WebGLUniformLocation;
}

export interface WebGLBuffers {
    position: WebGLBuffer;
    color: WebGLBuffer;
    indices?: WebGLBuffer;
    normal?: WebGLBuffer;
    [index: string]: WebGLBuffer;
    vertexCount?: number;
}

export function appendColorToArray(arr: number[], c: PlanetColor.Color) {
    arr.push(c[0]);
    arr.push(c[1]);
    arr.push(c[2]);
    arr.push(c[3]);
}

export function appendVecToArray(arr: number[], v: glM.vec3) {
    arr.push(v[0]);
    arr.push(v[1]);
    arr.push(v[2]);
}

export function loadShader(gl: WebGLRenderingContext, type: number, source: string) {
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);

    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
        webGLError();
        gl.deleteShader(shader);
        return null;
    }

    return shader;
}

export function initShaderProgram(gl: WebGLRenderingContext, vsSource: string, fsSource: string) {
    const vertexShader = loadShader(gl, gl.VERTEX_SHADER, vsSource);
    const fragmentShader = loadShader(gl, gl.FRAGMENT_SHADER, fsSource);

    const shaderProgram = gl.createProgram();
    gl.attachShader(shaderProgram, vertexShader);
    gl.attachShader(shaderProgram, fragmentShader);
    gl.linkProgram(shaderProgram);

    if (!gl.getProgramParameter(shaderProgram, gl.LINK_STATUS)) {
        webGLError();
        return null;
    }

    return shaderProgram;
}

function webGLError() {
    DotNetInterop.showError("WebGL wasn't able to load correctly in your browser.");
}
