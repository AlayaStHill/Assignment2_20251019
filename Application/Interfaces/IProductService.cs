using Application.DTOs;
using Application.Results;
using Domain.Entities;

namespace Infrastructure.Interfaces;
public interface IProductService // lägg in cancellationToken
{
    void Cancel();
    Task<ServiceResult<IEnumerable<Product>>> GetProductsAsync(); // GetProductById(string id);//Name ??
    Task<ServiceResult<Product>> SaveProductAsync(Product product);
    Task<ServiceResult> UpdateProductAsync(ProductUpdateRequest productUpdateRequest);
    Task<ServiceResult> DeleteProductAsync(string id);
}
