using ApplicationLayer.DTOs;
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

    // XAML kan bara binda publika properties. Readonly - värdet sätts i konstruktorn och kommandon ändras aldrig = ej ObservableProperty. ASYNCRelayCommand (-> LoadASYNC), kan skicka med en CancellationToken.
    public IAsyncRelayCommand LoadCommand { get; } // Initial laddning
    public IAsyncRelayCommand RefreshCommand { get; } // Omladdning

    public ProductListViewModel(IViewNavigationService navigationService, IProductService productService)
    {
        _viewNavigationService = navigationService;
        _productService = productService;

        // AsyncRelay.. injicerar CancellationToken i execute-metoden LoadAsync (den metod som ska exekveras när programmet startar)
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        // Återanvänder samma execute
        RefreshCommand = new AsyncRelayCommand(LoadAsync);      
    }


    // Exponera en read-only ObservableCollection<Product> och uppdatera innehållet in-place.
    // varför inte med observableproperty
    //public ObservableCollection<Product> Products { get; } = new();


    [ObservableProperty]
    // ObservableCollection = lista som implementerar INotifyCollectionChanged (signalerar när innehållet förändras) och INotifyPropertyChanged (signalerar när propertyn byts ut mot en ny instans - propertyn pekar på en ny lista)
    private ObservableCollection<Product> _productList = [];

    [ObservableProperty] // genererar automatiskt en publik property bindbar till UI: public string Title {get => _title; set => SetProperty(ref _title, value }
    private string _title = "Produktlista";

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _statusColor;

    public async Task PopulateProductListAsync(CancellationToken ct = default)
    {
        ServiceResult<IEnumerable<Product>> loadResult = await _productService.GetProductsAsync();
        // loadresult är inte null, returnerar alltid ett nytt ServiceResult-objekt i GetProducts
        if (!loadResult.Succeeded)
        {
            StatusMessage = loadResult!.ErrorMessage ?? "Kunde inte hämta produkterna. Försök igen senare.";
            StatusColor = "red";
            return;
        }
        // Data från fil kan vara null (inget alls) eller tom (listan innehåller inga element)
        ProductList = new ObservableCollection<Product>(loadResult.Data ?? []);
    }

    // mellanlager, ansvarsseparation. styr när laddning sker, fångar tekniska/oförutsedda fel
    // 
    private async Task LoadAsync(CancellationToken ct) 
    {
        try
        {   // logiken: hämtar/uppdaterar data, hanterar affärslogiska/förväntade fel, test- och återanvändbar
            await PopulateProductListAsync(); 
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
    private void Edit(Product selectedProduct)
    {
        ProductUpdateRequest dto = new ProductUpdateRequest
        {
            Id = selectedProduct.Id,
            Name = selectedProduct.Name,
            Price = selectedProduct.Price,
            CategoryName = selectedProduct.Category?.Name,
            ManufacturerName = selectedProduct.Manufacturer?.Name
        };

        // konfigurera ProductEditViewModel med metoden SetProduct(product) innan ProductEditView visas. 
        _viewNavigationService.NavigateTo<ProductEditViewModel>(viewmodel => viewmodel.SetProduct(dto));
    }

    [RelayCommand] 
    private async Task Delete(string productId) 
    {
        try // Pratar med fil -> try-catch fånga oförutsedda tekniska fel
        {
            ServiceResult deleteResult = await _productService.DeleteProductAsync(productId); 
            if (!deleteResult.Succeeded)
            {
                StatusMessage = deleteResult.ErrorMessage ?? "Kunde inte ta bort produkten";
                StatusColor = "red";
                return; // Metoden avbryts om något gick fel, istället för att fortsätta på nästa rad
            }

            await PopulateProductListAsync();

            StatusMessage = "Produkten har tagits bort";
            StatusColor = "green";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ett oväntat fel uppstod: {ex.Message}";
            StatusColor = "red";    
        }
      
    }
}

/*
CANCELLATIONTOKEN-flöde:
ProductListViewModel -> ProductService:
1. AsyncRelayCommand skapar en CancellationTokenSource och skickar in cts.Token till LoadAsync (som måste ha CancellationToken som parameter - ej default).
2. LoadAsync skickar vidare samma ct till ProductService via PopulateListAsync:
ServiceResult<IEnumerable<Product>> loadResult = await _productService.GetProductsAsync(ct);

ProductService -> JsonRepository:
3. GetProductsAsync anropar EnsureLoaded(ct), som skickar token vidare till JsonRepository.ReadAsync(ct).

Om användaren trycker Avbryt kallar LoadCommand.CancelCommand.Execute(null)) cts.Cancel(), vilket gör att Token blir canceled. 
Nästa gång en async metod, som accepterar token (cancellation-aware)anropas, kastas OperationCanceledException.

OperationCanceledException bubblar vidare till EnsureLoaded - som fångar det i sin catch (OperationCanceledException)
och returnerar ErrorMessage: "Hämtning avbröts".
*/




/* !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
Statusmeddelande Produkten togs bort måste försvinna efter ett  tag. Hur göra?

StatusMessage = "Produkten har tagits bort";
StatusColor = "green";
_ = HideStatusSoon(3000);

private async Task HideStatusSoon(int ms = 3000)
{
    await Task.Delay(ms);
    StatusMessage = null;
    StatusColor = null;
}
*/