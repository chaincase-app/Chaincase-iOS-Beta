using System;
using Hara.Abstractions;
using Hara.Abstractions.Contracts;
using Hara.Abstractions.Services;
using Hara.UI.Services;
using Hara.XamarinCommon.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.MobileBlazorBindings;
using Xamarin.Forms;

namespace Hara.XamarinCommon
{
    public class App :Application
                          {
                              private readonly IHost _host;
                      
                              public IServiceProvider ServiceProvider => _host.Services;
                      
                              public App(IFileProvider fileProvider = null, Action<IServiceCollection> configureDI = null)
                              {
                                  var hostBuilder = MobileBlazorBindingsHost.CreateDefaultBuilder()
                                      .ConfigureServices((hostContext, services) =>
                                      {
                                          // Adds web-specific services such as NavigationManager
                                          services.AddBlazorHybrid();
                      
                                          services.AddUIServices();
                                          // Register app-specific services
                                          services.AddSingleton<ICounterState, CounterState>();
                                          services.AddSingleton<ILocalContentFetcher, FileProviderLocalContentFetcher>();
                                          services.AddSingleton<IWebsiteLauncher, XamarinEssentialsWebsiteLauncher>();
                                          services.AddSingleton<IWeatherForecastFetcher, WeatherForecastFetcher>();
                                          services.AddSingleton<IConfigProvider, XamarinEssentialsConfigProvider>();
                                          services.AddSingleton<ISecureConfigProvider, XamarinEssentialsSecureConfigProvider>();
                                          configureDI?.Invoke(services);
                                      })
                                      .UseWebRoot("wwwroot");
                      
                                  if (fileProvider != null)
                                  {
                                      hostBuilder.UseStaticFiles(fileProvider);
                                  }
                                  else
                                  {
                                      hostBuilder.UseStaticFiles();
                                  }
                      
                                  _host = hostBuilder.Build();
                      
                                  MainPage = new ContentPage {Title = "My Application"};
                                  _host.AddComponent<Main>(parent: MainPage);
                              }
                      
                              protected override void OnStart()
                              {
                              }
                      
                              protected override void OnSleep()
                              {
                              }
                      
                              protected override void OnResume()
                              {
                              }
                          } 
}