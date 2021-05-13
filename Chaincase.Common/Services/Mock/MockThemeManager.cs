using System;
using Chaincase.Common.Contracts;

namespace Chaincase.Common.Services.Mock
{
    public class MockThemeManager : IThemeManager
    {
        public bool IsDarkTheme() => true;

        public void SubscribeToThemeChanged(Action handler)
        {
            return;
        }
    }
}
