using Microsoft.JSInterop;
using WorldFoundry.Blazor.Client.Services;

namespace WorldFoundry.Blazor.Client.JSInterop
{
    public class Season
    {
        private static ISeasonService _season;

        public Season(ISeasonService seasonService) => _season = seasonService;

        [JSInvokable("SeasonChanged")]
        public static void SeasonChanged(int value) => _season.SeasonChanged(value);
    }
}
