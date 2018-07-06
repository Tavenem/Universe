import * as WebGLUtil from './webgl/util';
import * as PlanetData from './webgl/planet-data';
import * as PlanetColor from './webgl/planet-color';
import * as Cube from './webgl/cube-render';
import * as Globe from './webgl/globe-render';
import * as Hammer from './webgl/hammer-render';
import * as DotNetInterop from './dotnet_interop';

interface ColorMode {
    mode: PlanetColor.PlanetColorMode,
    colors?: PlanetColor.PlanetColors,
    buffers?: WebGLUtil.WebGLBuffers[],
}
interface ColorModeDictionary {
    [index: string]: ColorMode;
}

enum RenderMode {
    Cube,
    Globe,
    Hammer,
}
interface RenderModeDictionary {
    [index: string]: WebGLUtil.RenderInfoInit;
}

export class PlanetDisplay {

    animationParams: WebGLUtil.ParamInitData = {
        autorotate: true,
        scale: 1,
        season: 0,
        smooth: false,
    };

    autoSeason = true;

    colorModes: ColorModeDictionary = {
        Elevation: { mode: PlanetColor.PlanetColorMode.Elevation },
        Precipitation: { mode: PlanetColor.PlanetColorMode.Precipitation },
        Temperature: { mode: PlanetColor.PlanetColorMode.Temperature },
        Vegetation: { mode: PlanetColor.PlanetColorMode.Vegetation },
    };

    currentColorMode = PlanetColor.PlanetColorMode.Vegetation;

    currentRenderMode = RenderMode.Cube;

    frame = 0;

    gl: WebGLRenderingContext;

    lastFrameTime = 0;

    planet: PlanetData.Planet;

    renderModes: RenderModeDictionary = {
        Cube: Cube.initRenderInfo,
        Globe: Globe.initRenderInfo,
        Hammer: Hammer.initRenderInfo,
    };

    get renderColor(): ColorMode {
        return this.colorModes[PlanetColor.PlanetColorMode[this.currentColorMode]];
    }
    set renderColor(value: ColorMode) {
        this.currentColorMode = value.mode;
    }

    get renderInfoInit(): WebGLUtil.RenderInfoInit {
        return this.renderModes[RenderMode[this.currentRenderMode]];
    }

    renderInfo: WebGLUtil.RenderInfo;

    seasonLength = 10;

    suspendUpdates = false;

    time = 0;

    init(canvasId: string): boolean {
        const canvas = document.getElementById(canvasId) as HTMLCanvasElement;
        if (!canvas) {
            return false;
        }

        this.gl = canvas.getContext("webgl", { alpha: false });

        if (!this.gl) {
            DotNetInterop.showError("WebGL wasn't able to load correctly in your browser.");
            return false;
        }

        this.gl.clearColor(0.0, 0.0, 0.0, 1.0);
        this.gl.clear(this.gl.COLOR_BUFFER_BIT);

        this.switchRenderer();

        return true;
    }

    animate() {
        this.seasonLength = !!this.planet ? 10 / this.planet.seasons.length : 0;
        this.lastFrameTime = 0;
        this.frame = requestAnimationFrame(this.render);
    }

    autoRotate(value: boolean) {
        this.animationParams.autorotate = value;
        this.renderInfo.params.autorotate = value;
        if (!value) {
            this.animationParams.longitude = this.renderInfo.params.longitude;
        } else {
            delete this.animationParams.longitude;
        }
    }

    changeColorMode(value: PlanetColor.PlanetColorMode) {
        this.currentColorMode = value;
        if (!!this.planet) {
            this.initColors();
            this.initBuffers();
        }
    }

    changeRenderMode(value: RenderMode) {
        this.currentRenderMode = value;
        this.switchRenderer();
    }

    changeSeason(value: number) {
        let num = Math.min(this.planet.seasons.length, value) - 1;
        this.renderInfo.params.season = num;
        this.animationParams.season = num;
        DotNetInterop.seasonChanged(num + 1);
        this.switchBuffers();
    }

    clearColorModeCaches() {
        for (let x in this.colorModes) {
            let colorMode = this.colorModes[x];
            delete colorMode.buffers;
            delete colorMode.colors;
        }
    }

    initBuffers() {
        if (!this.renderColor.buffers) {
            this.renderColor.buffers = Array.from({ length: this.planet.seasons.length }, v => undefined);
        }
        this.planet.buffers = this.renderColor.buffers;
        this.switchBuffers();
    }

    initColors() {
        if (!this.renderColor.colors) {
            DotNetInterop.showLoading("Painting planet colors...");
            this.renderColor.colors = new PlanetColor.PlanetColors(this.planet, this.animationParams.season, this.currentColorMode);
        } else if (!this.renderColor.colors.tileColors[this.animationParams.season]) {
            DotNetInterop.showLoading("Painting planet colors...");
            this.renderColor.colors.addSeason(this.planet, this.animationParams.season, this.currentColorMode);
        }
        this.planet.colors = this.renderColor.colors;
    }

    render = (now: number) => {
        now *= 0.001;
        const deltaTime = now - this.lastFrameTime;
        this.lastFrameTime = now;

        this.renderInfo.renderer(this.gl, this.renderInfo.sceneInfo, this.renderInfo.params, deltaTime);

        if (!!this.planet && this.autoSeason && !this.suspendUpdates) {
            this.time += deltaTime;
            if (this.time >= this.seasonLength) {
                this.time %= this.seasonLength;
                let s = this.animationParams.season;
                s++;
                if (s >= this.planet.seasons.length) {
                    s = 0;
                }
                this.changeSeason(s);
            }
        }

        this.frame = requestAnimationFrame(this.render);
    }

    scale(value: number) {
        this.animationParams.scale = value;
        this.renderInfo.params.scale = value;
    }

    setPlanet(data: PlanetData.PlanetData) {
        DotNetInterop.showLoading("Loading planet data...");
        cancelAnimationFrame(this.frame);

        this.planet = PlanetData.planetFromData(data);

        DotNetInterop.showLoading("Loading climate data...");

        let seasonCount = !!this.planet && !!this.planet.seasons ? this.planet.seasons.length : 0;
        if (this.animationParams.season >= seasonCount) {
            this.changeSeason(seasonCount);
        }

        if (this.currentRenderMode === RenderMode.Cube) {
            this.currentRenderMode = RenderMode.Globe;
        }

        this.switchRenderer();
    }

    smooth(value: boolean) {
        this.animationParams.smooth = value;
        this.renderInfo.params.smooth = value;
        this.clearColorModeCaches();
        if (!!this.planet) {
            this.initBuffers();
        }
    }

    switchBuffers() {
        this.initColors();
        if (!this.renderColor.buffers[this.animationParams.season]) {
            DotNetInterop.showLoading("Rendering planet shape...");
            this.suspendUpdates = true;
            this.renderColor.buffers[this.animationParams.season] = this.renderInfo.updateBuffers(this.gl, this.planet, this.animationParams.season, this.animationParams.smooth);
            this.planet.buffers = this.renderColor.buffers;
            this.suspendUpdates = false;
        }
        this.renderInfo.sceneInfo.buffers = this.planet.buffers[this.animationParams.season];
        DotNetInterop.hideLoading();
    }

    switchRenderer() {
        DotNetInterop.showLoading("Loading display...");
        cancelAnimationFrame(this.frame);
        this.suspendUpdates = true;
        this.clearColorModeCaches();
        if (!!this.planet) {
            if (!!this.planet.tileBuffers) {
                delete this.planet.tileBuffers;
            }
            if (!!this.planet.oceanBuffers) {
                delete this.planet.oceanBuffers;
            }
        }
        this.renderInfo = this.renderInfoInit(this.gl, this.planet, this.animationParams);
        if (!!this.planet) {
            this.initBuffers();
        }
        this.suspendUpdates = false;
        this.animate();
        DotNetInterop.hideLoading();
    }
}

interface PlanetDictionary {
    [index: string]: PlanetDisplay;
}
const planets: PlanetDictionary = {};

export const planetBlazorFunctions = {

    autoRotate: function (planetId: string, value: boolean) {
        planets[planetId].autoRotate(value);
        return true;
    },

    autoSeason: function (planetId: string, value: boolean) {
        planets[planetId].autoSeason = value;
        return true;
    },

    changeColorMode: function (planetId: string, value: PlanetColor.PlanetColorMode) {
        planets[planetId].changeColorMode(value);
        return true;
    },

    changeRenderMode: function (planetId: string, value: RenderMode) {
        planets[planetId].changeRenderMode(value);
        return true;
    },

    changeSeason: function (planetId: string, value: number) {
        planets[planetId].changeSeason(value);
        return true;
    },

    create: function (planetId: string, canvasId: string) {
        planets[planetId] = new PlanetDisplay();
        return planets[planetId].init(canvasId);
    },

    scale: function (planetId: string, value: number) {
        planets[planetId].scale(value);
        return true;
    },

    setPlanet: function (planetId: string, data: PlanetData.PlanetData) {
        planets[planetId].setPlanet(data);
        return true;
    },

    smooth: function (planetId: string, value: boolean) {
        planets[planetId].smooth(value);
        return true;
    },
};
(window as any).planetBlazorFunctions = planetBlazorFunctions;
