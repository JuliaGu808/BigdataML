using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using Microsoft.Azure.Devices;
using AzureFunction.Models;
using System.Data.SqlClient;

namespace AzureFunction
{
    public static class AddDevice
    {
        private static string iotHub = Environment.GetEnvironmentVariable("IotHub");
        private static RegistryManager registryManager = RegistryManager.CreateFromConnectionString(iotHub);

        [FunctionName("AddDevice")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
           
            var connectionstring = await AddDeviceAsync(JsonConvert.DeserializeObject<RegisterDevice>(await new StreamReader(req.Body).ReadToEndAsync()), log);

            if (connectionstring != "")
                return new OkObjectResult(connectionstring);
            else
                return new BadRequestObjectResult("deviceid must be a valid mac-address (eg. 0f:0f:0f:0f:0f:0f)");
        }

        public static async Task<string> AddtoSqlAsync(RegisterDevice data, ILogger log)
        {
            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnection")))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("", conn))
                {
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM TemperatureAlerts WHERE Status=@TemperatureAlertStatus) INSERT INTO TemperatureAlerts OUTPUT inserted.Id VALUES(@TemperatureAlertStatus) ELSE SELECT Id FROM TemperatureAlerts WHERE Status=@TemperatureAlertStatus";
                    cmd.Parameters.AddWithValue("@TemperatureAlertStatus", data.TemperatureAlertStatus);
                    var temperatureAlertId = int.Parse(cmd.ExecuteScalar().ToString());
                    
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM HumidityAlerts WHERE Status=@HumidityAlertStatus) INSERT INTO HumidityAlerts OUTPUT inserted.Id VALUES(@HumidityAlertStatus) ELSE SELECT Id FROM HumidityAlerts WHERE Status=@HumidityAlertStatus";
                    cmd.Parameters.AddWithValue("@HumidityAlertStatus", data.HumidityAlertStatus);
                    var humidityAlertId = int.Parse(cmd.ExecuteScalar().ToString());
                   
                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceVendors WHERE VendorName=@Vendor) INSERT INTO DeviceVendors OUTPUT inserted.Id VALUES(@Vendor) ELSE SELECT Id FROM DeviceVendors WHERE VendorName=@Vendor";
                    cmd.Parameters.AddWithValue("@Vendor", data.Vendor);
                    var vendorId =int.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceModels WHERE ModelName=@ModelName) INSERT INTO DeviceModels OUTPUT inserted.Id VALUES(@ModelName, @VendorId) ELSE SELECT Id FROM DeviceModels WHERE ModelName=@ModelName";
                    cmd.Parameters.AddWithValue("@ModelName", data.Model);
                    cmd.Parameters.AddWithValue("@VendorId", vendorId);               
                    var modelId = int.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM DeviceTypes WHERE TypeName=@TypeName) INSERT INTO DeviceTypes OUTPUT inserted.Id VALUES(@TypeName) ELSE SELECT Id FROM DeviceTypes WHERE TypeName=@TypeName";
                    cmd.Parameters.AddWithValue("@TypeName", data.Type);
                    var deviceTypeId = cmd.ExecuteScalar().ToString();

                    cmd.CommandText = "IF NOT EXISTS (SELECT Id FROM GeoLocations WHERE Latitude=@Latitude AND Longitude=@Longitude) INSERT INTO GeoLocations OUTPUT inserted.Id VALUES(@Latitude, @Longitude) ELSE SELECT Id FROM GeoLocations WHERE Latitude=@Latitude AND Longitude=@Longitude";
                    cmd.Parameters.AddWithValue("@Latitude", data.Latitude);
                    cmd.Parameters.AddWithValue("@Longitude", data.Longitude);
                    var geoLocationId = long.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "IF NOT EXISTS (SELECT DeviceName FROM Devices WHERE DeviceName=@DeviceName) INSERT INTO Devices OUTPUT inserted.DeviceName VALUES(@DeviceName, @DeviceTypeId, @GeoLocationId, @ModelId) ELSE SELECT DeviceName FROM Devices WHERE DeviceName=@DeviceName";
                    cmd.Parameters.AddWithValue("@DeviceName", data.DeviceName);
                    cmd.Parameters.AddWithValue("@DeviceTypeId", deviceTypeId);
                    cmd.Parameters.AddWithValue("@GeoLocationId", geoLocationId);
                    cmd.Parameters.AddWithValue("@ModelId ", modelId);
                    var deviceName = cmd.ExecuteScalar().ToString();
                    
                    cmd.CommandText = "SELECT Id FROM Devices WHERE DeviceName=@DeviceName";
                    var deviceId = long.Parse(cmd.ExecuteScalar().ToString());

                    cmd.CommandText = "INSERT INTO DhtMessurements OUTPUT inserted.Id VALUES(@DeviceId, @MeasurementTime, @Temperature, @Humidity,@TemperatureAlertId,@HumidityAlertId)";
                    cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                    cmd.Parameters.AddWithValue("@MeasurementTime", data.Unixutctime);
                    cmd.Parameters.AddWithValue("@Temperature", data.Temperature);
                    cmd.Parameters.AddWithValue("@Humidity", data.Humidity);
                    cmd.Parameters.AddWithValue("@TemperatureAlertId", temperatureAlertId);
                    cmd.Parameters.AddWithValue("@HumidityAlertId", humidityAlertId);
                    var uselessId = long.Parse(cmd.ExecuteScalar().ToString());
                    return deviceName;
                    //cmd.ExecuteNonQuery();
                    
                }

            }
        }

        public static async Task<string> AddDeviceAsync(RegisterDevice data, ILogger log)
        {
            Device device;
            if (data.DeviceName != null)
            {
                if (data.DeviceName.Length == 17)
                {
                    try
                    {
                        var deviceName = await AddtoSqlAsync(data, log);
                        try
                        {
                            device = await registryManager.GetDeviceAsync(data.DeviceName);
                            if(device == null)
                                device = await registryManager.AddDeviceAsync(new Device(data.DeviceName));
                            if(device.Id==data.DeviceName)
                                return $"{iotHub.Split(";")[0]};Device={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
                        }
                        catch{}
                        
                    }
                    catch{}
                }
            }
            return "";
        }
    }
}

