namespace WebApiToDatabaseProxy.Managers
{
    public partial class LexwareManager
    {
        private const string DeliveryPreviewSql = @"
SELECT         
    CASE WHEN LEN(AuftragMain.KundenMatchcode) > 15 THEN LEFT(AuftragMain.KundenMatchcode, 15) + '...' ELSE AuftragMain.KundenMatchcode END AS Customer,       
    AuftragPosMain.ArtikelNr AS ProductNumber,    
    CASE WHEN AuftragPosMain.PosTyp = 1 THEN
        CASE WHEN LEN(AuftragPosMain.PosText) > 30 THEN LEFT(AuftragPosMain.PosText, 30) + ' ...' ELSE AuftragPosMain.PosText END
    ELSE AuftragPosMain.Artikel_Bezeichnung END AS ProductDescription,
    AuftragPosMain.Artikel_Menge - ISNULL((SELECT
        SUM(AuftragPosInner1.Artikel_Menge)
     FROM F1.FK_Auftrag AS AuftragInner1
     INNER JOIN F1.FK_AuftragPos AS AuftragPosInner1 ON AuftragInner1.AuftragsNr = AuftragPosInner1.AuftragsNr AND AuftragInner1.AuftragsKennung = AuftragPosInner1.Auftragskennung
     WHERE
        AuftragInner1.AuftragsKennung = 2
        AND AuftragInner1.Verweis_weiter_aus_nr = AuftragMain.SheetNr
        AND AuftragPosInner1.ArtikelNr = AuftragPosMain.ArtikelNr
    ), 0) AS QuantityOutstanding,         
    AuftragPosMain.Artikel_Einheit AS Unit,
    CASE AuftragMain.bStatus_geliefert WHEN 1 THEN 'Geliefert' ELSE CASE WHEN AuftragPosMain.Artikel_Menge > QuantityOutstanding THEN 'Teilgeliefert' ELSE 'Nicht geliefert' END END AS DeliveryStatus,  
    CONVERT(DATE, ISNULL(AufgabeMain.datEnde, AuftragMain.tsLieferTermin)) AS DeliveryDate,    
    STRING(datepart(calyearofweek, DeliveryDate), ' / ', datepart(Calweekofyear, DeliveryDate)) AS DeliveryWeek,        
    AuftragMain.AuftragsNr AS OrderConfirmationNumber,     
    CASE WHEN ISDATE(AuftragMain.szUserdefined4)=0 THEN NULL ELSE CONVERT(DATE, AuftragMain.szUserdefined4) END AS DesiredDeliveryDateCustomer,    
    (SELECT TOP 1 KontaktInner.szBetreff + ' - ' + KontaktInner.szBemerkung
     FROM F1.FK_KONTAKT AS KontaktInner
     INNER JOIN F1.FK_AuftragNotizKontakt NotizInner ON KontaktInner.lID = NotizInner.lKontaktID
     WHERE NotizInner.lAuftragID = AuftragMain.SheetNr
     ORDER BY NotizInner.lID) AS Note,
    (SELECT SUM(LagBest.Bestand) FROM F1.FK_LagerBestand AS LagBest WHERE LagBest.lArtikelId = ArtikelMain.SheetNr) AS QuantityInStock,        
    (SELECT SUM(LagBest2.Menge_bestellt) FROM F1.FK_LagerBestand AS LagBest2 WHERE LagBest2.lArtikelId = ArtikelMain.SheetNr) AS QuantityOrderedByPurchasing,
    CASE WHEN ArtikelMain.fGesperrt = 1 THEN 'Gesperrt!' ELSE NULL END AS ProductLockedStatus
FROM F1.FK_Auftrag AS AuftragMain
INNER JOIN F1.FK_AuftragPos AS AuftragPosMain ON AuftragMain.AuftragsNr = AuftragPosMain.AuftragsNr AND AuftragMain.AuftragsKennung = AuftragPosMain.Auftragskennung
LEFT OUTER JOIN F1.FK_Artikel AS ArtikelMain ON AuftragPosMain.ArtikelNr = ArtikelMain.ArtikelNr
LEFT OUTER JOIN F1.LX_Aufgabe AS AufgabeMain ON AuftragMain.lWiedervorlageID = AufgabeMain.ID_AUFGABE
WHERE AuftragMain.Auftragskennung = 1 
        AND AuftragMain.Datum_erfassung > '2018-01-01' 
        AND AuftragPosMain.PosTyp != 2 
        AND DeliveryStatus != 'Geliefert'        
        AND DeliveryDate <= DATEADD(mm, 3, GETDATE())
ORDER BY DeliveryDate, AuftragMain.AuftragsNr ASC";

    }
}