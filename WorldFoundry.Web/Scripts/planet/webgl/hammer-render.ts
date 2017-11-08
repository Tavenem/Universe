import * as glM from 'gl-matrix';
import * as WebGLUtil from './util';
import * as PlanetData from '../planet-data';
import * as PlanetColor from '../planet-color';

const vsSource = `
        attribute vec4 aVertexPosition;
        attribute vec4 aVertexColor;

        uniform mat4 uModelViewMatrix;
        uniform mat4 uProjectionMatrix;

        varying lowp vec4 vColor;

        void main() {
            gl_Position = uProjectionMatrix * uModelViewMatrix * aVertexPosition;
            vColor = aVertexColor;
        }
    `;
const fsSource = `
        varying lowp vec4 vColor;

        void main() {
            gl_FragColor = vColor;
        }
    `;
const shallowOceanColor: PlanetColor.Color = [0.0, 0.0, 0.75, 1.0];
const deepOceanColor: PlanetColor.Color = [0.0, 0.0, 0.50, 1.0];
const lakeColor: PlanetColor.Color = [0.2, 0.2, 0.85, 1.0];
const riverColor: PlanetColor.Color = [0.0, 0.0, 1.0, 1.0];
const seaIceColor: PlanetColor.Color = [1.0, 1.0, 1.0, 1.0];

function addRiverToBuffers(planet: PlanetData.Planet, data: WebGLUtil.BufferData, season: number, i: number, k: number, maxFlow: number) {
    let riverZ = 0.01;

    let ratio = Math.max(0, Math.min(1, (planet.seasons[season].edgeRiverFlows[planet.tiles[i].edges[k]] - 10000) / maxFlow));
    let scale = 0.23 * ratio + 0.1;

    let v1 = glM.vec3.create();
    let vc1 = planet.hammerTiles[i].corners[k === 0
        ? planet.tiles[i].edges.length - 1
        : k - 1];
    let v2 = planet.hammerTiles[i].corners[k];
    glM.vec3.subtract(v1, vc1, v2);
    glM.vec3.scale(v1, v1, scale);
    glM.vec3.add(v1, v2, v1);
    v1[2] = riverZ;

    v2[2] = riverZ;

    let vc3 = planet.hammerTiles[i].corners[(k + 1) % planet.tiles[i].edges.length];
    let v3 = glM.vec3.fromValues(vc3[0], vc3[1], riverZ);

    let v4 = glM.vec3.create();
    let vc4 = planet.hammerTiles[i].corners[(k + 2) % planet.tiles[i].edges.length];
    glM.vec3.subtract(v4, vc4, vc3);
    glM.vec3.scale(v4, v4, scale);
    glM.vec3.add(v4, vc3, v4);
    v4[2] = riverZ;

    WebGLUtil.appendVecToArray(data.positions, v1);
    WebGLUtil.appendColorToArray(data.colors, riverColor);
    let c1 = data.c++;
    data.indices.push(c1);

    WebGLUtil.appendVecToArray(data.positions, v2);
    WebGLUtil.appendColorToArray(data.colors, riverColor);
    data.indices.push(data.c++);

    WebGLUtil.appendVecToArray(data.positions, v3);
    WebGLUtil.appendColorToArray(data.colors, riverColor);
    let c3 = data.c++;
    data.indices.push(c3);

    data.indices.push(c3);

    WebGLUtil.appendVecToArray(data.positions, v4);
    WebGLUtil.appendColorToArray(data.colors, riverColor);
    data.indices.push(data.c++);

    data.indices.push(c1);
}

function addRiversToBuffers(planet: PlanetData.Planet, data: WebGLUtil.BufferData, season: number) {
    let maxFlow = planet.seasons.reduce((p, c) => {
        let crf = c.edgeRiverFlows.reduce((pr, cr) => { return cr > pr ? cr : pr; }, 0);
        return crf > p ? crf : p;
    }, 0);
    maxFlow -= 10000;
    maxFlow *= 0.5;
    for (let i = 0; i < planet.tiles.length; i++) {
        if (planet.tiles[i].terrainType !== PlanetData.TerrainType.Water) {
            for (let k = 0; k < planet.tiles[i].edges.length; k++) {
                if (planet.seasons.map(s => s.edgeRiverFlows[planet.tiles[i].edges[k]]).reduce((c, p) => c + p, 0) / planet.seasons.length > 10000) {
                    addRiverToBuffers(planet, data, season, i, k, maxFlow);
                }
            }
        }
    }
}

function addTilesToBuffers(planet: PlanetData.Planet, season: number) {
    planet.tileBuffers = {
        positions: [],
        indices: [],
        c: 0,
    };
    planet.hammerTiles = Array.from({ length: planet.tiles.length }, (v, k) => getHammerTile(planet, planet.tiles[k]));

    for (let i = 0; i < planet.tiles.length; i++) {
        let hammerTile = planet.hammerTiles[i];

        WebGLUtil.appendVecToArray(planet.tileBuffers.positions, hammerTile.center);
        let c0 = planet.tileBuffers.c;
        let c1 = -1;
        planet.tileBuffers.indices.push(planet.tileBuffers.c++);
        for (let k = 0; k < hammerTile.corners.length; k++) {
            WebGLUtil.appendVecToArray(planet.tileBuffers.positions, hammerTile.corners[k]);
            if (k > 0) {
                planet.tileBuffers.indices.push(planet.tileBuffers.c - 1);
                planet.tileBuffers.indices.push(planet.tileBuffers.c);
                planet.tileBuffers.indices.push(c0);
            }
            else {
                c1 = planet.tileBuffers.c;
            }
            planet.tileBuffers.c++;
        }
        planet.tileBuffers.indices.push(planet.tileBuffers.c - 1);
        planet.tileBuffers.indices.push(c1);
    }
}

function getHammerTile(planet: PlanetData.Planet, tile: PlanetData.Tile) {
    let latitude = tile.latitude;
    let longitude = tile.longitude;
    let h: PlanetData.HammerTile = {
        center: getHammerVector(latitude, longitude),
        corners: Array.from({ length: tile.edges.length }, v => undefined),
    };

    for (let k = 0; k < tile.edges.length; k++) {
        let c = planet.corners[tile.corners[k]];
        h.corners[k] = getHammerVector(c.latitude, c.longitude);
    }
    return h;
}

function getHammerVector(lat: number, lon: number) {
    let cosLat = Math.cos(lat);
    let halfLon = lon / 2;
    let z = Math.sqrt(1 + cosLat * Math.cos(halfLon));
    return glM.vec3.fromValues(2 * cosLat * Math.sin(halfLon) / z, Math.sin(lat) / z, 0);
}

function getTileColor(elevation: number, seaIce: boolean, fallback: PlanetColor.Color) {
    if (seaIce) {
        return seaIceColor;
    } else if (elevation < -250) {
        return deepOceanColor;
    } else if (elevation < 0) {
        return shallowOceanColor;
    } else {
        return fallback;
    }
}

function getTileColors(planet: PlanetData.Planet, season: number, smooth: boolean) {
    let colors = [];
    for (let i = 0; i < planet.tiles.length; i++) {
        let seaIce = planet.seasons[season].tileClimates[i].seaIce > 0;
        WebGLUtil.appendColorToArray(colors, getTileColor(planet.tiles[i].elevation, seaIce, planet.colors.tileColors[season][i]));
        for (let k = 0; k < planet.tiles[i].corners.length; k++) {
            let c: PlanetColor.Color;
            let corner = planet.corners[planet.tiles[i].corners[k]];
            if (corner.lakeDepth > 0) {
                c = lakeColor;
            } else if (smooth) {
                c = PlanetColor.PlanetColors.interpolate(planet.colors.tileColors[season][corner.tiles[0]],
                    planet.colors.tileColors[season][corner.tiles[1]], 0.5);
                c = PlanetColor.PlanetColors.interpolate(c, planet.colors.tileColors[season][corner.tiles[2]], 0.33);
            } else {
                c = planet.colors.tileColors[season][i];
            }
            WebGLUtil.appendColorToArray(colors, getTileColor(corner.elevation, seaIce, c));
        }
    }
    return colors;
}

function initBuffers(gl: WebGLRenderingContext, planet: PlanetData.Planet, season: number, smooth: boolean): WebGLUtil.WebGLBuffers {
    if (!planet.tileBuffers) {
        addTilesToBuffers(planet, season);
    }

    let data: WebGLUtil.BufferData = {
        positions: planet.tileBuffers.positions,
        colors: getTileColors(planet, season, smooth),
        indices: planet.tileBuffers.indices,
        c: planet.tileBuffers.c,
    };

    addRiversToBuffers(planet, data, season);

    const positionBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, positionBuffer);
    gl.bufferData(gl.ARRAY_BUFFER,
        new Float32Array(data.positions),
        gl.STATIC_DRAW);

    const colorBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, colorBuffer);
    gl.bufferData(
        gl.ARRAY_BUFFER,
        new Float32Array(data.colors),
        gl.STATIC_DRAW);

    const indexBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBuffer);
    gl.bufferData(
        gl.ELEMENT_ARRAY_BUFFER,
        new Uint32Array(data.indices),
        gl.STATIC_DRAW);

    return {
        position: positionBuffer,
        color: colorBuffer,
        indices: indexBuffer,
        vertexCount: data.indices.length,
    };
}

export let initRenderInfo: WebGLUtil.RenderInfoInit;
initRenderInfo = function (gl: WebGLRenderingContext, planet: PlanetData.Planet, data: WebGLUtil.ParamInitData): WebGLUtil.RenderInfo {
    return {
        params: WebGLUtil.initParams(planet, data),
        renderer: render,
        sceneInfo: initSceneInfo(gl, planet, data.season),
        updateBuffers: initBuffers,
    };
}

function initSceneInfo(gl: WebGLRenderingContext, planet: PlanetData.Planet, season: number): WebGLUtil.SceneInfo {
    const shaderProgram = WebGLUtil.initShaderProgram(gl, vsSource, fsSource);

    const programInfo: WebGLUtil.ProgramInfo = {
        program: shaderProgram,
        attribLocations: {
            vertexPosition: gl.getAttribLocation(shaderProgram, 'aVertexPosition'),
            vertexColor: gl.getAttribLocation(shaderProgram, 'aVertexColor'),
        },
        uniformLocations: {
            projectionMatrix: gl.getUniformLocation(shaderProgram, 'uProjectionMatrix'),
            modelViewMatrix: gl.getUniformLocation(shaderProgram, 'uModelViewMatrix'),
        }
    };

    return {
        programInfo: programInfo,
        buffers: undefined,
    };
}

let render: WebGLUtil.Renderer;
render = function (
    gl: WebGLRenderingContext,
    sceneInfo: WebGLUtil.SceneInfo,
    params: WebGLUtil.RendererParams,
    deltaTime: number) {

    gl.clearColor(0.0, 0.0, 0.0, 1.0);
    gl.clear(gl.COLOR_BUFFER_BIT);

    const zNear = 0.1;
    const zFar = 100.0;
    const projectionMatrix = glM.mat4.create();
    let scale = 2 / params.scale;
    glM.mat4.ortho(
        projectionMatrix,
        -scale, scale,
        -scale, scale,
        zNear, zFar);

    const modelViewMatrix = glM.mat4.create();
    glM.mat4.translate(
        modelViewMatrix,
        modelViewMatrix,
        [-0.0, 0.0, -3.0]);

    {
        const numComponents = 3;
        const type = gl.FLOAT;
        const normalize = false;
        const stride = 0;
        const offset = 0;
        gl.bindBuffer(gl.ARRAY_BUFFER, sceneInfo.buffers.position);
        gl.vertexAttribPointer(
            sceneInfo.programInfo.attribLocations.vertexPosition,
            numComponents,
            type,
            normalize,
            stride,
            offset);
        gl.enableVertexAttribArray(sceneInfo.programInfo.attribLocations.vertexPosition);
    }

    {
        const numComponents = 4;
        const type = gl.FLOAT;
        const normalize = false;
        const stride = 0;
        const offset = 0;
        gl.bindBuffer(gl.ARRAY_BUFFER, sceneInfo.buffers.color);
        gl.vertexAttribPointer(
            sceneInfo.programInfo.attribLocations.vertexColor,
            numComponents,
            type,
            normalize,
            stride,
            offset);
        gl.enableVertexAttribArray(sceneInfo.programInfo.attribLocations.vertexColor);
    }

    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, sceneInfo.buffers.indices);

    gl.useProgram(sceneInfo.programInfo.program);

    gl.uniformMatrix4fv(
        sceneInfo.programInfo.uniformLocations.projectionMatrix,
        false,
        projectionMatrix);
    gl.uniformMatrix4fv(
        sceneInfo.programInfo.uniformLocations.modelViewMatrix,
        false,
        modelViewMatrix);

    {
        gl.enable(gl.CULL_FACE);
        gl.cullFace(gl.FRONT);

        let ext = gl.getExtension("OES_element_index_uint");
        const type = gl.UNSIGNED_INT;
        const vertexCount = sceneInfo.buffers.vertexCount;
        const offset = 0;
        gl.drawElements(gl.TRIANGLES, vertexCount, type, offset);
    }
}
