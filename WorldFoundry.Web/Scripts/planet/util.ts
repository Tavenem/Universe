export function $(selectors: string, container?: Element): Element {
    return (container || document).querySelector(selectors);
}

const loadingBar = $("#loading-bar");
const loadingMsg = $("#loading-msg");

export function disable(element: Element): void {
    element.setAttribute("disabled", "disabled");
}

export function enable(element: Element): void {
    element.removeAttribute("disabled");
}

export function hide(selectors: string, container?: Element): void {
    $(selectors, container).classList.add("hidden");
}

export function hideLoading(): void {
    loadingBar.classList.add("invisible");
}

export function show(selectors: string, container?: Element): void {
    $(selectors, container).classList.remove("hidden");
}

export function showLoading(message: string): void {
    loadingBar.classList.remove("invisible");
    loadingMsg.innerHTML = message || "";
}
