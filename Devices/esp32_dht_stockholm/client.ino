void registerSensorConn() {
  http.begin(REGISTER_CONN);
  http.addHeader("Content-Type", "application/json");
  deviceId = WiFi.macAddress();

  String payload = "{\"deviceName\":\"" + deviceId + "\"}";
  

  int httpResponseCode = http.POST(payload);
  
  if (httpResponseCode == 200) {
    String response = http.getString();  //Get the response to the request
    connectionString = const_cast<char*>(response.c_str());
    initIotHub();
  } else {
    Serial.print("Error on getting Hub connection: ");
    connectToHub = false;
  }
  // Free resources
  http.end();
}

void generateTimeTable(){
  if(connectToHub){
   // while(httpResponseCode>0 && !hasTimeTable){
      sendToPopulateTimeTable();
   // }
  } 
}

void sendToPopulateTimeTable() {
  beginTime = getTime();
 // Serial.println(beginTime);
  String serverPath = TIME_SERVERNAME + "?unixutctime=" + beginTime;
  http.begin(serverPath.c_str());
  httpResponseCode = http.GET();
  if (httpResponseCode == 200) {
    String payload = http.getString();
    hasTimeTable=true;
    tempTime = payload;
    endTime = beginTime+60;
    Serial.println("TIMETABLE OK.");
    Serial.println(beginTime);
    Serial.println(endTime);
    Serial.println(payload);
  }
  // Free resources
  http.end();
}
