using Infrastructure.Interfaces;
using Infrastructure.Models;
using System.Text.Json;

namespace Infrastructure.Repositories;
// baseDirectory-användning: ändras automatiskt beroende på var programmet körs ifrån (annan dator ex.), eftersom den bygger vägen dynamiskt utifrån där .exe-filen befinner sig just då..  
public class FileRepository : IFileRepository
{
    private readonly string _filePath;
    private static JsonSerializerOptions _jsonOptions = new() // varför static, varför la man den här i konstruktorn i andra projektet, men inte här 
    {
        WriteIndented = true
    };

    public FileRepository()
    {
        // Hämtar sökvägen till mappen där .exe-filen ligger. Sökvägen lagras i baseDirectory 
        string? baseDirectory = AppContext.BaseDirectory; // Talar om var applikationen körs ifrån: bin\Debug\net9.0-windows\ - mappen där .exe-filen ligger
        // Bygger på segmentet Data sist i sökvägen. Blir sökvägen till mappen Data som ska ligga i samma katalog som .exe
        string? dataDirectory = Path.Combine(baseDirectory, "Data");
        // Bygger på filnamnet products.json i slutet av sökvägen. Den fullständiga sökvägen till json-filen lagras i _filePath.
        _filePath = Path.Combine(dataDirectory, "products.json");

        // Om inte mappen Data finns på den angivna mapp-sökvägen, skapa en.
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);

        }  
    }
    // CancellationToken cancellationToken här är kopplad till CancellationTokenSourse i ProductService, varifrån dessa metoder kan avbrytas
    public async Task<FileRepositoryResult<IEnumerable<Product>>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            // Om inte filen products.json finns på den angivna filsökvägen, skapa den och skriv in en tom lista i json-format
            await File.WriteAllTextAsync(_filePath, "[]", cancellationToken);

            return new FileRepositoryResult<IEnumerable<Product>>
            {
                Succeeded = true,
                Data = new List<Product>()
            }; 
        }

        string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        // Kan innehålla text som inte kan omformatteras till json, då blir det false. Fångas upp i try eftersom det inte går att serializera.
        if (string.IsNullOrWhiteSpace(json)) 
        {
            return new FileRepositoryResult<IEnumerable<Product>>
            {
                Succeeded = false,
                ErrorMessage = "Filen innehöll inget giltigt JSON-format.",
                Data = new List<Product>() // undviker NullReferenceException
            };
        }

        try
        {
            List<Product>? products = JsonSerializer.Deserialize<List<Product>>(json, _jsonOptions);
            return new FileRepositoryResult<IEnumerable<Product>>
            {
                Succeeded = true,
                Data = products ?? []
            };
        }
        catch (JsonException ex)
        {
            // Om JSON var ogiltig, återställ filen till en giltig tom lista
            await File.WriteAllTextAsync(_filePath, "[]", cancellationToken);
            return new FileRepositoryResult<IEnumerable<Product>>
            {
                Succeeded = false,
                ErrorMessage = $"Ogiltig JSON: {ex.Message}",
                Data = []
            };
        }
    }

    public async Task<FileRepositoryResult> WriteAsync(IEnumerable<Product> products, CancellationToken cancellationToken)
    {
        try
        {
            string json = JsonSerializer.Serialize(products, _jsonOptions); 
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);

            return new FileRepositoryResult
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            return new FileRepositoryResult
            {
                Succeeded = false,
                ErrorMessage = $"Kunde inte spara till fil: {ex.Message}"
            };
        }
    }
}

