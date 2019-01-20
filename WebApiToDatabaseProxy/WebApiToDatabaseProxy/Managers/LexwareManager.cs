using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using WebApiToDatabaseProxy.Database;
using WebApiToDatabaseProxy.Models;
using WebApiToDatabaseProxy.Services.Interfaces;

namespace WebApiToDatabaseProxy.Managers
{
    public partial class LexwareManager : ILexwareManager
    {
        private readonly IDatabaseSession dbSession;

        private readonly ISnapshotService snapshotService;

        public LexwareManager(IDatabaseSession session, ISnapshotService snapshotService)
        {
            this.dbSession = session;
            this.snapshotService = snapshotService;
        }

        public IEnumerable<SalesOrderConfirmationDetail> GetSalesOrderConfirmationDetails()
        {
            IEnumerable<SalesOrderConfirmationDetail> results = null;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<SalesOrderConfirmationDetail>(ConfirmationDetailsSql);
            }

            return results;
        }

        public IEnumerable<ProductInStoreDetail> GetProductInStoreDetails()
        {
            IEnumerable<ProductInStoreDetail> results = null;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<ProductInStoreDetail>(InventoryValuationSql);
            }

            return results;
        }

        public IEnumerable<DeliveryPreviewDetail> GetDeliveryPreviewDetails()
        {
            IEnumerable<DeliveryPreviewDetail> results = null;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<DeliveryPreviewDetail>(DeliveryPreviewSql);
            }

            return results;
        }

        public IEnumerable<NotifyingDeliveryPreviewDetail> GetDeliveryPreviewDetailsWithNotificationStatus()
        {
            IEnumerable<NotifyingDeliveryPreviewDetail> results = null;
            var currentUser = System.Web.HttpContext.Current.User.Identity.Name;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<NotifyingDeliveryPreviewDetail>(DeliveryPreviewSql);                            
            }

            var snapshots = this.snapshotService.ReadSnapshots(currentUser);
            this.snapshotService.InsertSnapshot(results, currentUser);

            // enrich each object with notification status information
            // telling the user whether the information new, recently changed or already known
            this.SetDeliveryPreviewNotificationStatuses(results, snapshots);

            return results;
        }

        private void SetDeliveryPreviewNotificationStatuses(IEnumerable<NotifyingDeliveryPreviewDetail> previewDetials, IEnumerable<DeliveryPreviewSnapshot> snapshots)
        {
            foreach(var detail in previewDetials)
            {                
                detail.NotificationStatus = NotificationStatus.Unchanged;
                detail.NotificationAge = 0;

                var currentComparisionBase = detail;
                  
                // iterate through history to detect different change events
                foreach(var snapshot in snapshots)
                {
                    var snapshotedDetail = snapshot.DeliveryPreviewDetails.SingleOrDefault(
                                                                            x => x.Pk1 == currentComparisionBase.Pk1 
                                                                                && x.Pk2 == currentComparisionBase.Pk2 
                                                                                && x.Pk3 == currentComparisionBase.Pk3);

                    // record has been created
                    if(snapshotedDetail == null)
                    {                        
                        if (detail.NotificationStatus == NotificationStatus.Unchanged)
                        {
                            detail.NotificationStatus = NotificationStatus.Created;
                        }

                        // not further seach in the past needed
                        break;
                    }

                    var changeInfo = this.CompareDeliveryPreviewDetails(currentComparisionBase, snapshotedDetail);

                    if(changeInfo != DeliveryPreviewChangeInfo.None)
                    {
                        detail.NotificationStatus = NotificationStatus.Changed;
                        if(changeInfo.HasFlag(DeliveryPreviewChangeInfo.DeliveryDateChange))
                        {
                            detail.DeliveryDateChanged = true;
                        }
                        if(changeInfo.HasFlag(DeliveryPreviewChangeInfo.QuanityChange))
                        {
                            detail.QuantityChanged = true;
                        }
                    }

                    // only record the age of the latest change
                    if (detail.NotificationStatus == NotificationStatus.Unchanged)
                    {
                        detail.NotificationAge = (DateTime.Now - snapshot.SnapshotDateTime).Days;
                    }
                    currentComparisionBase = snapshotedDetail;
                }
            }
        }

        private DeliveryPreviewChangeInfo CompareDeliveryPreviewDetails(DeliveryPreviewDetail detail1, DeliveryPreviewDetail detail2)
        {
            var result = DeliveryPreviewChangeInfo.None;

            if(detail1.DeliveryDate != detail2.DeliveryDate)
            {
                result = result | DeliveryPreviewChangeInfo.DeliveryDateChange;
            }

            if(detail1.QuantityOutstanding != detail2.QuantityOutstanding)
            {
                result = result | DeliveryPreviewChangeInfo.QuanityChange;
            }

            if(detail1.Customer?.Trim() != detail2.Customer?.Trim()
                || detail1.ProductNumber?.Trim() != detail2.ProductNumber?.Trim()
                || detail1.ProductDescription?.Trim() != detail2.ProductDescription?.Trim()
                || detail1.Unit?.Trim() != detail2.Unit?.Trim()
                || detail1.DeliveryStatus != detail2.DeliveryStatus            
                || detail1.DeliveryWeek != detail2.DeliveryWeek
                || detail1.OrderConfirmationNumber != detail2.OrderConfirmationNumber
                || detail1.DesiredDeliveryDateCustomer != detail2.DesiredDeliveryDateCustomer
                || detail1.Note?.Trim() != detail2.Note?.Trim()
                || detail1.QuantityInStock != detail2.QuantityInStock
                || detail1.QuantityOrderedByPurchasing != detail2.QuantityOrderedByPurchasing
                || detail1.ProductLockedStatus != detail2.ProductLockedStatus)
            {
                result = result | DeliveryPreviewChangeInfo.OtherChange;
            }

            return result;
        }



        public IEnumerable<TestDetail> GetTestDetails()
        {
            IEnumerable<TestDetail> results = null;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<TestDetail>(TestDetailSql);
            }

            return results;
        }



        public IEnumerable<Warenausgang> GetWarenausgangsListe()
        {
            IEnumerable<Warenausgang> results = null;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<Warenausgang>(WarenausgangSql);
            }

            return results;
        }






        public IEnumerable<Stueckliste> GetStuecklisteBestand()
        {
            IEnumerable<Stueckliste> results = null;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<Stueckliste>(StuecklisteBestandSql);
            }

            return results;
        }



        [Flags]
        private enum DeliveryPreviewChangeInfo
        {
            None = 0,            
            QuanityChange = 1,
            DeliveryDateChange = 2,
            OtherChange = 4
        }
    }
}