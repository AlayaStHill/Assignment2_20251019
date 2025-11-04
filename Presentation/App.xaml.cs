using ApplicationLayer.Interfaces;
using ApplicationLayer.Services;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Interfaces;
using Presentation.Services;
using Presentation.ViewModels;
using Presentation.Views;
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
                    services.AddScoped<IProductService, ProductService>();

                    string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
                    services.AddScoped<IRepository<Product>>(serviceProvider => new JsonRepository<Product>(dataDirectory, "products.json"));
                    services.AddScoped<IRepository<Category>>(serviceProvider => new JsonRepository<Category>(dataDirectory, "categories.json"));
                    services.AddScoped<IRepository<Manufacturer>>(serviceProvider => new JsonRepository<Manufacturer>(dataDirectory, "manufacturers.json"));

                    //Presentation
                    services.AddSingleton<IViewNavigationService, ViewNavigationService>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainViewModel>();

                    services.AddScoped<ProductListViewModel>();
                    // Startvyn. Transient, laddar in listan på nytt varje gång man går in på vyn
                    services.AddScoped<ProductListView>();

                    services.AddTransient<ProductAddViewModel>();
                    services.AddTransient<ProductAddView>();

                    services.AddScoped<ProductEditViewModel>();
                    services.AddScoped<ProductEditView>();


                })
                .Build();

        }

        // Måste ta bort StartupUri="MainWindow.xaml"> från App.xaml:  programmet startar annars upp utan dependency injection
        // Eftersom vi tog bort StartupUri måste vi aktivera programmet på ett annat sätt:
        protected override void OnStartup(StartupEventArgs e)
        {
            // Alla konfigurationer som behövs vid startup???
            base.OnStartup(e);

            // Hämtar en instans av MainViewModel från DI. 
            MainViewModel mainViewModel = _host!.Services.GetRequiredService<MainViewModel>();
            // Sätter propertyn CurrentViewModel inne i MainViewModel till en instans av ProductListViewModel. Bestämmer vilken vy programmet startas upp med (kan göras inuti MainViewModel-filen också)
            mainViewModel.CurrentViewModel = _host!.Services.GetRequiredService<ProductListViewModel>(); 
            
            // Hämtar MainWindow från DI
            MainWindow mainWindow = _host!.Services.GetRequiredService<MainWindow>();
            // Mappar MainWindow till MainViewModel. 
            mainWindow.DataContext = mainViewModel;

            mainWindow.Show();

        }

        
    }
}
/* 
Singleton: informationen kvarstår från tidigare när man går in på vyn igen. instansieras en gång
Transient: informationen nollställs varje gång. Instansieras på nytt varje gång man går in på vyn

*/



