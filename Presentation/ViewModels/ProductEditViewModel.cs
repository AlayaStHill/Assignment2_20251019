using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Interfaces;


namespace Presentation.ViewModels;

public partial class ProductEditViewModel(IViewNavigationService viewNavigationService, IProductService productService) : StatusViewModelBase
{
    private readonly IViewNavigationService _viewNavigationService = viewNavigationService;
    private readonly IProductService _productService = productService;

    // Varför nullable: ProductEditViewModel skapas först, och sedan kallas SetProduct() via navigationen efter tryck på Redigera-knapp. Under det fönstret kan ProductData vara null. Null-check i Save().
    [ObservableProperty]
    private ProductUpdateRequest? _productData;

    [ObservableProperty]
    private string _title = "Uppdatera produkt";

    // Kopierar över den valda produktens data till ViewModelns redigeringsinstans, så att användaren kan se och ändra informationen i UI:t innan ändringarna sparas.
    public void SetProduct(ProductUpdateRequest product)
    {
        // Skapar en ny instans för redigering. Originalet påverkas inte förrän SaveCommand körs, vilket gör att CancelCommand kan avbryta utan att ändra originalet.
        ProductData = new ProductUpdateRequest
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            CategoryName = product.CategoryName,
            ManufacturerName = product.ManufacturerName,
        };
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            if (ProductData is null) 
            {
                SetStatus("Inga produktuppgifter angivna.", "red");
                return;
            }

            // Tillägg för korrekt dublettlogik. string.Empty fångas nedan
            string name = ProductData.Name?.Trim() ?? string.Empty;

            List<string> errors = [];

            if (string.IsNullOrWhiteSpace(ProductData.Name))
                errors.Add("Namn måste anges.");

            if (ProductData.Price is null)
                errors.Add("Pris måste anges.");
            else if (ProductData.Price <= 0)
                errors.Add("Pris måste vara större än 0.");

            if (errors.Count > 0)
            {
                SetStatus(string.Join("\n", errors), "red");
                return;
            }

            // Den trimmade varianten sparas
            ProductData.Name = name;

            ServiceResult saveResult = await _productService.UpdateProductAsync(ProductData);

            if (!saveResult.Succeeded)
            {
                SetStatus(saveResult.ErrorMessage ?? "Produkten kunde inte uppdateras.", "red");
                return;
            }

            // Om allt gick bra
            SetStatus("Produkten har uppdaterats.", "green");

            // Användaren hinner se statusmeddelandet
            await Task.Delay(1000);

            await _viewNavigationService.NavigateToAsync<ProductListViewModel>(viewmodel => viewmodel.PopulateProductListAsync());
        }
        catch (Exception ex)
        {
            SetStatus($"Ett oväntat fel uppstod: {ex.Message}", "red");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _viewNavigationService.NavigateTo<ProductListViewModel>();
    }
}
