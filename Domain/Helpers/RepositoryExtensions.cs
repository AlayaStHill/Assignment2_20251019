using Domain.Interfaces;
using Domain.Results;

namespace Domain.Helpers;

public static class RepositoryExtensions
{
    // Försök hämta annars skapa en ny
    public static async Task<RepositoryResult<T>> GetOrCreateAsync<T>(
        // gör metoden till en extension-metod för alla objekt som implementerar IRepository<T>. Det objekt som anropar, ex. _categoryRepository, blir parametern repository inne i metoden.
        this IRepository<T> repository,
        // Func<T, bool>, för att kunna skicka in ett villkor (lambda-uttryck) som varje objekt i samlingen ska kontrolleras mot. isMatch(objekt) för att hitta en befintlig
        Func<T, bool> isMatch,
        // skapar en ny om ingen hittas
        Func<T> createEntity, 
        CancellationToken cancellationToken)
        where T : class
    {
        RepositoryResult<IEnumerable<T>> readResult = await repository.ReadAsync(cancellationToken);
        if (!readResult.Succeeded)
            return new RepositoryResult<T> { Succeeded = false, StatusCode = readResult.StatusCode, ErrorMessage = readResult.ErrorMessage, Data = null };


        //Försök hitta en match
        T? entity = readResult.Data!.FirstOrDefault(isMatch);
        // Om match hittas
        if (entity != null)
            return RepositoryResult<T>.OK(entity);

        // Om ingen match, skapa en ny
        entity = createEntity(); // lambda skickas in i ProductService: () => new Category { Id = ..., Name = ... } tar ingen inparameter = ()

        // Skapa en List för att kunna lägga till 
        List<T> list = readResult.Data!.ToList();
        list.Add(entity);

        // Skriv till fil så det blir beständigt. 
        RepositoryResult writeResult = await repository.WriteAsync(list, cancellationToken);
        if (!writeResult.Succeeded)
        {
            return new RepositoryResult<T>
            {
                Succeeded = false,
                StatusCode = writeResult.StatusCode,
                ErrorMessage = writeResult.ErrorMessage,
                Data = null
            };
        }
        
        // Returnera nyskapat objekt
        return RepositoryResult<T>.Created(entity);
    }
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