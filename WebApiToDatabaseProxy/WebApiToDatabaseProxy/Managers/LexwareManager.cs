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

            // Hinweistexte einfügen
            this.SalesOrderConfirmationDetails_CalculateHints(results);

            return results;
        }




        // Neuer Prozedur JA 20190802
        // Auswertungen zum Ermitteln des Hinweis-Textes werden nicht mehr im SQL Skript, sondern hier gemacht (wegen Wartbarkeit des Programms!)
        private void SalesOrderConfirmationDetails_CalculateHints(IEnumerable<SalesOrderConfirmationDetail> confirmationDetails)
        {
            foreach (var rec in confirmationDetails)
            {
                try
                {
                    // Zeilenumbrüche aus der Spalte Positionstext entfernen
                    if (rec.OrderConfirmationLineText != null)
                    {
                        rec.OrderConfirmationLineText = rec.OrderConfirmationLineText.Replace(System.Environment.NewLine, " ");
                    }



                    // Weitere Auswertungen nur machen, wenn nicht schon geliefert wurde und eine Artikelnummer vorhanden ist
                    // und wenn diese AB in der Liste der nicht abgeschlossenen ABs für diesen Artikel als nächstes ansteht (sortiert nach Liefertermin, Wunschliefertermin)
                    // und wenn der Wunsch-LT nicht ungültig ist

                    if ((rec.OrderConfirmationLineDeliveryStatus != "Geliefert") & (rec.ProductNumber != null) & (rec.IstErsteABFuerDiesenArtikel == 1) & (rec.Hint != "Wunsch-LT ungültig"))
                    {


                        // *** Früher liefern?! ***
                        // wenn Kundenwunsch LT vorhanden 
                        // und zugesagter LT liegt noch in der Zukunft 
                        // und zugesagter LT und Wunsch LT liegen mehr als eine Woche auseinander
                        // und es sind mindestens 90 % der bestellten Menge auf Lager
                        // und wenn diese AB in der Liste der nicht abgeschlossenen ABs für diesen Artikel als nächstes ansteht(sortiert nach Liefertermin, Wunschliefertermin)
                        // und Artikel nicht gesperrt
                        // und wenn der Wunsch LT innerhalb des nächsten halben Jahres liegt

                        if ((rec.OrderConfirmationDesiredDeliveryDate != null)
                            & (rec.OrderConfirmationDeliveryDate >= DateTime.Today)
                            & ((rec.OrderConfirmationDesiredDeliveryDate - rec.OrderConfirmationDeliveryDate) < TimeSpan.FromDays(-7))
                            & (rec.ProductQuantityInStock >= (rec.OrderConfirmationLineQuantityOutstanding * 0.9))
                            & (rec.IstErsteABFuerDiesenArtikel == 1)
                            & (rec.ProductLocked == 0)
                            & ((rec.OrderConfirmationDesiredDeliveryDate - DateTime.Today) < TimeSpan.FromDays(183)))
                        {
                            if (rec.Hint != "") { rec.Hint += "\n"; }
                            rec.Hint += "Früher liefern?!";
                        }


                        // *** Teillieferung möglich ***
                        // wenn zwischen 40% und 90% auf Lager 
                        // und wenn Artikel nicht gesperrt und 
                        // wenn der Wunsch LT innerhalb des nächsten halben Jahres liegt

                        if ((rec.ProductQuantityInStock < (rec.OrderConfirmationLineQuantityOutstanding))
                            & (rec.ProductQuantityInStock >= rec.OrderConfirmationLineQuantityOutstanding * 0.4)
                            & (rec.ProductQuantityInStock <= rec.OrderConfirmationLineQuantityOutstanding * 0.9)
                            & (rec.ProductLocked == 0)
                            &((rec.OrderConfirmationDesiredDeliveryDate - DateTime.Today) < TimeSpan.FromDays(183)))
                        {
                            if (rec.Hint != "") { rec.Hint += "\n"; }
                            rec.Hint += "Teillieferung möglich";
                        }


                        // *** Vorartikel auf Lager! ***
                        // wenn Kundenwunsch LT vorhanden 
                        // und zugesagter LT liegt noch in der Zukunft 
                        // und der Wunsch LT liegt nicht mehr als 4 Wochen in der Zukunft (sonst braucht der User noch keinen Hinweis)
                        // und es ein Stücklisten-Artikel ist
                        // und abgesehen von Dienstleistungsartikeln existiert mindestens ein Vorartikel dieser Stückliste 
                        // und von diesem Vorartikel mindestens 50% der Bestellmenge auf Lager sind

                        if ((rec.OrderConfirmationDesiredDeliveryDate != null)
                            & (rec.OrderConfirmationDeliveryDate >= DateTime.Today)
                            & ((rec.OrderConfirmationDesiredDeliveryDate - DateTime.Today) <= TimeSpan.FromDays(28))
                            & (rec.StuecklistenArtikel == 1)
                            & (rec.AnzahlVorartikelAufStueckliste >= 1)
                            & (rec.LagBestVorartikel > (rec.OrderConfirmationLineQuantityOutstanding * 0.5)))
                        {
                            if (rec.Hint != "") { rec.Hint += "\n"; }
                            rec.Hint += "Vorartikel auf Lager!";
                        }


                        if (rec.ProductLocked == 1)
                        {

                            // *** Gesperrte Ware auf Lager ***
                            // wenn der Artikel gesperrt und
                            // wenn die gesamte Menge der Kundenbestellung auf Lager ist
                            if (rec.ProductQuantityInStock >= rec.OrderConfirmationLineQuantityOutstanding)
                            {
                                if (rec.Hint != "") { rec.Hint += "\n"; }
                                rec.Hint += "Gesperrte Ware auf Lager";
                            }

                            else
                            {
                                // *** Gesperrte Teilmenge ***
                                // wenn der Artikel gesperrt und
                                // wenn mindestens 40% auf Lager ist und
                                // wenn der Liefertermin nicht mehr als ein halbes Jahr im Voraus liegt
                                if ((rec.ProductQuantityInStock >= (rec.OrderConfirmationLineQuantityOutstanding * 0.4))
                                    &(rec.OrderConfirmationDeliveryDate - DateTime.Today) < TimeSpan.FromDays(183))
                                {
                                    if (rec.Hint != "") { rec.Hint += "\n"; }
                                    rec.Hint += "Gesperrte Teilmenge";
                                }
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    if (rec.Hint != "") { rec.Hint += "\n"; }
                    rec.Hint += err.Message;
                }
            }
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






        // 20200618 JA: Neue Artikelauswertung hinzugefügt
        public IEnumerable<ArtikelUmsatz> GetArtikelUmsatz(string von, string bis, double USD, double RMB)
        {
            IEnumerable<ArtikelUmsatz> results = null;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<ArtikelUmsatz>(GetArtikelUmsatzSQL(von, bis, USD, RMB));
            }

            return results;
        }



        // 20200621 JA: Neue Kundenauswertung hinzugefügt
        public IEnumerable<KundenUmsatz> GetKundenUmsatz(string von, string bis, double USD, double RMB)
        {
            IEnumerable<KundenUmsatz> results = null;

            using (var connection = this.dbSession.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                results = connection.Query<KundenUmsatz>(GetKundenUmsatzSQL(von, bis, USD, RMB));
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