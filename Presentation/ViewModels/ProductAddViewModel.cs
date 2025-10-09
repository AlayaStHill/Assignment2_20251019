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

        }
        if (ProductData is not null)
        {
            ServiceResult<Product> saveResult = await _productService.SaveProductAsync(ProductData);
            if (!saveResult.Succeeded)
            {
                StatusMessage = saveResult.ErrorMessage ?? "Produkten kunde inte sparas.";
                StatusColor = "Red";
                return; // ???? hoppar utr metoden om något gich fel. Behövs bara om det finns mer kod 
            }
        }

    }

    [RelayCommand]
    private void Cancel()
    {

    }
}

// Lägga till Clear()??