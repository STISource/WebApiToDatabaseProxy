using System.Collections.Generic;
using WebApiToDatabaseProxy.Models;

namespace WebApiToDatabaseProxy.Managers
{
    public interface ILexwareManager
    {
        IEnumerable<SalesOrderConfirmationDetail> GetSalesOrderConfirmationDetails();

        IEnumerable<ProductInStoreDetail> GetProductInStoreDetails();
    }
}
