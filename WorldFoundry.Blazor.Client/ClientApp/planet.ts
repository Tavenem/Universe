import * as WebGLUtil from './webgl/util';
import * as PlanetData from './webgl/planet-data';
import * as PlanetColor from './webgl/planet-color';
import * as Cube from './webgl/cube-render';
import * as Globe from './webgl/globe-render';
import * as Hammer from './webgl/hammer-render';

function $(selectors: string, container?: Element): Element {
    return (container || document).querySelector(selectors);
}

const loadingBar = $("#loading-bar");
const loadingMsg = $("#loading-msg");

function showLoading(message: string): void {
    loadingBar.classList.remove("invisible");
    loadingMsg.innerHTML = message || "";
}
function hideLoading(): void {
    loadingBar.classList.add("invisible");
}

export function disable(element: Element): void {
    element.setAttribute("disabled", "disabled");
}
export function enable(element: Element): void {
    element.removeAttribute("disabled");
}

export function hide(selectors: string, container?: Element): void {
    $(selectors, container).classList.add("hidden");
}
export function show(selectors: string, container?: Element): void {
    $(selectors, container).classList.remove("hidden");
}

let animationInfo = {
    frame: 0,
    gl: undefined as WebGLRenderingContext,
    params: {
        autorotate: true,
        scale: 1,
        season: 0,
    } as WebGLUtil.ParamInitData,
    renderBtn: undefined as RenderButton,
    renderColor: undefined as ColorRadio,
    renderInfo: undefined as WebGLUtil.RenderInfo,
    elevationSeed: undefined as number,
    suspendUpdates: false,
    time: 0,
};

let planet: PlanetData.Planet;

interface RenderButton {
    btn: HTMLInputElement,
    init: WebGLUtil.RenderInfoInit,
}
const rendererButtons = {
    cube: {
        btn: $("#cube-renderer-button") as HTMLInputElement,
        init: Cube.initRenderInfo,
    } as RenderButton,
    globe: {
        btn: $("#globe-renderer-button") as HTMLInputElement,
        init: Globe.initRenderInfo,
    } as RenderButton,
    hammer: {
        btn: $("#hammer-renderer-button") as HTMLInputElement,
        init: Hammer.initRenderInfo,
    } as RenderButton,
}

interface ColorRadio {
    rad: HTMLInputElement,
    mode: PlanetColor.PlanetColorMode,
    colors?: PlanetColor.PlanetColors,
    buffers?: WebGLUtil.WebGLBuffers[],
}
const colorRadios = {
    vegetation: {
        rad: $("#vegetation-radio") as HTMLInputElement,
        mode: PlanetColor.PlanetColorMode.Vegetation,
    } as ColorRadio,
    elevation: {
        rad: $("#elevation-radio") as HTMLInputElement,
        mode: PlanetColor.PlanetColorMode.Elevation,
    } as ColorRadio,
    temperature: {
        rad: $("#temperature-radio") as HTMLInputElement,
        mode: PlanetColor.PlanetColorMode.Temperature,
    } as ColorRadio,
    precipitation: {
        rad: $("#precipitation-radio") as HTMLInputElement,
        mode: PlanetColor.PlanetColorMode.Precipitation,
    } as ColorRadio,
}

const autorotateCheck = $("#autorotate-check") as HTMLInputElement;
const autoseasonCheck = $("#autoseason-check") as HTMLInputElement;
const gridNum = $("#grid-num") as HTMLInputElement;
const paramButton = $("#param-button") as HTMLButtonElement;
const planetButton = $("#planet-button") as HTMLButtonElement;
const scaleNum = $("#scale-num") as HTMLInputElement;
const seasonNum = $("#season-num") as HTMLInputElement;
const seasonTotal = $("#season-total") as HTMLInputElement;
const pressureNum = $("#pressure-num") as HTMLInputElement;
const radiusNum = $("#radius-num") as HTMLInputElement;
const rotationNum = $("#rotation-num") as HTMLInputElement;
const smoothCheck = $("#smooth-check") as HTMLInputElement;
const tiltNum = $("#tilt-num") as HTMLInputElement;
const waterNum = $("#water-num") as HTMLInputElement;

function changeSeason(num) {
    animationInfo.renderInfo.params.season = num;
    animationInfo.params.season = num;
    seasonNum.value = num + 1;
    switchBuffers();
}

function clearColorRadioCaches() {
    for (var radName in colorRadios) {
        let cRad = colorRadios[radName] as ColorRadio;
        delete cRad.buffers;
        delete cRad.colors;
    }
}

function getColorRadioFromEvent(ev: Event) {
    let rad = ev.target as HTMLInputElement;
    let cRad = Object.entries(colorRadios).find((v) => v[1].rad === rad);
    return cRad === undefined ? undefined : cRad[1];
}

function getRendererBtnFromEvent(ev: MouseEvent) {
    let btn = ev.target as HTMLInputElement;
    let rBtn = Object.entries(rendererButtons).find((v) => v[1].btn === btn);
    return rBtn === undefined ? undefined : rBtn[1];
}

function incrementSeason() {
    let s = animationInfo.params.season;
    s++;
    if (s >= planet.seasons.length) {
        s = 0;
    }
    changeSeason(s);
}

function initBuffers() {
    if (!animationInfo.renderColor.buffers) {
        animationInfo.renderColor.buffers = Array.from({ length: planet.seasons.length }, v => undefined);
    }
    planet.buffers = animationInfo.renderColor.buffers;
    switchBuffers();
}

function initColorRadios() {
    for (var radName in colorRadios) {
        (colorRadios[radName] as ColorRadio).rad.addEventListener("change", ev => {
            let cRad = getColorRadioFromEvent(ev);
            if (!!cRad) {
                animationInfo.renderColor = cRad;
                if (!!planet) {
                    initColors();
                    initBuffers();
                }
            }
        });
    }
}

function initColors() {
    if (!animationInfo.renderColor.colors) {
        showLoading("Painting planet colors...");
        animationInfo.renderColor.colors = new PlanetColor.PlanetColors(planet, animationInfo.params.season, getColorMode());
    } else if (!animationInfo.renderColor.colors.tileColors[animationInfo.params.season]) {
        showLoading("Painting planet colors...");
        animationInfo.renderColor.colors.addSeason(planet, animationInfo.params.season, getColorMode());
    }
    planet.colors = animationInfo.renderColor.colors;
}

function initControls() {
    initColorRadios();
    initRendererButtons();

    autorotateCheck.addEventListener("change", (ev) => {
        animationInfo.params.autorotate = autorotateCheck.checked;
        animationInfo.renderInfo.params.autorotate = autorotateCheck.checked;
        if (!autorotateCheck.checked) {
            animationInfo.params.longitude = animationInfo.renderInfo.params.longitude;
        } else {
            delete animationInfo.params.longitude;
        }
    });

    gridNum.addEventListener("change", (ev) => {
        if (!!planet) {
            enable(paramButton);
        }
    });

    paramButton.addEventListener("click", (ev) => {
        getPlanet();
    });

    planetButton.addEventListener("click", (ev) => {
        animationInfo.elevationSeed = undefined;
        getPlanet();
    });

    pressureNum.addEventListener("change", (ev) => {
        if (!!planet) {
            enable(paramButton);
        }
    });

    radiusNum.addEventListener("change", (ev) => {
        if (!!planet) {
            enable(paramButton);
        }
    });

    rotationNum.addEventListener("change", (ev) => {
        if (!!planet) {
            enable(paramButton);
        }
    });

    seasonNum.addEventListener("change", (ev) => {
        changeSeason(Math.min(planet.seasons.length, seasonNum.valueAsNumber) - 1);
    });

    seasonTotal.addEventListener("change", (ev) => {
        if (!!planet) {
            enable(paramButton);
        }
    });

    scaleNum.addEventListener("change", (ev) => {
        animationInfo.params.scale = scaleNum.valueAsNumber;
        animationInfo.renderInfo.params.scale = scaleNum.valueAsNumber;
    });

    smoothCheck.addEventListener("change", (ev) => {
        clearColorRadioCaches();
        if (!!planet) {
            initBuffers();
        }
    });

    tiltNum.addEventListener("change", (ev) => {
        if (!!planet) {
            enable(paramButton);
        }
    });

    waterNum.addEventListener("change", (ev) => {
        if (!!planet) {
            enable(paramButton);
        }
    });
}

function initRendererButtons() {
    for (var btnName in rendererButtons) {
        (rendererButtons[btnName] as RenderButton).btn.addEventListener("click", ev => {
            let rBtn = getRendererBtnFromEvent(ev);
            if (!!rBtn) {
                animationInfo.renderBtn = rBtn;
                switchRenderer();
            }
        });
    }
}

function disableRendererButtons() {
    for (var btnName in rendererButtons) {
        disable(rendererButtons[btnName].btn);
    }
}

function enableRendererButtons() {
    for (var btnName in rendererButtons) {
        enable(rendererButtons[btnName].btn);
    }
}

function animate() {
    let seasonLength = !!planet ? 10 / planet.seasons.length : 0;

    let then = 0;
    function render(now: number) {
        now *= 0.001;
        const deltaTime = now - then;
        then = now;

        animationInfo.renderInfo.renderer(animationInfo.gl, animationInfo.renderInfo.sceneInfo, animationInfo.renderInfo.params, deltaTime);

        if (!!planet && autoseasonCheck.checked && !animationInfo.suspendUpdates) {
            animationInfo.time += deltaTime;
            if (animationInfo.time >= seasonLength) {
                animationInfo.time %= seasonLength;
                incrementSeason();
            }
        }

        animationInfo.frame = requestAnimationFrame(render);
    }
    animationInfo.frame = requestAnimationFrame(render);
}

function getColorMode(): PlanetColor.PlanetColorMode {
    for (var radName in colorRadios) {
        let cr = colorRadios[radName] as ColorRadio;
        if (cr.rad.checked) {
            return cr.mode;
        }
    }
    return PlanetColor.PlanetColorMode.Vegetation;
}

function getPlanet(btn?: RenderButton) {
    hide("#err-alert");
    showLoading("Loading planet data...");
    disable(paramButton);
    disable(planetButton);
    disableRendererButtons();
    cancelAnimationFrame(animationInfo.frame);

    let seasonCount = seasonTotal.valueAsNumber;
    if (animationInfo.params.season >= seasonCount) {
        animationInfo.params.season = seasonCount - 1;
        seasonNum.value = seasonCount.toString();
    }

    let radius = radiusNum.valueAsNumber * 1000;
    let rotation = rotationNum.valueAsNumber * 60;
    let grid = gridNum.valueAsNumber + 2;
    let planetUrl = `/Home/GetPlanet?atmosphericPressure=${pressureNum.valueAsNumber}&axialTilt=${tiltNum.valueAsNumber}&radius=${radius}&rotationalPeriod=${rotation}&waterRatio=${waterNum.valueAsNumber}&gridSize=${grid}`;
    if (!!animationInfo.elevationSeed) {
        planetUrl += `&seed=${animationInfo.elevationSeed}`;
    }
    return fetch(planetUrl)
        .then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error(response.statusText);
            }
        })
        .then(data => {
            planet = PlanetData.planetFromData(data);
            animationInfo.elevationSeed = planet.elevationSeed;

            getSeasons(0, seasonCount, btn);
        })
        .catch((err: Error) => {
            console.log(err.message);
            enableRendererButtons();
            enable(planetButton);
            hideLoading();
            show("#err-alert");
        });
}

function getSeasons(n: number, total: number, btn?: RenderButton) {
    let seasonUrl = `/Home/GetSeason?key=${planet.elevationSeed}&amount=${total}&index=${n}`;
    if (n === 0) {
        showLoading("Loading climate data...");
    }
    fetch(seasonUrl)
        .then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error(response.statusText);
            }
        })
        .then(data => {
            planet.seasons.push(data);
            n++;
            if (n < total) {
                getSeasons(n, total, btn);
            } else {
                animationInfo.params.season = Math.min(total, seasonNum.valueAsNumber) - 1;

                if (!!btn) {
                    animationInfo.renderBtn = btn;
                } else if (!animationInfo.renderBtn || animationInfo.renderBtn === rendererButtons.cube) {
                    animationInfo.renderBtn = rendererButtons.globe;
                }

                switchRenderer();
                enable(planetButton);
            }
        })
        .catch((err: Error) => {
            console.log(err.message);
            enableRendererButtons();
            enable(planetButton);
            hideLoading();
            show("#err-alert");
        });
}

function switchBuffers() {
    initColors();
    if (!animationInfo.renderColor.buffers[animationInfo.params.season]) {
        showLoading("Rendering planet shape...");
        animationInfo.suspendUpdates = true;
        animationInfo.renderColor.buffers[animationInfo.params.season] = animationInfo.renderInfo.updateBuffers(animationInfo.gl, planet, animationInfo.params.season, smoothCheck.checked);
        planet.buffers = animationInfo.renderColor.buffers;
        animationInfo.suspendUpdates = false;
    }
    animationInfo.renderInfo.sceneInfo.buffers = planet.buffers[animationInfo.params.season];
    hideLoading();
}

function switchRenderer() {
    showLoading("Loading display...");
    cancelAnimationFrame(animationInfo.frame);
    animationInfo.suspendUpdates = true;
    clearColorRadioCaches();
    if (!!planet) {
        if (!!planet.tileBuffers) {
            delete planet.tileBuffers;
        }
        if (!!planet.oceanBuffers) {
            delete planet.oceanBuffers;
        }
    }
    animationInfo.renderInfo = animationInfo.renderBtn.init(animationInfo.gl, planet, animationInfo.params);
    if (!!planet) {
        initBuffers();
    }
    animationInfo.suspendUpdates = false;
    animate();
    if (!!planet) {
        enableRendererButtons();
    }
    disable(animationInfo.renderBtn.btn);
    hideLoading();
}

function main() {
    const canvas = $("#planet-canvas") as HTMLCanvasElement;
    animationInfo.gl = canvas.getContext("webgl", { alpha: false });

    if (!animationInfo.gl) {
        show("#webgl-alert");
        return;
    }

    animationInfo.gl.clearColor(0.0, 0.0, 0.0, 1.0);
    animationInfo.gl.clear(animationInfo.gl.COLOR_BUFFER_BIT);

    initControls();

    animationInfo.renderBtn = rendererButtons.cube;
    animationInfo.renderColor = colorRadios.vegetation;
    switchRenderer();
}
main();
