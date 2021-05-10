using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
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
    public static class ViewDHTsaOutputEvent
    {
        [FunctionName("ViewDHTsaOutputEvent")]
        public static async Task Run([EventHubTrigger("realdhtoutputhub", Connection = "dhtoutputevent")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);                   
                    AddtoSqlAsync(messageBody, log).GetAwaiter();
                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }

        public static async Task AddtoSqlAsync(string data,  ILogger log)
        {
            //log.LogInformation($"{data}");
            JObject obj = JObject.Parse(data);

            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionReal")))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("", conn))
                {
                    string temperatureAlertStatus = (string)obj.SelectToken("temperatureAlertStatus");
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM TemperatureAlertsReal WHERE Status=@TemperatureAlertStatus) INSERT INTO TemperatureAlertsReal OUTPUT inserted.Id VALUES(@TemperatureAlertStatus) ELSE SELECT Id FROM TemperatureAlertsReal WHERE Status=@TemperatureAlertStatus";
                    cmd.Parameters.AddWithValue("@TemperatureAlertStatus", temperatureAlertStatus);
                    var temperatureAlertId = int.Parse(cmd.ExecuteScalar().ToString());

                    string humidityAlertStatus = (string)obj.SelectToken("humidityAlertStatus");
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM HumidityAlertsReal WHERE Status=@HumidityAlertStatus) INSERT INTO HumidityAlertsReal OUTPUT inserted.Id VALUES(@HumidityAlertStatus) ELSE SELECT Id FROM HumidityAlertsReal WHERE Status=@HumidityAlertStatus";
                    cmd.Parameters.AddWithValue("@HumidityAlertStatus", humidityAlertStatus);
                    var humidityAlertId = int.Parse(cmd.ExecuteScalar().ToString());

                    string vendor = obj.SelectToken("userprops")["vendor"].ToString();
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceVendorsReal WHERE VendorName=@Vendor) INSERT INTO DeviceVendorsReal OUTPUT inserted.Id VALUES(@Vendor) ELSE SELECT Id FROM DeviceVendorsReal WHERE VendorName=@Vendor";
                    cmd.Parameters.AddWithValue("@Vendor", vendor);
                    var vendorId = int.Parse(cmd.ExecuteScalar().ToString());

                    string model = obj.SelectToken("userprops")["model"].ToString();
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceModelsReal WHERE ModelName=@ModelName) INSERT INTO DeviceModelsReal OUTPUT inserted.Id VALUES(@ModelName, @VendorId) ELSE SELECT Id FROM DeviceModelsReal WHERE ModelName=@ModelName";
                    cmd.Parameters.AddWithValue("@ModelName", model);
                    cmd.Parameters.AddWithValue("@VendorId", vendorId);
                    var modelId = int.Parse(cmd.ExecuteScalar().ToString());

                    string type = obj.SelectToken("userprops")["type"].ToString();
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceTypesReal WHERE TypeName=@TypeName) INSERT INTO DeviceTypesReal OUTPUT inserted.Id VALUES(@TypeName) ELSE SELECT Id FROM DeviceTypesReal WHERE TypeName=@TypeName";
                    cmd.Parameters.AddWithValue("@TypeName", type);
                    var deviceTypeId = cmd.ExecuteScalar().ToString();

                    string latitude = obj.SelectToken("userprops")["latitude"].ToString();
                    string longitude = obj.SelectToken("userprops")["longitude"].ToString();
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM GeoLocationsReal WHERE Latitude=@Latitude AND Longitude=@Longitude) INSERT INTO GeoLocationsReal OUTPUT inserted.Id VALUES(@Latitude, @Longitude) ELSE SELECT Id FROM GeoLocationsReal WHERE Latitude=@Latitude AND Longitude=@Longitude";
                    cmd.Parameters.AddWithValue("@Latitude", latitude);
                    cmd.Parameters.AddWithValue("@Longitude", longitude);
                    var geoLocationId = long.Parse(cmd.ExecuteScalar().ToString());

                    string deviceName = (string)obj.SelectToken("deviceName");
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DevicesReal WHERE DeviceName=@DeviceName) INSERT INTO DevicesReal OUTPUT inserted.Id VALUES(@DeviceName, @DeviceTypeId, @GeoLocationId, @ModelId) ELSE SELECT Id FROM DevicesReal WHERE DeviceName=@DeviceName";
                    cmd.Parameters.AddWithValue("@DeviceName", deviceName);
                    cmd.Parameters.AddWithValue("@DeviceTypeId", deviceTypeId);
                    cmd.Parameters.AddWithValue("@GeoLocationId", geoLocationId);
                    cmd.Parameters.AddWithValue("@ModelId ", modelId);
                    var deviceId = long.Parse(cmd.ExecuteScalar().ToString());

                    var unixutctime = int.Parse(obj.SelectToken("unixutctime").ToString());
                    var temperatureAnomaly = int.Parse(obj.SelectToken("temperatureAnomaly")["IsAnomaly"].ToString()) == 0 ? "NO" : "YES";
                    cmd.CommandText = "INSERT INTO DhtMessurementsReal OUTPUT inserted.Id VALUES(@DeviceId, @MeasurementTime, @Temperature, @Humidity,@TemperatureAlertId,@HumidityAlertId,@SuperviseTemperatureAlert,@UnsuperviseTemperatureAlert)";
                    cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                    cmd.Parameters.AddWithValue("@MeasurementTime", unixutctime);
                    cmd.Parameters.AddWithValue("@Temperature", float.Parse(obj.SelectToken("result")["Temperature"].ToString(), CultureInfo.InvariantCulture));
                    cmd.Parameters.AddWithValue("@Humidity", float.Parse(obj.SelectToken("result")["Humidity"].ToString(), CultureInfo.InvariantCulture));
                    cmd.Parameters.AddWithValue("@TemperatureAlertId", temperatureAlertId);
                    cmd.Parameters.AddWithValue("@HumidityAlertId", humidityAlertId);
                    cmd.Parameters.AddWithValue("@SuperviseTemperatureAlert", obj.SelectToken("result")["Scored Labels"].ToString());
                    cmd.Parameters.AddWithValue("@UnsuperviseTemperatureAlert", temperatureAnomaly);
                    var uselessId = long.Parse(cmd.ExecuteScalar().ToString());
                    log.LogInformation($"ok...");
                }

            }

           
        }
    }
}
