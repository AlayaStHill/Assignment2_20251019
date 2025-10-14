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
        try// BORDE KOLLA OM PRODUCTdATA == NULL? också
        {
            // Defense in depth: även om ProductService validerar fälten, en snabb kontroll här för att ge direkt feedback till användaren,  utan onödigt anrop till fil. 
            if (string.IsNullOrWhiteSpace(ProductData?.Name) || ProductData.Price <= 0)
            {
                SetStatus("Fälten är inte korrekt ifyllda.", "red");
                return;
            }

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
