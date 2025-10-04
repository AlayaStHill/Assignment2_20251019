using ApplicationLayer.Interfaces;
using ApplicationLayer.Services;
using ApplicationLayer.Vet_ejvadskaheta;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Infrastructure.Vet_ejvadskaheta;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Interfaces;
using Presentation.Services;
using System.IO;
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
                    // ApplicationLayer
                    services.AddSingleton<IProductService, ProductService>();

                    // Infrastructure
                    string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
                    services.AddSingleton<IRepository<Product>>(serviceProvider => new JsonRepository<Product>(dataDirectory, "products.json"));
                    services.AddSingleton<IRepository<Category>>(serviceProvider => new JsonRepository<Category>(dataDirectory, "categories.json"));
                    services.AddSingleton<IRepository<Manufacturer>>(serviceProvider => new JsonRepository<Manufacturer>(dataDirectory, "manufacturers.json"));

                    //Presentation
                    services.AddSingleton<IViewNavigationService, ViewNavigationService>();
                    services.AddSingleton<MainWindow>();


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



