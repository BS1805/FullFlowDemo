using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNative.CloudEvents;
using Newtonsoft.Json.Linq;

namespace FullFlowDemo
{
    public class MessageProcessor
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _telemetryQueueUrl;
        private readonly CloudEventParser _cloudEventParser;
        private readonly TelemetryService _telemetryService;

        public MessageProcessor(IAmazonSQS sqsClient, string telemetryQueueUrl)
        {
            _sqsClient = sqsClient;
            _telemetryQueueUrl = telemetryQueueUrl;
            _cloudEventParser = new CloudEventParser();
            _telemetryService = new TelemetryService(_sqsClient, _telemetryQueueUrl);
        }

        /// <summary>
        /// Processes a single message by determining its type and delegating to the appropriate handler.
        /// Deletes the message after successful processing.
        /// </summary>
        public async Task ProcessMessageAsync(string messageBody, string receiptHandle, string queueUrl)
        {
            try
            {
                var cloudEvent = _cloudEventParser.Parse(messageBody);
                var type = cloudEvent.Type;

                Console.WriteLine($"[*] Message type: {type}");

                switch (type)
                {
                    case "com.evergen.energy.onboarding-request.v1":
                        await HandleOnboardingRequestAsync(cloudEvent);
                        break;

                    case "com.evergen.energy.battery-inverter.command.v1":
                        await HandleBatteryCommandAsync(cloudEvent);
                        break;

                    case "com.evergen.energy.offboarding-request.v1":
                        await HandleOffboardingRequestAsync(cloudEvent);
                        break;

                    case "com.evergen.energy.onboarding-response.v1":
                        HandleOnboardingResponse(cloudEvent);
                        break;

                    case "com.evergen.energy.offboarding-response.v1":
                        HandleOffboardingResponse(cloudEvent);
                        break;

                    default:
                        Console.WriteLine($"[!] Unknown message type: {type}");
                        break;
                }

                await DeleteMessageAsync(receiptHandle, queueUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error processing message: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a message from the SQS queue.
        /// </summary>
        private async Task DeleteMessageAsync(string receiptHandle, string queueUrl)
        {
            try
            {
                await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = receiptHandle
                });

                Console.WriteLine("[*] Message deleted from the queue.");
            }
            catch (ReceiptHandleIsInvalidException ex)
            {
                Console.WriteLine($"[!] Invalid receipt handle: {receiptHandle}");
                Console.WriteLine($"[!] Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error deleting message: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles an onboarding request by sending a response and starting the telemetry loop.
        /// </summary>
        private async Task HandleOnboardingRequestAsync(CloudEvent cloudEvent)
        {
            var onboardingRequestData = _cloudEventParser.DeserializeData<OnboardingRequestV1Data>(cloudEvent.Data);
            Console.WriteLine($"[<] OnboardingRequest for serial: {onboardingRequestData?.serialNumber}");

            await _telemetryService.SendOnboardingResponseAsync(onboardingRequestData?.serialNumber);
            _ = Task.Run(() => _telemetryService.StartTelemetryLoopAsync());
        }

        /// <summary>
        /// Handles an offboarding request by stopping the telemetry loop.
        /// </summary>
        private async Task HandleOffboardingRequestAsync(CloudEvent cloudEvent)
        {
            var offboardingRequestData = _cloudEventParser.DeserializeData<OffboardingRequestV1Data>(cloudEvent.Data);
            Console.WriteLine($"[<] OffboardingRequest for serial: {offboardingRequestData?.serialNumber}");

            await _telemetryService.SendOffboardingResponseAsync(offboardingRequestData?.serialNumber);
            _telemetryService.StopTelemetryLoop();
        }

        /// <summary>
        /// Handles a battery command by deserializing the command data and processing it.
        /// </summary>
        private async Task HandleBatteryCommandAsync(CloudEvent cloudEvent)
        {
            var commandData = _cloudEventParser.DeserializeData<CommandV1Data>(cloudEvent.Data);
            var command = CreateCommand(cloudEvent, commandData);

            Console.WriteLine($"[*] Deserialized command data - DeviceId: {commandData?.deviceId}, DurationSeconds: {commandData?.durationSeconds}");
            await ProcessBatteryCommand(command);
        }

        private void HandleOnboardingResponse(CloudEvent cloudEvent)
        {
            var onboardingResponse = _cloudEventParser.DeserializeData<OnboardingResponseV1>(cloudEvent.Data);
            Console.WriteLine($"[<] OnboardingResponse received for serial: {onboardingResponse?.data?.serialNumber}, status: {onboardingResponse?.data?.connectionStatus}");

        }

        private void HandleOffboardingResponse(CloudEvent cloudEvent)
        {
            var offboardingResponse = _cloudEventParser.DeserializeData<OffboardingResponseV1>(cloudEvent.Data);
            Console.WriteLine($"[<] OffboardingResponse received for serial: {offboardingResponse?.data?.serialNumber}, status: {offboardingResponse?.data?.connectionStatus}");
            
        }

        /// <summary>
        /// Creates a CommandV1 object from the CloudEvent and its data.
        /// </summary>
        private CommandV1 CreateCommand(CloudEvent cloudEvent, CommandV1Data commandData)
        {
            return new CommandV1
            {
                specversion = cloudEvent.SpecVersion?.ToString() ?? "1.0",
                type = cloudEvent.Type,
                source = cloudEvent.Source?.ToString(),
                id = cloudEvent.Id,
                time = cloudEvent.Time?.ToString("o"),
                datacontenttype = cloudEvent.DataContentType,
                data = commandData
            };
        }

        /// <summary>
        /// Processes the battery command by handling its realMode and durationSeconds properties.
        /// </summary>
        private async Task ProcessBatteryCommand(CommandV1 command)
        {
            if (command?.data == null)
            {
                Console.WriteLine("[!] Invalid command message: Command or data is null.");
                return;
            }

            Console.WriteLine($"[>] Handling BatteryCommand for device: {command.data.deviceId}");

            HandleRealMode(command.data.realMode);
            HandleDurationSeconds(command.data.durationSeconds);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Handles the realMode property of the battery command.
        /// </summary>
        private void HandleRealMode(RealModeCommand realMode)
        {
            if (realMode == null)
            {
                Console.WriteLine("[!] realMode is missing in the command.");
                return;
            }

            // Check for specific command types in realMode
            if (realMode.chargeCommand != null)
            {
                Console.WriteLine($"    ChargeCommand: {realMode.chargeCommand.powerW}W");
            }
            else if (realMode.dischargeCommand != null)
            {
                Console.WriteLine($"    DischargeCommand: {realMode.dischargeCommand.powerW}W");
            }
            else if (realMode.selfConsumptionCommand != null)
            {
                Console.WriteLine($"    SelfConsumptionCommand");
            }
            else if (realMode.chargeOnlySelfConsumptionCommand != null)
            {
                Console.WriteLine($"    ChargeOnlySelfConsumptionCommand");
            }
            else
            {
                Console.WriteLine("[!] Unknown realMode command type.");
            }
        }

        /// <summary>
        /// Handles the durationSeconds property of the battery command.
        /// </summary>
        private void HandleDurationSeconds(int? durationSeconds)
        {
            if (durationSeconds.HasValue)
            {
                Console.WriteLine($"    durationSeconds: {durationSeconds.Value}");
            }
            else
            {
                Console.WriteLine("[!] durationSeconds is missing in the command.");
            }
        }
    }
}
