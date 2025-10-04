using ApplicationLayer.DTOs;
using Domain.Entities;

namespace ApplicationLayer.Factories;

public class ProductFactory
{
    public static Product MapRequestToProduct(ProductCreateRequest productCreateRequest)
    {
        return new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = productCreateRequest.Name,
            Price = productCreateRequest.Price,
            Category = null,
            Manufacturer = null
        };
    }
}