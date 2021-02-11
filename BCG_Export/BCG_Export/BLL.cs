using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using log4net;
using System.Threading.Tasks;

namespace BCG_Export
{
    public static class BLL
    {
        private static readonly ILog Log =
     LogManager.GetLogger(typeof(BLL));

        public static DataTable ExecuteQueryGetDataTable(Database db, string query, List<WhereConditionParameters> keyValuePairs, List<string> orderByColumnNames)
        {
            try
            {
                StringBuilder whereQuery = new StringBuilder(query);

                DbCommand dbCmd = db.GetSqlStringCommand(whereQuery.ToString());
                Log.Info("The query generated was");
                Log.Info(whereQuery.ToString());
                dbCmd.CommandType = CommandType.Text;


                foreach (var item in keyValuePairs)
                {
                    db.AddInParameter(dbCmd, item.ColumnName, (DbType)item.DBTypeEnumValue, item.Value);
                }

                var ds = db.ExecuteDataSet(dbCmd);
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    return ds.Tables[0];
                }
                else
                    return null;

            }
            catch (Exception ex)
            {
                Log.Error("Exception while fetching source table data for query : " + query, ex);
                return null;
            }
        }
    }
}
