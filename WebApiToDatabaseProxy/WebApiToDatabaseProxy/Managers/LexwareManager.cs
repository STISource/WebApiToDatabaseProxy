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

                results = connection.Query<SalesOrderConfirmationDetail>("SELECT TOP 10 KundenMatchcode AS Customer, tsLieferTermin AS DateOfDelivery, AuftragsNr AS OrderConfirmationId FROM FK_Auftrag ORDER BY Datum_erfassung;");
            }

            return results;
        }
    }
}