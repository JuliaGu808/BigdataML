using System;
using System.Collections.Generic;
using System.Text;

namespace AzureFunction.Models
{
    public class DhtMeasurement
    {
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public int MeasureTime { get; set; }
        public string TemperatureAlert { get; set; }
    }
}
