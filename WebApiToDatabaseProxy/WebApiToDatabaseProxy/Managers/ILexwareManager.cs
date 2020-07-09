using System.Collections.Generic;
using WebApiToDatabaseProxy.Models;

namespace WebApiToDatabaseProxy.Managers
{
    public interface ILexwareManager
    {
        IEnumerable<SalesOrderConfirmationDetail> GetSalesOrderConfirmationDetails();

        IEnumerable<ProductInStoreDetail> GetProductInStoreDetails(); 
        IEnumerable<ProductInStoreDetail_v2> GetProductInStoreDetails_v2();

        IEnumerable<DeliveryPreviewDetail> GetDeliveryPreviewDetails();

        IEnumerable<NotifyingDeliveryPreviewDetail> GetDeliveryPreviewDetailsWithNotificationStatus();

        IEnumerable<Warenausgang> GetWarenausgangsListe();

        IEnumerable<Stueckliste> GetStuecklisteBestand();

        IEnumerable<TestDetail> GetTestDetails();

        IEnumerable<ArtikelUmsatz> GetArtikelUmsatz(string von, string bis, double USD, double RMB);

        IEnumerable<KundenUmsatz> GetKundenUmsatz(string von, string bis, double USD, double RMB);
    }
}
