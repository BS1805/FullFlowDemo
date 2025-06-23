// CloudEvents Spec Version
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class CloudEventsSpecVersion
{
    public string specversion { get; set; } // always "1.0"
}

// Telemetry Message Envelope
public class TelemetryV1
{
    public string specversion { get; set; }
    public string type { get; set; }
    public string source { get; set; }
    public string id { get; set; }
    public string time { get; set; }
    public string datacontenttype { get; set; }
    public TelemetryV1Data data { get; set; }
}

// Telemetry Payload
public class TelemetryV1Data
{
    public string siteId { get; set; }
    public List<BatteryInverter> batteryInverters { get; set; }
    public List<HybridInverter> hybridInverters { get; set; }
    public List<SolarInverter> solarInverters { get; set; }
    public List<Meter> meters { get; set; }
}

// Battery Inverter
public class BatteryInverter
{
    public string deviceId { get; set; }
    public string deviceTime { get; set; }
    public int batteryPowerW { get; set; }
    public int meterPowerW { get; set; }
    public int solarPowerW { get; set; }
    public int batteryReactivePowerVar { get; set; }
    public int? solarReactivePowerVar { get; set; }
    public int? meterReactivePowerVar { get; set; }
    public double gridVoltage1V { get; set; }
    public double? gridVoltage2V { get; set; }
    public double? gridVoltage3V { get; set; }
    public double gridFrequencyHz { get; set; }
    public double cumulativeBatteryChargeEnergyWh { get; set; }
    public double cumulativeBatteryDischargeEnergyWh { get; set; }
    public double? cumulativePvGenerationWh { get; set; }
    public double? cumulativeGridImportWh { get; set; }
    public double? cumulativeGridExportWh { get; set; }
    public double stateOfCharge { get; set; }
    public double stateOfHealth { get; set; }
    public int maxChargePowerW { get; set; }
    public int maxDischargePowerW { get; set; }
}

// Hybrid Inverter
public class HybridInverter
{
    public string deviceId { get; set; }
    public string deviceTime { get; set; }
    public int batteryPowerW { get; set; }
    public int meterPowerW { get; set; }
    public int solarPowerW { get; set; }
    public int batteryReactivePowerVar { get; set; }
    public int? meterReactivePowerVar { get; set; }
    public double gridVoltage1V { get; set; }
    public double? gridVoltage2V { get; set; }
    public double? gridVoltage3V { get; set; }
    public double gridFrequencyHz { get; set; }
    public double cumulativeBatteryChargeEnergyWh { get; set; }
    public double cumulativeBatteryDischargeEnergyWh { get; set; }
    public double? cumulativePvGenerationWh { get; set; }
    public double? cumulativeGridImportWh { get; set; }
    public double? cumulativeGridExportWh { get; set; }
    public double stateOfCharge { get; set; }
    public double stateOfHealth { get; set; }
    public int maxChargePowerW { get; set; }
    public int maxDischargePowerW { get; set; }
}

// Solar Inverter
public class SolarInverter
{
    public string deviceId { get; set; }
    public string deviceTime { get; set; }
    public int powerW { get; set; }
    public int reactivePowerVar { get; set; }
    public double gridVoltage1V { get; set; }
    public double? gridVoltage2V { get; set; }
    public double? gridVoltage3V { get; set; }
    public double? gridFrequencyHz { get; set; }
    public double cumulativeGenerationWh { get; set; }
}

// Meter
public class Meter
{
    public string deviceId { get; set; }
    public string deviceTime { get; set; }
    public int powerW { get; set; }
    public int reactivePowerVar { get; set; }
    public double gridVoltage1V { get; set; }
    public double? gridVoltage2V { get; set; }
    public double? gridVoltage3V { get; set; }
    public double gridFrequencyHz { get; set; }
    public double cumulativeGridImportWh { get; set; }
    public double cumulativeGridExportWh { get; set; }
}

// Command Message Envelope
public class CommandV1
{
    public string specversion { get; set; }
    public string type { get; set; }
    public string source { get; set; }
    public string id { get; set; }
    public string time { get; set; }
    public string datacontenttype { get; set; }
    public CommandV1Data data { get; set; }
}

// Command Payload
public class CommandV1Data
{
    public string deviceId { get; set; }
    public RealModeCommand realMode { get; set; }
    public ReactiveModeCommand reactiveMode { get; set; }
    public string startTime { get; set; }
    public int? durationSeconds { get; set; }
}



public class RealModeCommand
{
    public ChargeProperty chargeCommand { get; set; }
    public DischargeProperty dischargeCommand { get; set; }
    public SelfConsumptionCommand selfConsumptionCommand { get; set; }
    public ChargeOnlySelfConsumptionCommand chargeOnlySelfConsumptionCommand { get; set; }
}



// Reactive Mode Command
public class ReactiveModeCommand
{
    public PowerFactorCorrection powerFactorCorrection { get; set; }
    public Inject inject { get; set; }
    public Absorb absorb { get; set; }
}

public class PowerFactorCorrection
{
    public double targetPowerFactor { get; set; }
}

public class Inject
{
    public int reactivePowerVar { get; set; }
}

public class Absorb
{
    public int reactivePowerVar { get; set; }
}

// RealMode Command Variants
public class SelfConsumptionCommand
{
    public object selfConsumptionCommand { get; set; }
}

public class ChargeOnlySelfConsumptionCommand
{
    public object chargeOnlySelfConsumptionCommand { get; set; }
}

public class ChargeCommand
{
    public ChargeProperty chargeCommand { get; set; }
}
public class DischargeCommand
{
    public DischargeProperty dischargeCommand { get; set; }
}


public class ChargeProperty
{
    public int powerW { get; set; }
}

public class DischargeProperty
{
    public int powerW { get; set; }
}

// OnboardingRequest Message Envelope
public class OnboardingRequestV1
{
    public string specversion { get; set; }
    public string type { get; set; }
    public string source { get; set; }
    public string id { get; set; }
    public string time { get; set; }
    public string datacontenttype { get; set; }
    public OnboardingRequestV1Data data { get; set; }
}

public class OnboardingRequestV1Data
{
    public string serialNumber { get; set; }
}

// OffboardingRequest Message Envelope
public class OffboardingRequestV1
{
    public string specversion { get; set; }
    public string type { get; set; }
    public string source { get; set; }
    public string id { get; set; }
    public string time { get; set; }
    public string datacontenttype { get; set; }
    public OffboardingRequestV1Data data { get; set; }
}

public class OffboardingRequestV1Data
{
    public string serialNumber { get; set; }
}

// OnboardingResponse Message Envelope
public class OnboardingResponseV1
{
    public string specversion { get; set; }
    public string type { get; set; }
    public string source { get; set; }
    public string id { get; set; }
    public string time { get; set; }
    public string datacontenttype { get; set; }
    public OnboardingResponseV1Data data { get; set; }
}

public class OnboardingResponseV1Data
{
    public string serialNumber { get; set; }
    public string deviceId { get; set; }
    public string connectionStatus { get; set; }
    public string errorReason { get; set; }
    public SiteStaticDataV1 siteStaticData { get; set; }
}

// OffboardingResponse Message Envelope
public class OffboardingResponseV1
{
    public string specversion { get; set; }
    public string type { get; set; }
    public string source { get; set; }
    public string id { get; set; }
    public string time { get; set; }
    public string datacontenttype { get; set; }
    public OffboardingResponseV1Data data { get; set; }
}

public class OffboardingResponseV1Data
{
    public string serialNumber { get; set; }
    public string deviceId { get; set; }
    public string connectionStatus { get; set; }
}

// Site Static Data
public class SiteStaticDataV1
{
    public string siteId { get; set; }
    public string uniqueMeterIdentifier { get; set; }
    public string country { get; set; }
    public string distributionNetworkOperator { get; set; }
    public string state { get; set; }
    public string postcode { get; set; }
    public string address { get; set; }
    public int? exportLimitW { get; set; }
    public List<BatteryStaticData> batteriesStaticData { get; set; }
    public List<BatteryInverterStaticData> batteryInvertersStaticData { get; set; }
    public List<HybridInverterStaticData> hybridInvertersStaticData { get; set; }
    public List<SolarInverterStaticData> solarInvertersStaticData { get; set; }
    public List<MeterStaticData> metersStaticData { get; set; }
}

// Battery Static Data
public class BatteryStaticData
{
    public string deviceId { get; set; }
    public string serialNumber { get; set; }
    public string manufacturer { get; set; }
    public string model { get; set; }
    public string firmware { get; set; }
    public int nameplateEnergyCapacityWh { get; set; }
    public int maxChargePowerW { get; set; }
    public int maxDischargePowerW { get; set; }
    public int cumulativeBatteryChargeEnergyWh { get; set; }
    public int cumulativeBatteryDischargeEnergyWh { get; set; }
}

// Battery Inverter Static Data
public class BatteryInverterStaticData
{
    public string deviceId { get; set; }
    public string serialNumber { get; set; }
    public string manufacturer { get; set; }
    public string model { get; set; }
    public string firmware { get; set; }
    public string installationDate { get; set; }
    public int batteryInverterAcCapacityW { get; set; }
    public int solarInverterAcCapacityW { get; set; }
    public List<string> connectedBatteryIds { get; set; }
}

// Hybrid Inverter Static Data
public class HybridInverterStaticData
{
    public string deviceId { get; set; }
    public string serialNumber { get; set; }
    public string manufacturer { get; set; }
    public string model { get; set; }
    public string firmware { get; set; }
    public string installationDate { get; set; }
    public int hybridInverterAcCapacityW { get; set; }
    public int solarArrayRatedDcOutputW { get; set; }
    public int solarInverterAcCapacityW { get; set; }
    public List<string> connectedBatteryIds { get; set; }
}

// Solar Inverter Static Data
public class SolarInverterStaticData
{
    public string deviceId { get; set; }
    public string serialNumber { get; set; }
    public string manufacturer { get; set; }
    public string model { get; set; }
    public string firmware { get; set; }
    public string installationDate { get; set; }
    public int solarInverterAcCapacityW { get; set; }
    public int solarArrayRatedDcOutputW { get; set; }
}

// Meter Static Data
public class MeterStaticData
{
    public string deviceId { get; set; }
    public string serialNumber { get; set; }
    public string manufacturer { get; set; }
    public string model { get; set; }
    public string firmware { get; set; }
    public int phase { get; set; }
    public bool hasControllableLoad { get; set; }
}
