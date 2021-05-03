using System.IO;
using Bunit;
using NBitcoin;
using WalletWasabi.Blockchain.Keys;
using Xunit;
using Chaincase.UI;
using Chaincase.SSB;
using Chaincase.UI.Pages;
using Chaincase.UI.Services;
using Chaincase.UI.ViewModels;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Chaincase.Common;
using Chaincase.Common.Contracts;
using WalletWasabi.Helpers;

namespace Chaincase.Tests
{
    public class UiTests
    {
        private TestContext ctx;
        public UiTests()
        {
            ctx = new TestContext();
            ctx.JSInterop.SetupVoid("IonicBridge.registerBlazorCustomHandler", _ => true);

            ctx.Services.AddCommonServices();
            ctx.Services.AddSingleton<IDataDirProvider, TestDataDirProvider>();
            ctx.Services.AddSingleton<INotificationManager, MockServices.MockNotificationManager>();
            ctx.Services.AddSingleton<ITorManager, MockTorManager>();
            ctx.Services.AddSingleton<IThemeManager, MockThemeManager>();
            ctx.Services.AddSingleton<UIStateService>();
            ctx.Services.AddSingleton<StackService>();
            ctx.Services.AddScoped<ThemeSwitcher>();

            ctx.Services.AddSingleton<PINViewModel>();
            ctx.Services.AddSingleton<SendViewModel>();
            ctx.Services.AddSingleton<SelectCoinsViewModel>();


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

        [Fact]
        public void SpendButtonIsEnabled()
		{
            var sendViewModel = ctx.Services.GetRequiredService<SendViewModel>();

            var sendAmountPage = ctx.RenderComponent<SendAmountPage>();
            var spendButton = sendAmountPage.Find("ion-button");
            var disabledAttr = spendButton.Attributes.GetNamedItem("disabled");

            Assert.True(disabledAttr.Value == "true");
		}
    }

    public class TestDataDirProvider : IDataDirProvider
    {
        public string Get()
        {
            return EnvironmentHelpers.GetDataDir(Path.Combine("Test", "Client"));
        }
    }
}
