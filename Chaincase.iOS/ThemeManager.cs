using System;
using Chaincase.Common.Contracts;

using UIKit;
using Xamarin.Forms;

namespace Chaincase.iOS
{
    public class ThemeManager : IThemeManager
    {
        public bool IsDarkTheme() => Xamarin.Forms.Application.Current.RequestedTheme == OSAppTheme.Dark;

        public void SubscribeToThemeChanged(Action handler)
        {
            Xamarin.Forms.Application.Current.RequestedThemeChanged += (s, a) =>
            {
                handler.Invoke();
            };
        }
    }
}
