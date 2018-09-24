using System.Collections.Generic;
using System.Data;
using Dapper;
using WebApiToDatabaseProxy.Database;
using WebApiToDatabaseProxy.Models;

namespace WebApiToDatabaseProxy.Managers
{
    public partial class LexwareManager : ILexwareManager
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
    }
}