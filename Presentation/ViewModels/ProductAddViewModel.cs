using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Presentation.Interfaces;

namespace Presentation.ViewModels;

public partial class ProductAddViewModel : ObservableObject
{
    private readonly IViewNavigationService _viewNavigationService;
    private readonly IProductService _productService;

    public ProductAddViewModel(IViewNavigationService viewNavigationService, IProductService productService)
    {
        _viewNavigationService = viewNavigationService;
        _productService = productService;
    }

    [ObservableProperty]
    private ProductCreateRequest _productData = new();

    [ObservableProperty]
    private string _title = "Ny Produkt";

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _statusColor;

    [RelayCommand]
    private async Task Save() 
    {
        try
        {
            // (defense in depth), validering görs i productservice, men för att förhindra onödiga serviceanrop - hoppa ur metoden direkt här och ge användaren feedback utan möjlig fördröjning.
            if (string.IsNullOrWhiteSpace(ProductData?.Name) || ProductData.Price <= 0)
            {
                StatusMessage = "Fälten namn och pris är inte korrekt ifyllda.";
                StatusColor = "Red";
                return;
            }


            ServiceResult<Product> saveResult = await _productService.SaveProductAsync(ProductData);

            if (!saveResult.Succeeded)
            {
                StatusMessage = saveResult.ErrorMessage ?? "Produkten kunde inte sparas.";
                StatusColor = "Red";
                return;
            }

            // Om allt gick bra
            StatusMessage = "Produkten har sparats.";
            StatusColor = "Green";

            await _viewNavigationService.NavigateToAsync<ProductListViewModel>(viewmodel => viewmodel.PopulateProductListAsync());
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ett oväntat fel uppstod: {ex.Message}";
            StatusColor = "Red";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _viewNavigationService.NavigateTo<ProductListViewModel>();
    }
}

// Lägga till Clear()??


