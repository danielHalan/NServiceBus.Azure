using System;
using System.Threading;
using Messages;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Subscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            var id = "subscriber";
            CreateQueueFor(ConnectionStrings.PrimaryNamespace, ConnectionStrings.SecondaryNamespace, id);
            CreateSubscriptions(ConnectionStrings.PrimaryNamespace, ConnectionStrings.SecondaryNamespace, id);
            
            Console.WriteLine("Ready, start receiving, hit any key to stop");

            StartReceiving(ConnectionStrings.PrimaryNamespace, ConnectionStrings.SecondaryNamespace, id);

            Console.ReadLine();
        }

        private static void StartReceiving(string primaryNamespace, string secondaryNamespace, string id)
        {
            var primaryFactory = MessagingFactory.CreateFromConnectionString(primaryNamespace);
            var secondaryFactory = MessagingFactory.CreateFromConnectionString(secondaryNamespace);

            var client1 = primaryFactory.CreateQueueClient(id);
            client1.OnMessage(Callback);

            var client2 = secondaryFactory.CreateQueueClient(id);
            client2.OnMessage(Callback);
        }

        private static void Callback(BrokeredMessage brokeredMessage)
        {
            Console.WriteLine("Received message of type {0}", brokeredMessage.Properties["EnclosedMessageTypes"]);
        }

        private static void CreateSubscriptions(string primaryNamespace, string secondaryNamespace, string id)
        {
            CreateSubscriptionFor(typeof(BaseMessage), primaryNamespace, secondaryNamespace, id);
        }

        private static void CreateSubscriptionFor(Type type, string primaryNamespace, string secondaryNamespace, string id)
        {
            var primaryNamespaceManager = NamespaceManager.CreateFromConnectionString(primaryNamespace);
            var secondaryNamespaceManager = NamespaceManager.CreateFromConnectionString(secondaryNamespace);

            if (!primaryNamespaceManager.SubscriptionExists(type.Name, id))
            {
                var desc = new SubscriptionDescription(type.Name, id) {ForwardTo = id};
                primaryNamespaceManager.CreateSubscription(desc);
                Console.WriteLine("Created subscription {0} on topic {1} in namespace {2}", id, type.Name, secondaryNamespace);
            }
            else
            {
                Console.WriteLine("Subscription {0} on topic {1} already exists in namespace {2}", id, type.Name, secondaryNamespace);
            }

            if (!secondaryNamespaceManager.SubscriptionExists(type.Name, id))
            {
                var desc = new SubscriptionDescription(type.Name, id) { ForwardTo = id };
                secondaryNamespaceManager.CreateSubscription(desc);
                Console.WriteLine("Created subscription {0} on topic {1} in namespace {2}", id, type.Name, secondaryNamespace);
            }
            else
            {
                Console.WriteLine("Subscription {0} on topic {1} already exists in namespace {2}", id, type.Name, secondaryNamespace);
            }
        }

        private static void CreateQueueFor(string primaryNamespace, string secondaryNamespace, string id)
        {
            var primaryNamespaceManager = NamespaceManager.CreateFromConnectionString(primaryNamespace);
            var secondaryNamespaceManager = NamespaceManager.CreateFromConnectionString(secondaryNamespace);

            if (!primaryNamespaceManager.QueueExists(id))
            {
                primaryNamespaceManager.CreateQueue(id);
                Console.WriteLine("Created queue {0} in namespace {1}", id, primaryNamespace);
            }
            else
            {
                Console.WriteLine("Queue {0} already exists in namespace {1}", id, primaryNamespace);
            }

            if (!secondaryNamespaceManager.QueueExists(id))
            {
                secondaryNamespaceManager.CreateQueue(id);
                Console.WriteLine("Created queue {0} in namespace {1}", id, secondaryNamespace);
            }
            else
            {
                Console.WriteLine("Queue {0} already exists in namespace {1}", id, secondaryNamespace);
            }
        }
    }
}
