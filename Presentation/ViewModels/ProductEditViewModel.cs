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
    private Product? _product;

    // Kopierar över den valda produktens data till ViewModelns redigeringsinstans, så att användaren kan se och ändra informationen i UI:t innan ändringarna sparas.
    public void SetProduct(Product selectedProduct)
    {
        // Skapar en ny instans för redigering. Originalet påverkas inte förrän SaveCommand körs, vilket gör att CancelCommand kan avbryta utan att ändra originalet.
        Product = new Product
        {
            Name = selectedProduct.Name,
            Price = selectedProduct.Price,
            Category = selectedProduct.Category,
            Manufacturer = selectedProduct.Manufacturer
        };
        

    }
}
//  visa Id som inte kan editeras i edit-view