using ApplicationLayer.DTOs;
using ApplicationLayer.Results;
using Domain.Entities;

namespace ApplicationLayer.Interfaces;
public interface IProductService 
{
    Task<ServiceResult<IEnumerable<Product>>> GetProductsAsync(CancellationToken ct = default); 
    Task<ServiceResult<Product>> SaveProductAsync(ProductCreateRequest createRequest, CancellationToken ct = default);
    Task<ServiceResult> UpdateProductAsync(ProductUpdateRequest updateRequest, CancellationToken ct = default);
    Task<ServiceResult> DeleteProductAsync(string id, CancellationToken ct = default);
}


