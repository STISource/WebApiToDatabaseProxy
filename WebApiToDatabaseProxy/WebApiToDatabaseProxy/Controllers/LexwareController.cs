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
    }
}
