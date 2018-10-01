using System;
using System.Runtime.Serialization;

namespace WebApiToDatabaseProxy.Models
{
    [DataContract]
    public class DeliveryPreviewDetail
    {
        [DataMember(Order = 0)]
        public string Customer { get; set; }
                
        [DataMember(Order = 1)]
        public string ProductNumber { get; set; }

        [DataMember(Order = 2)]
        public string ProductDescription { get; set; }

        [DataMember(Order = 3)]
        public double QuantityOutstanding { get; set; }

        [DataMember(Order = 4)]
        public string Unit { get; set; }

        [DataMember(Order = 5)]
        public string DeliveryStatus { get; set; }

        [DataMember(Order = 6)]
        public DateTime? DeliveryDate { get; set; }

        [DataMember(Order = 7)]
        public string DeliveryWeek { get; set; }        

        [DataMember(Order = 8)]
        public string OrderConfirmationNumber { get; set; }
          
        [DataMember(Order = 9)]
        public DateTime? DesiredDeliveryDateCustomer { get; set; }

        [DataMember(Order = 10)]
        public double? QuantityInStock { get; set; }

        [DataMember(Order = 11)]
        public double? QuantityOrderedByPurchasing { get; set; }
        
        [DataMember(Order = 12)]
        public string ProductLockedStatus { get; set; }
    }
}