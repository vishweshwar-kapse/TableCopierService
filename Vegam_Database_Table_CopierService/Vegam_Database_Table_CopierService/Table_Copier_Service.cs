using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Timers;
using log4net;

namespace Vegam_Database_Table_CopierService
{
    public partial class Table_Copier_Service : ServiceBase
    {
        Timer tableCopyTriggerTimer = new Timer();
        private static int tableCopyTimerTriggerCount = 0;
        private static bool tableCopyInProgress = false;

        public static Dictionary<Type, DbType> TypeMap = null;

        private static readonly ILog tableCopierServiceLog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Table_Copier_Service()
        {
            InitializeComponent();
        }

        ////uncomment below code and comment above codes for debugging
        //public void Run()
        //{
        //    OnStart(null);
        //}

        protected override void OnStart(string[] args)
        {
            try
            {
                tableCopierServiceLog.Info("******Started Running Vegam_Database_Table_CopierService*****");

                InitializeTypeMapDictionary();

                tableCopyTriggerTimer.Interval = int.Parse(ConfigurationManager.AppSettings["TableCopyTimerInterval"]);
                tableCopyTriggerTimer.Elapsed += tableCopyTriggerTimer_Elapsed;

                MongoDB_DAL.MongoDB_Initialize();

                try
                {
                    List<TableCopier> tableCopierList = MongoDB_DAL.GetListOfTableCopiers();

                    foreach (var item in tableCopierList)
                    {
                        item.FetchDataAndImport();
                    }
                }
                catch (Exception ex)
                {
                    tableCopierServiceLog.Error(ex);
                }

                tableCopyTriggerTimer.Start();
            }
            catch(Exception ex)
            {
                tableCopierServiceLog.Error(ex);
            }
        }

        private void tableCopyTriggerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (tableCopyInProgress == false)
            {
                tableCopyInProgress = true;
                tableCopyTimerTriggerCount = 0;

                try
                {
                    List<TableCopier> tableCopierList = MongoDB_DAL.GetListOfTableCopiers();

                    foreach (var item in tableCopierList)
                    {
                        item.FetchDataAndImport();
                    }

                    tableCopyInProgress = false;
                    tableCopyTimerTriggerCount = 0;
                }
                catch (Exception ex)
                {
                    tableCopierServiceLog.Error(ex);
                }
            }
            else
            {
                tableCopyTimerTriggerCount++;
                if (tableCopyTimerTriggerCount >= Convert.ToInt32(ConfigurationManager.AppSettings["TableCopyTimerCounter"].ToString()))
                {
                    tableCopyTimerTriggerCount = 0;
                    tableCopyInProgress = false;
                }
            }
        }

        protected override void OnStop()
        {
            tableCopierServiceLog.Info("****Stopping Vegam_Database_Table_CopierService****");
        }

        private void InitializeTypeMapDictionary()
        {
            TypeMap = new Dictionary<Type, DbType>();
            TypeMap[typeof(byte)] = DbType.Byte;
            TypeMap[typeof(sbyte)] = DbType.SByte;
            TypeMap[typeof(short)] = DbType.Int16;
            TypeMap[typeof(ushort)] = DbType.UInt16;
            TypeMap[typeof(int)] = DbType.Int32;
            TypeMap[typeof(uint)] = DbType.UInt32;
            TypeMap[typeof(long)] = DbType.Int64;
            TypeMap[typeof(ulong)] = DbType.UInt64;
            TypeMap[typeof(float)] = DbType.Single;
            TypeMap[typeof(double)] = DbType.Double;
            TypeMap[typeof(decimal)] = DbType.Decimal;
            TypeMap[typeof(bool)] = DbType.Boolean;
            TypeMap[typeof(string)] = DbType.String;
            TypeMap[typeof(char)] = DbType.StringFixedLength;
            TypeMap[typeof(Guid)] = DbType.Guid;
            TypeMap[typeof(DateTime)] = DbType.DateTime;
            TypeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
            TypeMap[typeof(byte[])] = DbType.Binary;
            TypeMap[typeof(byte?)] = DbType.Byte;
            TypeMap[typeof(sbyte?)] = DbType.SByte;
            TypeMap[typeof(short?)] = DbType.Int16;
            TypeMap[typeof(ushort?)] = DbType.UInt16;
            TypeMap[typeof(int?)] = DbType.Int32;
            TypeMap[typeof(uint?)] = DbType.UInt32;
            TypeMap[typeof(long?)] = DbType.Int64;
            TypeMap[typeof(ulong?)] = DbType.UInt64;
            TypeMap[typeof(float?)] = DbType.Single;
            TypeMap[typeof(double?)] = DbType.Double;
            TypeMap[typeof(decimal?)] = DbType.Decimal;
            TypeMap[typeof(bool?)] = DbType.Boolean;
            TypeMap[typeof(char?)] = DbType.StringFixedLength;
            TypeMap[typeof(Guid?)] = DbType.Guid;
            TypeMap[typeof(DateTime?)] = DbType.DateTime;
            TypeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
            //TODO: Find the appropriate mapping for DbType.Binary currently ignoring it as none of our tables are using this type
            //typeMap[typeof(System.Data.Linq.Binary)] = DbType.Binary;
        }
    }
}
