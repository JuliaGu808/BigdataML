void initDHT() {
  pinMode(DHT_PIN, INPUT);
  dht.begin();
}

void sendDhtMessage() {
  current_temperature = random(1500,3500)/100.0;
  humidity = random(30,70);

  if ((currentMillis - PREV_DHT_MILLIS) >= DHT_INTERVAL 
        && !messagePending 
        && !std::isnan(current_temperature) && !std::isnan(humidity) 
        && beginTime > 28800) 
  {
    PREV_DHT_MILLIS = currentMillis;
    messagePending = true;
    Serial.println("send");
    char payload[MESSAGE_LEN_MAX];    
    DynamicJsonDocument doc(sizeof(payload));
    
    doc["deviceName"] = deviceId;
    doc["temperature"] = current_temperature;
    doc["humidity"] = humidity;   
    doc["unixutctime"] = beginTime++;
    doc["temperatureAlertStatus"] = temperatureRules(current_temperature, humidity); //1false,2true
    doc["humidityAlertStatus"] = humidityRules(humidity); //1false,2true
    serializeJson(doc, payload);
    sentAzureIothub(payload);
  }
}

String temperatureRules(float temp_value, float humi_value){
  if(temp_value<20 || temp_value>28) return "true";
  if(humi_value<40 || humi_value>60) return "true";
  if((temp_value>=20&&temp_value<=28) && (humi_value>=40&&humi_value<=60)) return "false";
  else return "true";
}

String humidityRules(float humi_value){
  if(humi_value<38 || humi_value>65) return "true";
  else return "false";
}
