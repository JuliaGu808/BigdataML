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
    public static class RegisterSensor
    {
        private static string iotHub = Environment.GetEnvironmentVariable("IotHub");
        private static RegistryManager registryManager = RegistryManager.CreateFromConnectionString(iotHub);

        [FunctionName("RegisterSensor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Device device;
            //string mac = req.Query["deviceName"];
            dynamic data = JsonConvert.DeserializeObject(await new StreamReader(req.Body).ReadToEndAsync());
            string mac = data.deviceName;
            if (mac != null)
            {
                if (mac.Length == 17)
                {
                    device = await registryManager.GetDeviceAsync(mac);
                    if (device == null)
                        device = await registryManager.AddDeviceAsync(new Device(mac));
                    if (device.Id == mac)
                        return new OkObjectResult($"{iotHub.Split(";")[0]};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}");
                }
            }
            return new BadRequestObjectResult("deviceid must be a valid mac-address (eg. 0f:0f:0f:0f:0f:0f)");
        }
    }
}

