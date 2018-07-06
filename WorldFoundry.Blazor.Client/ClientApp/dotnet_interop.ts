export function showError(message: string) {
    DotNet.invokeMethodAsync("WorldFoundry.Blazor.Client", "ShowError", "WebGL wasn't able to load correctly in your browser.");
}

export function showLoading(message: string): void {
    DotNet.invokeMethodAsync("WorldFoundry.Blazor.Client", "ShowLoading", message);
}
export function hideLoading(): void {
    DotNet.invokeMethodAsync("WorldFoundry.Blazor.Client", "HideLoading");
}

export function seasonChanged(value: number): void {
    DotNet.invokeMethodAsync("WorldFoundry.Blazor.Client", "SeasonChanged", value);
}
