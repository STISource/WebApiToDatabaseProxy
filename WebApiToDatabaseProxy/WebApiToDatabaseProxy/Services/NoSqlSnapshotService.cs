using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LiteDB;
using WebApiToDatabaseProxy.Models;
using WebApiToDatabaseProxy.Services.Interfaces;

namespace WebApiToDatabaseProxy.Services
{
    public class NoSqlSnapshotService : ISnapshotService
    {
        private const string DatabaseName = @"DeliveryPreview.db";
        private const string CollectionName = @"DeliveryPreviewSnapshots";
        private const int MaxSnapshotAge = 10; // days
        private const int MaxSnapshotsPerUser = 30;

        public void InsertSnapshot(IEnumerable<NotifyingDeliveryPreviewDetail> deliveryPreviewDetails, string userName)
        {
            var databasePath = System.IO.Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, DatabaseName);

            using (var database = new LiteDatabase(databasePath))
            {
                var snapshots = database.GetCollection<DeliveryPreviewSnapshot>(CollectionName);

                var snapshot = new DeliveryPreviewSnapshot
                {
                    SnapshotDateTime = DateTime.Now,
                    UserName = userName.ToLower(),
                    DeliveryPreviewDetails = deliveryPreviewDetails
                };

                snapshots.Insert(snapshot);
            }
        }

        public IEnumerable<DeliveryPreviewSnapshot> ReadSnapshots(string userName)
        {
            List<DeliveryPreviewSnapshot> results = null;

            var databasePath = System.IO.Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, DatabaseName);

            // Open database (or create if not exits)
            using (var database = new LiteDatabase(databasePath))
            {                
                var snapshots = database.GetCollection<DeliveryPreviewSnapshot>(CollectionName);
                                
                results = snapshots.Find(x => x.UserName == userName.ToLower()).OrderByDescending(x => x.SnapshotDateTime).ToList();
                var outDatedSnapshots = results.Where(x => x.SnapshotDateTime < DateTime.Now.AddDays(MaxSnapshotAge * -1)).ToList();

                // remove outdated snapshots from results and database
                if (outDatedSnapshots.Any())
                {
                    foreach(var outdatedSnapshot in outDatedSnapshots)
                    {
                        snapshots.Delete(outdatedSnapshot.Id);
                        results.Remove(outdatedSnapshot);
                    }
                }

                if(results.Count() > MaxSnapshotsPerUser)
                {
                    var tooExtensiveHistoryRecords = results.Reverse<DeliveryPreviewSnapshot>().Take(results.Count() - MaxSnapshotsPerUser).ToList();
                    foreach (var deleteSnapshot in tooExtensiveHistoryRecords)
                    {
                        snapshots.Delete(deleteSnapshot.Id);
                        results.Remove(deleteSnapshot);
                    }
                }
            }

            return results;
        }
    }
}