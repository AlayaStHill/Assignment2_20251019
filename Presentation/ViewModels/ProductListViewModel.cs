using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Entities;
using Presentation.Interfaces;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class ProductListViewModel : ObservableObject
{
    private readonly IViewNavigationService _viewNavigationService;
    private readonly IProductService _productService;
    [ObservableProperty]
    // ObservableCollection = lista som implementerar INotifyCollectionChanged (signalerar när innehållet förändras) och INotifyPropertyChanged (signalerar när propertyn byts ut mot en ny instans - propertyn pekar på en ny lista)
    private ObservableCollection<Product> _productList = [];

    public ProductListViewModel(IViewNavigationService navigationService, IProductService productService)
    {
        _viewNavigationService = navigationService;
        _productService = productService;

        /* Konstruktorn bygger upp objektet ProductListViewModel när programmet startar och måste returnera en konkret instans direkt. Dvs Task och await kan inte användas,
           då returvärdet Task inte är ett färdigt objekt utan ett löfte om ett färdigt objekt senare. Objektet behöver visa en lista så snart vyn visas, laddningsprocessen måste därför startas direkt när viewmodeln skapas (därav initierar man laddningen i ctor).  Async används för att förhindra att UI:t fryser under hämtningen.
           _, gör det möjligt att starta en asynkron metoden i bakgrunden i konstruktorn eftersom det signalerar: ignorera den returnerade Tasken och tillåt konstruktorn att leverera ett färdigt objekt direkt. Konstruktorn har redan skapat ett objekt med en lista, men LoadAsync färdigställer innehållet i listan bakgrunden. I UI kan listan alltså först vara tom.
        */
        _ = LoadAsync(); 
    }


    [ObservableProperty] // genererar automatiskt en publik property bindbar till UI: public string Title {get => _title; set => SetProperty(ref _title, value }
    private string _title = "Produktlista";

    [ObservableProperty]
    private string? _errorMessage;

    private async Task PopulateProductListAsync()
    {
        ServiceResult<IEnumerable<Product>> loadResult = await _productService.GetProductsAsync();
        // loadresult är inte null, returnerar alltid ett nytt ServiceResult-objekt i GetProducts
        if (loadResult.Succeeded)
        {
            // Data från fil kan vara null (inget alls) eller tom (listan innehåller inga element)
            ProductList = new ObservableCollection<Product>(loadResult.Data ?? []);
        }
        else
        {
            ErrorMessage = loadResult!.ErrorMessage ?? "Ett fel uppstod när produkterna hämtades"; // Få upp i UI
        }
    }

    private async Task LoadAsync() // mellanlager, ansvarsseparation. styr när laddning sker, fångar tekniska/oförutsedda fel
    {
        try
        {
            await PopulateProductListAsync(); // logiken: hämtar/uppdaterar data, hanterar affärslogiska/förväntade fel, test- och återanvändbar
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ett fel uppstod när produkterna hämtades: {ex.Message}";
        }
    }


}

