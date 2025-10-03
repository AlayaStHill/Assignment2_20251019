using Infrastructure.Models;

namespace Infrastructure.Interfaces;
public interface IProductRepository // säger att den hanterar produkter, oavsett lagring
{
    Task<RepositoryResult<IEnumerable<Product>>> ReadAsync(CancellationToken cancellationToken);
    Task<RepositoryResult> WriteAsync(IEnumerable<Product> products, CancellationToken cancellationToken); 

}
