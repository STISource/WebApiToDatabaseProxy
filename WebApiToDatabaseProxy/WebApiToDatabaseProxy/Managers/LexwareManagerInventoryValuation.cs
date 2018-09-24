namespace WebApiToDatabaseProxy.Managers
{
    public partial class LexwareManager
    {
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

    }
}