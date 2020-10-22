using Microsoft.MobileBlazorBindings.WebView.Windows;
using System;
using Chaincase.Common;
using Chaincase.Common.Xamarin;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

namespace Chaincase.Windows
{
    public class MainWindow : FormsApplicationPage
    {
        [STAThread]
        public static void Main()
        {
            var app = new System.Windows.Application();
            app.Run(new MainWindow());
        }

        public MainWindow()
        {
            Forms.Init();
            BlazorHybridWindows.Init();
            LoadApplication(new App(ConfigureDi));
        }

        private void ConfigureDi(IServiceCollection obj)
        {
	        obj.AddSingleton<IHsmStorage, XamarinHsmStorage>();
        }
    }
}
