﻿using System.IO;
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
        public UiTests()
        {
           
            //var dataDir = ServiceProvider.GetRequiredService<IDataDirProvider>().Get();
            //Directory.CreateDirectory(dataDir);
            //var config = ServiceProvider.GetRequiredService<Config>();
            //config.LoadOrCreateDefaultFile();
            //var uiConfig = ServiceProvider.GetRequiredService<UiConfig>();
            //uiConfig.LoadOrCreateDefaultFile();

        }
        [Fact]
        public void LoginPageHasAButton()
        {
            using var ctx = new TestContext();

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
            var config = ctx.Services.GetRequiredService<Config>();
            var loginPage = ctx.RenderComponent<LoginPage>();

            //assert
            loginPage.Find("button").MarkupMatches("LOG IN");
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