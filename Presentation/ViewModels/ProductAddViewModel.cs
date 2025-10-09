using ApplicationLayer.Interfaces;
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
    private Product _productData = new();

    [ObservableProperty]
    private string _title = "Ny Produkt";

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private string _statusColor;

    [RelayCommand]
    private void Save()
    {

    }

    [RelayCommand]
    private void Cancel()
    {

    }
}

// Lägga till Clear()??