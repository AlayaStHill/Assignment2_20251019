using Domain.Entities;
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
        _dataDirectory = dataDirectory;
        _filePath = Path.Combine(dataDirectory, fileName);

        // Ska finnas innan applikationen laddas, annars kan den krascha. Om tex kör writeasync först. DRY - istället för att lägga in samma sak i flera metoder
        EnsureInitialized(_dataDirectory, _filePath);
    }

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
    

    public async Task<RepositoryResult<bool>> ExistsAsync(Func<T, bool> predicate, CancellationToken cancellationToken)
    {
        try
        {

            var readResult = await ReadAsync(cancellationToken);
            if (!readResult.Succeeded) //lyckades inte göra kollen
            {
                return new RepositoryResult<bool>
                {
                    Succeeded = false,
                    StatusCode = readResult.StatusCode,
                    ErrorMessage = readResult.ErrorMessage,
                    Data = false
                };
            }

            bool exists = readResult.Data!.Any(predicate);

            return new RepositoryResult<bool>
            {
                Succeeded = true,
                StatusCode = 200,
                Data = exists
            };
        }
        catch (Exception ex)
        {
            return new RepositoryResult<bool>
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Ogiltig JSON: {ex.Message}",
                Data = false
            };
        }

    }


    // CancellationToken cancellationToken här är kopplad till CancellationTokenSourse i ProductService, varifrån dessa metoder kan avbrytas
    public async Task<RepositoryResult<IEnumerable<T>>> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {

            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            // Om det är giltig text klarar denna koll, men inte giltig json att deserialisera fångas upp i catch
            if (string.IsNullOrWhiteSpace(json))
            {
                return new RepositoryResult<IEnumerable<T>>
                {
                    Succeeded = false,
                    StatusCode = 500,
                    ErrorMessage = "Filen innehöll inget giltigt JSON-format.",
                    // undviker NullReferenceException
                    Data = []
                };
            }


            List<T>? entities = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            return new RepositoryResult<IEnumerable<T>>
            {
                Succeeded = true,
                StatusCode = 200,
                Data = entities ?? []
            };
        }
        catch (JsonException ex)
        {
            // Om JSON är ogiltig, återställ filen till en giltig tom lista
            await File.WriteAllTextAsync(_filePath, "[]", cancellationToken);
            return new RepositoryResult<IEnumerable<T>>
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Ogiltig JSON: {ex.Message}",
                Data = []
            };
        }
    }

    public async Task<RepositoryResult> WriteAsync(IEnumerable<T> entities, CancellationToken cancellationToken)
    {
        try
        {
            string json = JsonSerializer.Serialize(entities, _jsonOptions);
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


    public async Task<RepositoryResult<T>> GetEntityAsync(Func<T, bool> predicate, CancellationToken cancellationToken)
    {
        try
        {
            RepositoryResult<IEnumerable<T>> entitiesResult = await ReadAsync(cancellationToken);
            if (entitiesResult.Succeeded)
            {
                var entity = entitiesResult.Data!.FirstOrDefault(predicate);

                return new RepositoryResult<T>
                {
                    Succeeded = true,
                    StatusCode = 200,
                    Data = entity
                };

              
            }
       
        }
        catch (Exception ex)
        {
            return new RepositoryResult<T>
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Kunde inte hämta från fil: {ex.Message}"
            };
        }

        return new RepositoryResult<T>
        {
            Succeeded = false,
            StatusCode = 500,
            ErrorMessage = "Kunde inte hämta från fil"
        };
    }
}