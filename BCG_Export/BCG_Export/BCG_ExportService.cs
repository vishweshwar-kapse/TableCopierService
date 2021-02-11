using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BCG_Export
{
    public partial class BCG_ExportService : ServiceBase
    {
        System.Timers.Timer _queryRunner = new System.Timers.Timer();

        public static Dictionary<Type, DbType> TypeMap = null;

        private static readonly ILog Log =
              LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string NoRecordsFound = "NoRecordsFound";


        DateTime toDateTime = DateTime.MinValue;
        DateTime fromDateTime = DateTime.MinValue;

        private static int maxNumberOfRetries = int.Parse(ConfigurationManager.AppSettings["TimerMaxNumberOfRetries"]);

        private static int timerTriggerCount = 0;

        private static bool timerElapsedEventInProgress = false;

        public BCG_ExportService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Log.Info("******Started Running the Query Scheduler*****");
                TypeMap = Helper.InitializeTypeHash();
                //_queryRunner.Interval = int.Parse(ConfigurationManager.AppSettings["ScheduleInterval"]);
                _queryRunner.Interval = 5000;
                _queryRunner.Elapsed += _queryRunner_Elapsed; ;

                MongoDbDal.MongoDB_Initialize();

                try
                {
                    List<TableCopier> configSettings = MongoDbDal.GetListOfTableCopiers();

                    foreach (var tab in configSettings)
                    {
                        tab.ImportNextBatch();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }


                _queryRunner.Start();
            }
            catch (Exception ex)
            {

                Log.Error(ex);
            }
        }

        private void _queryRunner_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
           
            if (timerElapsedEventInProgress == false)
            {
                timerElapsedEventInProgress = true;
                timerTriggerCount = 0;

                try
                {
                    List<TableCopier> configSettings = MongoDbDal.GetListOfTableCopiers();

                    foreach (var tab in configSettings)
                    {
                        tab.ImportNextBatch();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
            else
            {
                timerTriggerCount++;
                if (timerTriggerCount >= maxNumberOfRetries)
                {
                    timerTriggerCount = 0;
                    timerElapsedEventInProgress = false;
                }
            }
        }

        protected override void OnStop()
        {
            Log.Info("****Attempting to stop the Service****");
        }
    }
}
