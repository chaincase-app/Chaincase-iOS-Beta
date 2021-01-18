using System;
namespace Chaincase.Common.Contracts
{
    public interface IThemeManager
    {
        public bool IsDarkTheme();

        public void SubscribeToThemeChanged(Action handler);
    }
}
