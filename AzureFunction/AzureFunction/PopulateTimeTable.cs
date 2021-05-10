using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunction
{
    public static class PopulateTimeTable
    {
        [FunctionName("PopulateTimeTable")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string unixUtcTime = req.Query["unixutctime"];
            long begin_time;// = long.Parse(unixUtcTime);
            long end_time; // = DateTimeOffset.Now.AddMinutes(1).ToUnixTimeSeconds();
            long max_time;
            log.LogInformation("ok...");
            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnection")))
            {
                await conn.OpenAsync();
                

                using (var cmd = new SqlCommand("", conn))
                {
                    if (unixUtcTime != null)
                    {
                        begin_time = long.Parse(unixUtcTime);
                    }
                    else
                    {
                        begin_time = DateTimeOffset.Now.ToUnixTimeSeconds();
                    }
                   
                    try
                    {
                        cmd.CommandText = "SELECT MAX(UnixUtcTime) FROM TimeTable";
                        max_time = long.Parse(cmd.ExecuteScalar().ToString());
                        if (begin_time < max_time)
                        {
                            begin_time = max_time;
                        }
                    }
                    catch {}
                    end_time = begin_time + 60; // one minute
                
                    while (begin_time < end_time)
                    {
                        cmd.CommandText = $"IF NOT EXISTS (SELECT 1 FROM TimeTable WHERE UnixUtcTime={begin_time}) INSERT INTO TimeTable VALUES ({begin_time})";
                       // cmd.Parameters.AddWithValue("@current", current);
                        cmd.ExecuteNonQuery();
                        begin_time++;
                    }
                }
            }
            return new OkObjectResult(end_time.ToString());
        }
    }
}

