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
public partial class ProductService(IRepository<Product> productRepository, IRepository<Category> categoryRepository, IRepository<Manufacturer> manufacturerRepository) : IProductService
{
    // fältets namn ska reflektera interfacet (vad det gör), inte implementationen (hur det görs)
    private readonly IRepository<Product> _productRepository = productRepository;
    private readonly IRepository<Category> _categoryRepository = categoryRepository;
    private readonly IRepository<Manufacturer> _manufacturerRepository = manufacturerRepository;
    private List<Product> _products = [];
    // Gäller för hela klassen - pekar alltid på den senaste instansen som newats i metoderna. När Cancel anropas stoppas just den instansen..Token tar emot stoppsignalen och vidarebefodrar den till koden som använder den. 
    private CancellationTokenSource _cts = null!;
    private bool _isLoaded;

    public async Task<ServiceResult<Product>> SaveProductAsync(ProductCreateRequest createRequest) // MÅSTE FÅNGA UPP DATA = NULL I MAINWINDOW.XAML.CS 
    {
        try
        {
            // Typen är redan specificerad i fältet. Här kommer en ny tilldelning bara. Deklarerar jag typen frånkopplar jag den från fältet och skapar en ny lokal variabel. Skapar en ny instans för att säkerställa en ny nollställd _cts utan pågående cancellation. 
            _cts = new CancellationTokenSource();

            ServiceResult validationResult = ValidateRequest(createRequest);
            if (!validationResult.Succeeded)
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 400, ErrorMessage = "Produktfälten är inte korrekt ifyllda." };

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage, Data = null };

            // Jämför product.Name med createRequest.Name tecken för tecken och ignorerar skillnaden mellan stora/små bokstäver, Ordinal jämförelse = jämför bokstävernas nummer i dator (Unicode-värden), inte deras plats i alfabetet som kan skilja sig beroende på språkinställning.
            if (IsDuplicateName(createRequest.Name))
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 409, ErrorMessage = $"En produkt med namnet {createRequest.Name} finns redan." };

            Product newProduct = ProductFactory.MapRequestToProduct(createRequest);
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


    public async Task<ServiceResult> UpdateProductAsync(ProductUpdateRequest updateRequest)
    {
        try
        {
            _cts = new CancellationTokenSource();

            ServiceResult validationResult = ValidateRequest(updateRequest); 
            if (!validationResult.Succeeded)
                return validationResult;


            ServiceResult ensureResult = await EnsureLoadedAsync();
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
            ServiceResult manufacturerResult = await UpdateManufacturerAsync(existingProduct, updateRequest.CategoryName);
            if (!categoryResult.Succeeded)
                return manufacturerResult;

            // Uppdatera produkten
            existingProduct.Name = updateRequest.Name;
            existingProduct.Price = updateRequest.Price;

            RepositoryResult saveResult = await _productRepository.WriteAsync(_products, _cts.Token);
            if (!saveResult.Succeeded)
                return saveResult.MapToServiceResult("Ett okänt fel uppstod vid filsparning");

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


