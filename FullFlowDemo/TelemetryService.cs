using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;

namespace FullFlowDemo
{
    public class TelemetryService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _telemetryQueueUrl;

        public TelemetryService(IAmazonSQS sqsClient, string telemetryQueueUrl)
        {
            _sqsClient = sqsClient;
            _telemetryQueueUrl = telemetryQueueUrl;
        }

        public async Task SendOnboardingResponseAsync(string serialNumber)
        {
            var onboardingResponse = new OnboardingResponseV1
            {
                specversion = "1.0",
                type = "com.evergen.energy.onboarding-response.v1",
                source = "urn:example:oem",
                id = Guid.NewGuid().ToString(),
                time = DateTimeOffset.UtcNow.ToString("o"),
                datacontenttype = "application/json",
                data = new OnboardingResponseV1Data
                {
                    serialNumber = serialNumber ?? "SN123",
                    deviceId = "Device123",
                    connectionStatus = "connected"
                }
            };

            var cloudEvent = new CloudEvent
            {
                Id = onboardingResponse.id,
                Type = onboardingResponse.type,
                Source = new Uri(onboardingResponse.source),
                Time = DateTimeOffset.Parse(onboardingResponse.time),
                DataContentType = onboardingResponse.datacontenttype,
                Data = onboardingResponse
            };

            var formatter = new JsonEventFormatter();
            var jsonBytes = formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
            var json = Encoding.UTF8.GetString(jsonBytes.ToArray());

            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _telemetryQueueUrl,
                MessageBody = json
            });

            Console.WriteLine("[>] Sent OnboardingResponse");
        }


        public async Task SendOffboardingResponseAsync(string serialNumber)
        {
            var offboardingResponse = new OffboardingResponseV1
            {
                specversion = "1.0",
                type = "com.evergen.energy.offboarding-response.v1",
                source = "urn:example:oem",
                id = Guid.NewGuid().ToString(),
                time = DateTimeOffset.UtcNow.ToString("o"),
                datacontenttype = "application/json",
                data = new OffboardingResponseV1Data
                {
                    serialNumber = serialNumber ?? "SN123",
                    deviceId = "Device123",
                    connectionStatus = "not-connected"
                }
            };

            var cloudEvent = new CloudEvent
            {
                Id = offboardingResponse.id,
                Type = offboardingResponse.type,
                Source = new Uri(offboardingResponse.source),
                Time = DateTimeOffset.Parse(offboardingResponse.time),
                DataContentType = offboardingResponse.datacontenttype,
                Data = offboardingResponse
            };

            var formatter = new JsonEventFormatter();
            var jsonBytes = formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
            var json = Encoding.UTF8.GetString(jsonBytes.ToArray());

            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _telemetryQueueUrl,
                MessageBody = json
            });

            Console.WriteLine("[>] Sent OffboardingResponse");
        }

        public async Task SendTelemetryAsync()
        {
            var telemetryData = new TelemetryV1Data
            {
                siteId = "SiteABC",
                batteryInverters = new List<BatteryInverter>
           {
               new BatteryInverter
               {
                   deviceId = "Device123",
                   deviceTime = DateTimeOffset.UtcNow.ToString("o"),
                   batteryPowerW = 100,
                   meterPowerW = 0,
                   solarPowerW = 0,
                   batteryReactivePowerVar = 0,
                   gridVoltage1V = 230,
                   gridFrequencyHz = 50,
                   cumulativeBatteryChargeEnergyWh = 0,
                   cumulativeBatteryDischargeEnergyWh = 0,
                   stateOfCharge = 1,
                   stateOfHealth = 1,
                   maxChargePowerW = 100,
                   maxDischargePowerW = 100
               }
           }
            };

            var cloudEvent = new CloudEvent
            {
                Id = Guid.NewGuid().ToString(),
                Type = "com.evergen.energy.telemetry.v1",
                Source = new Uri("urn:example:oem"),
                Time = DateTimeOffset.UtcNow,
                DataContentType = "application/json",
                Data = telemetryData
            };

            var formatter = new JsonEventFormatter();
            var jsonBytes = formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
            var json = Encoding.UTF8.GetString(jsonBytes.ToArray());

            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _telemetryQueueUrl,
                MessageBody = json
            });

            Console.WriteLine("[*] Telemetry sent: " + json);
        }


        public async Task StartTelemetryLoopAsync()
        {
            var formatter = new JsonEventFormatter();

            while (true)
            {
                var telemetryData = new TelemetryV1Data
                {
                    siteId = "SiteABC",
                    batteryInverters = new List<BatteryInverter>
                    {
                        new BatteryInverter
                        {
                            deviceId = "Device123",
                            deviceTime = DateTimeOffset.UtcNow.ToString("o"),
                            batteryPowerW = 100,
                            meterPowerW = 0,
                            solarPowerW = 0,
                            batteryReactivePowerVar = 0,
                            gridVoltage1V = 230,
                            gridFrequencyHz = 50,
                            cumulativeBatteryChargeEnergyWh = 0,
                            cumulativeBatteryDischargeEnergyWh = 0,
                            stateOfCharge = 1,
                            stateOfHealth = 1,
                            maxChargePowerW = 100,
                            maxDischargePowerW = 100
                        }
                    }
                };

                var cloudEvent = new CloudEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "com.evergen.energy.telemetry.v1",
                    Source = new Uri("urn:example:oem"),
                    Time = DateTimeOffset.UtcNow,
                    DataContentType = "application/json",
                    Data = telemetryData
                };

                var jsonBytes = formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
                var json = Encoding.UTF8.GetString(jsonBytes.ToArray());

                await _sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = _telemetryQueueUrl,
                    MessageBody = json
                });

                Console.WriteLine("[*] Telemetry sent: " + json);
                await Task.Delay(60_000); // every 60 seconds
            }
        }
    }
}
