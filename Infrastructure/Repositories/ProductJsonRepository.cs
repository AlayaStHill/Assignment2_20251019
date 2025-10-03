using Infrastructure.Interfaces;
using Infrastructure.Models;
using Infrastructure.Results;
using System.Text.Json;

namespace Infrastructure.Repositories;
 
public class ProductJsonRepository : IProductRepository
{
    private readonly string _filePath;
    private static JsonSerializerOptions _jsonOptions = new() 
    {
        WriteIndented = true
    };

    public ProductJsonRepository()
    {
        // baseDirectory-användning: ändras automatiskt beroende på var programmet körs ifrån (annan dator ex.), eftersom den bygger vägen dynamiskt utifrån där .exe-filen befinner sig just då.. 
        // Hämtar sökvägen till mappen där .exe-filen ligger. Sökvägen lagras i baseDirectory 
        string baseDirectory = AppContext.BaseDirectory; // Talar om var applikationen körs ifrån: bin\Debug\net9.0-windows\ - mappen där .exe-filen ligger
        // Bygger på segmentet Data sist i sökvägen. Blir sökvägen till mappen Data som ska ligga i samma katalog som .exe
        string dataDirectory = Path.Combine(baseDirectory, "Data");
        // Bygger på filnamnet products.json i slutet av sökvägen. Den fullständiga sökvägen till json-filen lagras i _filePath.
        _filePath = Path.Combine(dataDirectory, "products.json");

        // Ska finnas innan applikationen laddas, annars kan den krascha. Om tex kör writeasync först. DRY - istället för att lägga in samma sak i flera metoder
        EnsureInitialized(dataDirectory, _filePath);
    }

    //Kan ej ha async i konstruktorn
    public static void EnsureInitialized(string dataDirectory, string filePath) 
    {
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }
        if (!File.Exists(filePath))
        {
            // Om inte filen products.json finns på den angivna filsökvägen, skapa den och skriv in en tom lista i json-format
            File.WriteAllText(filePath, "[]");
        }
    }


    // CancellationToken cancellationToken här är kopplad till CancellationTokenSourse i ProductService, varifrån dessa metoder kan avbrytas
    public async Task<RepositoryResult<IEnumerable<Product>>> ReadAsync(CancellationToken cancellationToken)
    {
        string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        // Om det är giltig text klarar denna koll, men inte giltig json att deserialisera fångas upp i catch
        if (string.IsNullOrWhiteSpace(json)) 
        {
            return new RepositoryResult<IEnumerable<Product>>
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = "Filen innehöll inget giltigt JSON-format.",
                // undviker NullReferenceException
                Data = [] 
            };
        }

        try
        {
            List<Product>? products = JsonSerializer.Deserialize<List<Product>>(json, _jsonOptions);
            return new RepositoryResult<IEnumerable<Product>>
            {
                Succeeded = true,
                StatusCode = 200,
                Data = products ?? []
            };
        }
        catch (JsonException ex)
        {
            // Om JSON är ogiltig, återställ filen till en giltig tom lista
            await File.WriteAllTextAsync(_filePath, "[]", cancellationToken);
            return new RepositoryResult<IEnumerable<Product>>
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Ogiltig JSON: {ex.Message}",
                Data = []
            };
        }
    }

   
    public async Task<RepositoryResult> WriteAsync(IEnumerable<Product> products, CancellationToken cancellationToken)
    {
        try
        {
            string json = JsonSerializer.Serialize(products, _jsonOptions); 
            await File.WriteAllTextAsync(_filePath, json, cancellationToken); // fungerar med små filer. Stream tar bara 100 första delar upp stora filen i olika portioner effektivare- för systemet lättare med flera småbitar, kan deka ut i processorn i flera olika trådar istället för en enda stor tråd.

            return new RepositoryResult
            {
                Succeeded = true,
                StatusCode = 204
            };
        }
        catch (Exception ex)
        {
            return new RepositoryResult
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Kunde inte spara till fil: {ex.Message}"
            };
        }
    }
}

