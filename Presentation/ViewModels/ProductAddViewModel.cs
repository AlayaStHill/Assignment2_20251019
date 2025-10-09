using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Presentation.Interfaces;
using System.Windows.Controls.Primitives;

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
    private Product _productData = new();

    [ObservableProperty]
    private string _title = "Ny Produkt";

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private string _statusColor;

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            if (ProductData is null)
            {
                StatusMessage = "Inga uppgifter att spara.";
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

    }
}

// Lägga till Clear()??