using Chaincase.Navigation;

using Foundation;
using Splat;
using UIKit;

namespace Chaincase.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		//
		// This method is invoked when the application has loaded and is ready to run. In this 
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching(UIApplication application, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init();
			var compositionRoot = new CompositionRoot();
			var app = compositionRoot.ResolveApp();
			var registrar = new DependencyRegistrar();
			registrar.Register(Locator.CurrentMutable, compositionRoot);
			app.Initialize();

			LoadApplication(app);
			return base.FinishedLaunching(application, options);
		}
	}
}
