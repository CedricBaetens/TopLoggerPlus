using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.MobileBlazorBindings;
using Microsoft.MobileBlazorBindings.WebView;
using TopLoggerPlus.ApiWrapper;
using TopLoggerPlus.Logic;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TopLoggerPlus.Mobile
{
    public class App : Application
    {
        public App()
        {
            BlazorHybridHost.AddResourceAssembly(GetType().Assembly, contentRoot: "WebUI/wwwroot");

            var host = MobileBlazorBindingsHost.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Adds web-specific services such as NavigationManager
                    services.AddBlazorHybrid();

                    // Register app-specific services
                    services.AddTransient<RouteService>();
                    services.AddTransient<UserService>();
                    services.AddTransient<TopLoggerService>();
                })
                .Build();

            MainPage = new ContentPage { Title = "My Application" };
            host.AddComponent<Main>(parent: MainPage);
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
