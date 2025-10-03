using Domain.Entities;
using Domain.Interfaces;
using Domain.Results;
using System.Text.Json;

namespace Infrastructure.Repositories;

public class ManufacturerJsonRepository : IManufacturerRepository
{
    private readonly string _filePath;
    private static JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
   

    public ManufacturerJsonRepository()
    {
        string baseDirectory = AppContext.BaseDirectory; 
        string dataDirectory = Path.Combine(baseDirectory, "Data");
        _filePath = Path.Combine(dataDirectory, "manufacturers.json");

        EnsureInitialized(dataDirectory, _filePath);
    }


    public static void EnsureInitialized(string dataDirectory, string filePath)
    {
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "[]");
        }
    }



    public async Task<RepositoryResult<IEnumerable<Manufacturer>>> ReadAsync(CancellationToken cancellationToken)
    {
        string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new RepositoryResult<IEnumerable<Manufacturer>>
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
            List<Manufacturer>? manufacturers = JsonSerializer.Deserialize<List<Manufacturer>>(json, _jsonOptions);
            return new RepositoryResult<IEnumerable<Manufacturer>>
            {
                Succeeded = true,
                StatusCode = 200,
                Data = manufacturers ?? []
            };
        }
        catch (JsonException ex)
        {
            // Om JSON är ogiltig, återställ filen till en giltig tom lista
            await File.WriteAllTextAsync(_filePath, "[]", cancellationToken);
            return new RepositoryResult<IEnumerable<Manufacturer>>
            {
                Succeeded = false,
                StatusCode = 500,
                ErrorMessage = $"Ogiltig JSON: {ex.Message}",
                Data = []
            };
        }
    }

    public async Task<RepositoryResult> WriteAsync(IEnumerable<Manufacturer> manufacturers, CancellationToken cancellationToken)
    {
        try
        {
            string json = JsonSerializer.Serialize(manufacturers, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken); 

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