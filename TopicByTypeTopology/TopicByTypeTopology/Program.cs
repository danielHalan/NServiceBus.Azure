using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Messages;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace TopicByTypeTopology
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateTopics(ConnectionStrings.PrimaryNamespace, ConnectionStrings.SecondaryNamespace);

            Console.WriteLine("Done");
            Console.ReadLine();

        }

        private static void CreateTopics(string primaryNamespace, string secondaryNamespace)
        {
            CreateTopicsFor(typeof(LeafMessage), primaryNamespace, secondaryNamespace);
            CreateTopicsFor(typeof(SecondLeafMessage), primaryNamespace, secondaryNamespace);
        }

        private static void CreateTopicsFor(Type type, string primaryNamespace, string secondaryNamespace)
        {
            var primaryNamespaceManager = NamespaceManager.CreateFromConnectionString(primaryNamespace);
            var secondaryNamespaceManager = NamespaceManager.CreateFromConnectionString(secondaryNamespace);

            var hierarchy = GetAllTypes(type);
            Type toSubscribe = null;

            foreach (var t in hierarchy)
            {
                var d = true;

                if (!primaryNamespaceManager.TopicExists(t.Name))
                {
                    d = false;
                    primaryNamespaceManager.CreateTopic(t.Name);
                    Console.WriteLine("Created topic {0} in namespace {1}", t.Name, primaryNamespace);
                }
                else
                {
                    Console.WriteLine("Topic {0} already exists in namespace {1}", t.Name, primaryNamespace);
                }

                if (!secondaryNamespaceManager.TopicExists(t.Name))
                {
                    d = false;
                    secondaryNamespaceManager.CreateTopic(t.Name);
                    Console.WriteLine("Created topic {0} in namespace {1}", t.Name, secondaryNamespace);
                }
                else
                {
                    Console.WriteLine("Topic {0} already exists in namespace {1}", t.Name, secondaryNamespace);
                }

                if (toSubscribe != null)
                {
                    if (!primaryNamespaceManager.SubscriptionExists(toSubscribe.Name, t.Name))
                    {
                        var desc = new SubscriptionDescription(toSubscribe.Name, t.Name) {ForwardTo = t.Name};
                        primaryNamespaceManager.CreateSubscription(desc);
                        Console.WriteLine("Created subscription {0} on topic {1} in namespace {2}", t.Name, toSubscribe.Name, secondaryNamespace);
                    }
                    else
                    {
                        Console.WriteLine("Subscription {0} on topic {1} already exists in namespace {2}", t.Name, toSubscribe.Name, secondaryNamespace);
                    }

                    AddFilterFor(primaryNamespace, t, primaryNamespaceManager, toSubscribe);

                    if (!secondaryNamespaceManager.SubscriptionExists(toSubscribe.Name, t.Name))
                    {
                        var desc = new SubscriptionDescription(toSubscribe.Name, t.Name) { ForwardTo = t.Name };
                        secondaryNamespaceManager.CreateSubscription(desc);
                        Console.WriteLine("Created subscription {0} on topic {1} in namespace {2}", t.Name, toSubscribe.Name, secondaryNamespace);
                    }
                    else
                    {
                        Console.WriteLine("Subscription {0} on topic {1} already exists in namespace {2}", t.Name, toSubscribe.Name, secondaryNamespace);
                    }

                    AddFilterFor(secondaryNamespace, t, secondaryNamespaceManager, toSubscribe);
                }

                toSubscribe = t;

                //if (d) return;
            }
        }

        private static void AddFilterFor(string ns, Type t, NamespaceManager namespaceManager,
            Type toSubscribe)
        {
            var s = string.Format("[{0}] LIKE '{1}%' OR [{0}] LIKE '%{1}%' OR [{0}] LIKE '%{1}' OR [{0}] = '{1}'",
                "EnclosedMessageTypes", t.FullName);

            var rules = namespaceManager.GetRules(toSubscribe.Name, t.Name);
            var messagingFactory = MessagingFactory.CreateFromConnectionString(ns);
            var subscriptionClient = messagingFactory.CreateSubscriptionClient(toSubscribe.Name, t.Name);

            AddFilter(rules, subscriptionClient, s);
        }

        private static void AddFilter(IEnumerable<RuleDescription> rules, SubscriptionClient subscriptionClient, string s)
        {
            if (rules.Any(r => r.Name == "$Default"))
            {
                subscriptionClient.RemoveRule("$Default");

                subscriptionClient.AddRule("TypeFilter", new SqlFilter(s));
            }
            else
            {
                subscriptionClient.RemoveRule("TypeFilter");
                subscriptionClient.AddRule("TypeFilter", new SqlFilter(s));
            }

            Console.WriteLine("Filter added to subscription");
        }

        private static IList<Type> GetAllTypes(Type type)
        {
            var types = new List<Type>();
            var t = type;
            while (t != null && t != typeof(object))
            {
                types.Add(t);
                t = t.BaseType;
            }
            return types;
        }
    }
}
