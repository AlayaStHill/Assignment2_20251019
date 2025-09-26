using Infrastructure.Interfaces;
using Infrastructure.Models;

namespace Infrastructure.Services;
// Registrera i App.xaml
public class ProductService : IProductService
{
    private readonly IFileRepository _fileRepository;
    // initieras automatiskt till null (standardvärde för referenstyper)
    private List<Product> _products;
    // gäller för hela klassen - pekar alltid på den senaste instansen som newats i metoderna. När Cancel anropas stoppas just den instansen..Token tar emot stoppsignalen och vidarebefodrar den till koden som använden den. 
    private CancellationTokenSource _cts = null!; 

    public ProductService(IFileRepository fileRepository) 
    {
        _fileRepository = fileRepository;
        // instansierar en ny tom lista och tilldelar fältet sitt första användbara värde. Fördel: lätt att ändra logik senare, t.ex. populera listan med innehåll från fil direkt när ProductService skapas (det är bara i konstruktorn man har tillgång till inparametrar som FileRepository ex., direkt vid uppstart)
        _products = []; 
    }

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

    // UI tar emot Button_click med delete UI. ProductService.DeleteProductAsync anropas med id som inparameter. 
    public async Task<ProductServiceResult> DeleteProductAsync(string id)
    {
        try
        {
            _cts = new CancellationTokenSource();

            FileRepositoryResult<IEnumerable<Product>> repoReadResult = await _fileRepository.ReadAsync(_cts.Token);
            if (!repoReadResult.Succeeded)
            {
                return new ProductServiceResult
                {
                    Succeeded = false,
                    ErrorMessage = repoReadResult.ErrorMessage ?? "Ett okänt fel inträffade vid filhämtning"
                };
            }
            // FileRepository returnerar products eller [] om Succeeded = true och [] om false, så Data blir inte null här. ?? [] mest säker kod
            _products = repoReadResult.Data?.ToList() ?? [];

            Product? productToDelete = _products.FirstOrDefault(product => product.Id == id);

            if (productToDelete == null)
            {
                return new ProductServiceResult
                {
                    Succeeded = false,
                    ErrorMessage = $"Produkten med Id {id} kunde inte hittas"
                };
            }

            _products.Remove(productToDelete);

            // Spara till fil, annars uppdateras inte listan och ändringen ligger bara i minnet och försvinner när programmet stängs.
            FileRepositoryResult repoSaveResult = await _fileRepository.WriteAsync(_products, _cts.Token);

            if (!repoSaveResult.Succeeded)
            {
                return new ProductServiceResult
                {
                    Succeeded = false,
                    ErrorMessage = repoSaveResult.ErrorMessage ?? "Ett okänt fel inträffade vid filsparning"
                };
            }

            return new ProductServiceResult
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            Cancel();
            return new ProductServiceResult
            {
                Succeeded = false,
                ErrorMessage = $"Det gick inte att ta bort produkten: {ex.Message}"
            };
        }
        finally
        {
            _cts.Dispose();
        }
    }


    public async Task<ProductServiceResult<IEnumerable<Product>>> GetProductsAsync()
    {
        try
        {
            _cts = new CancellationTokenSource();

            FileRepositoryResult<IEnumerable<Product>> repoReadResult = await _fileRepository.ReadAsync(_cts.Token); 
            if (!repoReadResult.Succeeded)
            {
                return new ProductServiceResult<IEnumerable<Product>>
                {
                    Succeeded = false,
                    ErrorMessage = repoReadResult.ErrorMessage ?? "Okänt fel vid filhämtning",
                    Data = []
                };
            }

            return new ProductServiceResult<IEnumerable<Product>>
            {
                Succeeded = true,
                // spreadoperator, tar den nya listan och sprider det i den nya. Istället för att loopa igenom med foreach tar den hela listan på en gång
                Data = [.. repoReadResult.Data!] 
            }; 
        }
        catch (Exception ex)
        {
            Cancel();
            _products = [];
            return new ProductServiceResult<IEnumerable<Product>>
            {
                Succeeded = false,
                ErrorMessage = $"Fel vid filhämtning: {ex.Message}",
                Data = []
            };
        }
        finally
        {
            _cts.Dispose(); 
        }

    }

    public async Task<ProductServiceResult<Product>> SaveProductAsync(Product product) // MÅSTE FÅNGA UPP DATA = NULL I MAINWINDOW.XAML.CS
    {
        try
        {
            // Typen är redan specificerad i fältet. Här kommer en ny tilldelning bara. Deklarerar jag typen frånkopplar jag den från fältet och skapar en ny lokal variabel.
            // Skapar en ny instans för att säkerställa en ny nollställd _cts utan pågående cancellation. 
            _cts = new CancellationTokenSource(); 
            product.Id = Guid.NewGuid().ToString();
            _products.Add(product);

            await _fileRepository.WriteAsync(_products, _cts.Token);

            return new ProductServiceResult<Product>
            {
                Succeeded = true,
                Data = product
            };
        }
        catch (Exception ex)
        {
            Cancel();

            return new ProductServiceResult<Product>
            {
                Succeeded = false,
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

    public async Task<ProductServiceResult> UpdateProductAsync(Product updatedProduct)
    {
        try
        {
            _cts = new CancellationTokenSource();

            FileRepositoryResult<IEnumerable<Product>> repoReadResult = await _fileRepository.ReadAsync(_cts.Token);
            if (!repoReadResult.Succeeded)
            {
                return new ProductServiceResult
                {
                    Succeeded = false,
                    ErrorMessage = repoReadResult.ErrorMessage ?? "Ett okänt fel inträffade vid filhämtning"
                };

            }
            // FileRepository returnerar products eller [] om Succeeded = true och [] om false, så Data blir inte null här. ?? [] mest säker kod
            _products = repoReadResult.Data?.ToList() ?? [];

            Product? existingProduct = _products.FirstOrDefault(product => product.Id == updatedProduct.Id);
            if (existingProduct == null)
            {
                return new ProductServiceResult
                {
                    Succeeded = false,
                    ErrorMessage = $"Produkten med Id {updatedProduct.Id} kunde inte hittas"
                };
            }

            _products.Remove(existingProduct);
            _products.Add(updatedProduct);

            FileRepositoryResult repoSaveResult = await _fileRepository.WriteAsync(_products, _cts.Token);
            if (!repoSaveResult.Succeeded)
            {
                return new ProductServiceResult
                {
                    Succeeded = false,
                    ErrorMessage = repoSaveResult.ErrorMessage ?? "Ett okänt fel inträffade vid filsparning"
                };
            }

            return new ProductServiceResult
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            Cancel();
            return new ProductServiceResult
            {
                Succeeded = false,
                ErrorMessage = $"Det gick inte att uppdatera produkten: {ex.Message}"
            };
        }
        finally
        {
            _cts.Dispose();
        }
    }
}