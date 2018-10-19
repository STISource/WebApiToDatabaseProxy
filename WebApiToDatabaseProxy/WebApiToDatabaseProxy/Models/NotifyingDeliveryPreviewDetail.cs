using System.Runtime.Serialization;

namespace WebApiToDatabaseProxy.Models
{
    [DataContract]
    public class NotifyingDeliveryPreviewDetail : DeliveryPreviewDetail
    {
        [DataMember(Order = 14)]
        public NotificationStatus NotificationStatus { get; set; }

        [DataMember(Order = 15)]
        public int NotificationAge { get; set; }

        [DataMember(Order = 16)]
        public bool DeliveryDateChanged { get; set; }

        [DataMember(Order = 17)]
        public bool QuantityChanged { get; set; }
                
        public int Pk1 { get; set; }

        public string Pk2 { get; set; }

        public int Pk3 { get; set; }
    }

    public enum NotificationStatus
    {
        Unchanged = 0,        
        Created = 1,                
        Changed = 2        
    }
}