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
using NBitcoin;
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

            //ctx.services.AddSingleton(x => {
            //    var network = x.GetRequiredService<Config>().Network;
            //    var dataDir = x.GetRequiredService<IDataDirProvider>().Get();
            //    var walletDirectories = x.GetService<WalletDirectories>() ?? new WalletDirectories(dataDir);
            //    var notificationManager = x.GetRequiredService<INotificationManager>();
            //    return new ChaincaseWalletManager(network, walletDirectories, notificationManager);
            //});
            ctx.Services.AddSingleton<IDataDirProvider, TestDataDirProvider>();
            ctx.Services.AddSingleton<INotificationManager, MockNotificationManager>();
            ctx.Services.AddSingleton<ITorManager, MockTorManager>();
            ctx.Services.AddSingleton<IThemeManager, MockThemeManager>();
            ctx.Services.AddSingleton<IHsmStorage, MockHsmStorage>();
            ctx.Services.AddSingleton<UIStateService>();
            ctx.Services.AddSingleton<StackService>();
            ctx.Services.AddScoped<ThemeSwitcher>();

            ctx.Services.AddSingleton<PINViewModel>();
            ctx.Services.AddTransient<SendViewModel>();
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


        [Fact]
        public void DestinationUrlPropertyParsedFromString()
        {
            var sendViewModel = ctx.Services.GetRequiredService<SendViewModel>();
            string amountString = "0.0001111";

            sendViewModel.DestinationString = $"bitcoin:BC1Q6APR55WRXTDTK3CJ6M69MXAYW99VVGTVDNVUNT?amount={amountString}&pj=https://testnet.demo.btcpayserver.org/BTC/pj";
            var amount = Money.Parse(amountString);
            Assert.True(sendViewModel.DestinationUrl.Amount == amount);
            Assert.True(sendViewModel.AmountText == amountString);
            Assert.True(sendViewModel.OutputAmount == amount);
        }

        [Fact]
        public void SendMaxAmountIsMaxSelected()
        {
            var sendViewModel = ctx.Services.GetRequiredService<SendViewModel>();
            string amountString = "0.0001111";
            sendViewModel.AmountText = amountString;
            var amount = Money.Parse(amountString);
            Assert.False(sendViewModel.IsMax);
            Assert.True(sendViewModel.OutputAmount == amount);
            sendViewModel.IsMax = true;
            Assert.False(sendViewModel.OutputAmount == amount);
        }
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
