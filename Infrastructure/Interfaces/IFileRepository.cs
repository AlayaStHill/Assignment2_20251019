using Infrastructure.Models;

namespace Infrastructure.Interfaces;
public interface IFileRepository
{
    Task<FileRepositoryResult<IEnumerable<Product>>> ReadAsync(CancellationToken cancellationToken);
    Task<FileRepositoryResult> WriteAsync(IEnumerable<Product> products, CancellationToken cancellationToken); // varför bool

}
