using ApplicationLayer.DTOs;
using ApplicationLayer.Results;
using Domain.Entities;

namespace ApplicationLayer.Interfaces;
public interface IProductService // lägg in cancellationToken
{
    void Cancel();
    Task<ServiceResult<IEnumerable<Product>>> GetProductsAsync(); // GetProductById(string id);//Name ??
    Task<ServiceResult<Product>> SaveProductAsync(ProductCreateRequest productCreateRequest);
    Task<ServiceResult> UpdateProductAsync(ProductUpdateRequest productUpdateRequest);
    Task<ServiceResult> DeleteProductAsync(string id);
}
