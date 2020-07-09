using System.Runtime.Serialization;

namespace WebApiToDatabaseProxy.Models
{
    [DataContract]
    public class ProductInStoreDetail_v2
    {
        [DataMember(Order = 0)]
        public string ProductGroup { get; set; }

        [DataMember(Order = 1)]
        public string ProductNumber { get; set; }

        [DataMember(Order = 2)]
        public string ProductDescription { get; set; }

        [DataMember(Order = 3)]
        public double? ProductQuantityInStock { get; set; }

        [DataMember(Order = 4)]
        public double? PurchasePrice { get; set; }

        [DataMember(Order = 5)]
        public string Currency { get; set; }

        [DataMember(Order = 6)]
        public int PricePerQuantity { get; set; }

        [DataMember(Order = 7)]
        public string ProductLocked { get; set; }

        [DataMember(Order = 8)]
        public string NoStockMovementForMoreThanTwoYears { get; set; }

        [DataMember(Order = 9)]
        public string ManualMovementLast2Years { get; set; }
    }

}