using System;
using System.Collections.Generic;
using System.Text;
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

            var messageProcessor = new MessageProcessor(sqsClient, telemetryQueueUrl);

            Console.WriteLine("[*] Waiting for OnboardingRequest or BatteryCommand...");

            while (true)
            {
                var messages = await ReceiveMessagesAsync(sqsClient, commandsQueueUrl);

                foreach (var msg in messages)
                {
                    Console.WriteLine($"[<] Received message on commands-demo: {msg.Body}");

                    try
                    {
                        await messageProcessor.ProcessMessageAsync(msg.Body);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[!] Error processing message: {e.Message}");
                        Console.WriteLine($"[!] Stack trace: {e.StackTrace}");
                    }

                    await DeleteMessageAsync(sqsClient, commandsQueueUrl, msg.ReceiptHandle);
                }
            }
        }

        private static async Task<List<Message>> ReceiveMessagesAsync(IAmazonSQS sqsClient, string queueUrl)
        {
            var receiveRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 10
            };

            var response = await sqsClient.ReceiveMessageAsync(receiveRequest);
            return response.Messages ?? new List<Message>();
        }

        private static async Task DeleteMessageAsync(IAmazonSQS sqsClient, string queueUrl, string receiptHandle)
        {
            try
            {
                await sqsClient.DeleteMessageAsync(queueUrl, receiptHandle);
                Console.WriteLine("[*] Message deleted from queue");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[!] Error deleting message: {e.Message}");
            }
        }
    }
}
