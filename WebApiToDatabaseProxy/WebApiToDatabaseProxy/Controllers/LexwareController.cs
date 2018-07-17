using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    }
}
