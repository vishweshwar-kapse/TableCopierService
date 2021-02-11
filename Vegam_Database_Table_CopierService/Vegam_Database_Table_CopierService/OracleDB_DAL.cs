using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;
using log4net;

namespace Vegam_Database_Table_CopierService
{
    public static class OracleDB_DAL
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OracleDB_DAL));

        public static DataTable SelectDataFromTable(Database db, string query, List<ColumnValueInfo> whereConditionColumnInfo)
        {
            try
            {
                DbCommand dbCmd = db.GetSqlStringCommand(query.Trim());
                dbCmd.CommandType = CommandType.Text;

                Log.Info("Query generated is: ");
                Log.Info(query.Trim());

                foreach (var item in whereConditionColumnInfo)
                {
                    db.AddInParameter(dbCmd, item.ColumnName, (DbType)item.DBTypeEnumValue, item.Value);
                }

                var resultDataSet = db.ExecuteDataSet(dbCmd);

                if (resultDataSet != null && resultDataSet.Tables != null && resultDataSet.Tables.Count > 0)
                {
                    return resultDataSet.Tables[0];
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception occured while fetching source table data for query : " + query, ex);
                return null;
            }
        }

        public static int InsertRecordToTable(Database db, DbTransaction transaction, string tableName, List<dynamic> columnValueInfoList)
        {
            StringBuilder sqlCommand = new StringBuilder();

            string columnNameString = string.Empty;
            string valueString = string.Empty;

            sqlCommand.AppendFormat(" INSERT INTO {0} (", tableName);

            foreach (dynamic columnValueInfo in columnValueInfoList)
            {
                columnNameString += columnValueInfo.Key + ",";
                valueString += ":" + columnValueInfo.Key + ",";
            }

            columnNameString = columnNameString.TrimEnd(',');
            valueString = valueString.TrimEnd(',');

            sqlCommand.Append(columnNameString + ") VALUES (" + valueString + ")");

            DbCommand dbCmd = db.GetSqlStringCommand(sqlCommand.ToString());
            dbCmd.CommandType = CommandType.Text;

            foreach (var columnValueInfo in columnValueInfoList)
            {
                if (columnValueInfo.Value != null && columnValueInfo.Value.GetType() != typeof(DBNull))
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, Table_Copier_Service.TypeMap[columnValueInfo.Value.GetType()], columnValueInfo.Value);
                }
                else
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, DbType.String, columnValueInfo.Value);
                }
            }

            if (transaction != null)
                return db.ExecuteNonQuery(dbCmd, transaction);
            else
                return db.ExecuteNonQuery(dbCmd);
        }

        public static int UpdateRecordInTable(Database db, DbTransaction transaction, string tableName, List<dynamic> columnValueInfoList, List<dynamic> primaryKeyColumnValueInfoList)
        {
            StringBuilder sqlCommand = new StringBuilder();

            string updateColumnString = string.Empty;
            string updateConditionString = string.Empty;

            sqlCommand.AppendFormat(" UPDATE {0} SET ", tableName);

            foreach (dynamic columnValueInfo in columnValueInfoList)
            {
                updateColumnString += columnValueInfo.Key + "=:" + columnValueInfo.Key + ",";
            }

            updateColumnString = updateColumnString.TrimEnd(',');

            sqlCommand.Append(updateColumnString);
            sqlCommand.Append(" WHERE ");

            foreach (dynamic columnValueInfo in primaryKeyColumnValueInfoList)
            {
                updateConditionString = columnValueInfo.Key + "=:" + columnValueInfo.Key + ",";
            }

            updateConditionString = updateConditionString.TrimEnd(',');

            sqlCommand.Append(updateConditionString);

            DbCommand dbCmd = db.GetSqlStringCommand(sqlCommand.ToString());
            dbCmd.CommandType = CommandType.Text;

            foreach (var columnValueInfo in columnValueInfoList)
            {
                if (columnValueInfo.Value != null && columnValueInfo.Value.GetType() != typeof(DBNull))
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, Table_Copier_Service.TypeMap[columnValueInfo.Value.GetType()], columnValueInfo.Value);
                }
                else
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, DbType.String, columnValueInfo.Value);
                }
            }

            foreach (var columnValueInfo in primaryKeyColumnValueInfoList)
            {
                if (columnValueInfo.Value != null && columnValueInfo.Value.GetType() != typeof(DBNull))
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, Table_Copier_Service.TypeMap[columnValueInfo.Value.GetType()], columnValueInfo.Value);
                }
                else
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, DbType.String, columnValueInfo.Value);
                }
            }

            if (transaction != null)
                return db.ExecuteNonQuery(dbCmd, transaction);
            else
                return db.ExecuteNonQuery(dbCmd);
        }

        public static int DeleteRecordFromTable(Database db, DbTransaction transaction, string tableName, List<dynamic> columnValueInfoList)
        {
            StringBuilder sqlCommand = new StringBuilder();

            string columnNameString = string.Empty;
            string valueString = string.Empty;

            sqlCommand.AppendFormat(" DELETE {0} ", tableName);
            sqlCommand.Append(" WHERE ");

            int index = 0;
            foreach (dynamic columnValueInfo in columnValueInfoList)
            {
                if (index > 0)
                    sqlCommand.Append(" AND ");

                sqlCommand.AppendFormat("{0}=:{0}", columnValueInfo.Key);
                index++;
            }

            DbCommand dbCmd = db.GetSqlStringCommand(sqlCommand.ToString());
            dbCmd.CommandType = CommandType.Text;

            foreach (var columnValueInfo in columnValueInfoList)
            {
                if (columnValueInfo.Value != null && columnValueInfo.Value.GetType() != typeof(DBNull))
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, Table_Copier_Service.TypeMap[columnValueInfo.Value.GetType()], columnValueInfo.Value);
                }
                else
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, DbType.String, columnValueInfo.Value);
                }
            }

            if (transaction != null)
                return db.ExecuteNonQuery(dbCmd, transaction);
            else
                return db.ExecuteNonQuery(dbCmd);
        }

        public static DataTable CheckReordExistsInTable(Database db, DbTransaction transaction, string tableName, List<dynamic> primaryKeycolumnValueInfoList, List<string> selectCoulmnList)
        {
            StringBuilder sqlCommand = new StringBuilder();

            string columnNameString = string.Empty;
            string valueString = string.Empty;

            string selectCoumns = string.Empty;
            if(selectCoulmnList!=null && selectCoulmnList.Count>0)
            {
                selectCoumns = String.Join(",", selectCoulmnList);
            }
            else
            {
                selectCoumns = primaryKeycolumnValueInfoList[0].Key;
            }

            sqlCommand.AppendFormat(" SELECT " + selectCoumns + " FROM {0} ", tableName);
            sqlCommand.Append(" WHERE ");

            int index = 0;
            foreach (dynamic columnValueInfo in primaryKeycolumnValueInfoList)
            {
                if (index > 0)
                    sqlCommand.Append(" AND ");

                sqlCommand.AppendFormat("{0}=:{0}", columnValueInfo.Key);
                index++;
            }

            DbCommand dbCmd = db.GetSqlStringCommand(sqlCommand.ToString());
            dbCmd.CommandType = CommandType.Text;

            foreach (var columnValueInfo in primaryKeycolumnValueInfoList)
            {
                if (columnValueInfo.Value != null && columnValueInfo.Value.GetType() != typeof(DBNull))
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, Table_Copier_Service.TypeMap[columnValueInfo.Value.GetType()], columnValueInfo.Value);
                }
                else
                {
                    db.AddInParameter(dbCmd, ":" + columnValueInfo.Key, DbType.String, columnValueInfo.Value);
                }
            }

            if (transaction != null)
                return db.ExecuteDataSet(dbCmd, transaction).Tables[0];
            else
                return db.ExecuteDataSet(dbCmd).Tables[0];
        }

        public static int UpdateValuesForRecord(Database db, DbTransaction transaction, string tableName, List<ColumnValueInfo> updateColumnInfo, object rowId)
        {
            StringBuilder sqlCommand = new StringBuilder();

            sqlCommand.AppendFormat(" UPDATE {0} SET ", tableName);

            if (updateColumnInfo.Count == 1)
            {
                sqlCommand.AppendFormat(" {0} = :{0} ", updateColumnInfo[0].ColumnName);
            }
            else
            {
                for (int i = 0; i < updateColumnInfo.Count - 1; i++)
                {
                    sqlCommand.AppendFormat(" {0} = :{0}, ", updateColumnInfo[i].ColumnName);
                }
                sqlCommand.AppendFormat(" {0} = :{0} ", updateColumnInfo[updateColumnInfo.Count - 1].ColumnName);
            }

            sqlCommand.AppendFormat(" WHERE ROWID=:ROWIDA ");

            DbCommand dbCmd = db.GetSqlStringCommand(sqlCommand.ToString());
            dbCmd.CommandType = CommandType.Text;

            db.AddInParameter(dbCmd, ":ROWIDA", Table_Copier_Service.TypeMap[rowId.GetType()], rowId);

            foreach (var field in updateColumnInfo)
            {
                if (field.Value != null && field.Value.GetType() != typeof(System.DBNull))
                {
                    db.AddInParameter(dbCmd, ":" + field.ColumnName, Table_Copier_Service.TypeMap[field.Value.GetType()], field.Value);
                }
                else
                {
                    db.AddInParameter(dbCmd, ":" + field.ColumnName, DbType.String, field.Value);
                }
            }

            if (transaction != null)
                return db.ExecuteNonQuery(dbCmd, transaction);
            else
                return db.ExecuteNonQuery(dbCmd);
        }
    }
}
