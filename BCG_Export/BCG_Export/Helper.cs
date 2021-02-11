using log4net;
using Microsoft.Practices.EnterpriseLibrary.Data;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCG_Export
{
    public static class Helper
    {
        public static Dictionary<Type, DbType> InitializeTypeHash()
        {
            Dictionary<Type, DbType> typeMap = new Dictionary<Type, DbType>();
            typeMap[typeof(byte)] = DbType.Byte;
            typeMap[typeof(sbyte)] = DbType.SByte;
            typeMap[typeof(short)] = DbType.Int16;
            typeMap[typeof(ushort)] = DbType.UInt16;
            typeMap[typeof(int)] = DbType.Int32;
            typeMap[typeof(uint)] = DbType.UInt32;
            typeMap[typeof(long)] = DbType.Int64;
            typeMap[typeof(ulong)] = DbType.UInt64;
            typeMap[typeof(float)] = DbType.Single;
            typeMap[typeof(double)] = DbType.Double;
            typeMap[typeof(decimal)] = DbType.Decimal;
            typeMap[typeof(bool)] = DbType.Boolean;
            typeMap[typeof(string)] = DbType.String;
            typeMap[typeof(char)] = DbType.StringFixedLength;
            typeMap[typeof(Guid)] = DbType.Guid;
            typeMap[typeof(DateTime)] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
            typeMap[typeof(byte[])] = DbType.Binary;
            typeMap[typeof(byte?)] = DbType.Byte;
            typeMap[typeof(sbyte?)] = DbType.SByte;
            typeMap[typeof(short?)] = DbType.Int16;
            typeMap[typeof(ushort?)] = DbType.UInt16;
            typeMap[typeof(int?)] = DbType.Int32;
            typeMap[typeof(uint?)] = DbType.UInt32;
            typeMap[typeof(long?)] = DbType.Int64;
            typeMap[typeof(ulong?)] = DbType.UInt64;
            typeMap[typeof(float?)] = DbType.Single;
            typeMap[typeof(double?)] = DbType.Double;
            typeMap[typeof(decimal?)] = DbType.Decimal;
            typeMap[typeof(bool?)] = DbType.Boolean;
            typeMap[typeof(char?)] = DbType.StringFixedLength;
            typeMap[typeof(Guid?)] = DbType.Guid;
            typeMap[typeof(DateTime?)] = DbType.DateTime;
            typeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
            //TODO: Find the appropriate mapping for DbType.Binary currently ignoring it as none of our tables are using this type
            //typeMap[typeof(System.Data.Linq.Binary)] = DbType.Binary;

            return typeMap;

        }
    }

    public class TableCopier
    {
        private static readonly ILog Log =
             LogManager.GetLogger(typeof(TableCopier));

        [BsonId]
        public object _id { get; set; }
        public int CopierId { get; set; }
        private const string _successColumnName = "IsInsertSuccess";
        public string SourceTableName { get; set; }
        public string DesntiationTableName { get; set; }
        public string SourceTableQuery { get; set; }
        public List<WhereConditionParameters> filterConditions { get; set; }
        public string LastSyncDate { get; set; }
        public string LastSyncTime { get; set; }
        public string SourceDbConnectionStringName { get; set; }
        public string DestinationDbConnectionStringName { get; set; }
        public List<WhereConditionParameters> UpdateFieldAfterInsert { get; set; }
        public TableCopyType CopyType { get; set; }
        public List<List<dynamic>> FailedRowIdList { get; set; }
        public List<string> OrderByColumnNames { get; set; }
        public double CastLength { get; set; }
        public double SequenceNumber { get; set; }
        public string Status { get; set; }


        private DataTable GetTableDataWithFitler()
        {
            try
            {
                Database sourceDb = DatabaseFactory.CreateDatabase(SourceDbConnectionStringName);

                //switch (CopyType)
                //{
                //    case TableCopyType.StatusBased:
                //        return BLL.ExecuteQueryGetDataTable(sourceDb, SourceTableQuery, filterConditions, OrderByColumnNames);
                //    case TableCopyType.SequenceBased:
                //        return null;
                //    case TableCopyType.TimeStampBased:
                //        filterConditions.Where(x => x.ColumnName == ":FDATE").First().Value = LastSyncDate; ;
                //        filterConditions.Where(x => x.ColumnName == ":FTIME").First().Value = LastSyncTime;
                //        return BLL.ExecuteQueryGetDataTable(sourceDb, SourceTableQuery, filterConditions, OrderByColumnNames);
                //    default: return null;
                //}

                return BLL.ExecuteQueryGetDataTable(sourceDb, SourceTableQuery, filterConditions, OrderByColumnNames);
            }
            catch (Exception ex)
            {
                Log.Error("Error while getting table data", ex);
                return null;
            }
        }

        public void ImportNextBatch()
        {
            
            DataTable sourceData = GetTableDataWithFitler();
            if (sourceData.Rows.Count == 0)
            {
                return;
            }
            DataRow[] successfullInserts = null;
            if (sourceData != null)
            {
                successfullInserts = SaveDataTableDestinationDb(sourceData);
                switch (CopyType)
                {
                    case TableCopyType.StatusBased:
                      
                        //Updating the source table with a flag to indicate that these records are already migrated so that we dont create duplicate records in the next iteration
                        UpdateSourceTableWithSuccessFlag(successfullInserts);
                        break;
                    case TableCopyType.SequenceBased:
                        var te1 = successfullInserts.OrderByDescending(z => int.Parse(z.Field<string>("FDATE"))).ThenByDescending(z => int.Parse(z.Field<string>("FTIME")));
                        break;
                    case TableCopyType.TimeStampBased:
                        var te = successfullInserts.OrderByDescending(z => int.Parse(z.Field<string>("FDATE"))).ThenByDescending(z => int.Parse(z.Field<string>("FTIME")));
                        LastSyncDate = te.First().Field<string>("FDATE");
                        LastSyncTime = te.First().Field<string>("FTIME");

                        MongoDbDal.UpdateLastSyncDateTime(CopierId, LastSyncDate, LastSyncTime);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Log.Info(" could not get data from source table: " + SourceTableName);
            }
        }

        private DataRow[] SaveDataTableDestinationDb(DataTable dt)
        {
            DumpDataTtableToDB(ref dt);
            return dt.Select(_successColumnName + "=" + true);
            // UpdateSourceTableWithSuccessFlag(successfullInserts);
        }

        private void UpdateSourceTableWithSuccessFlag(DataRow[] successDt)
        {
            Database db = DatabaseFactory.CreateDatabase(SourceDbConnectionStringName);
            foreach (var row in successDt)
            {
                UpdateSourceDbSuccessFlag(row["rowid"], db);
            }
        }

        private void DumpDataTtableToDB(ref DataTable srcTableData)
        {

            Database db = DatabaseFactory.CreateDatabase(DestinationDbConnectionStringName);
            srcTableData.Columns.Add(_successColumnName, typeof(bool));
            foreach (DataRow row in srcTableData.Rows)
            {
                dynamic currentKeyValuePair = null;
                List<dynamic> keyValuePairs = new List<dynamic>();

                foreach (DataColumn col in srcTableData.Columns)
                {
                    if (col.ColumnName.ToUpper() != "ROWID" && col.ColumnName.ToUpper() != _successColumnName.ToUpper())
                    {
                        currentKeyValuePair = new ExpandoObject();
                        currentKeyValuePair.Key = col.ColumnName;
                        currentKeyValuePair.Value = row[col.ColumnName];

                        keyValuePairs.Add(currentKeyValuePair);
                    }
                }

                try
                {
                    if (InsertDataToDestinationDB(keyValuePairs, db) == 1)
                    {
                        row[_successColumnName] = true;
                    }
                    else
                    {
                        row[_successColumnName] = false;
                    }
                }
                catch (Exception ex)
                {
                    row[_successColumnName] = false;
                    Log.Error(" Error while inserting record to db ", ex);
                }
            }
        }

        private int UpdateSourceDbSuccessFlag(object rowId, Database db)
        {
            StringBuilder sqlCommand = new StringBuilder();
            sqlCommand.AppendFormat(" UPDATE {0} SET ", SourceTableName);

            if (UpdateFieldAfterInsert.Count == 1)
            {
                sqlCommand.AppendFormat(" {0} = :{0}zxz1 ", UpdateFieldAfterInsert[0].ColumnName);
            }
            else
            {
                for (int i = 0; i < UpdateFieldAfterInsert.Count - 1; i++)
                {
                    sqlCommand.AppendFormat(" {0} = :{0}zxz1, ", UpdateFieldAfterInsert[i].ColumnName);
                }
                sqlCommand.AppendFormat(" {0} = :{0}zxz1 ", UpdateFieldAfterInsert[UpdateFieldAfterInsert.Count - 1].ColumnName);
            }

            sqlCommand.AppendFormat(" where rowid=:rowida ");
            DbCommand dbCmd = db.GetSqlStringCommand(sqlCommand.ToString());
            dbCmd.CommandType = CommandType.Text;

            db.AddInParameter(dbCmd, ":rowida", BCG_ExportService.TypeMap[rowId.GetType()], rowId);
            foreach (var field in UpdateFieldAfterInsert)
            {
                if (field.Value != null && field.Value.GetType() != typeof(System.DBNull))
                {
                    db.AddInParameter(dbCmd, field.ColumnName + "zxz1", BCG_ExportService.TypeMap[field.Value.GetType()], field.Value);
                }
                else
                {
                    db.AddInParameter(dbCmd, field.ColumnName + "zxz1", DbType.String, field.Value);
                }
            }


            return db.ExecuteNonQuery(dbCmd);
        }

        private int InsertDataToDestinationDB(List<dynamic> keyValueParis, Database db)
        {
            int i = 0;
            StringBuilder sqlCommand = new StringBuilder();
            sqlCommand.AppendFormat(" INSERT INTO {0} (", DesntiationTableName);
            for (i = 0; i < keyValueParis.Count - 1; i++)
            {
                sqlCommand.Append(keyValueParis[i].Key + ",");
            }
            sqlCommand.Append(keyValueParis[i].Key + ") VALUES (");

            for (i = 0; i < keyValueParis.Count - 1; i++)
            {
                sqlCommand.Append(":" + keyValueParis[i].Key + ",");
            }

            sqlCommand.Append(":" + keyValueParis[i].Key + ")");

            DbCommand dbCmd = db.GetSqlStringCommand(sqlCommand.ToString());
            dbCmd.CommandType = CommandType.Text;

            foreach (var kvp in keyValueParis)
            {
                if (kvp.Value != null && kvp.Value.GetType() != typeof(DBNull))
                {
                    db.AddInParameter(dbCmd, ":" + kvp.Key, BCG_ExportService.TypeMap[kvp.Value.GetType()], kvp.Value);
                }
                else
                {
                    db.AddInParameter(dbCmd, ":" + kvp.Key, DbType.String, kvp.Value);
                }
            }

            return db.ExecuteNonQuery(dbCmd);
        }
    }

    public class WhereConditionParameters
    {
        public string ColumnName { get; set; }
        public int DBTypeEnumValue { get; set; }
        public object Value { get; set; }
        public string RelationalOperator { get; set; }
    }

    public enum TableCopyType
    {
        StatusBased = 0,
        SequenceBased = 1,
        TimeStampBased = 2
    }
}
