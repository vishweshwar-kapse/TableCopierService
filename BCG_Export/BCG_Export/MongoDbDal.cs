using log4net;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCG_Export
{
    public static class MongoDbDal
    {
        private static MongoClient client = null;
        private static IMongoDatabase database = null;

        private static readonly ILog Log =
             LogManager.GetLogger(typeof(TableCopier));

        public static List<TableCopier> GetListOfTableCopiers()
        {
            var tableCollection = database.GetCollection<TableCopier>("TableCopier");

            return tableCollection.Find<TableCopier>("{Status:'A'}").ToList();

        }

        public static void MongoDB_Initialize()
        {
            try
            {
                client = new MongoClient(ConfigurationManager.AppSettings["MongoDbConnectionString"]);
                database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDbDataBaseName"]);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                
            }
        }

        public static void UpdateLastSyncDateTime(int copierId,string fdate,string ftime)
        {

            StringBuilder searchDateQuery = new StringBuilder();
            StringBuilder searchTimeQuery = new StringBuilder();

            searchDateQuery.Append("{ 	'CopierId': "+copierId+", 	'filterConditions': { '$elemMatch': {'ColumnName': ':FDATE' } } }");
            searchTimeQuery.Append("{ 	'CopierId': "+copierId+", 	'filterConditions': { '$elemMatch': {'ColumnName': ':FTIME' } } }");

            StringBuilder updateDateQuery = new StringBuilder();
            StringBuilder updateTimeQuery = new StringBuilder();

            updateDateQuery.Append("{$set:{'filterConditions.$.Value':'"+fdate+"'}}");
            updateTimeQuery.Append("{$set:{'filterConditions.$.Value':'"+ftime+"'}}");

            var tableCollection = database.GetCollection<TableCopier>("TableCopier");

            tableCollection.UpdateOne(searchDateQuery.ToString(), updateDateQuery.ToString());
            tableCollection.UpdateOne(searchTimeQuery.ToString(), updateTimeQuery.ToString());
        }
    }
}
