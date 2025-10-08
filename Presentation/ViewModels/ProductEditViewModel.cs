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

    public void SetProduct(Product product)
    {
        Product = new Product
        {
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            Manufacturer = product.Manufacturer
        };
        

    }
}
//  visa Id som inte kan editeras i edit-view