namespace WebApiToDatabaseProxy.Managers
{
    public partial class LexwareManager
    {
        private const string WarenausgangSql = @"SELECT 
    Artikel.ArtikelNr AS ArtNr,
    CASE WHEN LEN(Artikel.Bezeichnung) > 50 THEN LEFT(Artikel.Bezeichnung, 50) + '...' ELSE Artikel.Bezeichnung END AS Bez,
    FK_ArtikelBestandOptionen.Lagerort,
    ISNULL((SELECT SUM(LagBest.Bestand) FROM F1.FK_LagerBestand AS LagBest WHERE LagBest.lArtikelId = Artikel.SheetNr), 0) AS LagerBestand,  

    ISNULL(FK_SerienNr.dftMengeFrei, 0) AS BestandCharge,
	ISNULL(FK_SerienNr.szSerienNr, '') AS Charge,
    
	
	(SELECT
        ISNULL(LEFT(MAX(VK_Auftrag1.Datum_erfassung), 10), '')
        FROM F1.FK_Auftrag AS VK_Auftrag1
        INNER JOIN F1.FK_AuftragPos AS VK_AuftragPos1 ON VK_Auftrag1.AuftragsNr = VK_AuftragPos1.AuftragsNr AND VK_Auftrag1.AuftragsKennung = VK_AuftragPos1.AuftragsKennung
        WHERE
            VK_Auftrag1.AuftragsKennung = 3 -- Rechnung
            AND VK_AuftragPos1.ArtikelNr = Artikel.ArtikelNr)
		AS LetzteRechnung, 
		
	(SELECT
        ISNULL(LEFT(MIN(EK_Auftrag.Datum_erfassung), 10), '')
        FROM F1.FK_Auftrag AS EK_Auftrag
        INNER JOIN F1.FK_AuftragPos AS EK_AuftragPos ON EK_Auftrag.AuftragsNr = EK_AuftragPos.AuftragsNr AND EK_Auftrag.AuftragsKennung = EK_AuftragPos.AuftragsKennung
        WHERE
            EK_Auftrag.AuftragsKennung = 16-- Wareneingang
            AND EK_AuftragPos.ArtikelNr = Artikel.ArtikelNr)
		AS ErsterWareneingang,
	
	CASE WHEN Artikel.bStatus_stueckliste > 0 
    THEN 'X' 
    ELSE '' END
    AS HauptartikelStueckliste,
		
    isnull((SELECT
        MIN(Stueckliste.ArtikelNr)
        FROM F1.FK_Stueckliste AS Stueckliste
        WHERE
            Stueckliste.UnterArtikelnr = Artikel.ArtikelNr), '') AS BestandteilDerStueckliste,

    CASE WHEN
        ISNULL((SELECT
            MAX(VK_Auftrag.Datum_erfassung)
        FROM F1.FK_Auftrag AS VK_Auftrag
        INNER JOIN F1.FK_AuftragPos AS VK_AuftragPos ON VK_Auftrag.AuftragsNr = VK_AuftragPos.AuftragsNr AND VK_Auftrag.AuftragsKennung = VK_AuftragPos.AuftragsKennung
        WHERE
            VK_Auftrag.AuftragsKennung = 3 -- Rechnung
            AND VK_AuftragPos.ArtikelNr = Artikel.ArtikelNr), '1900-01-01') < DATEADD(month, -60, CURRENT_TIMESTAMP)
	AND
        ISNULL((SELECT
            MAX(EK_Auftrag.Datum_erfassung)
        FROM F1.FK_Auftrag AS EK_Auftrag
        INNER JOIN F1.FK_AuftragPos AS EK_AuftragPos ON EK_Auftrag.AuftragsNr = EK_AuftragPos.AuftragsNr AND EK_Auftrag.AuftragsKennung = EK_AuftragPos.AuftragsKennung
        WHERE
            EK_Auftrag.AuftragsKennung = 2-- Lieferschein
            AND EK_AuftragPos.ArtikelNr = Artikel.ArtikelNr), '1900-01-01') < DATEADD(month, -60, CURRENT_TIMESTAMP)
	AND
        ISNULL((SELECT
            MAX(EK_Auftrag.Datum_erfassung)
        FROM F1.FK_Auftrag AS EK_Auftrag
        INNER JOIN F1.FK_AuftragPos AS EK_AuftragPos ON EK_Auftrag.AuftragsNr = EK_AuftragPos.AuftragsNr AND EK_Auftrag.AuftragsKennung = EK_AuftragPos.AuftragsKennung
        WHERE
            EK_Auftrag.AuftragsKennung = 16-- Wareneingang
            AND EK_AuftragPos.ArtikelNr = Artikel.ArtikelNr), '1900-01-01') < DATEADD(month, -60, CURRENT_TIMESTAMP)
    THEN 'X' ELSE NULL END AS KeineRechnungWareneingSeit5Jahren

	FROM F1.FK_Artikel AS Artikel
    LEFT JOIN F1.FK_ArtikelBestandOptionen ON FK_ArtikelBestandOptionen.lArtikelID = Artikel.SheetNr
	LEFT JOIN F1.FK_SerienNr ON FK_SerienNr.lArtikelID = Artikel.SheetNr AND FK_SerienNr.dftMengeFrei > 0 

WHERE
    LagerBestand > 0 AND
    BestandteilDerStueckliste = '' AND
	KeineRechnungWareneingSeit5Jahren ='X'

ORDER BY Artikel.ArtikelNr
";

    }
}