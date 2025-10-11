using ApplicationLayer.DTOs;
using ApplicationLayer.Factories;
using ApplicationLayer.Helpers;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using Domain.Entities;
using Domain.Helpers;
using Domain.Interfaces;
using Domain.Results;

namespace ApplicationLayer.Services;
public class ProductService(IRepository<Product> productRepository, IRepository<Category> categoryRepository, IRepository<Manufacturer> manufacturerRepository) : IProductService
{
    // fältets namn ska reflektera interfacet (vad det gör), inte implementationen (hur det görs)
    private readonly IRepository<Product> _productRepository = productRepository;
    private readonly IRepository<Category> _categoryRepository = categoryRepository;
    private readonly IRepository<Manufacturer> _manufacturerRepository = manufacturerRepository;
    private List<Product> _products = [];
    // gäller för hela klassen - pekar alltid på den senaste instansen som newats i metoderna. När Cancel anropas stoppas just den instansen..Token tar emot stoppsignalen och vidarebefodrar den till koden som använder den. 
    private CancellationTokenSource _cts = null!;
    private bool _isLoaded;

    public async Task<ServiceResult<Product>> SaveProductAsync(ProductCreateRequest productCreateRequest) // MÅSTE FÅNGA UPP DATA = NULL I MAINWINDOW.XAML.CS 
    {
        try
        {
            // Typen är redan specificerad i fältet. Här kommer en ny tilldelning bara. Deklarerar jag typen frånkopplar jag den från fältet och skapar en ny lokal variabel. Skapar en ny instans för att säkerställa en ny nollställd _cts utan pågående cancellation. 
            _cts = new CancellationTokenSource();

            if (string.IsNullOrWhiteSpace(productCreateRequest.Name) || productCreateRequest.Price < 0)
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 400, ErrorMessage = "Produktfälten är inte korrekt ifyllda." };

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage, Data = null };
            
            // Jämför product.Name med productUpdateRequest.Name tecken för tecken och ignorerar skillnaden mellan stora/små bokstäver, Ordinal jämförelse = jämför bokstävernas nummer i dator (Unicode-värden), inte deras plats i alfabetet som kan skilja sig beroende på språkinställning.
            if (_products.Any(p => p.Name.Equals(productCreateRequest.Name, StringComparison.OrdinalIgnoreCase)))
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 409, ErrorMessage = $"En produkt med namnet {productCreateRequest.Name} finns redan.", Data = null };

            Product newProduct = ProductFactory.MapRequestToProduct(productCreateRequest);
            _products.Add(newProduct);

            RepositoryResult saveResult = await _productRepository.WriteAsync(_products, _cts.Token);
            if (!saveResult.Succeeded)
                return saveResult.MapToServiceResultAs<Product>("Kunde inte spara till fil.");

            return new ServiceResult<Product> { Succeeded = true, StatusCode = 201, Data = newProduct };
        }
        catch (Exception ex)
        {
            Cancel();
            // Oväntat fel som uppstått i SaveProductAsync()
            return new ServiceResult<Product> { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att spara produkten: {ex.Message}", Data = null };
        }
        finally
        {
            _cts.Dispose();
        }
    }

    public async Task<ServiceResult> EnsureLoadedAsync()
    {
        if (_isLoaded)
            return new ServiceResult { Succeeded = true, StatusCode = 200 };

        // Säkerställer att _cts inte är null vid anropning av ReadAsync eftersom EnsureLoaded är publik. Om _cts är null -> skapa en ny
        _cts ??= new CancellationTokenSource();

        RepositoryResult<IEnumerable<Product>>? loadResult = await _productRepository.ReadAsync(_cts.Token);

        if (!loadResult.Succeeded)
            return loadResult.MapToServiceResult("Ett okänt fel uppstod vid filhämtning");

        _products = [.. (loadResult.Data ?? [])];
        _isLoaded = true;

        // Listan var inte laddad från början, ReadAsync kördes och det lyckades.
        return new ServiceResult { Succeeded = true, StatusCode = 200 };
    }

    public async Task<ServiceResult<IEnumerable<Product>>> GetProductsAsync()
    {
        try
        {
            _cts = new CancellationTokenSource();

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return new ServiceResult<IEnumerable<Product>> { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage, Data = [] };

            // spreadoperator, tar den nya listan och sprider det i den nya. Istället för att loopa igenom med foreach tar den hela listan på en gång
            return new ServiceResult<IEnumerable<Product>> { Succeeded = true, StatusCode = 200, Data = [.. _products] };
        }
        catch (Exception ex)
        {
            Cancel();
            // Tömmer data som finns sparad i listan, så att den inte innehåller gammal information efter ett fel.
            _products = [];
            return new ServiceResult<IEnumerable<Product>> { Succeeded = false, StatusCode = 500, ErrorMessage = $"Fel vid filhämtning: {ex.Message}", Data = [] };
        }
        finally
        {
            _cts.Dispose();
        }

    }


    public async Task<ServiceResult> UpdateProductAsync(ProductUpdateRequest productUpdateRequest)
    {
        try
        {
            _cts = new CancellationTokenSource();

            if (string.IsNullOrWhiteSpace(productUpdateRequest.Name) || productUpdateRequest.Price <= 0)
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 400, ErrorMessage = "Produktfälten namn och pris är inte korrekt ifyllda." };

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return ensureResult;

            Product? existingProduct = _products.FirstOrDefault(product => product.Id == productUpdateRequest.Id);
            if (existingProduct == null)
                return new ServiceResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Produkten med Id {productUpdateRequest.Id} kunde inte hittas" };

            // Kontrollera om produkten redan finns. Om true = existerar redan
            if (_products.Any(p => p.Name.Equals(productUpdateRequest.Name, StringComparison.OrdinalIgnoreCase) && p.Id != productUpdateRequest.Id))
                return new ServiceResult { Succeeded = false, StatusCode = 409, ErrorMessage = $"En produkt med namnet {productUpdateRequest.Name} finns redan." };

            // Uppdatera produkten
            existingProduct.Name = productUpdateRequest.Name;
            existingProduct.Price = productUpdateRequest.Price;

            // Hämta eller skapa en kategori enbart om CategoryName inskickat
            if (!string.IsNullOrWhiteSpace(productUpdateRequest.CategoryName))
            {
                RepositoryResult<Category> categoryResult = await _categoryRepository.GetOrCreateAsync(category => category.Name == productUpdateRequest.CategoryName,
                    () => new Category { Id = Guid.NewGuid().ToString(), Name = productUpdateRequest.CategoryName }, _cts.Token);

                if (!categoryResult.Succeeded || categoryResult.Data == null)
                    return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = categoryResult.ErrorMessage ?? "Kunde inte hämta eller skapa kategori." };

                existingProduct.Category = categoryResult.Data;
            }

            // Hämta eller skapa tillverkare enbart om ManufacturerName inskickat
            if (!string.IsNullOrWhiteSpace(productUpdateRequest.ManufacturerName))
            {
                RepositoryResult<Manufacturer> manufacturerResult = await _manufacturerRepository.GetOrCreateAsync(manufacturer => manufacturer.Name == productUpdateRequest.ManufacturerName,
                    () => new Manufacturer { Id = Guid.NewGuid().ToString(), Name = productUpdateRequest.ManufacturerName }, _cts.Token);

                if (!manufacturerResult.Succeeded || manufacturerResult.Data == null)
                    return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = manufacturerResult.ErrorMessage ?? "Kunde inte hämta eller skapa tillverkare." };

                existingProduct.Manufacturer = manufacturerResult.Data;
            }

            RepositoryResult saveResult = await _productRepository.WriteAsync(_products, _cts.Token);
            if (!saveResult.Succeeded)
                return saveResult.MapToServiceResult("Ett okänt fel inträffade vid filsparning");

            // Operationen lyckas
            return new ServiceResult { Succeeded = true, StatusCode = 204 };

        }
        catch (Exception ex)
        {
            Cancel();
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att uppdatera produkten: {ex.Message}" };
        }
        finally
        {
            _cts.Dispose();
        }
    }



    public async Task<ServiceResult> DeleteProductAsync(string id)
    {
        try
        {
            _cts = new CancellationTokenSource();

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return ensureResult;

            Product? productToDelete = _products.FirstOrDefault(product => product.Id == id);
            if (productToDelete == null)
                return new ServiceResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Produkten med Id {id} kunde inte hittas" };

            _products.Remove(productToDelete);

            // Spara till fil, annars uppdateras inte listan och ändringen ligger bara i minnet och försvinner när programmet stängs.
            RepositoryResult repoSaveResult = await _productRepository.WriteAsync(_products, _cts.Token);
            if (!repoSaveResult.Succeeded)
                return repoSaveResult.MapToServiceResult("Ett okänt fel uppstod vid filsparning");

            //Operationen lyckades
            return new ServiceResult { Succeeded = true, StatusCode = 204 };
        }
        catch (Exception ex)
        {
            Cancel();
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att ta bort produkten: {ex.Message}" };
        }
        finally
        {
            _cts.Dispose();
        }
    }


    // Cancel-button aktiveras bara när "nedladdning" pågår, men ändå säkra upp för att skydda mot ett scenario där Cancel anropas då _cts är disposed eller fortfarande null innan någon metod körs
    public void Cancel()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            try
            {
                _cts.Cancel();
            }
            // kastas om _cts är disposad då Cancel() anropas
            catch (ObjectDisposedException)
            {
                // neutraliserat undantag, programmet kraschar ej och kan köra vidare om en ny metod anropas
            }
        }
    }
}


