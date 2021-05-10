SET DATEFIRST 1
SET DATEFORMAT ymd
SET LANGUAGE Swedish

SELECT * FROM TimeTableReal
SELECT * FROM DhtMessurementsReal
SELECT * FROM DeviceModelsReal
SELECT * FROM DeviceVendorsReal
SELECT * FROM DeviceTypesReal
SELECT * FROM DevicesReal
SELECT * FROM GeoLocationsReal
SELECT * FROM TemperatureAlertsReal
SELECT * FROM HumidityAlertsReal

DECLARE @DeviceName nvarchar(50) SET @DeviceName='A8:03:2A:EA:C9:84'
DECLARE @MeasureUnixTime int SET @MeasureUnixTime=1617488126
DECLARE @Scored nvarchar(50) SET @Scored='test'

IF NOT EXISTS (SELECT Id FROM SuperviseTemperatureAlertsReal WHERE DeviceName=@DeviceName AND MeasureUnixTime=@MeasureUnixTime) 
INSERT INTO SuperviseTemperatureAlertsReal OUTPUT inserted.Id VALUES(@DeviceName,@MeasureUnixTime,@Scored) 
ELSE SELECT Id FROM SuperviseTemperatureAlertsReal WHERE DeviceName=@DeviceName AND MeasureUnixTime=@MeasureUnixTime

DELETE FROM DhtMessurementsReal
DELETE FROM TimeTableReal
DELETE FROM DevicesReal
DELETE FROM DeviceModelsReal
DELETE FROM GeoLocationsReal
DELETE FROM DeviceVendorsReal
DELETE FROM DeviceTypesReal
DELETE FROM TemperatureAlertsReal
DELETE FROM HumidityAlertsReal

UPDATE DhtMessurementsReal SET DhtMessurementsReal.SuperviseTemperatureAlert='TEST' FROM DhtMessurementsReal WHERE Id=1 AND MeasureUnixTime=1617476511

