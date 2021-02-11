using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using log4net;
using Microsoft.Practices.EnterpriseLibrary.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel;

namespace Vegam_Database_Table_CopierService
{
    public class TableCopier
    {
        private static readonly ILog _tableCopierLog = LogManager.GetLogger(typeof(TableCopier));

        [BsonId]
        public object _id { get; set; }
        public int TableCopierID { get; set; }
        public string SourceTableName { get; set; }
        public string DesntiationTableName { get; set; }
        public string SourceTableSelectQuery { get; set; }
        public List<ColumnValueInfo> SourceTableSelectFilters { get; set; }
        public string SourceDbConnectionStringName { get; set; }
        public string DestinationDbConnectionStringName { get; set; }
        public List<ColumnValueInfo> SourceTableUpdateFilters { get; set; }
        public TableCopyType CopyType { get; set; }
        public string Status { get; set; }
        public string UpdateDestinationTableIfDataAlreadyExists { get; set; }
        public List<string> DestinationTablePrimaryKeyFieldList { get; set; }
        public List<ColumnValueInfo> DestinationTableUpdateConditions { get; set; }

        private const string _successColumnName = "IsInsertSuccess";

        private DataTable GetDataFromSourceDBTable(Database sourceDb)
        {
            try
            {
                return OracleDB_DAL.SelectDataFromTable(sourceDb, SourceTableSelectQuery, SourceTableSelectFilters);
            }
            catch (Exception ex)
            {
                _tableCopierLog.Error("Error while getting data from source table for Table Copier ID : " + TableCopierID, ex);
                return null;
            }
        }

        private void InsertOrUpdateDataToDestiantionDBTable(ref DataTable sourceDataTable)
        {
            IDataReader dataReaderInfo = null;
            try
            {
                Database destinationDB = DatabaseFactory.CreateDatabase(DestinationDbConnectionStringName);

                sourceDataTable.Columns.Add(_successColumnName, typeof(bool));

                foreach (DataRow row in sourceDataTable.Rows)
                {
                    dynamic columnValueInfo = null;
                    List<dynamic> columnValueInfoList = new List<dynamic>();
                    List<dynamic> primaryKeyColumnValueInfoList = new List<dynamic>();

                    foreach (DataColumn col in sourceDataTable.Columns)
                    {
                        if (col.ColumnName.ToUpper() != "ROWID" && col.ColumnName.ToUpper() != "FDATEVAL" && col.ColumnName.ToUpper() != "FTIMEVAL" && col.ColumnName.ToUpper() != _successColumnName.ToUpper())
                        {
                            columnValueInfo = new ExpandoObject();
                            columnValueInfo.Key = col.ColumnName;
                            columnValueInfo.Value = row[col.ColumnName];

                            columnValueInfoList.Add(columnValueInfo);

                            if (UpdateDestinationTableIfDataAlreadyExists == "Y" && DestinationTablePrimaryKeyFieldList.Contains(col.ColumnName))
                            {
                                primaryKeyColumnValueInfoList.Add(columnValueInfo);
                            }
                        }
                    }

                    if (UpdateDestinationTableIfDataAlreadyExists == "Y")
                    {
                        if (primaryKeyColumnValueInfoList.Count > 0)
                        {
                            List<string> selectCoulmnList = new List<string>();
                            if (DestinationTableUpdateConditions != null && DestinationTableUpdateConditions.Count > 0)
                            {
                                selectCoulmnList = DestinationTableUpdateConditions.Select(l => l.ColumnName).ToList();
                            }

                            DataTable existingInfoTable = OracleDB_DAL.CheckReordExistsInTable(destinationDB, null, DesntiationTableName, primaryKeyColumnValueInfoList, selectCoulmnList);
                            if (existingInfoTable != null && existingInfoTable.Rows.Count > 0)
                            {
                                DataRow existingInfoRow = existingInfoTable.Rows[0];
                                bool updateRecord = true;

                                if (DestinationTableUpdateConditions != null && DestinationTableUpdateConditions.Count > 0)
                                {
                                    foreach (ColumnValueInfo info in DestinationTableUpdateConditions)
                                    {
                                        if (info.Value.ToString().Trim() == existingInfoRow[info.ColumnName].ToString().Trim())
                                        {
                                            updateRecord = true;
                                        }
                                        else
                                        {
                                            updateRecord = false;
                                            break;
                                        }
                                    }
                                }

                                if (updateRecord)
                                {
                                    //OracleDB_DAL.DeleteRecordFromTable(destinationDB, transaction, DesntiationTableName, primaryKeyColumnValueInfoList);
                                    foreach (var pkColumnValueInfo in primaryKeyColumnValueInfoList)
                                    {
                                        foreach (var colValueInfo in columnValueInfoList)
                                        {
                                            if (columnValueInfo.Key == pkColumnValueInfo.Key)
                                            {
                                                columnValueInfoList.Remove(colValueInfo);
                                                break;
                                            }
                                        }
                                    }

                                    if (OracleDB_DAL.UpdateRecordInTable(destinationDB, null, DesntiationTableName, columnValueInfoList, primaryKeyColumnValueInfoList) >= 1)
                                    {
                                        row[_successColumnName] = true;
                                    }
                                    else
                                    {
                                        row[_successColumnName] = false;
                                    }
                                }
                            }
                            else
                            {
                                if (OracleDB_DAL.InsertRecordToTable(destinationDB, null, DesntiationTableName, columnValueInfoList) == 1)
                                {
                                    row[_successColumnName] = true;
                                }
                                else
                                {
                                    row[_successColumnName] = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        if(OracleDB_DAL.InsertRecordToTable(destinationDB, null, DesntiationTableName, columnValueInfoList)==1)
                        {
                            row[_successColumnName] = true;
                        }
                        else
                        {
                            row[_successColumnName] = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (dataReaderInfo != null && !dataReaderInfo.IsClosed)
                    dataReaderInfo.Close();

                _tableCopierLog.Error(" Error while inserting/updating record in destination db table for Table Copier ID : " + TableCopierID, ex);
            }
        }

        public void FetchDataAndImport()
        {
            try
            {
                Database sourceDb = DatabaseFactory.CreateDatabase(SourceDbConnectionStringName);

                DataTable sourceDataTable = GetDataFromSourceDBTable(sourceDb);

                if (sourceDataTable == null || sourceDataTable.Rows.Count == 0)
                {
                    _tableCopierLog.Info(" No data fetched from source table for Table Copier ID : " + TableCopierID);
                    return;
                }
                else
                {
                    _tableCopierLog.Info(" Data fetched successfully from source table for Table Copier ID : " + TableCopierID);

                    InsertOrUpdateDataToDestiantionDBTable(ref sourceDataTable);

                    _tableCopierLog.Info(" Data updated successfully in destination table for Table Copier ID : " + TableCopierID);

                    DataRow[] insertSuccessRows = sourceDataTable.Select(_successColumnName + "=" + true);

                    switch (CopyType)
                    {
                        case TableCopyType.StatusBased:
                            //Updating the source table with a flag to indicate that these records are already migrated so that we dont create duplicate records in the next iteration                   
                            foreach (var row in insertSuccessRows)
                            {
                                OracleDB_DAL.UpdateValuesForRecord(sourceDb, null, SourceTableName, SourceTableUpdateFilters, row);
                            }
                            break;
                        case TableCopyType.SequenceBased:
                            break;
                        case TableCopyType.TimeStampBased:
                            var te = insertSuccessRows.OrderByDescending(z => int.Parse(z.Field<string>("FDATEVAL"))).ThenByDescending(z => int.Parse(z.Field<string>("FTIMEVAL")));
                            string lastSyncDate = te.First().Field<string>("FDATEVAL");
                            string lastSyncTime = te.First().Field<string>("FTIMEVAL");
                            MongoDB_DAL.UpdateLastSyncDateTime(TableCopierID, lastSyncDate, lastSyncTime);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _tableCopierLog.Error(" Error Occured While Copying data for Table Copier ID : " + TableCopierID, ex);
            }
        }
    }

    public class ColumnValueInfo
    {
        public string ColumnName { get; set; }
        public int DBTypeEnumValue { get; set; }
        public object Value { get; set; }
    }

    public enum TableCopyType
    {
        StatusBased = 0,
        SequenceBased = 1,
        TimeStampBased = 2
    }
}
