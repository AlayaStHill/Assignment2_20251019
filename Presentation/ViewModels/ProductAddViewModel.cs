using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using ApplicationLayer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Presentation.Interfaces;

namespace Presentation.ViewModels;

public partial class ProductAddViewModel(IViewNavigationService viewNavigationService, IProductService productService) : StatusViewModelBase
{
    private readonly IViewNavigationService _viewNavigationService = viewNavigationService;
    private readonly IProductService _productService = productService;

    [ObservableProperty]
    private ProductCreateRequest _productData = new();

    [ObservableProperty]
    private string _title = "Ny Produkt";

    [RelayCommand]
    private async Task Save() 
    {
        try
        {
            // Defense in depth: även om ProductService validerar fälten, en snabb kontroll här för att ge direkt feedback till användaren,  utan onödigt anrop till fil. 
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

            ServiceResult<Product> saveResult = await _productService.SaveProductAsync(ProductData);

            if (!saveResult.Succeeded)
            {
                SetStatus(saveResult.ErrorMessage ?? "Produkten kunde inte sparas.", "red");
                return;
            }

            // Om allt gick bra
            SetStatus("Produkten har sparats.", "green");

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



