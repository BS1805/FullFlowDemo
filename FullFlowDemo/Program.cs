using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FullFlowDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var region = RegionEndpoint.APSoutheast2;
            var endpoint = "http://localhost:4566";
            var telemetryQueueUrl = "http://localhost:4566/000000000000/telemetry-demo";
            var commandsQueueUrl = "http://localhost:4566/000000000000/commands-demo";

            var sqsClient = new AmazonSQSClient(
                new BasicAWSCredentials("test", "test"),
                new AmazonSQSConfig
                {
                    ServiceURL = endpoint,
                    AuthenticationRegion = region.SystemName
                });

            Console.WriteLine("[*] Waiting for OnboardingRequest or BatteryCommand...");

            var formatter = new JsonEventFormatter();

            while (true)
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = commandsQueueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10
                };

                var response = await sqsClient.ReceiveMessageAsync(receiveRequest);

                if (response.Messages == null || response.Messages.Count == 0)
                {
                    Console.WriteLine("[*] No messages received, continuing to poll...");
                    continue;
                }

                Console.WriteLine($"Received {response.Messages.Count} message(s).");

                foreach (var msg in response.Messages)
                {
                    Console.WriteLine($"[<] Received message on commands-demo: {msg.Body}");

                    try
                    {
                        // Parse as CloudEvent
                        var cloudEvent = formatter.DecodeStructuredModeMessage(
                            Encoding.UTF8.GetBytes(msg.Body), null, null);

                        var type = cloudEvent.Type;
                        Console.WriteLine($"[*] Message type: {type}");

                        if (type == "com.evergen.energy.onboarding-request.v1")
                        {
                            // Handle onboarding request with proper JsonElement handling
                            OnboardingRequestV1Data onboardingRequestData = null;

                            if (cloudEvent.Data is System.Text.Json.JsonElement jsonElement)
                            {
                                var dataJson = jsonElement.GetRawText();
                                Console.WriteLine($"[*] Raw OnboardingRequest Data: {dataJson}");
                                onboardingRequestData = JsonConvert.DeserializeObject<OnboardingRequestV1Data>(dataJson);
                            }
                            else if (cloudEvent.Data is JObject jObject)
                            {
                                var dataJson = jObject.ToString();
                                Console.WriteLine($"[*] Raw OnboardingRequest Data: {dataJson}");
                                onboardingRequestData = JsonConvert.DeserializeObject<OnboardingRequestV1Data>(dataJson);
                            }
                            else
                            {
                                var dataJson = JsonConvert.SerializeObject(cloudEvent.Data);
                                Console.WriteLine($"[*] Raw OnboardingRequest Data (Fallback): {dataJson}");
                                onboardingRequestData = JsonConvert.DeserializeObject<OnboardingRequestV1Data>(dataJson);
                            }

                            Console.WriteLine($"[<] OnboardingRequest for serial: {onboardingRequestData?.serialNumber}");

                            await SendOnboardingResponse(sqsClient, telemetryQueueUrl, onboardingRequestData?.serialNumber);

                            _ = Task.Run(() => StartTelemetryLoop(sqsClient, telemetryQueueUrl));
                        }
                        else if (type == "com.evergen.energy.battery-inverter.command.v1")
                        {
                            // Handle battery command - PROPER DESERIALIZATION
                            CommandV1Data commandData = null;

                            // Handle different data types that CloudEvent.Data might be
                            if (cloudEvent.Data is System.Text.Json.JsonElement jsonElement)
                            {
                                // Convert JsonElement to string, then deserialize
                                var dataJson = jsonElement.GetRawText();
                                Console.WriteLine($"[*] Raw CloudEvent Data (JsonElement): {dataJson}");
                                commandData = JsonConvert.DeserializeObject<CommandV1Data>(dataJson);
                            }
                            else if (cloudEvent.Data is JObject jObject)
                            {
                                // Handle JObject
                                var dataJson = jObject.ToString();
                                Console.WriteLine($"[*] Raw CloudEvent Data (JObject): {dataJson}");
                                commandData = JsonConvert.DeserializeObject<CommandV1Data>(dataJson);
                            }
                            else if (cloudEvent.Data is string dataString)
                            {
                                // Handle string data
                                Console.WriteLine($"[*] Raw CloudEvent Data (String): {dataString}");
                                commandData = JsonConvert.DeserializeObject<CommandV1Data>(dataString);
                            }
                            else
                            {
                                // Fallback - try direct conversion
                                var dataJson = JsonConvert.SerializeObject(cloudEvent.Data);
                                Console.WriteLine($"[*] Raw CloudEvent Data (Fallback): {dataJson}");
                                commandData = JsonConvert.DeserializeObject<CommandV1Data>(dataJson);
                            }

                            // Create the full command object for processing
                            var command = new CommandV1
                            {
                                specversion = cloudEvent.SpecVersion?.ToString() ?? "1.0",
                                type = cloudEvent.Type,
                                source = cloudEvent.Source?.ToString(),
                                id = cloudEvent.Id,
                                time = cloudEvent.Time?.ToString("o"),
                                datacontenttype = cloudEvent.DataContentType,
                                data = commandData
                            };

                            Console.WriteLine($"[*] Deserialized command data - DeviceId: {commandData?.deviceId}, DurationSeconds: {commandData?.durationSeconds}");

                            await HandleBatteryCommand(command);
                        }
                        else
                        {
                            Console.WriteLine($"[!] Unknown message type: {type}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[!] Error parsing message: {e.Message}");
                        Console.WriteLine($"[!] Stack trace: {e.StackTrace}");
                    }

                    // Delete the message after processing
                    try
                    {
                        await sqsClient.DeleteMessageAsync(commandsQueueUrl, msg.ReceiptHandle);
                        Console.WriteLine("[*] Message deleted from queue");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[!] Error deleting message: {e.Message}");
                    }
                }
            }
        }

        static async Task SendOnboardingResponse(IAmazonSQS sqsClient, string telemetryQueueUrl, string serialNumber)
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

            await sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = telemetryQueueUrl,
                MessageBody = json
            });

            Console.WriteLine("[>] Sent OnboardingResponse");
        }

        static async Task StartTelemetryLoop(IAmazonSQS sqsClient, string telemetryQueueUrl)
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

                await sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = telemetryQueueUrl,
                    MessageBody = json
                });

                Console.WriteLine("[*] Telemetry sent: " + json);
                await Task.Delay(60_000); // every 60 seconds
            }
        }

        static async Task HandleBatteryCommand(CommandV1 command)
        {
            if (command?.data == null)
            {
                Console.WriteLine("[!] Invalid command message: Command or data is null.");
                return;
            }

            Console.WriteLine($"[>] Handling BatteryCommand for device: {command.data.deviceId}");

            // Check for different command types in realMode
            if (command.data.realMode != null)
            {
                if (command.data.realMode.chargeCommand != null)
                {
                    Console.WriteLine($"    ChargeCommand: {command.data.realMode.chargeCommand.powerW}W");
                }
                else if (command.data.realMode.dischargeCommand != null)
                {
                    Console.WriteLine($"    DischargeCommand: {command.data.realMode.dischargeCommand.powerW}W");
                }
                else if (command.data.realMode.selfConsumptionCommand != null)
                {
                    Console.WriteLine($"    SelfConsumptionCommand");
                }
                else if (command.data.realMode.chargeOnlySelfConsumptionCommand != null)
                {
                    Console.WriteLine($"    ChargeOnlySelfConsumptionCommand");
                }
                else
                {
                    Console.WriteLine("[!] Unknown realMode command type.");
                }
            }
            else
            {
                Console.WriteLine("[!] realMode is missing in the command.");
            }

            if (command.data.durationSeconds.HasValue)
            {
                Console.WriteLine($"    durationSeconds: {command.data.durationSeconds.Value}");
            }
            else
            {
                Console.WriteLine("[!] durationSeconds is missing in the command.");
            }

            await Task.CompletedTask;
        }

    }
}
