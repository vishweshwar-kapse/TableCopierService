using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Vegam_Database_Table_CopierService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Table_Copier_Service()
            };
            ServiceBase.Run(ServicesToRun);

            ////uncomment below code and comment above codes for debugging
            //Table_Copier_Service service = new Table_Copier_Service();
            //service.Run();
            //System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        }  
    }
}
