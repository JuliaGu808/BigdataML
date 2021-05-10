using System;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AzureFunction
{
    public static class SBsampleQueue
    {
        [FunctionName("SBsampleQueue")]
        public static void Run([ServiceBusTrigger("samplesbqueue", Connection = "sampleSBqueue")]string myQueueItem, ILogger log)
        {
            JObject obj = JObject.Parse(myQueueItem);
            float temperature = float.Parse(obj["temperature"].ToString());
            float humidity = float.Parse(obj["humidity"].ToString());
            string temperatureAlertStatus = obj["temperatureAlertStatus"].ToString();
            string humidityAlertStatus = obj["humidityAlertStatus"].ToString();
            //log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionReal")))
            {
                var _sampleSQL = "INSERT INTO SampleDHT VALUES(@temperature, @humidity, @temperatureAlertStatus, @humidityAlertStatus)";
                conn.Open();
                using (var cmd = new SqlCommand(_sampleSQL, conn))
                {
                    cmd.Parameters.AddWithValue("@temperature", temperature);
                    cmd.Parameters.AddWithValue("@humidity", humidity);
                    cmd.Parameters.AddWithValue("@temperatureAlertStatus", temperatureAlertStatus);
                    cmd.Parameters.AddWithValue("@humidityAlertStatus", humidityAlertStatus);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
