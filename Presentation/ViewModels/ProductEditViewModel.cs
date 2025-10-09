using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Entities;
using Presentation.Interfaces;


namespace Presentation.ViewModels;

public partial class ProductEditViewModel(IViewNavigationService viewNavigationService, IProductService productService) : ObservableObject
{
    private readonly IViewNavigationService _viewNavigationService = viewNavigationService;
    private readonly IProductService _productService = productService;

    [ObservableProperty]
    private ProductUpdateRequest _productData;

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
}
