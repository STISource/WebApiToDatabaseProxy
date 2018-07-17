using System;
using System.Collections.Generic;
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
    STRING(datepart(year, OrderConfirmationDeliveryDate), ' / ', datepart(Calweekofyear, OrderConfirmationDeliveryDate)) AS OrderConfirmationDeliveryWeek,
    CASE WHEN AuftragMain.bStatus_geliefert = 1 
                OR AuftragPosMain.ArtikelNr IS NULL
        THEN
            ''
        ELSE
            CASE WHEN AuftragMain.szUserdefined4 IS NOT NULL AND OrderConfirmationDesiredDeliveryDate IS NULL
                --((AuftragPosMain.ArtikelNr<> ArtikelMain.ArtikelNr)
                --OR(AuftragPosMain.Artikel_Einheit<> ArtikelMain.Einheit)
                --OR(AuftragPosMain.Artikel_Bezeichnung<> ArtikelMain.Bezeichnung)
                --OR(AuftragPosMain.Artikel_Matchcode<> ArtikelMain.Matchcode)
                --OR(AuftragPosMain.PosText<> ArtikelMain.Beschreibung))               
            THEN 
                'Wunsch-LT ungültig'
            ELSE
                CASE WHEN OrderConfirmationDesiredDeliveryDate IS NOT NULL
                    AND OrderConfirmationDeliveryDate > GETDATE()
                    AND(OrderConfirmationDesiredDeliveryDate - OrderConfirmationDeliveryDate) < -7
                    AND(OrderConfirmationDesiredDeliveryDate - GETDATE()) < 14
                    AND(ProductQuantityInStock >= OrderConfirmationLineQuantityOutstanding)
                    AND(SELECT TOP 1 AuftragPosInner0.LNr
                            FROM FK_Auftrag AS AuftragInner0
                            INNER JOIN FK_AuftragPos AS AuftragPosInner0 ON AuftragInner0.AuftragsNr = AuftragPosInner0.AuftragsNr AND AuftragInner0.AuftragsKennung = AuftragPosInner0.Auftragskennung
                            WHERE AuftragPosInner0.ArtikelNr = AuftragPosMain.ArtikelNr
                                AND AuftragInner0.bStatus_geliefert != 1
                                AND AuftragInner0.Auftragskennung = 1
                                AND (AuftragPosInner0.Artikel_Menge* 0.92) > (ISNULL((SELECT
                                                                               SUM(AuftragPosInner01.Artikel_Menge)
                                                                            FROM FK_Auftrag AS AuftragInner01
                                                                            INNER JOIN FK_AuftragPos AS AuftragPosInner01 ON AuftragInner01.AuftragsNr = AuftragPosInner01.AuftragsNr AND AuftragInner01.AuftragsKennung = AuftragPosInner01.Auftragskennung

                                                                            WHERE
                                                                               AuftragInner01.AuftragsKennung = 2

                                                                               AND AuftragInner01.Verweis_weiter_aus_nr = AuftragInner0.SheetNr

                                                                               AND AuftragPosInner01.ArtikelNr = AuftragPosInner0.ArtikelNr), 0))
                            ORDER BY AuftragInner0.tsLieferTermin ASC,
                                    CASE WHEN ISDATE(AuftragInner0.szUserdefined4)=0 THEN AuftragInner0.tsLieferTermin ELSE CONVERT(DATE, AuftragInner0.szUserdefined4) END ASC) = AuftragPosMain.LNr
                THEN
                    'Früher liefern?!'
                ELSE
                    ''
                END
            END
         END AS Hint,    
    AuftragMain.AuftragsNr AS OrderConfirmationNumber,
    CAST(AuftragMain.tsLieferTermin as DATE) AS OrderConfirmationOriginalDeliveryDate,    
    STRING(datepart(year, AuftragMain.tsLieferTermin), ' / ', datepart(Calweekofyear, AuftragMain.tsLieferTermin)) AS OrderConfirmationOriginalDeliveryWeek,        
    CASE WHEN ISDATE(AuftragMain.szUserdefined4)=0 THEN NULL ELSE CONVERT(DATE, AuftragMain.szUserdefined4) END AS OrderConfirmationDesiredDeliveryDate,
    AuftragMain.szUserdefined1 as StiProjectNumber,    
    AuftragMain.szUserdefined3 AS GeneralAgreement,
    AufgabeMain.szBetreff + ' - ' + AufgabeMain.szRemark AS Comments,
    (SELECT TOP 1 KontaktInner.szBetreff + ' - ' + KontaktInner.szBemerkung
     FROM FK_KONTAKT AS KontaktInner
     INNER JOIN FK_AuftragNotizKontakt NotizInner ON KontaktInner.lID = NotizInner.lKontaktID
     WHERE NotizInner.lAuftragID = AuftragMain.SheetNr
     ORDER BY NotizInner.lID) AS Note,           
    ROUND(AuftragPosMain.Summen_preis, 3) AS OrderConfirmationLinePrice,
    (AuftragPosMain.Artikel_Menge / CASE WHEN AuftragPosMain.Artikel_Preisfaktor = 0 THEN 1 ELSE AuftragPosMain.Artikel_Preisfaktor END) AS OrderConfirmationLinePricePerQuantity,    
    ROUND((AuftragPosMain.Summen_preis* AuftragPosMain.Artikel_Preisfaktor), 2) AS OrderConfirmationLinePriceSum,
    ISNULL((SELECT
        SUM(AuftragPosInner1.Artikel_Menge)
     FROM FK_Auftrag AS AuftragInner1
     INNER JOIN FK_AuftragPos AS AuftragPosInner1 ON AuftragInner1.AuftragsNr = AuftragPosInner1.AuftragsNr AND AuftragInner1.AuftragsKennung = AuftragPosInner1.Auftragskennung
     WHERE
        AuftragInner1.AuftragsKennung = 2
        AND AuftragInner1.Verweis_weiter_aus_nr = AuftragMain.SheetNr
        AND AuftragPosInner1.ArtikelNr = AuftragPosMain.ArtikelNr
    ), 0) AS OrderConfirmationLineQuantityDelivered,
    (AuftragPosMain.Artikel_Menge - OrderConfirmationLineQuantityDelivered) AS OrderConfirmationLineQuantityOutstanding,            
    (SELECT SUM(LagBest.Bestand) FROM FK_LagerBestand AS LagBest WHERE LagBest.lArtikelId = ArtikelMain.SheetNr) AS ProductQuantityInStock,        
    (SELECT SUM(LagBest2.Menge_bestellt) FROM FK_LagerBestand AS LagBest2 WHERE LagBest2.lArtikelId = ArtikelMain.SheetNr) AS ProductQuantityOrderedByPurchasing,    
    (SELECT
        SUM((ArtikelResInner1.dftResMenge - ArtikelResInner1.dftGeliefertMenge))
     FROM FK_Artikelreservierung ArtikelResInner1
     WHERE
        ArtikelResInner1.lArtikelID = ArtikelMain.SheetNr
        AND fAbgeschlossen = 0
    ) AS ProductQuantityReserved, 
    ArtikelMain.fGesperrt AS ProductLocked
FROM FK_Auftrag AS AuftragMain
INNER JOIN FK_AuftragPos AS AuftragPosMain ON AuftragMain.AuftragsNr = AuftragPosMain.AuftragsNr AND AuftragMain.AuftragsKennung = AuftragPosMain.Auftragskennung
LEFT OUTER JOIN FK_Artikel AS ArtikelMain ON AuftragPosMain.ArtikelNr = ArtikelMain.ArtikelNr
LEFT OUTER JOIN LX_Aufgabe AS AufgabeMain ON AuftragMain.lWiedervorlageID = AufgabeMain.ID_AUFGABE
WHERE AuftragMain.Auftragskennung = 1 AND AuftragMain.Datum_erfassung > '2018-01-01' AND AuftragPosMain.PosTyp != 2
ORDER BY AuftragMain.Datum_erfassung, AuftragMain.AuftragsNr ASC
";

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
    }
}