void initDHT() {
  pinMode(DHT_PIN, INPUT);
  dht.begin();
}

void sendDhtMessage() {
  current_temperature = dht.readTemperature();
  humidity = dht.readHumidity();
  
  if ((currentMillis - PREV_DHT_MILLIS) >= DHT_INTERVAL 
        && !messagePending 
        && !std::isnan(current_temperature) && !std::isnan(humidity) 
        && beginTime > 28800 
        && (beginTime<endTime)) 
  {
    PREV_DHT_MILLIS = currentMillis;
    messagePending = true;
    
    char payload[MESSAGE_LEN_MAX];    
    DynamicJsonDocument doc(sizeof(payload));
    
    doc["deviceName"] = deviceId;
    doc["temperature"] = current_temperature;
    doc["humidity"] = humidity;   
    doc["unixutctime"] = beginTime++;
    doc["temperatureAlertStatus"] = current_temperature > 28 ? "true" : "false"; //1false,2true
    doc["humidityAlertStatus"] = humidity > 60 ? "true" : "false"; //1false,2true
    serializeJson(doc, payload);
    sentAzureIothub(payload);
  }
}
