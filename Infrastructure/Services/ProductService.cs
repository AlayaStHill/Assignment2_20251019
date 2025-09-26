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

    public void Cancel()
    {
        throw new NotImplementedException();
    }

    public async Task<ProductServiceResult> DeleteProductAsync(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<ProductServiceResult<IEnumerable<Product>>> GetProductsAsync()
    {
        throw new NotImplementedException();
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