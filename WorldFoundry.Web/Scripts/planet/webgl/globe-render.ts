import * as glM from 'gl-matrix';
import * as WebGLUtil from './util';
import * as PlanetData from '../planet-data';
import * as PlanetColor from '../planet-color';

const TwoPI = 2 * Math.PI;

const vsSource = `
        attribute vec4 aVertexPosition;
        attribute vec3 aVertexNormal;
        attribute vec4 aVertexColor;

        uniform mat4 uNormalMatrix;
        uniform mat4 uModelViewMatrix;
        uniform mat4 uProjectionMatrix;

        varying lowp vec4 vColor;
        varying highp vec3 vLighting;

        void main() {
            gl_Position = uProjectionMatrix * uModelViewMatrix * aVertexPosition;
            vColor = aVertexColor;

            highp vec3 ambientLight = vec3(0.5, 0.5, 0.5);
            highp vec3 directionalLightColor = vec3(1, 1, 1);
            highp vec3 directionalVector = normalize(vec3(0.85, 0.8, 0.75));

            highp vec4 transformedNormal = uNormalMatrix * vec4(aVertexNormal, 1.0);

            highp float directional = max(dot(transformedNormal.xyz, directionalVector), 0.0);
            vLighting = ambientLight + (directionalLightColor * directional);
        }
    `;
const fsSource = `
        varying lowp vec4 vColor;
        varying highp vec3 vLighting;

        void main() {
            gl_FragColor = vec4(vColor.rgb * vLighting, vColor.a);
        }
    `;
const oceanColor_shallow: PlanetColor.Color = [0.0, 0.0, 0.75, 0.66];
const oceanColor_deep: PlanetColor.Color = [0.0, 0.0, 0.75, 0.5];
const lakeColor: PlanetColor.Color = [0.2, 0.2, 0.85, 1.0];
const riverColor: PlanetColor.Color = [0.2, 0.2, 0.85, 1.0];
const seaIceColor: PlanetColor.Color = [1.0, 1.0, 1.0, 0.75];

function addLakeToBuffers(planet: PlanetData.Planet, i: number) {
    let h = planet.corners[i].elevation + planet.corners[i].lakeDepth;
    WebGLUtil.appendVecToArray(planet.lakeBuffers.positions, PlanetData.scaleForElevation(planet.corners[i].vector, h));
    WebGLUtil.appendColorToArray(planet.lakeBuffers.colors, lakeColor);
    let c0 = planet.lakeBuffers.c;
    let c1 = -1;
    planet.lakeBuffers.indices.push(planet.lakeBuffers.c++);
    for (let k = 0; k < 3; k++) {
        let corner = planet.corners[planet.corners[i].corners[k]];
        let tile = planet.tiles[planet.corners[i].tiles[k]];
        WebGLUtil.appendVecToArray(planet.lakeBuffers.positions, PlanetData.scaleForElevation(corner.vector, h));
        WebGLUtil.appendVecToArray(planet.lakeBuffers.positions, PlanetData.scaleForElevation(tile.vector, h));
        WebGLUtil.appendColorToArray(planet.lakeBuffers.colors, lakeColor);
        WebGLUtil.appendColorToArray(planet.lakeBuffers.colors, lakeColor);
        if (k > 0) {
            planet.lakeBuffers.indices.push(planet.lakeBuffers.c - 1);
            planet.lakeBuffers.indices.push(planet.lakeBuffers.c);
            planet.lakeBuffers.indices.push(c0);
            planet.lakeBuffers.c++;
            planet.lakeBuffers.indices.push(planet.lakeBuffers.c - 1);
            planet.lakeBuffers.indices.push(planet.lakeBuffers.c);
            planet.lakeBuffers.indices.push(c0);
        } else {
            c1 = planet.lakeBuffers.c;
            planet.lakeBuffers.c++;
            planet.lakeBuffers.indices.push(planet.lakeBuffers.c - 1);
            planet.lakeBuffers.indices.push(planet.lakeBuffers.c);
            planet.lakeBuffers.indices.push(c0);
        }
        planet.lakeBuffers.c++;
    }
    planet.lakeBuffers.indices.push(planet.lakeBuffers.c - 1);
    planet.lakeBuffers.indices.push(c1);
}

function addLakesToBuffers(planet: PlanetData.Planet, c: number) {
    planet.lakeBuffers = {
        positions: [],
        colors: [],
        indices: [],
        c: c,
    };
    for (let i = 0; i < planet.corners.length; i++) {
        if (planet.corners[i].lakeDepth > 0) {
            addLakeToBuffers(planet, i);
        }
    }
}

function addOceanToBuffers(planet: PlanetData.Planet, season: number, c: number, o: number, elevation: number) {
    planet.oceanBuffers[o] = {
        positions: [],
        indices: [],
        c: c,
    };
    for (let i = 0; i < planet.tiles.length; i++) {
        WebGLUtil.appendVecToArray(planet.oceanBuffers[o].positions, PlanetData.scaleForElevation(planet.tiles[i].vector, elevation));
        let c0 = planet.oceanBuffers[o].c;
        let c1 = -1;
        planet.oceanBuffers[o].indices.push(planet.oceanBuffers[o].c++);
        for (let k = 0; k < planet.tiles[i].edges.length; k++) {
            let corner = planet.corners[planet.tiles[i].corners[k]];
            WebGLUtil.appendVecToArray(planet.oceanBuffers[o].positions, PlanetData.scaleForElevation(corner.vector, elevation));
            if (k > 0) {
                planet.oceanBuffers[o].indices.push(planet.oceanBuffers[o].c - 1);
                planet.oceanBuffers[o].indices.push(planet.oceanBuffers[o].c);
                planet.oceanBuffers[o].indices.push(c0);
            }
            else {
                c1 = planet.oceanBuffers[o].c;
            }
            planet.oceanBuffers[o].c++;
        }
        planet.oceanBuffers[o].indices.push(planet.oceanBuffers[o].c - 1);
        planet.oceanBuffers[o].indices.push(c1);
    }
}

function addRiverToBuffers(planet: PlanetData.Planet, data: WebGLUtil.BufferData, season: number, i: number, k: number, maxFlow: number) {
    let ratio = Math.max(0, Math.min(1, (planet.seasons[season].edgeRiverFlows[planet.tiles[i].edges[k]] - 10000) / maxFlow));
    let scale = 0.23 * ratio + 0.1;

    let v1 = glM.vec3.create();
    let corner1 = planet.corners[planet.tiles[i].corners[k === 0
        ? planet.tiles[i].edges.length - 1
        : k - 1]];
    let corner2 = planet.corners[planet.tiles[i].corners[k]];
    glM.vec3.subtract(v1, corner1.vectorScaled, corner2.vectorScaled);
    glM.vec3.scale(v1, v1, scale);
    glM.vec3.add(v1, corner2.vectorScaled, v1);
    glM.vec3.scale(v1, v1, 1.001);

    let v2 = glM.vec3.create();
    glM.vec3.scale(v2, corner2.vectorScaled, 1.001);

    let v3 = glM.vec3.create();
    let corner3 = planet.corners[planet.tiles[i].corners[(k + 1) % planet.tiles[i].edges.length]];
    let vc3 = corner3.vectorScaled;
    glM.vec3.scale(v3, vc3, 1.001);

    let v4 = glM.vec3.create();
    let corner4 = planet.corners[planet.tiles[i].corners[(k + 2) % planet.tiles[i].edges.length]];
    glM.vec3.subtract(v4, corner4.vectorScaled, vc3);
    glM.vec3.scale(v4, v4, scale);
    glM.vec3.add(v4, vc3, v4);
    glM.vec3.scale(v4, v4, 1.001);

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
    for (let i = 0; i < planet.tiles.length; i++) {
        WebGLUtil.appendVecToArray(planet.tileBuffers.positions, planet.tiles[i].vectorScaled);
        let c0 = planet.tileBuffers.c;
        let c1 = -1;
        planet.tileBuffers.indices.push(planet.tileBuffers.c++);
        for (let k = 0; k < planet.tiles[i].edges.length; k++) {
            let corner = planet.corners[planet.tiles[i].corners[k]];
            WebGLUtil.appendVecToArray(planet.tileBuffers.positions, corner.vectorScaled);
            if (k > 0) {
                planet.tileBuffers.indices.push(planet.tileBuffers.c - 1);
                planet.tileBuffers.indices.push(planet.tileBuffers.c);
                planet.tileBuffers.indices.push(c0);
            } else {
                c1 = planet.tileBuffers.c;
            }
            planet.tileBuffers.c++;
        }
        planet.tileBuffers.indices.push(planet.tileBuffers.c - 1);
        planet.tileBuffers.indices.push(c1);
    }
}

function getAxisRotation(axis: glM.vec3) {
    let r = glM.quat.create();
    glM.quat.rotationTo(r, axis, [0, 1, 0]);
    return r;
}

function getLatitudeRotation(latitude: number) {
    let q = glM.quat.create();
    glM.quat.setAxisAngle(q, [1, 0, 0], latitude);
    return q;
}

function getLongitudeRotation(longitude: number) {
    let q = glM.quat.create();
    glM.quat.setAxisAngle(q, [0, 1, 0], longitude);
    return q;
}

function getOceanColors(planet: PlanetData.Planet, season: number, oceanColor: PlanetColor.Color) {
    let colors = [];
    for (let i = 0; i < planet.tiles.length; i++) {
        WebGLUtil.appendColorToArray(colors, planet.seasons[season].tileClimates[i].seaIce > 0 ? seaIceColor : oceanColor);
        for (let k = 0; k < planet.tiles[i].corners.length; k++) {
            WebGLUtil.appendColorToArray(colors, planet.seasons[season].tileClimates[i].seaIce > 0 ? seaIceColor : oceanColor);
        }
    }
    return colors;
}

function getRotation(axis: glM.vec3, latitude: number, longitude: number) {
    let q = glM.quat.create();
    glM.quat.multiply(q, getLatitudeRotation(latitude), getLongitudeRotation(longitude));
    glM.quat.multiply(q, q, getAxisRotation(axis));
    return q;
}

function getTileColors(planet: PlanetData.Planet, season: number, smooth: boolean) {
    let colors = [];
    for (let i = 0; i < planet.tiles.length; i++) {
        WebGLUtil.appendColorToArray(colors, planet.colors.tileColors[season][i]);
        for (let k = 0; k < planet.tiles[i].corners.length; k++) {
            if (smooth) {
                let corner = planet.corners[planet.tiles[i].corners[k]];
                let c = PlanetColor.PlanetColors.interpolate(planet.colors.tileColors[season][corner.tiles[0]],
                    planet.colors.tileColors[season][corner.tiles[1]], 0.5);
                c = PlanetColor.PlanetColors.interpolate(c, planet.colors.tileColors[season][corner.tiles[2]], 0.33);
                WebGLUtil.appendColorToArray(colors, c);
            } else {
                WebGLUtil.appendColorToArray(colors, planet.colors.tileColors[season][i]);
            }
        }
    }
    return colors;
}

function initBuffers(gl: WebGLRenderingContext, planet: PlanetData.Planet, season: number, smooth: boolean): WebGLUtil.WebGLBuffers {
    if (!planet.tileBuffers) {
        addTilesToBuffers(planet, season);
    }
    if (!planet.oceanBuffers) {
        planet.oceanBuffers = [];
        addOceanToBuffers(planet, season, planet.tileBuffers.c, 0, -250);
        addOceanToBuffers(planet, season, planet.oceanBuffers[0].c, 1, 0);
    }
    if (!planet.lakeBuffers) {
        addLakesToBuffers(planet, planet.oceanBuffers[1].c);
    }

    let data: WebGLUtil.BufferData = {
        positions: planet.tileBuffers.positions,
        colors: getTileColors(planet, season, smooth),
        indices: planet.tileBuffers.indices,
        c: planet.lakeBuffers.c,
    };

    for (let i = 0; i < 2; i++) {
        data.positions = data.positions.concat(planet.oceanBuffers[i].positions);
        data.indices = data.indices.concat(planet.oceanBuffers[i].indices);
    }
    data.colors = data.colors
        .concat(getOceanColors(planet, season, oceanColor_deep)
        .concat(getOceanColors(planet, season, oceanColor_shallow)));
    data.positions = data.positions.concat(planet.lakeBuffers.positions);
    data.colors = data.colors.concat(planet.lakeBuffers.colors);
    data.indices = data.indices.concat(planet.lakeBuffers.indices);

    addRiversToBuffers(planet, data, season);

    let vertexNormals = data.positions.slice();

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

    const normalBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, normalBuffer);
    gl.bufferData(
        gl.ARRAY_BUFFER,
        new Float32Array(vertexNormals),
        gl.STATIC_DRAW);

    return {
        position: positionBuffer,
        normal: normalBuffer,
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
            vertexNormal: gl.getAttribLocation(shaderProgram, 'aVertexNormal'),
            vertexColor: gl.getAttribLocation(shaderProgram, 'aVertexColor'),
        },
        uniformLocations: {
            projectionMatrix: gl.getUniformLocation(shaderProgram, 'uProjectionMatrix'),
            normalMatrix: gl.getUniformLocation(shaderProgram, 'uNormalMatrix'),
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
    gl.clearDepth(1.0);
    gl.enable(gl.DEPTH_TEST);
    gl.depthFunc(gl.LESS);
    gl.enable(gl.BLEND);
    gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
    gl.enable(gl.CULL_FACE);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

    const fieldOfView = 45 * Math.PI / 180;
    const aspect = gl.canvas.clientWidth / gl.canvas.clientHeight;
    const zNear = 0.1;
    const zFar = 100.0;
    const projectionMatrix = glM.mat4.create();
    glM.mat4.perspective(
        projectionMatrix,
        fieldOfView,
        aspect,
        zNear,
        zFar);

    const modelViewMatrix = glM.mat4.create();
    glM.mat4.translate(
        modelViewMatrix,
        modelViewMatrix,
        [-0.0, 0.0, -3.0 / params.scale]);

    let r = glM.mat4.create();
    glM.mat4.fromQuat(r, getRotation(params.planet.axis, params.latitude, params.longitude));
    glM.mat4.multiply(modelViewMatrix, modelViewMatrix, r);

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

    {
        const numComponents = 3;
        const type = gl.FLOAT;
        const normalize = false;
        const stride = 0;
        const offset = 0;
        gl.bindBuffer(gl.ARRAY_BUFFER, sceneInfo.buffers.normal);
        gl.vertexAttribPointer(
            sceneInfo.programInfo.attribLocations.vertexNormal,
            numComponents,
            type,
            normalize,
            stride,
            offset);
        gl.enableVertexAttribArray(sceneInfo.programInfo.attribLocations.vertexNormal);
    }

    const normalMatrix = glM.mat4.create();
    glM.mat4.invert(normalMatrix, modelViewMatrix);
    glM.mat4.transpose(normalMatrix, normalMatrix);

    gl.useProgram(sceneInfo.programInfo.program);

    gl.uniformMatrix4fv(
        sceneInfo.programInfo.uniformLocations.projectionMatrix,
        false,
        projectionMatrix);
    gl.uniformMatrix4fv(
        sceneInfo.programInfo.uniformLocations.modelViewMatrix,
        false,
        modelViewMatrix);
    gl.uniformMatrix4fv(
        sceneInfo.programInfo.uniformLocations.normalMatrix,
        false,
        normalMatrix);

    {
        let ext = gl.getExtension("OES_element_index_uint");
        const type = gl.UNSIGNED_INT;
        const offset = 0;
        const vertexCount = sceneInfo.buffers.vertexCount;

        gl.cullFace(gl.FRONT);
        gl.drawElements(gl.TRIANGLES, vertexCount, type, offset);

        gl.cullFace(gl.BACK);
        gl.drawElements(gl.TRIANGLES, vertexCount, type, offset);
    }

    if (params.autorotate) {
        params.longitude += deltaTime / 3;
        if (params.longitude >= TwoPI) {
            params.longitude -= TwoPI;
        }
    }
}
