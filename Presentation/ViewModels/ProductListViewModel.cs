using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Presentation.Interfaces;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class ProductListViewModel : ObservableObject
{
    private readonly IViewNavigationService _viewNavigationService;
    private readonly IProductService _productService;

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

    [ObservableProperty]
    // ObservableCollection = lista som implementerar INotifyCollectionChanged (signalerar när innehållet förändras) och INotifyPropertyChanged (signalerar när propertyn byts ut mot en ny instans - propertyn pekar på en ny lista)
    private ObservableCollection<Product> _productList = [];

    [ObservableProperty] // genererar automatiskt en publik property bindbar till UI: public string Title {get => _title; set => SetProperty(ref _title, value }
    private string _title = "Produktlista";

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _statusColor;

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
            StatusMessage = loadResult!.ErrorMessage ?? "Kunde inte hämta produkterna. Försök igen senare."; 
            StatusColor = "red";
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
            StatusMessage = $"Ett oväntat fel uppstod: {ex.Message}";
            StatusColor = "red";
        }
    }


   [RelayCommand] // Kopplingen mellan UI-kontroller (Button ex) och ViewModelns metoder. Istället för att viewn direkt anropar dem i code-behind (ej MVVM). Command="{Binding NavigateTo..Command}" fungerar nu i viewn.
    private void NavigateToProductAddView()
    {
        _viewNavigationService.NavigateTo<ProductAddViewModel>();
    }

    [RelayCommand]
    private void Edit(Product product)
    { 
        // konfigurera ProductEditViewModel med metoden SetProduct(product) innan ProductEditView visas. 
        _viewNavigationService.NavigateTo<ProductEditViewModel>(viewmodel => viewmodel.SetProduct(product));

    }

    [RelayCommand] 
    private async Task Delete(string productId)
    {
        try // Pratar med fil -> try-catch fånga oförutsedda tekniska fel
        {
            ServiceResult deleteResult = await _productService.DeleteProductAsync(productId); 
            if (deleteResult.Succeeded)
            {
                await PopulateProductListAsync();

                StatusMessage = "Produkten har tagits bort";
                StatusColor = "green";
            }
            else
            {
                StatusMessage = deleteResult.ErrorMessage ?? "Kunde inte ta bort produkten";
                StatusColor = "red";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ett oväntat fel uppstod: {ex.Message}";
            StatusColor = "red";    
        }
      
    }
}

// CANCELLATIONTOKEN - hanteras på lägre nivå enbart??

// Ska statusmeddelande förv´svinna efter ett  tag?