CREATE TABLE DimDeviceReal(
	DeviceKey bigint not null primary key,
	DeviceName nvarchar(50) null,
	DeviceType nvarchar(50) null,
	Model nvarchar(50) null,
	Vendor nvarchar(50) null,
	Latitude nvarchar(50) null,
	Longitude nvarchar(50) null
)

CREATE TABLE DimTemperatureAlertReal(
	TemperatureAlertKey int not null primary key,
	Status nvarchar(50) null
)

CREATE TABLE DimHumidityAlertReal(
	HumidityAlertKey int not null primary key,
	Status nvarchar(50) null
)

CREATE TABLE DimDateTimeReal(
	[UnixUtcTimeKey]	INT PRIMARY KEY,
	[UtcDateTime]	AS DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00'),
	[Date]			AS CONVERT(DATE, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[Year]			AS DATEPART(YEAR, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[Quarter]		AS DATEPART(QUARTER, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[QuarterName]	AS CONVERT(CHAR(2), CASE DATEPART(QUARTER, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')) WHEN 1 THEN 'Q1' WHEN 2 THEN 'Q2' WHEN 3 THEN 'Q3' WHEN 4 THEN 'Q4' END),
	[Month]			AS DATEPART(MONTH, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[MonthName]		AS DATENAME(MONTH, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[WeekdayOfMonth]AS DATEPART(DAY, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[WeekdayName]	AS DATENAME(WEEKDAY, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[DayOfWeek]		AS DATEPART(WEEKDAY, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[DayOfYear]		AS DATEPART(DAYOFYEAR, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[Hour]			AS DATEPART(HOUR, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[Minute]		AS DATEPART(MINUTE, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
	[Second]		AS DATEPART(SECOND, DATEADD(S, [UnixUtcTimeKey], '1970-01-01 00:00:00')),
)
GO

CREATE TABLE FactDhtMeasurementsReal(
	Id bigint not null identity(1,1) primary key,
	Device bigint not null references DimDeviceReal(DeviceKey),
	MeasureTime int not null references DimDateTimeReal(UnixUtcTimeKey),
	TemperatureAlert int not null references DimTemperatureAlertReal(TemperatureAlertKey),
	HumidityAlert int not null references DimHumidityAlertReal(HumidityAlertKey),
	Temperature float null,
	Humidity float null,
	SuperviseTemperatureAlert nvarchar(50) null,
	UnsuperviseTemperatureAlert nvarchar(50) null
)

DELETE FROM FactDhtMeasurementsReal
DBCC CHECKIDENT('FactDhtMeasurementsReal', RESEED,1)

DELETE FROM DimDeviceReal
DELETE FROM DimTemperatureAlertReal
DELETE FROM DimHumidityAlertReal
DELETE FROM DimDateTimeReal

SELECT * FROM DimDeviceReal
SELECT * FROM DimTemperatureAlertReal
SELECT * FROM DimHumidityAlertReal
SELECT * FROM DimDateTimeReal
SELECT * FROM FactDhtMeasurementsReal
