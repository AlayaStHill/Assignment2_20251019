using Infrastructure.Models;

namespace Infrastructure.Interfaces;
// Istället för att duplicera ReadAsync, WriteAsync i tre olika interfaces (DRY).
public interface IRepository<T>
{
    Task<RepositoryResult<IEnumerable<T>>> ReadAsync(CancellationToken cancellationToken);
    Task<RepositoryResult> WriteAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
}

/*
DDD (Domain-Driven Design)
--------------------------
Domänen:    Produkthantering (området programmet handlar om)
Entities:   Product, Category, Manufacturer. Även kallat domänens kärnmodeller. Har alltid en unik Id-property, vilket är det som gör dem till entities.
DTOs:       ProductUpdateRequest, ProductRequest (Data Transfer Object - används bara för att flytta data mellan lager, ex. UI -> ProductService)
Repositories: ProductRepository, CategoryRepository, ManufacturerRepository
Service:    ProductService (samordnar logiken)
*/