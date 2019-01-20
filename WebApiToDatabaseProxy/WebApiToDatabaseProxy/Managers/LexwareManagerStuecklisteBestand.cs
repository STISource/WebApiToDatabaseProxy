namespace WebApiToDatabaseProxy.Managers
{
    public partial class LexwareManager
    {
        private const string StuecklisteBestandSql = @"SELECT 
FK_Stueckliste.ArtikelNr as HauptArtikel, FK_Stueckliste.UnterartikelNr, 

    ISNULL((SELECT SUM(LagBest.Bestand) FROM F1.FK_LagerBestand AS LagBest WHERE LagBest.lArtikelId = FK_Artikel.SheetNr), 0) AS LagerBestand

FROM F1.FK_Stueckliste
LEFT JOIN F1.FK_Artikel ON F1.FK_Artikel.ArtikelNr = FK_Stueckliste.UnterArtikelNr
where LagerBestand > 0

order by HauptArtikel
";

    }
}