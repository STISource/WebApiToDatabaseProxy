using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WebApiToDatabaseProxy.Models
{
    [DataContract]
    public class SalesOrderConfirmationDetail
    {
        [DataMember]
        public int OrderConfirmationId { get; set; }

        [DataMember]
        public string Customer { get; set; }

        [DataMember]
        public DateTime DateOfDelivery { get; set; }
    }
}