using System;

namespace WorldFoundry.Blazor.Client.Services
{
    public class SeasonService : ISeasonService
    {
        public event EventHandler<IntEventArgs> SeasonChange;

        public void SeasonChanged(int value) => OnSeasonChanged(new IntEventArgs { Value = value });

        protected virtual void OnSeasonChanged(IntEventArgs e) => SeasonChange?.Invoke(this, e);
    }
}
