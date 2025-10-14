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
    // Refererar alltid till den senaste instansen av CancellationTokenSource som skapats i HideStatusSoon. När statusCts.Cancel() anropas (då ett nytt statusmeddelande visas) avbryts just den instansen, via token i Task.Delay.. Är null tills första gången HideStatusSoon() körs.
    private CancellationTokenSource? _statusCts;

    // XAML kan bara binda publika properties. Readonly - värdet sätts i konstruktorn och kommandon ändras aldrig = ej ObservableProperty. ASYNCRelayCommand (-> LoadASYNC), kan skicka med en CancellationToken.
    public IAsyncRelayCommand LoadCommand { get; } // Initial laddning
    public IAsyncRelayCommand RefreshCommand { get; } // Omladdning

    public ProductListViewModel(IViewNavigationService navigationService, IProductService productService)
    {
        _viewNavigationService = navigationService;
        _productService = productService;

        //Kopplar kommandona till LoadAsync. AsyncRelay.. injicerar CancellationToken i LoadAsync  - execute-metoden (den metod som exekveras när LoadCommand körs) 
        // Att wrappa en async-metod i ett kommando fungerar likt _ (fire-and-forget), konstruktorn behöver inte vänta
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        //AbortCommand = new AsyncRelayCommand(AbortAsync);

        // LoadCommand körs direkt när ViewModel skapas, dvs laddning av listan  
        LoadCommand.Execute(null);
    }

    // Uppdaterar innehållet i samma instans istället för att ersätta hela listan (som med användning av [ObservableProperty]). ObservableCollection notifierar UI via INotifyCollectionChanged.
    public ObservableCollection<Product> ProductList { get; } = new();

    [ObservableProperty] // genererar automatiskt en publik property bindbar till UI: public string Title {get => _title; set => SetProperty(ref _title, value }
    private string _title = "Produktlista";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _statusColor;

    // Ingen ct skickas in som parameter. Metoden sköter sin egen avbrytlogik
    private async Task HideStatusSoon(int ms = 3000)
    {
        // Avbryt eventuell tidigare timer = aldrig två timers igång samtidigt. Dispose avlastar operativsystemet direkt från att hålla liv i onödiga resurser, tills garbage collector städar.  
        _statusCts?.Cancel();
        _statusCts?.Dispose();
        // Skapa en ny cts-instans som gäller för just den här pågående väntan. Fältet refererar nu till denna nya instans 
        _statusCts = new CancellationTokenSource();
        // Hämtar en token från den nya instansen (via fältet). Tokenen är bara en kopia som används lokalt här, men är kopplad till _statusCts.
        CancellationToken ctoken = _statusCts.Token; 

        try
        {
            // Väntar i 3000 ms, men avbryts direkt om HideStatusSoon anropas igen, (för körs statusCts.Cancel() på föregående cts).
            await Task.Delay(ms, ctoken);
            // Om väntan inte avbryts, rensa status efter 3 sek.
            StatusMessage = null;
            StatusColor = null;
        }
        // Kastas om statusCts.Cancel() anropas på cts (alltså när en ny HideStatusSoon startas).
        catch (TaskCanceledException)
        {
            // Ignorera – det betyder bara att ett nytt statusmeddelande avbröt den gamla väntan.
        }
    }

    // logiken: hämtar/uppdaterar data, hanterar affärslogiska/förväntade fel, test- och återanvändbar
    public async Task PopulateProductListAsync(CancellationToken ct = default)
    {
        ServiceResult<IEnumerable<Product>> loadResult = await _productService.GetProductsAsync(ct);

        // loadresult är inte null, returnerar alltid ett nytt ServiceResult-objekt i GetProducts
        if (!loadResult.Succeeded)
        {
            StatusMessage = loadResult!.ErrorMessage ?? "Kunde inte hämta produkterna. Försök igen senare.";
            StatusColor = "red";

            // Fire-and-forget – kör i bakgrunden tills den är klar utan att blockera. Den här metoden ska kunna avslutas direkt ändå.
            _ = HideStatusSoon();
            return;
        }

        // Ersätter inte ProductList-instansen utan fyller på samma lista. Tom lista så att loopen inte kraschar om Data är null. 
        ProductList.Clear();

        // Låt UI visa att listan blev tom (och ta emot klick) ?????????????????????????
        await Task.Yield();

        foreach (Product product in loadResult.Data ?? [])
        {
            // Gör appen mer responsiv  - loopen avbryts snabbt efter Avbryt. Inte nödvändigt med få objekt (prestanda?)
            ct.ThrowIfCancellationRequested(); 
            ProductList.Add(product);
        }
    }

    // Körs av Load-kommandon. CancellationToken injiceras automatiskt av AsyncRelayCommand. 
    private async Task LoadAsync(CancellationToken ct) 
    {
        try
        {
            await PopulateProductListAsync(ct);
        }
        // OperationCanceledException fångas här för att ge feedback till användaren. OperationCanceledException i ProductService.EnsureLoaded stoppar själva arbetsprocessen.
        catch (OperationCanceledException) 
        {
            StatusMessage = "Laddning avbröts.";
            StatusColor = "red";
            _ = HideStatusSoon();
        }
        // Fångar tekniska/oförutsedda fel
        catch (Exception ex) 
        {
            StatusMessage = $"Ett oväntat fel uppstod: {ex.Message}";
            StatusColor = "red";
            _ = HideStatusSoon();
        }
    }

    private async Task RefreshAsync(CancellationToken ct) 
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Laddar om...";
            StatusColor = "black";

            // Simulerar fördröjning i laddningen av listan 
            await Task.Delay(3000, ct);

            await PopulateProductListAsync(ct);

            int count = ProductList.Count;
            string plural = count == 1 ? "produkt" : "produkter";

            StatusMessage = $"Listan är uppdaterad. {count} {plural}."; // Försvinner inte varför
            StatusColor = "green";
            _ = HideStatusSoon();
        }
        catch (OperationCanceledException) // Fångar CancelRefresh
        {
            StatusMessage = "Omladdning avbröts.";
            StatusColor = "red";
            _ = HideStatusSoon();
        }
        // Garanterar att UI inte "fastnar" i laddning-läge
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelRefresh()
    {
        if (RefreshCommand.IsRunning)
        {
            RefreshCommand.Cancel();
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
                _ = HideStatusSoon();
                return; // Metoden avbryts om något gick fel, istället för att fortsätta på nästa rad
            }

            await PopulateProductListAsync();

            StatusMessage = "Produkten har tagits bort";
            StatusColor = "green";
            _ = HideStatusSoon();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ett oväntat fel uppstod: {ex.Message}";
            StatusColor = "red";  
            _ = HideStatusSoon();
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

