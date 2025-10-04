using Domain.Interfaces;
using Domain.Results;

namespace Infrastructure.Helpers;

public static class RepositoryExtensions
{
    public static async Task<RepositoryResult<bool>> ExistsAsync<T>(
         this IRepository<T> repository, // gör metoden till en extension-metod för alla objekt som implementerar IRepository<T>. Det objekt som anropar, ex. _categoryRepository, blir parametern repository inne i metoden.
         Func<T, bool> isMatch, // Func<T, bool>, för att kunna skicka in ett villkor (lambda-uttryck) som varje objekt i samlingen ska kontrolleras mot. isMatch(objekt)
         CancellationToken cancellationToken)
         where T : class
    {
        RepositoryResult<IEnumerable<T>> readResult = await repository.ReadAsync(cancellationToken);
        if (!readResult.Succeeded) //lyckades inte göra kollen
        {
            return new RepositoryResult<bool>
            {
                Succeeded = false,
                StatusCode = readResult.StatusCode,
                ErrorMessage = readResult.ErrorMessage,
                Data = false // false = vi lyckades läsa, men inget matchade. null = vi misslyckades att ens kolla (Read gick fel).
            };
        }

        bool exists = readResult.Data!.Any(isMatch); 

        return new RepositoryResult<bool>
        {
            Succeeded = true,
            StatusCode = 200,
            Data = exists
        };
    }


    /*
    Extension = säger att den här klassen riktar in sig på och agerar hjälpare (ger extra funktionalitet) till en specifik typ av klass. 
    this = extension-klassen kan anropas direkt på alla objekt som implementerar IRepository<Category>, som om GetOrCreateAsync() var skapad inuti själva CategoryJsonRepository-klassen.
    vs
    Helper = anses kunna användas i projektet i stort.
    Vid anrop måste man skicka in objektet som inparameter - RepositoryHelpers.GetOrCreateAsync(_categoryRepository), till en fristående metod.

    I anropet (lambda): () => new Category { Name = name } 

    where T : class = typbegränsning, T måste vara en klass.
    */




}