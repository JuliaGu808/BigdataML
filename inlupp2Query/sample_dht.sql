CREATE TABLE SampleDHT(
	Id bigint not null identity(1,1) primary key,
	Temperature float not null,
	Humidity float not null,
	TemperatureAlertStatus nvarchar(50) not null,
	HumidityAlertStatus nvarchar(50) not null
)

SELECT * FROM SampleDHT

SELECT 
Id AS Id, 
FORMAT(Temperature, 'N', 'en-US') AS Temperature, 
Humidity AS Humidity, 
TemperatureAlertStatus AS TemperatureAlertStatus, 
HumidityAlertStatus AS HumidityAlertStatus 
FROM SampleDHT

DECLARE @temperature float SET @temperature=11.2
DECLARE @humidity float SET @humidity=39.9
DECLARE @temperatureAlertStatus nvarchar(50) SET @temperatureAlertStatus='true'
DECLARE @humidityAlertStatus nvarchar(50) SET @humidityAlertStatus='false'
INSERT INTO SampleDHT VALUES(@temperature, @humidity, @temperatureAlertStatus, @humidityAlertStatus);

DELETE FROM SampleDHT
DBCC CHECKIDENT('SampleDHT', RESEED,1)