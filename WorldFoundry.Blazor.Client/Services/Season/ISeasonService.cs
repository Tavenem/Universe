using System;

namespace WorldFoundry.Blazor.Client.Services
{
    public interface ISeasonService
    {
        event EventHandler<IntEventArgs> SeasonChange;

        void SeasonChanged(int value);
    }
}
