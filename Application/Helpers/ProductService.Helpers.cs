using ApplicationLayer.Helpers;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using Domain.Entities;
using Domain.Extensions;
using Domain.Results;
namespace ApplicationLayer.Services;

public partial class ProductService
{
    private static ServiceResult ValidateRequest(IProductRequest request)
    {
        if (request is null)
            return new ServiceResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Ingen data skickades in." };

        // Validera på trimmat namn
        string name = request.Name?.Trim() ?? string.Empty;

        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Namn måste anges.");

        if (request.Price is null)
            errors.Add("Pris måste anges.");
        else if (request.Price <= 0)
            errors.Add("Pris måste vara större än 0.");

        if (errors.Count > 0)
            return new ServiceResult
            {
                Succeeded = false,
                StatusCode = 400,
                ErrorMessage = string.Join("\n", errors)
            };

        return new ServiceResult { Succeeded = true, StatusCode = 200 };
    }

    private Product? FindExistingProduct(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) 
            return null;

        return _productList.FirstOrDefault(product => product.Id == id);
    }

    // Finns en produkt i _productList, vars ID inte är = requestId men vars namn är = requestNamn. 
    private bool IsDuplicateName(string requestName, string? requestId = null)
    {
        return _productList.Any(product => string.Equals(product.Name, requestName, StringComparison.OrdinalIgnoreCase)
            && (requestId is null || product.Id != requestId));
    }


    private async Task<ServiceResult> UpdateCategoryAsync(Product existingProduct, string? categoryName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            existingProduct.Category = null;
        }

        else
        {
            RepositoryResult<Category> categoryResult = await _categoryRepository.GetOrCreateAsync(category => string.Equals(category.Name, categoryName.Trim(), StringComparison.OrdinalIgnoreCase),
                () => new Category { Id = Guid.NewGuid().ToString(), Name = categoryName.Trim() }, ct);

            if (!categoryResult.Succeeded || categoryResult.Data == null)
                return categoryResult.MapToServiceResult("Kunde inte hämta eller skapa kategori.");

            existingProduct.Category = categoryResult.Data;
        }

        return new ServiceResult { Succeeded = true, StatusCode = 200 };
    }

    private async Task<ServiceResult> UpdateManufacturerAsync(Product existingProduct, string? manufacturerName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(manufacturerName))
        {
            existingProduct.Manufacturer = null;
        }

        else 
        {
            RepositoryResult<Manufacturer> manufacturerResult = await _manufacturerRepository.GetOrCreateAsync(manufacturer => string.Equals(manufacturer.Name, manufacturerName.Trim(), StringComparison.OrdinalIgnoreCase),
                () => new Manufacturer { Id = Guid.NewGuid().ToString(), Name = manufacturerName.Trim() },
                ct);

            if (!manufacturerResult.Succeeded || manufacturerResult.Data == null)
                return manufacturerResult.MapToServiceResult("Kunde inte hämta eller skapa tillverkare.");

            existingProduct.Manufacturer = manufacturerResult.Data;
        }

        return new ServiceResult { Succeeded = true, StatusCode = 200 };
    }
}

