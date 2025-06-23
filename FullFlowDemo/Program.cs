using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;

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

            Console.WriteLine("[*] Waiting for OnboardingRequest...");

            while (true)
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = commandsQueueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10
                };

                var response = await sqsClient.ReceiveMessageAsync(receiveRequest);

                if (response.Messages == null)
                {
                    Console.WriteLine("[!] No messages received or an error occurred (Messages is null).");
                    continue;
                }

                Console.WriteLine($"Received {response.Messages.Count} message(s).");

                foreach (var msg in response.Messages)
                {
                    Console.WriteLine($"[<] Received message on commands-demo: {msg.Body}");

                    try
                    {
                        var jsonDoc = JsonDocument.Parse(msg.Body);
                        var root = jsonDoc.RootElement;
                        string type = root.GetProperty("type").GetString();

                        if (type == "onboarding-request")
                        {
                            await SendOnboardingResponse(sqsClient, telemetryQueueUrl);
                            _ = Task.Run(() => StartTelemetryLoop(sqsClient, telemetryQueueUrl));
                        }
                        else if (type == "battery-inverter.command.v1")
                        {
                            await HandleBatteryCommand(root);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[!] Error parsing message: {e.Message}");
                    }

                    await sqsClient.DeleteMessageAsync(commandsQueueUrl, msg.ReceiptHandle);
                }
            }
        }

        static async Task SendOnboardingResponse(IAmazonSQS sqsClient, string telemetryQueueUrl)
        {
            var payload = new
            {
                serialNumber = "SN123",
                status = "Connected"
            };

            var cloudEvent = new CloudEvent
            {
                Id = Guid.NewGuid().ToString(),
                Type = "com.evergen.energy.onboarding-response.v1",
                Source = new Uri("urn:example:oem"),
                Time = DateTimeOffset.UtcNow,
                DataContentType = "application/json",
                Data = payload
            };

            var formatter = new JsonEventFormatter();
            var jsonBytes = formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes.ToArray());

            await sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = telemetryQueueUrl,
                MessageBody = json
            });

            Console.WriteLine("[>] Sent OnboardingResponse");
        }


        static async Task StartTelemetryLoop(IAmazonSQS sqsClient, string telemetryQueueUrl)
        {
            while (true)
            {
                var telemetryPayload = new
                {
                    siteID = "SiteABC",
                    batteryPowerW = 100
                };

                var cloudEvent = new CloudEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "com.evergen.energy.telemetry.v1",
                    Source = new Uri("urn:example:oem"),
                    Time = DateTimeOffset.UtcNow,
                    DataContentType = "application/json",
                    Data = telemetryPayload
                };

                var formatter = new JsonEventFormatter();
                var json = JsonSerializer.Serialize(cloudEvent);

                await sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = telemetryQueueUrl,
                    MessageBody = json
                });

                Console.WriteLine("[*] Telemetry sent: " + json);
                await Task.Delay(60_000); // every 60 seconds
            }
        }

        static async Task HandleBatteryCommand(JsonElement root)
        {
            string command = root.GetProperty("command").GetString();
            int value = root.GetProperty("value").GetInt32();

            Console.WriteLine($"[>] Received battery command: {command}, value={value}");

            if (command == "start")
            {
                Console.WriteLine($"[ACTION] Starting inverter at value: {value}");
            }
            else if (command == "stop")
            {
                Console.WriteLine("[ACTION] Stopping inverter");
            }
            else
            {
                Console.WriteLine("[ACTION] Unknown command");
            }
        }

    }
}
