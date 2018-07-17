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
        [DataMember(Order = 0)]
        public string Customer { get; set; }

        [DataMember(Order = 1)]
        public string CustomerOrderNumber { get; set; }

        [DataMember(Order = 2)]
        public string ProductNumber { get; set; }

        [DataMember(Order = 3)]
        public string ProductDescription { get; set; }

        [DataMember(Order = 4)]
        public string OrderConfirmationLineText { get; set; }

        [DataMember(Order = 5)]
        public double OrderConfirmationLineQuantity { get; set; }

        [DataMember(Order = 6)]
        public string OrderConfirmationLineUnit { get; set; }

        [DataMember(Order = 7)]
        public string OrderConfirmationLineDeliveryStatus { get; set; }

        [DataMember(Order = 8)]
        public DateTime? OrderConfirmationDeliveryDate { get; set; }

        [DataMember(Order = 9)]
        public string OrderConfirmationDeliveryWeek { get; set; }

        [DataMember(Order = 10)]
        public string Hint { get; set; }

        [DataMember(Order = 11)]
        public string OrderConfirmationNumber { get; set; }

        [DataMember(Order = 12)]
        public DateTime? OrderConfirmationOriginalDeliveryDate { get; set; }

        [DataMember(Order = 13)]
        public string OrderConfirmationOriginalDeliveryWeek { get; set; }

        [DataMember(Order = 14)]
        public DateTime? OrderConfirmationDesiredDeliveryDate { get; set; }

        [DataMember(Order = 15)]
        public string StiProjectNumber { get; set; }

        [DataMember(Order = 16)]
        public string GeneralAgreement { get; set; }

        [DataMember(Order = 17)]
        public string Comments { get; set; }

        [DataMember(Order = 18)]
        public string Note { get; set; }

        [DataMember(Order = 19)]
        public double OrderConfirmationLinePrice { get; set; }

        [DataMember(Order = 20)]
        public int OrderConfirmationLinePricePerQuantity { get; set; }

        [DataMember(Order = 21)]
        public double OrderConfirmationLinePriceSum { get; set; }

        [DataMember(Order = 22)]
        public double OrderConfirmationLineQuantityDelivered { get; set; }

        [DataMember(Order = 23)]
        public double OrderConfirmationLineQuantityOutstanding { get; set; }

        [DataMember(Order = 24)]
        public double ProductQuantityInStock { get; set; }

        [DataMember(Order = 25)]
        public double ProductQuantityOrderedByPurchasing { get; set; }

        [DataMember(Order = 26)]
        public double ProductQuantityReserved { get; set; }

        [DataMember(Order = 27)]
        public int ProductLocked { get; set; }
    }
}