using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KPCLCsvFileReader
{
    public class AuditLog
    {


        public static int SaveAuditLog(int LogId, DateTime last_update, int no_of_record, string Remark, int Status, string jsondata, string fromdate, string todate , int secondsTaken)
        {

            using var connKWRIS = new SqlConnection("Server=103.171.96.233,5022;User ID=kwrisuser;Password=S_Admin@Kwr!$@2025;Database=ACIWRM_Lang;MultipleActiveResultSets=True;TrustServerCertificate=true;");
            int success = 0;
            string FD = "";
            string TD = "";
            try
            {
                
                if (!string.IsNullOrEmpty(fromdate) && !string.IsNullOrEmpty(todate))
                {
                    FD = DateTime.ParseExact(fromdate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd").Replace("/", "-");
                    TD = DateTime.ParseExact(todate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd").Replace("/", "-");
                    if(FD== "0001-01-01")
                    {
                        FD = "1999-01-01";
                    }
                }
                else
                {
                    FD = null;
                    TD = null;
                }
                connKWRIS.Open();

                SqlParameter[] parameters = new SqlParameter[]
            {
                    new  SqlParameter ("@LogId",LogId),
                    new  SqlParameter ("@Last_update",last_update),
                    new  SqlParameter ("@No_of_record",no_of_record),
                    new  SqlParameter ("@Remarks",Remark),
                    new  SqlParameter ("@Status",Status),
                    new  SqlParameter ("@json_values",jsondata),
                    new  SqlParameter ("@fromDate",FD),
                    new  SqlParameter ("@toDate",TD),
                    new  SqlParameter ("@total_time",secondsTaken),
            };

                SqlCommand cmd = new SqlCommand("Proc_lwm_apilog", connKWRIS);
                cmd.CommandType = CommandType.StoredProcedure;
                foreach (SqlParameter param in parameters)
                {
                    cmd.Parameters.Add(param);
                }

                success = cmd.ExecuteNonQuery();
                connKWRIS.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save audit log: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                // Optional: Log to a monitoring system or a log file
            }

            return success;
        }




    }
}
