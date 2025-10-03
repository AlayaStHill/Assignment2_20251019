using Domain.Interfaces;
using Domain.Entities;
using Application.Results;
using Domain.Results;
using Application.DTOs;

namespace Application.Services;
public class ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, IManufacturerRepository manufacturerRepository) : IProductService
{
    // fältets namn ska reflektera interfacet (vad det gör), inte implementationen (hur det görs)
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IManufacturerRepository _manufacturerRepository = manufacturerRepository;
    private List<Product> _products = [];
    // gäller för hela klassen - pekar alltid på den senaste instansen som newats i metoderna. När Cancel anropas stoppas just den instansen..Token tar emot stoppsignalen och vidarebefodrar den till koden som använden den. 
    private CancellationTokenSource _cts = null!;
    private bool _isLoaded;

    // Cancel-button aktiveras bara när "nedladdning" pågår, men ändå säkra upp för att skydda mot ett scenario där Cancel anropas då _cts är disposed eller fortfarande null innan någon metod körs
    public void Cancel()
    {
        // efter Cancel() anropats är IsCancellationRequested == true
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


    public async Task<ServiceResult> EnsureLoadedAsync() 
    {
        // Listan är redan laddad
        if (_isLoaded)
            return new ServiceResult { Succeeded = true, StatusCode = 200 };

        // Säkerställer att _cts inte är null vid anropning av ReadAsync eftersom EnsureLoaded är publik. Om _cts är null -> skapa en ny
        _cts ??= new CancellationTokenSource();  

        RepositoryResult<IEnumerable<Product>>? loadResult = await _productRepository.ReadAsync(_cts.Token);

        if (!loadResult.Succeeded)
        {
            return new ServiceResult
            {
                Succeeded = false,
                // INTERNALSERVER ERROR
                StatusCode = 500,
                ErrorMessage = loadResult.ErrorMessage ?? "Ett okänt fel inträffade vid filhämtning"
            };
            
        }
           
        _products = [.. (loadResult.Data ?? [])];
        _isLoaded = true;
        
        // Listan var inte laddad från början, ReadAsync kördes och det lyckades.
        return new ServiceResult { Succeeded = true };
    }



    // UI tar emot Button_click med delete UI. ProductService.DeleteProductAsync anropas med id som inparameter. 
    public async Task<ServiceResult> DeleteProductAsync(string id)
    {
        try
        {
            _cts = new CancellationTokenSource();

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage };

            Product? productToDelete = _products.FirstOrDefault(product => product.ProductId == id);

            if (productToDelete == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    // NOT FOUND
                    StatusCode = 404,
                    ErrorMessage = $"Produkten med Id {id} kunde inte hittas"
                };
            }

            _products.Remove(productToDelete);

            // Spara till fil, annars uppdateras inte listan och ändringen ligger bara i minnet och försvinner när programmet stängs.
            RepositoryResult repoSaveResult = await _productRepository.WriteAsync(_products, _cts.Token);

            if (!repoSaveResult.Succeeded)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    StatusCode = 500,
                    ErrorMessage = repoSaveResult.ErrorMessage ?? "Ett okänt fel inträffade vid filsparning"
                };
            }

            return new ServiceResult
            {
                Succeeded = true,
                // OK, NO CONTENT
                StatusCode = 204
            };
        }
        catch (Exception ex)
        {
            Cancel();
            return new ServiceResult
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Det gick inte att ta bort produkten: {ex.Message}"
            };
        }
        finally
        {
            _cts.Dispose();
        }
    }


    public async Task<ServiceResult<IEnumerable<Product>>> GetProductsAsync()
    {
        try
        {
            _cts = new CancellationTokenSource();

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return new ServiceResult<IEnumerable<Product>> { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage, Data = [] };

            return new ServiceResult<IEnumerable<Product>>
            {
                Succeeded = true,
                // spreadoperator, tar den nya listan och sprider det i den nya. Istället för att loopa igenom med foreach tar den hela listan på en gång
                StatusCode = 200,
                Data = [.. _products] 
            }; 
        }
        catch (Exception ex)
        {
            Cancel();
            _products = [];
            return new ServiceResult<IEnumerable<Product>>
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Fel vid filhämtning: {ex.Message}",
                Data = []
            };
        }
        finally
        {
            _cts.Dispose(); 
        }

    }

    public async Task<ServiceResult<Product>> SaveProductAsync(Product product) // MÅSTE FÅNGA UPP DATA = NULL I MAINWINDOW.XAML.CS
    {
        try
        {
            // Typen är redan specificerad i fältet. Här kommer en ny tilldelning bara. Deklarerar jag typen frånkopplar jag den från fältet och skapar en ny lokal variabel.
            // Skapar en ny instans för att säkerställa en ny nollställd _cts utan pågående cancellation. 
            _cts = new CancellationTokenSource();

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage, Data = null };







            product.ProductId = Guid.NewGuid().ToString();
            _products.Add(product);

            await _productRepository.WriteAsync(_products, _cts.Token);

            return new ServiceResult<Product>
            {
                Succeeded = true,
                StatusCode = 201,
                Data = product
            };
        }
        catch (Exception ex)
        {
            Cancel();

            return new ServiceResult<Product>
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Det gick inte att spara produkten: {ex.Message}",
                Data = null // MÅSTE FÅNGA UPP DATA = NULL I MAINWINDOW.XAML.CS
            };
            
        }
        finally
        {
            // Kommer ätas upp av Garbage collector
            _cts.Dispose(); 
        }
    }

    public async Task<ServiceResult> UpdateProductAsync(ProductUpdateRequest productUpdateRequest) // HUR MATA IN CATEGORY OCH MANUFACTURER!!!
    {
        try
        {
            _cts = new CancellationTokenSource();

            ServiceResult ensureResult = await EnsureLoadedAsync();
            if (!ensureResult.Succeeded)
                return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = ensureResult.ErrorMessage };

            Product? existingProduct = _products.FirstOrDefault(product => product.ProductId == productUpdateRequest.Id);
            if (existingProduct == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    StatusCode = 404,
                    ErrorMessage = $"Produkten med Id {productUpdateRequest.Id} kunde inte hittas"
                };
            }

            Product updatedProduct = new()
            {
                ProductName = productUpdateRequest.Name,
                Price = productUpdateRequest.Price,
                Category = new Category { CategoryName = productUpdateRequest.CategoryName }, // OBS
                Manufacturer = new Manufacturer { ManufacturerName = productUpdateRequest.ManufacturerName } // OBS
            };

            _products.Remove(existingProduct);
            _products.Add(updatedProduct);

            RepositoryResult repoSaveResult = await _productRepository.WriteAsync(_products, _cts.Token);
            if (!repoSaveResult.Succeeded)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    StatusCode = 500,
                    ErrorMessage = repoSaveResult.ErrorMessage ?? "Ett okänt fel inträffade vid filsparning"
                };
            }

            return new ServiceResult
            {
                Succeeded = true,
                StatusCode = 204,
            };
        }
        catch (Exception ex)
        {
            Cancel();
            return new ServiceResult
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Det gick inte att uppdatera produkten: {ex.Message}"
            };
        }
        finally
        {
            _cts.Dispose();
        }
    }
}


/* 
sätta id tillverkare + kategori: var newCategory = new Category:

{
    Id = categories.Count == 0 ? 1 : categories.Max(c => c.Id) + 1,
    Name = product.Category.Name
};
*/