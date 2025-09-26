using Infrastructure.Interfaces;
using Infrastructure.Models;
using Infrastructure.Repositories;

namespace Infrastructure.Services;
// Registrera i App.xaml
public class ProductService : IProductService
{
    private readonly IFileRepository _fileRepository;
    private List<Product> _products; // initieras automatiskt till null (standardvärde för referenstyper)
    private CancellationTokenSource _cts = null!; // gäller för hela klassen - pekar alltid på den senaste instansen som newats i metoderna. När Cancel anropas stoppas just den instansen..Token tar emot stoppsignalen och vidarebefodrar den till koden som använden den. 

    public ProductService(IFileRepository fileRepository) 
    {
        _fileRepository = fileRepository;
        _products = []; // instansierar en ny tom lista och tilldelar fältet sitt första användbara värde. Fördel: lätt att ändra logik senare, t.ex. populera listan med innehåll från fil direkt när ProductService skapas (det är bara i konstruktorn man har tillgång till inparametrar som FileRepository ex. direkt vid uppstart)
    }

    // Cancel-button aktiveras bara när "nedladdning" pågår, men ändå säkra upp för att skydda mot ett scenario där Cancel anropas då _cts är disposed eller fortfarande null innan någon metod körs
    public void Cancel()
    {
        if (_cts != null && !_cts.IsCancellationRequested) // efter Cancel() anropats är IsCancellationRequested == true
        {
            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException) // kastas om _cts är disposad då Cancel() anropas
            {
                // neutraliserat undantag, programmet kraschar ej och kan köra vidare om en ny metod anropas
            }
            
        }
        
    }

    public async Task<ProductServiceResult> DeleteProductAsync(string id)
    {
        throw new NotImplementedException();
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
                Data = [.. repoReadResult.Data!] // // spreadoperator, tar den nya listan och sprider det i den nya. Istället för att loopa igenom med foreach tar den hela listan på en gång
            }; 
        }
        catch (Exception ex)
        {
            _cts.Cancel();
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
            _cts = new CancellationTokenSource(); // Skapar en ny instans för att säkerställa en ny nollställd _cts utan pågående cancellation. 
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
            _cts.Dispose(); // Kommer ätas upp av Garbage collector
        }
    }

    public async Task<ProductServiceResult> UpdateProductAsync(Product product)
    {
        throw new NotImplementedException();
    }
}