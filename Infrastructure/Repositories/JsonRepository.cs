using Domain.Interfaces;
using Domain.Results;
using System.Text.Json;

namespace Infrastructure.Repositories; 

public class JsonRepository<T> : IRepository<T> where T : class
{
    private readonly string _filePath;
    private readonly string _dataDirectory;
    private static JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonRepository(string dataDirectory, string fileName)
    {
        // Sätts från DI
        _dataDirectory = dataDirectory;
        // Kombinerar katalog + filnamn
        _filePath = Path.Combine(dataDirectory, fileName);

        // Ser till att katalog och fil finns. Ska finnas innan applikationen laddas, annars kan den krascha. Om tex kör writeasync först. DRY - istället för att lägga in samma sak i flera metoder
        EnsureInitialized(_dataDirectory, _filePath);
    }

    public static void EnsureInitialized(string dataDirectory, string filePath)
    {
        if (!Directory.Exists(dataDirectory))
            Directory.CreateDirectory(dataDirectory);


        if (!File.Exists(filePath))
            // Om inte filen products.json finns på den angivna filsökvägen, skapa den och skriv in en tom lista i json-format
            File.WriteAllText(filePath, "[]");

        // Om filen finns men är tom/whitespace, initiera den till en tom lista
        string existing = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(existing))
            File.WriteAllText(filePath, "[]");


        // Nytt: validera att filen verkligen är en JSON-lista för T
        try
        {
            JsonSerializer.Deserialize<List<T>>(existing, _jsonOptions);
        }
        catch (JsonException)
        {
            // Om ogiltig -> skriv tom lista så kommande ReadAsync inte kastar
            File.WriteAllText(filePath, "[]");
        }
    }

    // CancellationToken cancellationToken här är kopplad till CancellationTokenSourse i ProductService, varifrån dessa metoder kan avbrytas
    public async Task<RepositoryResult<IEnumerable<T>>> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            EnsureInitialized(_dataDirectory, _filePath);  

            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);

            System.Diagnostics.Debug.WriteLine($"[{typeof(T).Name}] Läser fil: {_filePath}");
            System.Diagnostics.Debug.WriteLine($"Innehåll i filen: {json}");

            List<T>? entities = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            return RepositoryResult<IEnumerable<T>>.OK(entities ?? []);

        }
        catch (JsonException ex)
        {
            // Om JSON är ogiltig, återställ filen till en giltig tom lista
            await File.WriteAllTextAsync(_filePath, "[]", cancellationToken);

            return RepositoryResult<IEnumerable<T>>.InternalServerError($"Ogiltig JSON i {_filePath}: {ex.Message}"); 

        }
    }

    public async Task<RepositoryResult> WriteAsync(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        try
        {
            string json = JsonSerializer.Serialize(entities, _jsonOptions);
            // fungerar med små filer. Stream tar bara 100 första delar upp stora filen i olika portioner effektivare- för systemet lättare med flera småbitar, kan deka ut i processorn i flera olika trådar istället för en enda stor tråd.
            await File.WriteAllTextAsync(_filePath, json, cancellationToken); 

            return RepositoryResult.NoContent();

        }
        catch (Exception ex)
        {
            return RepositoryResult.InternalServerError($"Kunde inte spara till fil: {ex.Message}");
        }
    }
}