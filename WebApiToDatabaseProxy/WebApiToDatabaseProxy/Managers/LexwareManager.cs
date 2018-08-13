﻿using System.Collections.Generic;
using System.Data;
using Dapper;
using WebApiToDatabaseProxy.Database;
using WebApiToDatabaseProxy.Models;

namespace WebApiToDatabaseProxy.Managers
{
    public class LexwareManager : ILexwareManager
    {
        private const string ConfirmationDetailsSql = @"
SELECT          
    CASE WHEN LEN(AuftragMain.KundenMatchcode) > 15 THEN LEFT(AuftragMain.KundenMatchcode, 15) + '...' ELSE AuftragMain.KundenMatchcode END AS Customer,   
    AuftragMain.BestellNr AS CustomerOrderNumber,
    AuftragPosMain.ArtikelNr AS ProductNumber,
    AuftragPosMain.Artikel_Bezeichnung AS ProductDescription,    
    CASE WHEN AuftragPosMain.PosTyp = 1 THEN
        CASE WHEN LEN(AuftragPosMain.PosText) > 30 THEN LEFT(AuftragPosMain.PosText, 30) + ' ...' ELSE AuftragPosMain.PosText END
    ELSE NULL END AS OrderConfirmationLineText,
    AuftragPosMain.Artikel_Menge AS OrderConfirmationLineQuantity,
    AuftragPosMain.Artikel_Einheit AS OrderConfirmationLineUnit,
    CASE AuftragMain.bStatus_geliefert WHEN 1 THEN 'Geliefert' ELSE CASE WHEN OrderConfirmationLineQuantityDelivered > 0 THEN 'Teilgeliefert' ELSE 'Nicht geliefert' END END AS OrderConfirmationLineDeliveryStatus,
    CONVERT(DATE, ISNULL(AufgabeMain.datEnde, AuftragMain.tsLieferTermin)) AS OrderConfirmationDeliveryDate,    
    STRING(datepart(calyearofweek, OrderConfirmationDeliveryDate), ' / ', datepart(Calweekofyear, OrderConfirmationDeliveryDate)) AS OrderConfirmationDeliveryWeek,
    CASE WHEN AuftragMain.bStatus_geliefert = 1 
                OR AuftragPosMain.ArtikelNr IS NULL
        THEN
            ''
        ELSE
            CASE WHEN AuftragMain.szUserdefined4 IS NOT NULL AND OrderConfirmationDesiredDeliveryDate IS NULL                
            THEN 
                'Wunsch-LT ungültig'
            ELSE
                CASE WHEN OrderConfirmationDesiredDeliveryDate IS NOT NULL                              -- Kundenwunsch LT verohanden
                    AND OrderConfirmationDeliveryDate >= GETDATE()                                      -- Zugesagter LT liegt noch in der Zukunft
                    AND (OrderConfirmationDesiredDeliveryDate - OrderConfirmationDeliveryDate) < -7     -- Zugesagter LT und Wunsch LT liegen mehr als eine Woche auseinander
                    AND (OrderConfirmationDesiredDeliveryDate - GETDATE()) < 14                         -- Wunsch LT liegt nicht mehr als 2 Wochen in der Zukunft (sonst braucht der User noch keinen Hinweis)
                    AND (ProductQuantityInStock >= (OrderConfirmationLineQuantityOutstanding * 0.3))    -- Es sind mindestens 30% der bestellten Menge auf Lager
                    AND (SELECT TOP 1 AuftragPosInner0.LNr                                              -- Wenn diese AB in de Liste der nicht abgeschlossenen ABs für diesen Artikel als nächstes ansteht (sortiert nach Liefertermin, Wunschliefertermin)
                            FROM F1.FK_Auftrag AS AuftragInner0
                            INNER JOIN F1.FK_AuftragPos AS AuftragPosInner0 ON AuftragInner0.AuftragsNr = AuftragPosInner0.AuftragsNr AND AuftragInner0.AuftragsKennung = AuftragPosInner0.Auftragskennung
                            WHERE AuftragPosInner0.ArtikelNr = AuftragPosMain.ArtikelNr
                                AND AuftragInner0.bStatus_geliefert != 1
                                AND AuftragInner0.Auftragskennung = 1
                                AND (AuftragPosInner0.Artikel_Menge* 0.9) > (ISNULL((SELECT
                                                                               SUM(AuftragPosInner01.Artikel_Menge)
                                                                            FROM F1.FK_Auftrag AS AuftragInner01
                                                                            INNER JOIN F1.FK_AuftragPos AS AuftragPosInner01 ON AuftragInner01.AuftragsNr = AuftragPosInner01.AuftragsNr AND AuftragInner01.AuftragsKennung = AuftragPosInner01.Auftragskennung

                                                                            WHERE
                                                                               AuftragInner01.AuftragsKennung = 2

                                                                               AND AuftragInner01.Verweis_weiter_aus_nr = AuftragInner0.SheetNr

                                                                               AND AuftragPosInner01.ArtikelNr = AuftragPosInner0.ArtikelNr), 0))
                            ORDER BY ISNULL(AuftragInner0.tsLieferTermin, CONVERT(DATE, '2099-12-31')) ASC,
                                    CASE WHEN ISDATE(AuftragInner0.szUserdefined4)=0 THEN AuftragInner0.tsLieferTermin ELSE CONVERT(DATE, AuftragInner0.szUserdefined4) END ASC) = AuftragPosMain.LNr
                THEN
                    'Früher liefern?!'
                ELSE
                    
                    CASE WHEN OrderConfirmationDesiredDeliveryDate IS NOT NULL
                        AND OrderConfirmationDeliveryDate >= GETDATE()
                        AND (OrderConfirmationDesiredDeliveryDate - OrderConfirmationDeliveryDate) < -7
                        AND (OrderConfirmationDesiredDeliveryDate - GETDATE()) < 14
                        AND ArtikelMain.bStatus_stueckliste = 1                         -- Das ist ein Stücklistenartikel
                        AND (SELECT                                                     -- Und diese Stückliste besteht abgesehen von Dienstleistungsartikeln nur aus einem Vorartikel
                                COUNT(*)
                            FROM F1.FK_Stueckliste AS StueckListe1
                            INNER JOIN F1.FK_Artikel AS UnterArtikel1 ON StueckListe1.UnterartikelNr = UnterArtikel1.ArtikelNr
                            INNER JOIN F1.FK_Warengruppe AS UnterWarenGrp1 ON UnterArtikel1.WarengrpNr = UnterWarenGrp1.WarengrpNr
                            WHERE
                                StueckListe1.ArtikelNr = AuftragPosMain.ArtikelNr
                                AND UnterWarenGrp1.Bezeichnung != 'Dienstleistungen'
                            ) = 1     
                        AND (SELECT                                                     -- Und von diesem Vorartikel sind mindestens 30% der Bestellmenge auf Lager
                                SUM(LagBest2.Bestand) AS LagBestVorartikel 
                            FROM F1.FK_LagerBestand AS LagBest2 
                            WHERE LagBest2.lArtikelId = (SELECT TOP 1
                                                            UnterArtikel2.SheetNr
                                                        FROM F1.FK_Stueckliste AS StueckListe2
                                                        INNER JOIN F1.FK_Artikel AS UnterArtikel2 ON StueckListe2.UnterartikelNr = UnterArtikel2.ArtikelNr
                                                        INNER JOIN F1.FK_Warengruppe AS UnterWarenGrp2 ON UnterArtikel2.WarengrpNr = UnterWarenGrp2.WarengrpNr
                                                        WHERE
                                                            StueckListe2.ArtikelNr = AuftragPosMain.ArtikelNr
                                                            AND UnterWarenGrp2.Bezeichnung != 'Dienstleistungen'
                                                        ORDER BY UnterArtikel2.SheetNr
                                                        )    
                            ) > (OrderConfirmationLineQuantityOutstanding * 0.3)                   
                    THEN
                        'Vorartikel auf Lager!'
                    ELSE
                        ''
                    END

                END
            END
         END AS Hint,    
    AuftragMain.AuftragsNr AS OrderConfirmationNumber,
    CAST(AuftragMain.tsLieferTermin as DATE) AS OrderConfirmationOriginalDeliveryDate,
    STRING(datepart(calyearofweek, AuftragMain.tsLieferTermin), ' / ', datepart(Calweekofyear, AuftragMain.tsLieferTermin)) AS OrderConfirmationOriginalDeliveryWeek,
    CASE WHEN ISDATE(AuftragMain.szUserdefined4)=0 THEN NULL ELSE CONVERT(DATE, AuftragMain.szUserdefined4) END AS OrderConfirmationDesiredDeliveryDate,
    AuftragMain.szUserdefined1 as StiProjectNumber,    
    AuftragMain.szUserdefined3 AS GeneralAgreement,
    AufgabeMain.szBetreff + ' - ' + AufgabeMain.szRemark AS Comments,
    (SELECT TOP 1 KontaktInner.szBetreff + ' - ' + KontaktInner.szBemerkung
     FROM F1.FK_KONTAKT AS KontaktInner
     INNER JOIN F1.FK_AuftragNotizKontakt NotizInner ON KontaktInner.lID = NotizInner.lKontaktID
     WHERE NotizInner.lAuftragID = AuftragMain.SheetNr
     ORDER BY NotizInner.lID) AS Note,
    ROUND(AuftragPosMain.Summen_preis, 3) AS OrderConfirmationLinePrice,
    (AuftragPosMain.Artikel_Menge / CASE WHEN AuftragPosMain.Artikel_Preisfaktor = 0 THEN 1 ELSE AuftragPosMain.Artikel_Preisfaktor END) AS OrderConfirmationLinePricePerQuantity,
    ROUND((AuftragPosMain.Summen_preis * AuftragPosMain.Artikel_Preisfaktor), 2) AS OrderConfirmationLinePriceSum,
     ISNULL((SELECT 
         SUM(AuftragPosInner1.Artikel_Menge) 
      FROM F1.FK_Auftrag AS AuftragInner1 
      INNER JOIN F1.FK_AuftragPos AS AuftragPosInner1 ON AuftragInner1.AuftragsNr = AuftragPosInner1.AuftragsNr AND AuftragInner1.AuftragsKennung = AuftragPosInner1.Auftragskennung 
      WHERE 
         AuftragInner1.AuftragsKennung = 2 
         AND AuftragInner1.Verweis_weiter_aus_nr = AuftragMain.SheetNr 
         AND AuftragPosInner1.ArtikelNr = AuftragPosMain.ArtikelNr
     ), 0) AS OrderConfirmationLineQuantityDelivered,
     (AuftragPosMain.Artikel_Menge - OrderConfirmationLineQuantityDelivered) AS OrderConfirmationLineQuantityOutstanding,
     (SELECT SUM(LagBest.Bestand) FROM F1.FK_LagerBestand AS LagBest WHERE LagBest.lArtikelId = ArtikelMain.SheetNr) AS ProductQuantityInStock,
     (SELECT SUM(LagBest2.Menge_bestellt) FROM F1.FK_LagerBestand AS LagBest2 WHERE LagBest2.lArtikelId = ArtikelMain.SheetNr) AS ProductQuantityOrderedByPurchasing,
     (SELECT 
         SUM((ArtikelResInner1.dftResMenge - ArtikelResInner1.dftGeliefertMenge)) 
      FROM F1.FK_Artikelreservierung ArtikelResInner1 
      WHERE 
         ArtikelResInner1.lArtikelID = ArtikelMain.SheetNr 
         AND fAbgeschlossen = 0
     ) AS ProductQuantityReserved,
     ArtikelMain.fGesperrt AS ProductLocked
FROM F1.F1.FK_Auftrag AS AuftragMain
INNER JOIN F1.FK_AuftragPos AS AuftragPosMain ON AuftragMain.AuftragsNr = AuftragPosMain.AuftragsNr AND AuftragMain.AuftragsKennung = AuftragPosMain.Auftragskennung
LEFT OUTER JOIN F1.FK_Artikel AS ArtikelMain ON AuftragPosMain.ArtikelNr = ArtikelMain.ArtikelNr
LEFT OUTER JOIN F1.LX_Aufgabe AS AufgabeMain ON AuftragMain.lWiedervorlageID = AufgabeMain.ID_AUFGABE
WHERE AuftragMain.Auftragskennung = 1 AND AuftragMain.Datum_erfassung > '2018-01-01' AND AuftragPosMain.PosTyp != 2
ORDER BY AuftragMain.Datum_erfassung, AuftragMain.AuftragsNr ASC";

        private const string InventoryValuationSql = @"SELECT 
    Warengruppe.Bezeichnung AS ProductGroup,
    Artikel.ArtikelNr AS ProductNumber,
    CASE WHEN LEN(Artikel.Bezeichnung) > 50 THEN LEFT(Artikel.Bezeichnung, 50) + '...' ELSE Artikel.Bezeichnung END AS ProductDescription,
    ISNULL((SELECT SUM(LagBest.Bestand) FROM F1.FK_LagerBestand AS LagBest WHERE LagBest.lArtikelId = Artikel.SheetNr), 0) AS ProductQuantityInStock,      
    ROUND(CASE WHEN ArtikelLieferant.ArtikelNr IS NULL      -- Kein Lieferant für den Artikel vorhanden und somit kein Preis
                AND Artikel.bStatus_stueckliste = 1-- Es ist zudem ein Artikel mit Stückliste
                AND (SELECT                                 -- Und diese Stückliste besteht abgesehen von Dienstleistungsartikeln nur aus einem Artikel
                        COUNT(*)
                    FROM F1.FK_Stueckliste AS StueckListe1
                    INNER JOIN F1.FK_Artikel AS UnterArtikel1 ON StueckListe1.UnterartikelNr = UnterArtikel1.ArtikelNr
                    INNER JOIN F1.FK_Warengruppe AS UnterWarenGrp1 ON UnterArtikel1.WarengrpNr = UnterWarenGrp1.WarengrpNr
                    WHERE
                        StueckListe1.ArtikelNr = Artikel.ArtikelNr
                        AND UnterWarenGrp1.Bezeichnung != 'Dienstleistungen'
                    ) = 1                           
    THEN 
        -- Dann nimm den EK des einzig von extern bezogenen Artikels der Stückliste
        (SELECT TOP 1
            UnterArtikelLieferant2.EK_preis_eur
        FROM F1.FK_Stueckliste AS StueckListe2
        INNER JOIN F1.FK_Artikel AS UnterArtikel2 ON StueckListe2.UnterartikelNr = UnterArtikel2.ArtikelNr
        INNER JOIN F1.FK_Warengruppe AS UnterWarenGrp2 ON UnterArtikel2.WarengrpNr = UnterWarenGrp2.WarengrpNr
        LEFT OUTER JOIN F1.FK_ArtikelBezugsQ AS UnterArtikelLieferant2 ON UnterArtikel2.ArtikelNr = UnterArtikelLieferant2.ArtikelNr AND UnterArtikelLieferant2.LieferPrio = 1
        WHERE
            StueckListe2.ArtikelNr = Artikel.ArtikelNr
            AND UnterWarenGrp2.Bezeichnung != 'Dienstleistungen'
        ORDER BY StueckListe2.LfNr
        )
    ELSE
        ArtikelLieferant.EK_preis_eur
    END, 3) AS PurchasePrice,
    CASE WHEN ArtikelLieferant.ArtikelNr IS NULL    -- Kein Lieferant für den Artikel vorhanden
                AND Artikel.bStatus_stueckliste = 1 -- Es ist zudem ein Artikel mit Stückliste
                AND (SELECT                         -- Und diese Stückliste besteht abgesehen von Dienstleistungsartikeln nur aus einem Artikel
                        COUNT(*)
                    FROM F1.FK_Stueckliste AS StueckListe3
                    INNER JOIN F1.FK_Artikel AS UnterArtikel3 ON StueckListe3.UnterartikelNr = UnterArtikel3.ArtikelNr
                    INNER JOIN F1.FK_Warengruppe AS UnterWarenGrp3 ON UnterArtikel3.WarengrpNr = UnterWarenGrp3.WarengrpNr
                    WHERE
                        StueckListe3.ArtikelNr = Artikel.ArtikelNr
                        AND UnterWarenGrp3.Bezeichnung != 'Dienstleistungen'
                    ) = 1                           
    THEN 
        -- Dann nimm die Währung des einzig von extern bezogenen Artikels der Stückliste
        (SELECT TOP 1
            UnterArtikel4.szUserdefined3
        FROM F1.FK_Stueckliste AS StueckListe4
        INNER JOIN F1.FK_Artikel AS UnterArtikel4 ON StueckListe4.UnterartikelNr = UnterArtikel4.ArtikelNr
        INNER JOIN F1.FK_Warengruppe AS UnterWarenGrp4 ON UnterArtikel4.WarengrpNr = UnterWarenGrp4.WarengrpNr
        WHERE
            StueckListe4.ArtikelNr = Artikel.ArtikelNr
            AND UnterWarenGrp4.Bezeichnung != 'Dienstleistungen'
        ORDER BY StueckListe4.LfNr
        )
    ELSE
        Artikel.szUserdefined3
    END AS Currency,
    CASE WHEN ArtikelLieferant.ArtikelNr IS NULL    -- Kein Lieferant für den Artikel vorhanden und somit kein Preis pro
                AND Artikel.bStatus_stueckliste = 1-- Es ist zudem ein Artikel mit Stückliste
                AND (SELECT                         -- Und diese Stückliste besteht abgesehen von Dienstleistungsartikeln nur aus einem Artikel
                        COUNT(*)
                    FROM F1.FK_Stueckliste AS StueckListe5
                    INNER JOIN F1.FK_Artikel AS UnterArtikel5 ON StueckListe5.UnterartikelNr = UnterArtikel5.ArtikelNr
                    INNER JOIN F1.FK_Warengruppe AS UnterWarenGrp5 ON UnterArtikel5.WarengrpNr = UnterWarenGrp5.WarengrpNr
                    WHERE
                        StueckListe5.ArtikelNr = Artikel.ArtikelNr
                        AND UnterWarenGrp5.Bezeichnung != 'Dienstleistungen'
                    ) = 1                           
    THEN 
        -- Dann nimm die Preis pro info des einzig von extern bezogenen Artikels der Stückliste
        (SELECT TOP 1            
            CASE WHEN UnterArtikelLieferant6.dftEk_preisfaktor = 0 THEN 1 ELSE UnterArtikelLieferant6.dftEk_preisfaktor END
        FROM F1.FK_Stueckliste AS StueckListe6
        INNER JOIN F1.FK_Artikel AS UnterArtikel6 ON StueckListe6.UnterartikelNr = UnterArtikel6.ArtikelNr
        INNER JOIN F1.FK_Warengruppe AS UnterWarenGrp6 ON UnterArtikel6.WarengrpNr = UnterWarenGrp6.WarengrpNr
        LEFT OUTER JOIN F1.FK_ArtikelBezugsQ AS UnterArtikelLieferant6 ON UnterArtikel6.ArtikelNr = UnterArtikelLieferant6.ArtikelNr AND UnterArtikelLieferant6.LieferPrio = 1
        WHERE
            StueckListe6.ArtikelNr = Artikel.ArtikelNr
            AND UnterWarenGrp6.Bezeichnung != 'Dienstleistungen'
        ORDER BY StueckListe6.LfNr
        )
    ELSE
        CASE WHEN ArtikelLieferant.dftEk_preisfaktor = 0 THEN 1 ELSE ArtikelLieferant.dftEk_preisfaktor END
    END AS PricePerQuantity,
    CASE WHEN Artikel.fGesperrt = 1 THEN 'X' ELSE NULL END AS ProductLocked,
    CASE WHEN
        ISNULL((SELECT
            MAX(VK_Auftrag.Datum_erfassung)
        FROM F1.FK_Auftrag AS VK_Auftrag
        INNER JOIN F1.FK_AuftragPos AS VK_AuftragPos ON VK_Auftrag.AuftragsNr = VK_AuftragPos.AuftragsNr AND VK_Auftrag.AuftragsKennung = VK_AuftragPos.AuftragsKennung
        WHERE
            VK_Auftrag.AuftragsKennung = 3-- Rechnung
            AND VK_AuftragPos.ArtikelNr = Artikel.ArtikelNr), '1900-01-01') < DATEADD(month, -24, CURRENT_TIMESTAMP)
        AND
        ISNULL((SELECT
            MAX(EK_Auftrag.Datum_erfassung)
        FROM F1.FK_Auftrag AS EK_Auftrag
        INNER JOIN F1.FK_AuftragPos AS EK_AuftragPos ON EK_Auftrag.AuftragsNr = EK_AuftragPos.AuftragsNr AND EK_Auftrag.AuftragsKennung = EK_AuftragPos.AuftragsKennung
        WHERE
            EK_Auftrag.AuftragsKennung = 16-- Wareneingang
            AND EK_AuftragPos.ArtikelNr = Artikel.ArtikelNr), '1900-01-01') < DATEADD(month, -24, CURRENT_TIMESTAMP)
    THEN 'X' ELSE NULL END AS NoStockMovementForMoreThanTwoYears    
FROM F1.FK_Artikel AS Artikel
INNER JOIN F1.FK_Warengruppe AS Warengruppe ON Artikel.WarengrpNr = Warengruppe.WarengrpNr
LEFT OUTER JOIN F1.FK_ArtikelBezugsQ AS ArtikelLieferant ON Artikel.ArtikelNr = ArtikelLieferant.ArtikelNr AND ArtikelLieferant.LieferPrio = 1
WHERE
    ProductQuantityInStock > 0
ORDER BY Warengruppe.Bezeichnung, Artikel.ArtikelNr";

        private readonly IDatabaseSession dbSession;

        public LexwareManager(IDatabaseSession session)
        {
            this.dbSession = session;
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
    }
}