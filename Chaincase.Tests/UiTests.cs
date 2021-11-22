using System.IO;
using Bunit;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using Chaincase.Common.Services.Mock;
using Chaincase.SSB;
using Chaincase.UI.Pages;
using Chaincase.UI.Services;
using Chaincase.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using WalletWasabi.Helpers;
using Xunit;

namespace Chaincase.Tests
{
	public class UiTests
    {
        private readonly TestContext ctx;
        public UiTests()
        {
            ctx = new TestContext();
            ctx.JSInterop.SetupVoid("IonicBridge.registerBlazorCustomHandler", _ => true);

            ctx.Services.AddCommonServices();
            ctx.Services.AddSingleton<IDataDirProvider, TestDataDirProvider>();
            ctx.Services.AddSingleton<INotificationManager, MockNotificationManager>();
            ctx.Services.AddSingleton<ITorManager, MockTorManager>();
            ctx.Services.AddSingleton<IThemeManager, MockThemeManager>();
            ctx.Services.AddSingleton<IHsmStorage, MockHsmStorage>();
            ctx.Services.AddSingleton<UIStateService>();
            ctx.Services.AddSingleton<StackService>();
            ctx.Services.AddScoped<ThemeSwitcher>();

            ctx.Services.AddSingleton<PINViewModel>();
            ctx.Services.AddSingleton<SendViewModel>();
            ctx.Services.AddSingleton<SelectCoinsViewModel>();
            ctx.Services.AddSingleton<BackUpViewModel>();

            var dataDir = ctx.Services.GetRequiredService<IDataDirProvider>().Get();
            Directory.CreateDirectory(dataDir);
            var config = ctx.Services.GetRequiredService<Config>();
            config.LoadOrCreateDefaultFile();
            var uiConfig = ctx.Services.GetRequiredService<UiConfig>();
            uiConfig.LoadOrCreateDefaultFile();

        }

        [Fact]
        public void LoginPageHasAButton()
        {
            var loginPage = ctx.RenderComponent<LoginPage>();

            //assert
            loginPage.Find("ion-button").TextContent.MarkupMatches("LOG IN");
        }

        // TODO this fails regardless of the bound disabled ViewModel value
        // Something is wrong with the test
        //[Fact] 
        //public void SendButtonValidationWorks()
        //{
        //    var sendViewModel = ctx.Services.GetRequiredService<SendViewModel>();

        //    var sendAmountPage = ctx.RenderComponent<SendAmountPage>();
        //    var spendButton = sendAmountPage.Find("ion-button");
        //    var disabledAttr = spendButton.Attributes.GetNamedItem("disabled");

        //    Assert.True(disabledAttr.Value == "true");
        //}
    }

    public class TestDataDirProvider : SSBDataDirProvider
    {
	    public TestDataDirProvider()
	    {
		    SubDirectory = Path.Combine("Test", "Client");
	    } 
	    public TestDataDirProvider(string dir)
	    {
		    SubDirectory = Path.Combine("Test", "Client", dir);
	    } 
    }
}
