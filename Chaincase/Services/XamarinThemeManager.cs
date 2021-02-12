using System;
using Chaincase.Common.Contracts;
using Xamarin.Forms;

namespace Chaincase.Services
{
	public class XamarinThemeManager : IThemeManager
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
