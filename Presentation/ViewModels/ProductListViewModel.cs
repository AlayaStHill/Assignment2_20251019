using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Presentation.Interfaces;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class ProductListViewModel : StatusViewModelBase 
{
    private readonly IViewNavigationService _viewNavigationService;
    private readonly IProductService _productService;
    // Undertryck statusmeddelande vid avbrott orsakat av navigering
    private bool _suppressCancelStatus;

    // XAML kan bara binda publika properties. Readonly - värdet sätts i konstruktorn och kommandon ändras aldrig = ej ObservableProperty. ASYNCRelayCommand (-> LoadASYNC), kan skicka med en CancellationToken.
    public IAsyncRelayCommand LoadCommand { get; } // Initial laddning
    public IAsyncRelayCommand RefreshCommand { get; } // Omladdning

    public ProductListViewModel(IViewNavigationService navigationService, IProductService productService)
    {
        _viewNavigationService = navigationService;
        _productService = productService;

        // Kopplar kommandona till LoadAsync. AsyncRelay.. injicerar CancellationToken i LoadAsync  - execute-metoden (den metod som exekveras när LoadCommand körs) 
        // Att wrappa en async-metod i ett kommando fungerar likt _ (fire-and-forget), konstruktorn behöver inte vänta
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        // LoadCommand körs direkt när ViewModel skapas, dvs laddning av listan  
        LoadCommand.Execute(null);
    }

    // Uppdaterar innehållet i samma instans istället för att ersätta hela listan (som med användning av [ObservableProperty]). ObservableCollection notifierar UI via INotifyCollectionChanged.
    public ObservableCollection<Product> ProductList { get; } = new();

    [ObservableProperty] // genererar automatiskt en publik property bindbar till UI: public string Title {get => _title; set => SetProperty(ref _title, value }
    private string _title = "Produktlista";

    [ObservableProperty]
    private bool _isLoading;


    // logiken: hämtar/uppdaterar data, hanterar affärslogiska/förväntade fel, test- och återanvändbar
    public async Task PopulateProductListAsync(CancellationToken ct = default)
    {
        ServiceResult<IEnumerable<Product>> loadResult = await _productService.GetProductsAsync(ct);

        // loadresult är inte null, returnerar alltid ett nytt ServiceResult-objekt i GetProducts
        if (!loadResult.Succeeded)
        {
            SetStatus(loadResult!.ErrorMessage ?? "Kunde inte hämta produkterna. Försök igen senare.", "red");
            return;
        }

        // Ersätter inte ProductList-instansen utan fyller på samma lista. Tom lista så att loopen inte kraschar om Data är null. 
        ProductList.Clear();

        // Låt UI visa att listan blev tom (och ta emot klick) 
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
            IsLoading = true;
            await PopulateProductListAsync(ct);
        }
        // OperationCanceledException fångas här för att ge feedback till användaren. OperationCanceledException i ProductService.EnsureLoaded stoppar själva arbetsprocessen.
        catch (OperationCanceledException) 
        {
            if (!_suppressCancelStatus)
                SetStatus("Laddning avbröts.", "red");
        }
        // Fångar tekniska/oförutsedda fel
        catch (Exception ex) 
        {
            SetStatus($"Ett oväntat fel uppstod: {ex.Message}", "red");
        }
        finally {  IsLoading = false; }
    }

    private async Task RefreshAsync(CancellationToken ct) 
    {
        try
        {
            IsLoading = true;

            // Visa inte: Laddar om..., om vi undertrycker status vid navigering
            if (!_suppressCancelStatus)
                SetStatus("Laddar om...", "black");

            // Simulerar fördröjning i laddningen av listan 
            await Task.Delay(3000, ct);

            await PopulateProductListAsync(ct);

            int count = ProductList.Count;
            string plural = count == 1 ? "produkt" : "produkter";

            SetStatus($"Listan är uppdaterad. {count} {plural}.", "green");
        }
        catch (OperationCanceledException) // Fångar CancelRefresh
        {
            if (!_suppressCancelStatus)
                SetStatus("Omladdning avbröts.", "red");
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

    // Avbryt ev. pågående Load/Refresh/Delete och undertryck statusmeddelanden tills pågående tasks är färdiga eller avbrutna
    private async Task CancelOngoingLoadsAsync(bool suppressStatus = true) 
    {
        if (!(RefreshCommand.IsRunning || LoadCommand.IsRunning))
            return;

        // previous sparar _suppressStatusCancel värdet så som det var innan CancelOngoingLoads anropades. Detta skyddar mot överkörning av ett redan aktivt läge och gör undertryckningen lokal och tillfällig. 
        bool previous = _suppressCancelStatus;
        // sätter det tillfälliga värdet
        _suppressCancelStatus = suppressStatus;

        try
        {
            RefreshCommand.Cancel();
            LoadCommand.Cancel();

            // ExecutionTask: en property på AsyncRelayCommand. Referens till den Task som just nu körs av kommandot. Om inget körs = null. Om Refresh- eller LoadCommand körs, pekar den på den pågående Tasken.
            Task waitRefresh = RefreshCommand.ExecutionTask ?? Task.CompletedTask;
            // CompletedTask: en inbyggd statisk property i .NET. Representerar en Task som redan är färdig. Används som dummy när det inte finns någon riktig Task att vänta på. 
            Task waitLoad = LoadCommand.ExecutionTask ?? Task.CompletedTask;

            // Väntar till Refresh-, och/eller LoadCommand är färdiga eller avbrutna innan finally körs. Utan await körs finally direkt och _suppressCancelStatus skulle återställas för tidigt.  
            // _suppressCancelStatus är = true under hela perioden tills uppgifterna verkligen avslutats.
            try { await Task.WhenAll(waitRefresh, waitLoad); }
            catch (OperationCanceledException) { }
        }
        finally
        {
            // återställ till tidigare värde (sker utan att ev. avaktivera undertryck läge som en annan process behövde, ex. två navigeringar som startar nära varandra)
            _suppressCancelStatus = previous;
        }
    }

    [RelayCommand] // Kopplingen mellan UI-kontroller (Button ex) och ViewModelns metoder. Istället för att viewn direkt anropar dem i code-behind (ej MVVM). Command="{Binding NavigateTo..Command}" fungerar nu i viewn.
    private async Task NavigateToProductAddView()
    {
        // (param: value) named argument, ett sätt att skriva ut vilken parameter värdet gäller för. Mer läsbart än (true)
        await CancelOngoingLoadsAsync(suppressStatus: true);
        await ClearStatusAfterAsync(0);
        // Säkerställer att UI inte "fastnar" i laddning-läge om RefreshAsync blir avbruten av navigeringen och inte når finally-blocket
        IsLoading = false;
        _viewNavigationService.NavigateTo<ProductAddViewModel>();
    }

    [RelayCommand]
    private async Task Edit(Product? selectedProduct)
    {
        // Null-check: om bindningen av CommandParameter misslyckas blir parametern null. (Bindningen från radens dataobjekt (Product) till metodparametern selectedProduct).
        if (selectedProduct is null)
        {
            SetStatus("Välj en produkt att redigera.", "red");
            return;
        }

        await CancelOngoingLoadsAsync(suppressStatus: true);
        await ClearStatusAfterAsync(0);
        IsLoading = false;

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

    // CancellationToken: kortvarig process men blir konsekvent med övriga asynkrona metoder och kan avbrytas vid navigering. Delete är disk-I/O och kan potentiellt ta tid om disken är upptagen/låst/stor.
    [RelayCommand] // Skickar automatiskt in token när kommandot körs
    private async Task Delete(string productId, CancellationToken ct) 
    {
        try // Pratar med fil -> try-catch fånga oförutsedda tekniska fel
        {
            ServiceResult deleteResult = await _productService.DeleteProductAsync(productId, ct); 
            if (!deleteResult.Succeeded)
            {
                SetStatus(deleteResult.ErrorMessage ?? "Kunde inte ta bort produkten", "red");
                // Metoden avbryts om något gick fel, istället för att fortsätta på nästa rad
                return; 
            }

            // Uppdatera listan med samma ct, så avbryts även detta om användaren avbryter
            await PopulateProductListAsync(ct);

            SetStatus("Produkten har tagits bort", "green");
        }
        catch (Exception ex)
        {
            SetStatus($"Ett oväntat fel uppstod: {ex.Message}", "red");  
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

/*
Händelseförlopp RefreshAsync:
1. Användaren klickar på “Ladda om”.
2. RefreshCommand.Execute() körs.
3. AsyncRelayCommand startar RefreshAsync i en ny task.
4. Den tasken sparas i RefreshCommand.ExecutionTask.
Så länge RefreshAsync körs, är ExecutionTask en pekare till den metoden.
5. När RefreshAsync är färdig blir ExecutionTask null igen.
*/

