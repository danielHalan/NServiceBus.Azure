using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using NServiceBus.Serialization;

namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using NServiceBus.Transports;
    using Unicast;
    using Unicast.Queuing;

    /// <summary>
    /// 
    /// </summary>
    public class AzureMessageQueueSender : ISendMessages
    {
        readonly Configure config;
        private static readonly Dictionary<string, CloudQueueClient> destinationQueueClients = new Dictionary<string, CloudQueueClient>();
        private static readonly Dictionary<string, bool> rememberExistance = new Dictionary<string, bool>();
        private static readonly object SenderLock = new Object();
        private static readonly object ExistanceLock = new Object();

        /// <summary>
        /// Gets or sets the message serializer
        /// </summary>
        public IMessageSerializer MessageSerializer { get; set; }

        public CloudQueueClient Client { get; set; }

        public AzureMessageQueueSender(Configure config)
        {
            this.config = config;
        }

        public async void Send(TransportMessage message, SendOptions options)
        {
           var address = options.Destination;

            var sendClient = GetClientForConnectionString(address.Machine) ?? Client;

            var sendQueue = sendClient.GetQueueReference(AzureMessageQueueUtils.GetQueueName(address));

            if (!Exists(sendQueue))
                throw new QueueNotFoundException { Queue = address };

            var timeToBeReceived = options.TimeToBeReceived.HasValue && options.TimeToBeReceived < TimeSpan.MaxValue ? options.TimeToBeReceived : null;

            var rawMessage = SerializeMessage(message, options);

            if (!config.Settings.Get<bool>("Transactions.Enabled") || Transaction.Current == null)
            {
                //sendQueue.AddMessage(rawMessage, timeToBeReceived);
                await sendQueue.AddMessageAsync(rawMessage, timeToBeReceived, null, new QueueRequestOptions(), new OperationContext() ).ConfigureAwait(false);

            }
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(sendQueue, rawMessage, timeToBeReceived), EnlistmentOptions.None);
        }

        bool Exists(CloudQueue sendQueue)
        {
            var key = sendQueue.Uri.ToString();
            bool exists;
            if (!rememberExistance.ContainsKey(key))
            {
                lock (ExistanceLock)
                {
                    exists = sendQueue.Exists();
                    rememberExistance[key] = exists;
                }
            }
            else
            {
                 exists = rememberExistance[key];
            }

            return exists;
        }

        private CloudQueueClient GetClientForConnectionString(string connectionString)
        {
            CloudQueueClient sendClient;

            var validation = new DeterminesBestConnectionStringForStorageQueues();
            if (!validation.IsPotentialStorageQueueConnectionString(connectionString))
            {
                connectionString = validation.Determine(config.Settings); 
            }

            if (!destinationQueueClients.TryGetValue(connectionString, out sendClient))
            {
                lock (SenderLock)
                {
                    if (!destinationQueueClients.TryGetValue(connectionString, out sendClient))
                    {
                        CloudStorageAccount account;

                        if (CloudStorageAccount.TryParse(connectionString, out account))
                        {
                            sendClient = account.CreateCloudQueueClient();
                        }

                        // sendClient could be null, this is intentional 
                        // so that it remembers a connectionstring was invald 
                        // and doesn't try to parse it again.

                        destinationQueueClients.Add(connectionString, sendClient);
                    }
                }
            }

            return sendClient;
        }

        private CloudQueueMessage SerializeMessage(TransportMessage message, SendOptions options)
        {
            using (var stream = new MemoryStream())
            {
                var validation = new DeterminesBestConnectionStringForStorageQueues();
                var replyToAddress = validation.Determine(config.Settings, message.ReplyToAddress ?? options.ReplyToAddress ?? Address.Local);

                var toSend = new MessageWrapper
                    {
                        Id = message.Id,
                        Body = message.Body,
                        CorrelationId = message.CorrelationId ?? options.CorrelationId,
                        Recoverable = message.Recoverable,
                        ReplyToAddress = replyToAddress,
                        TimeToBeReceived = options.TimeToBeReceived.HasValue ? options.TimeToBeReceived.Value : default(TimeSpan),
                        Headers = message.Headers,
                        MessageIntent = message.MessageIntent
                    };


                MessageSerializer.Serialize(toSend, stream);
                return new CloudQueueMessage(stream.ToArray());
            }
        }
    }
}