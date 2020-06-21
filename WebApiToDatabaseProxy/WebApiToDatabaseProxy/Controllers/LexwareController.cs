using System.Collections.Generic;
using System.Web.Http;
using WebApiToDatabaseProxy.Managers;
using WebApiToDatabaseProxy.Models;

namespace WebApiToDatabaseProxy.Controllers
{    
    public class LexwareController : ApiController
    {
        private readonly ILexwareManager lexwareManager;        

        public LexwareController(ILexwareManager lexwareManager)
        {
            this.lexwareManager = lexwareManager;            
        }

        [Route("api/lexware/SalesOrderConfirmationDetails")]
        public IEnumerable<SalesOrderConfirmationDetail> GetSalesOrderConfirmationDetails()
        {
            return this.lexwareManager.GetSalesOrderConfirmationDetails();
        }

        [Route("api/lexware/InventoryValuation")]
        public IEnumerable<ProductInStoreDetail> GetProductInStoreDetails()
        {
            return this.lexwareManager.GetProductInStoreDetails();
        }

        [Route("api/lexware/DeliveryPreview")]
        public IEnumerable<DeliveryPreviewDetail> GetDeliveryPreviewDetails()
        {
            return this.lexwareManager.GetDeliveryPreviewDetails();
        }

        [Route("api/lexware/DeliveryPreviewWithNotifyStatus")]
        public IEnumerable<NotifyingDeliveryPreviewDetail> GetDeliveryPreviewDetailsWithNotificationStatus()
        {
            return this.lexwareManager.GetDeliveryPreviewDetailsWithNotificationStatus();
        }

        [Route("api/lexware/WarenausgangsListe")]
        public IEnumerable<Warenausgang> GetWarenausgangsListe()
        {
            return this.lexwareManager.GetWarenausgangsListe();
        }


        [Route("api/lexware/StuecklistenMitBestand")]
        public IEnumerable<Stueckliste> GetStuecklisteBestand()
        {
            return this.lexwareManager.GetStuecklisteBestand();
        }

        [Route("api/lexware/Test")]
        public IEnumerable<TestDetail> GetTestDetail()
        {
            return this.lexwareManager.GetTestDetails();
        }

        [Route("api/lexware/ArtikelUmsatz/{von}/{bis}/{USD}/{RMB}")]
        public IEnumerable<ArtikelUmsatz> GetArtikelUmsatz(string von, string bis, double USD, double RMB)
        {
            return this.lexwareManager.GetArtikelUmsatz(von, bis, USD, RMB);
        }

        [Route("api/lexware/KundenUmsatz/{von}/{bis}/{USD}/{RMB}")]
        public IEnumerable<KundenUmsatz> GetKundenUmsatz(string von, string bis, double USD, double RMB)
        {
            return this.lexwareManager.GetKundenUmsatz(von, bis, USD, RMB);
        }
    }
}
