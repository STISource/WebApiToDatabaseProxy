using System.EnterpriseServices;
using System.Globalization;

namespace WebApiToDatabaseProxy.Managers
{
    public partial class LexwareManager
    {
        public string GetArtikelUmsatzSQL(string von, string bis, double USD, double RMB)
        {
            string strUSD = USD.ToString(CultureInfo.InvariantCulture);
            string strRMB = RMB.ToString(CultureInfo.InvariantCulture);
            return @"
SELECT 

RechnungsPos.ArtikelNr,
ROUND(SUM(RechnungsPos.Summen_netto), 2) AS VK_Netto_EUR, 

ROUND(SUM(CASE
    WHEN UPPER(TRIM(Artikel.szUserdefined3)) = 'EURO' THEN Artikel_Menge * ArtLieferant.EK_preis_eur / (IF ArtLieferant.dftEk_preisfaktor = 0 THEN 1 ELSE ArtLieferant.dftEk_preisfaktor ENDIF) 
    WHEN UPPER(TRIM(Artikel.szUserdefined3)) = 'EUR'  THEN Artikel_Menge * ArtLieferant.EK_preis_eur / (IF ArtLieferant.dftEk_preisfaktor = 0 THEN 1 ELSE ArtLieferant.dftEk_preisfaktor ENDIF) 
    WHEN UPPER(TRIM(Artikel.szUserdefined3)) = 'RMB'  THEN Artikel_Menge * ArtLieferant.EK_preis_eur / (IF ArtLieferant.dftEk_preisfaktor = 0 THEN 1 ELSE ArtLieferant.dftEk_preisfaktor ENDIF) * " + strRMB + @"  
    ELSE                                                   Artikel_Menge * ArtLieferant.EK_preis_eur / (IF ArtLieferant.dftEk_preisfaktor = 0 THEN 1 ELSE ArtLieferant.dftEk_preisfaktor ENDIF) * " + strUSD + @" 
END), 2) AS EK_Artikel_EUR,


-- Summe der EK Preise in EUR aller Artikel auf der Stückliste (falls Stücklisten-Artikel)
ROUND(SUM((SELECT 
    SUM(CASE
        WHEN (UPPER(TRIM(UnterArtikel.szUserdefined3)) = 'EUR') OR 
             (UPPER(TRIM(UnterArtikel.szUserdefined3)) = 'EURO') THEN Stueckliste.Menge * Lieferanten.EK_preis_eur / (IF Lieferanten.dftEk_preisfaktor = 0 THEN 1 ELSE Lieferanten.dftEk_preisfaktor ENDIF)            -- EUR
        WHEN  UPPER(TRIM(UnterArtikel.szUserdefined3)) = 'RMB'   THEN Stueckliste.Menge * Lieferanten.EK_preis_eur / (IF Lieferanten.dftEk_preisfaktor = 0 THEN 1 ELSE Lieferanten.dftEk_preisfaktor ENDIF) * " + strRMB + @" 
        ELSE                                                          Stueckliste.Menge * Lieferanten.EK_preis_eur / (IF Lieferanten.dftEk_preisfaktor = 0 THEN 1 ELSE Lieferanten.dftEk_preisfaktor ENDIF) * " + strUSD + @"            
    END)
    FROM F1.FK_Stueckliste AS Stueckliste
    LEFT JOIN F1.FK_Artikel AS UnterArtikel ON StueckListe.UnterartikelNr = UnterArtikel.ArtikelNr 
    LEFT JOIN F1.FK_ArtikelBezugsQ AS Lieferanten ON Lieferanten.ArtikelNr = StueckListe.UnterartikelNr AND Lieferanten.LieferPrio = 1
    WHERE Stueckliste.ArtikelNr = RechnungsPos.ArtikelNr) * RechnungsPos.Artikel_Menge), 2)
AS EK_UnterArt_EUR,

ROUND(VK_Netto_EUR - ISNULL(EK_Artikel_EUR, 0) - ISNULL(EK_UnterArt_EUR, 0), 2) as MARGE_EUR,
IF VK_Netto_EUR <> 0 THEN ROUND(MARGE_EUR / VK_Netto_EUR, 3) ELSE 0 ENDIF AS MARGE_Prozent

FROM F1.FK_Auftrag AS Rechnung

LEFT JOIN F1.FK_AuftragPos AS RechnungsPos ON Rechnung.AuftragsNr = RechnungsPos.AuftragsNr AND Rechnung.AuftragsKennung = RechnungsPos.AuftragsKennung
LEFT JOIN F1.FK_Artikel AS Artikel ON RechnungsPos.ArtikelNr = Artikel.ArtikelNr
LEFT JOIN F1.FK_ArtikelBezugsQ AS ArtLieferant ON ArtLieferant.ArtikelNr = RechnungsPos.ArtikelNr AND ArtLieferant.LieferPrio = 1
LEFT JOIN F1.FK_Lieferant AS ArtLiefStamm ON ArtLieferant.LieferantenNr = ArtLiefStamm.LieferantenNr

WHERE Rechnung.AuftragsKennung = 3
and Rechnung.Datum_erfassung >= '" + von + @"' and Rechnung.Datum_erfassung <= '" + bis + @"'
and RechnungsPos.PosTyp = 0

GROUP BY RechnungsPos.ArtikelNr
ORDER By RechnungsPos.ArtikelNr
";
        }
    }
}