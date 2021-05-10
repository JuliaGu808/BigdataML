using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AzureFunction.Models;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureFunction
{
    public class StringTable
    {
        public string[] ColumnNames { get; set; }
        public string[,] Values { get; set; }
    }

    public static class ViewRealDHTeventhub
    {
        [FunctionName("ViewRealDHTeventhub")]
        
        public static async Task AddtoSqlAsync(RegisterDevice data, EventData eventData, ILogger log)
        {
            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionReal")))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("", conn))
                {
                    log.LogInformation($"{data.TemperatureAlertStatus}");
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM TemperatureAlertsReal WHERE Status=@TemperatureAlertStatus) INSERT INTO TemperatureAlertsReal OUTPUT inserted.Id VALUES(@TemperatureAlertStatus) ELSE SELECT Id FROM TemperatureAlertsReal WHERE Status=@TemperatureAlertStatus";
                    cmd.Parameters.AddWithValue("@TemperatureAlertStatus", data.TemperatureAlertStatus);
                    var temperatureAlertId = int.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM HumidityAlertsReal WHERE Status=@HumidityAlertStatus) INSERT INTO HumidityAlertsReal OUTPUT inserted.Id VALUES(@HumidityAlertStatus) ELSE SELECT Id FROM HumidityAlertsReal WHERE Status=@HumidityAlertStatus";
                    cmd.Parameters.AddWithValue("@HumidityAlertStatus", data.HumidityAlertStatus);
                    var humidityAlertId = int.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceVendorsReal WHERE VendorName=@Vendor) INSERT INTO DeviceVendorsReal OUTPUT inserted.Id VALUES(@Vendor) ELSE SELECT Id FROM DeviceVendorsReal WHERE VendorName=@Vendor";
                    cmd.Parameters.AddWithValue("@Vendor", eventData.Properties["vendor"]);
                    var vendorId = int.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceModelsReal WHERE ModelName=@ModelName) INSERT INTO DeviceModelsReal OUTPUT inserted.Id VALUES(@ModelName, @VendorId) ELSE SELECT Id FROM DeviceModelsReal WHERE ModelName=@ModelName";
                    cmd.Parameters.AddWithValue("@ModelName", eventData.Properties["model"]);
                    cmd.Parameters.AddWithValue("@VendorId", vendorId);
                    var modelId = int.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceTypesReal WHERE TypeName=@TypeName) INSERT INTO DeviceTypesReal OUTPUT inserted.Id VALUES(@TypeName) ELSE SELECT Id FROM DeviceTypesReal WHERE TypeName=@TypeName";
                    cmd.Parameters.AddWithValue("@TypeName", eventData.Properties["type"]);
                    var deviceTypeId = cmd.ExecuteScalar().ToString();

                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM GeoLocationsReal WHERE Latitude=@Latitude AND Longitude=@Longitude) INSERT INTO GeoLocationsReal OUTPUT inserted.Id VALUES(@Latitude, @Longitude) ELSE SELECT Id FROM GeoLocationsReal WHERE Latitude=@Latitude AND Longitude=@Longitude";
                    cmd.Parameters.AddWithValue("@Latitude", eventData.Properties["latitude"]);
                    cmd.Parameters.AddWithValue("@Longitude", eventData.Properties["longitude"]);
                    var geoLocationId = long.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "IF NOT EXISTS (SELECT DeviceName FROM DevicesReal WHERE DeviceName=@DeviceName) INSERT INTO DevicesReal OUTPUT inserted.DeviceName VALUES(@DeviceName, @DeviceTypeId, @GeoLocationId, @ModelId) ELSE SELECT DeviceName FROM DevicesReal WHERE DeviceName=@DeviceName";
                    cmd.Parameters.AddWithValue("@DeviceName", data.DeviceName);
                    cmd.Parameters.AddWithValue("@DeviceTypeId", deviceTypeId);
                    cmd.Parameters.AddWithValue("@GeoLocationId", geoLocationId);
                    cmd.Parameters.AddWithValue("@ModelId ", modelId);
                    var deviceName = cmd.ExecuteScalar().ToString();

                    cmd.CommandText = "SELECT Id FROM DevicesReal WHERE DeviceName=@DeviceName";
                    var deviceId = long.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "INSERT INTO DhtMessurementsReal OUTPUT inserted.Id VALUES(@DeviceId, @MeasurementTime, @Temperature, @Humidity,@TemperatureAlertId,@HumidityAlertId,@SuperviseTemperatureAlert)";
                    cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                    cmd.Parameters.AddWithValue("@MeasurementTime", data.Unixutctime);
                    cmd.Parameters.AddWithValue("@Temperature", data.Temperature);
                    cmd.Parameters.AddWithValue("@Humidity", data.Humidity);
                    cmd.Parameters.AddWithValue("@TemperatureAlertId", temperatureAlertId);
                    cmd.Parameters.AddWithValue("@HumidityAlertId", humidityAlertId);
                    cmd.Parameters.AddWithValue("@SuperviseTemperatureAlert", "");
                    var uselessId = long.Parse(cmd.ExecuteScalar().ToString());

                }

            }
        }



        static async Task InvokeRequestResponseService(float temperature, float humidity)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {

                    Inputs = new Dictionary<string, StringTable>() {
                        {
                            "input1",
                            new StringTable()
                            {
                                ColumnNames = new string[] {"Temperature", "Humidity"},
                                Values = new string[,] {  { temperature.ToString(), humidity.ToString() },  }
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>()
                    {
                    }
                };
                const string apiKey = "XXX"; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                client.BaseAddress = new Uri("AzureML");

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    JObject obj = JObject.Parse(result);
                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                }
            }
        }
    }
}
