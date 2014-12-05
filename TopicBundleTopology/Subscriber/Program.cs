using System;
using System.Collections.Generic;
using System.Linq;
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
            CreateSubscriptionFor(typeof(BaseMessage), "partition", primaryNamespace, secondaryNamespace, id);
        }

        private static void CreateSubscriptionFor(Type type, string prefix, string primaryNamespace, string secondaryNamespace, string id)
        {
            var primaryNamespaceManager = NamespaceManager.CreateFromConnectionString(primaryNamespace);
            var secondaryNamespaceManager = NamespaceManager.CreateFromConnectionString(secondaryNamespace);

            SubscribeTo(type, prefix, primaryNamespace, id, primaryNamespaceManager);
            SubscribeTo(type, prefix, secondaryNamespace, id, secondaryNamespaceManager);
        }

        private static void SubscribeTo(Type type, string prefix, string ns, string id, NamespaceManager namespaceManager)
        {
            var topics = namespaceManager.GetTopics().Where(t => t.Path.StartsWith(prefix));

            foreach (var topic in topics)
            {
                if (!namespaceManager.SubscriptionExists(topic.Path, id))
                {
                    var desc = new SubscriptionDescription(topic.Path, id) {ForwardTo = id};
                    namespaceManager.CreateSubscription(desc);
                    Console.WriteLine("Created subscription {0} on topic {1} in namespace {2}", id, topic.Path, ns);
                }
                else
                {
                    Console.WriteLine("Subscription {0} on topic {1} already exists in namespace {2}", id, type.Name,
                        ns);
                }

                AddFilterFor(ns, type, namespaceManager, topic.Path, id);

            }
        }

        private static void AddFilterFor(string ns, Type t, NamespaceManager namespaceManager, string path, string id)
        {
            var s = string.Format("[{0}] LIKE '{1}%' OR [{0}] LIKE '%{1}%' OR [{0}] LIKE '%{1}' OR [{0}] = '{1}'", "EnclosedMessageTypes", t.FullName);

            var rules = namespaceManager.GetRules(path, id);
            var messagingFactory = MessagingFactory.CreateFromConnectionString(ns);
            var subscriptionClient = messagingFactory.CreateSubscriptionClient(path, id);

            AddFilter(rules, subscriptionClient, s, t.FullName);
        }

        private static void AddFilter(IEnumerable<RuleDescription> rules, SubscriptionClient subscriptionClient, string s, string n)
        {
            if (rules.Any(r => r.Name == "$Default"))
            {
                subscriptionClient.RemoveRule("$Default");

                subscriptionClient.AddRule(n, new SqlFilter(s));
            }
            else
            {
                subscriptionClient.RemoveRule(n);
                subscriptionClient.AddRule(n, new SqlFilter(s));
            }

            Console.WriteLine("Filter for {0} added to subscription", n);
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
