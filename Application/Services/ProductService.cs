using ApplicationLayer.DTOs;
using ApplicationLayer.Factories;
using ApplicationLayer.Helpers;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Results;

namespace ApplicationLayer.Services;
public partial class ProductService(IRepository<Product> productRepository, IRepository<Category> categoryRepository, IRepository<Manufacturer> manufacturerRepository) : IProductService
{
    private readonly IRepository<Product> _productRepository = productRepository;
    private readonly IRepository<Category> _categoryRepository = categoryRepository;
    private readonly IRepository<Manufacturer> _manufacturerRepository = manufacturerRepository;
    private List<Product> _products = [];
    private bool _isLoaded;

    public async Task<ServiceResult<Product>> SaveProductAsync(ProductCreateRequest createRequest, CancellationToken ct = default) // MÅSTE FÅNGA UPP DATA = NULL I MAINWINDOW.XAML.CS 
    {
        try 
        {
            ServiceResult validationResult = ValidateRequest(createRequest);
            if (!validationResult.Succeeded)
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 400, ErrorMessage = "Produktfälten är inte korrekt ifyllda." };

            ServiceResult ensureResult = await EnsureLoadedAsync(ct);
            if (!ensureResult.Succeeded)
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage, Data = null };

            // Jämför product.Name med createRequest.Name tecken för tecken och ignorerar skillnaden mellan stora/små bokstäver, Ordinal jämförelse = jämför bokstävernas nummer i dator (Unicode-värden), inte deras plats i alfabetet som kan skilja sig beroende på språkinställning.
            if (IsDuplicateName(createRequest.Name))
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 409, ErrorMessage = $"En produkt med namnet {createRequest.Name} finns redan." };

            Product newProduct = ProductFactory.MapRequestToProduct(createRequest);
            _products.Add(newProduct);

            RepositoryResult saveResult = await _productRepository.WriteAsync(_products, ct);
            if (!saveResult.Succeeded)
                return saveResult.MapToServiceResultAs<Product>("Kunde inte spara till fil.");

            return new ServiceResult<Product> { Succeeded = true, StatusCode = 201, Data = newProduct };
        }
        catch (OperationCanceledException) // cancelcommand via CancellationToken
        {
            return new ServiceResult<Product> { Succeeded = false, StatusCode = 500, ErrorMessage = "Sparande avbröts" };
        }
        catch (Exception ex) // Oväntat fel som uppstått i SaveProductAsync()
        {
            return new ServiceResult<Product> { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att spara produkten: {ex.Message}", Data = null };
        }
    }

    // OperationCanceledException centraliserad till EnsureLoaded (hanterar det i sin catch)
    public async Task<ServiceResult> EnsureLoadedAsync(CancellationToken ct) 
    {
        if (_isLoaded)
            return new ServiceResult { Succeeded = true, StatusCode = 200 };

        try
        {
            RepositoryResult<IEnumerable<Product>>? loadResult = await _productRepository.ReadAsync(ct);
            if (!loadResult.Succeeded)
                return loadResult.MapToServiceResult("Ett okänt fel uppstod vid filhämtning");

            _products = [.. (loadResult.Data ?? [])];
            _isLoaded = true;

            // Listan var inte laddad från början, ReadAsync kördes och det lyckades.
            return new ServiceResult { Succeeded = true, StatusCode = 200 };
        }
        // Catch-block: hanterar avbryt och oväntade buggar. Sådant som repot inte fångar
        catch (OperationCanceledException)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = "Hämtning avbröts." };
        }
        catch (Exception ex)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = $"Fel vid filhämtning: {ex.Message}" };
        }
    }


    // Behöver inte try-catch – anropar bara EnsureLoaded/ReadAsync som redan hanterar felen
    public async Task<ServiceResult<IEnumerable<Product>>> GetProductsAsync(CancellationToken ct = default)
    {
        ServiceResult ensureResult = await EnsureLoadedAsync(ct);
        if (!ensureResult.Succeeded)
            return new ServiceResult<IEnumerable<Product>> { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage, Data = [] };

        // spreadoperator, tar den nya listan och sprider det i den nya. Istället för att loopa igenom med foreach tar den hela listan på en gång
        return new ServiceResult<IEnumerable<Product>> { Succeeded = true, StatusCode = 200, Data = [.. _products] };
    }


    public async Task<ServiceResult> UpdateProductAsync(ProductUpdateRequest updateRequest, CancellationToken ct = default)
    {
        try 
        {
            ServiceResult validationResult = ValidateRequest(updateRequest); 
            if (!validationResult.Succeeded)
                return validationResult;


            ServiceResult ensureResult = await EnsureLoadedAsync(ct);
            if (!ensureResult.Succeeded)
                return ensureResult;

            if (FindExistingProduct(updateRequest.Id) is not Product existingProduct)
                return new ServiceResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Produkten med Id {updateRequest.Id} kunde inte hittas" };

            // Kontrollera om en produkt med samma namn, men olika ID redan finns. 
            if (IsDuplicateName(updateRequest.Name, updateRequest.Id))
                return new ServiceResult { Succeeded = false, StatusCode = 409, ErrorMessage = $"En produkt med namnet {updateRequest.Name} finns redan." };

            // Hämta eller skapa en kategori enbart om CategoryName inskickat
            ServiceResult categoryResult = await UpdateCategoryAsync(existingProduct, updateRequest.CategoryName);
            if (!categoryResult.Succeeded)
                return categoryResult;

            // Hämta eller skapa tillverkare enbart om ManufacturerName inskickat
            ServiceResult manufacturerResult = await UpdateManufacturerAsync(existingProduct, updateRequest.ManufacturerName);
            if (!categoryResult.Succeeded)
                return manufacturerResult;

            // Uppdatera produkten
            existingProduct.Name = updateRequest.Name;
            existingProduct.Price = updateRequest.Price;

            RepositoryResult saveResult = await _productRepository.WriteAsync(_products, ct);
            if (!saveResult.Succeeded)
                return saveResult.MapToServiceResult("Ett okänt fel uppstod vid filsparning");

            // Operationen lyckas
            return new ServiceResult { Succeeded = true, StatusCode = 204 };
        }
        catch (OperationCanceledException)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = "Uppdatering avbröts" };
        }
        catch (Exception ex)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att uppdatera produkten: {ex.Message}" };
        }
    }



    public async Task<ServiceResult> DeleteProductAsync(string id, CancellationToken ct = default)
    {
        try 
        {
            ServiceResult ensureResult = await EnsureLoadedAsync(ct);
            if (!ensureResult.Succeeded)
                return ensureResult;

            Product? productToDelete = FindExistingProduct(id);
            if (productToDelete is null)
                return new ServiceResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Produkten med Id {id} kunde inte hittas" };

            _products.Remove(productToDelete);

            // Spara till fil, annars uppdateras inte listan och ändringen ligger bara i minnet och försvinner när programmet stängs.
            RepositoryResult repoSaveResult = await _productRepository.WriteAsync(_products, ct);
            if (!repoSaveResult.Succeeded)
                return repoSaveResult.MapToServiceResult("Ett okänt fel uppstod vid filsparning");

            //Operationen lyckades
            return new ServiceResult { Succeeded = true, StatusCode = 204 };
        }
        catch (OperationCanceledException)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = "Borttagning avbröts" };
        }
        catch (Exception ex)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att ta bort produkten: {ex.Message}" };
        }
    }
}

