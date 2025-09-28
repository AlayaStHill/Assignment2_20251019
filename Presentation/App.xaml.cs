using Infrastructure.Interfaces;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Presentation
{
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IFileRepository, FileRepository>();
                    services.AddSingleton<IProductService, ProductService>();
                })
                .Build();

        }

        // Måste ta bort StartupUri="MainWindow.xaml"> från App.xaml:  programmet startar annars upp utan dependency injection
        // Eftersom vi tog bort StartupUri måste vi aktivera programmet på ett annat sätt:
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Alla konfigurationer som behövs vid startup???
            base.OnStartup(e);

            // Plockar ut IProductService från DI-containern. Så att man kan --->
            IProductService productService = _host.Services.GetRequiredService<IProductService>(); 
            // ---> hämta alla produkter direkt när applikationen startar. Det kan man göra pga. Build Action: Content och Copy to Output Directory: Always
            await productService.GetProductsAsync(); 

          
        }

        
    }
}



