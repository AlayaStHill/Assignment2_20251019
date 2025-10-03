using Domain.Entities;

namespace Domain.Interfaces;

public interface IProductRepository : IRepository<Product> // säger att den hanterar produkter, oavsett lagring
{
    // Metoder som är specifika för Product

}
