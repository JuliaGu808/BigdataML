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
    public static class PopulateTimeTableReal
    {
        [FunctionName("PopulateTimeTableReal")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string unixUtcTime = req.Query["unixutctime"];
            long begin_time;// = long.Parse(unixUtcTime);
            long end_time; // = DateTimeOffset.Now.AddMinutes(1).ToUnixTimeSeconds();
            long max_time, min_time;
            bool saveToTimeTable = true;
            log.LogInformation("ok...");
            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionReal")))
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
                        cmd.CommandText = "SELECT MAX(UnixUtcTime) FROM TimeTableReal";
                        max_time = long.Parse(cmd.ExecuteScalar().ToString());
                        cmd.CommandText = "SELECT MIN(UnixUtcTime) FROM TimeTableReal";
                        min_time = long.Parse(cmd.ExecuteScalar().ToString());
                    }
                    catch {
                        max_time = 0;
                        min_time = 0;
                    }
                    if (max_time != 0)
                    {
                        if (begin_time + 60 <= max_time && begin_time >= min_time)
                        {
                            saveToTimeTable = false;
                            end_time = 0;
                        }
                        else
                        {
                            if (begin_time <= max_time)
                            {
                                begin_time = max_time;
                                end_time = begin_time + 60; // one minute
                            }
                            else
                            {
                                end_time = begin_time + 60; // one minute
                                begin_time = max_time;
                            }
                        }                                                                                       
                    }
                    else
                    {
                        end_time = begin_time + 60;
                    }
                    

                    while (begin_time < end_time && saveToTimeTable)
                    {
                        cmd.CommandText = $"IF NOT EXISTS (SELECT 1 FROM TimeTableReal WHERE UnixUtcTime={begin_time}) INSERT INTO TimeTableReal VALUES ({begin_time})";
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

