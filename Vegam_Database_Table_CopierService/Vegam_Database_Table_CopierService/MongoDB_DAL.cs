using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Configuration;

namespace Vegam_Database_Table_CopierService
{
    public static class MongoDB_DAL
    {
        private static MongoClient mongoClient = null;
        private static IMongoDatabase mongoDatabase = null;
        private static string _mongoCollectionName = string.Empty;

        public static List<TableCopier> GetListOfTableCopiers()
        {
            var tableCollection = mongoDatabase.GetCollection<TableCopier>(_mongoCollectionName);
            return tableCollection.Find<TableCopier>("{Status:'A'}").ToList();
        }

        public static void MongoDB_Initialize()
        {
            try
            {
                mongoClient = new MongoClient(ConfigurationManager.AppSettings["MongoDbConnectionString"].Trim());
                mongoDatabase = mongoClient.GetDatabase(ConfigurationManager.AppSettings["MongoDbDataBaseName"].Trim());
                _mongoCollectionName = ConfigurationManager.AppSettings["MongoDbCollectionName"].Trim();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void UpdateLastSyncDateTime(int copierID, string fdate, string ftime)
        {
            StringBuilder searchDateQuery = new StringBuilder();
            StringBuilder searchTimeQuery = new StringBuilder();

            searchDateQuery.Append("{ 	'TableCopierID': " + copierID + ", 	'SourceTableSelectFilters': { '$elemMatch': {'ColumnName': ':FDATE' } } }");
            searchTimeQuery.Append("{ 	'TableCopierID': " + copierID + ", 	'SourceTableSelectFilters': { '$elemMatch': {'ColumnName': ':FTIME' } } }");

            StringBuilder updateDateQuery = new StringBuilder();
            StringBuilder updateTimeQuery = new StringBuilder();

            updateDateQuery.Append("{$set:{'SourceTableSelectFilters.$.Value':'" + fdate + "'}}");
            updateTimeQuery.Append("{$set:{'SourceTableSelectFilters.$.Value':'" + ftime + "'}}");

            var tableCollection = mongoDatabase.GetCollection<TableCopier>(_mongoCollectionName);

            tableCollection.UpdateOne(searchDateQuery.ToString(), updateDateQuery.ToString());
            tableCollection.UpdateOne(searchTimeQuery.ToString(), updateTimeQuery.ToString());
        }
    }
}
