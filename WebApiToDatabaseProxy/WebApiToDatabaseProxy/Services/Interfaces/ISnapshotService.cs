using System;
using System.Collections.Generic;
using WebApiToDatabaseProxy.Models;

namespace WebApiToDatabaseProxy.Services.Interfaces
{
    public interface ISnapshotService
    {
        IEnumerable<DeliveryPreviewSnapshot> ReadSnapshots(string userName);

        void InsertSnapshot(IEnumerable<NotifyingDeliveryPreviewDetail> deliveryPreviewDetails, string userName);
    }

    public class DeliveryPreviewSnapshot
    {
        public int Id { get; set; }

        public DateTime SnapshotDateTime { get; set; }

        public string UserName { get; set; }

        public IEnumerable<NotifyingDeliveryPreviewDetail> DeliveryPreviewDetails { get; set; }

    }
}
