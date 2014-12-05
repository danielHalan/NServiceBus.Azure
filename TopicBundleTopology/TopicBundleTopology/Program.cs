using System;
using Messages;
using Microsoft.ServiceBus;

namespace TopicBundleTopology
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
            CreateTopicsFor("partition", 2, primaryNamespace, secondaryNamespace);
        }

        private static void CreateTopicsFor(string prefix, int amount, string primaryNamespace, string secondaryNamespace)
        {
            var primaryNamespaceManager = NamespaceManager.CreateFromConnectionString(primaryNamespace);
            var secondaryNamespaceManager = NamespaceManager.CreateFromConnectionString(secondaryNamespace);

            for (int i = 0; i < amount; i++)
            {
                var name = prefix + "-" + i;

                if (!primaryNamespaceManager.TopicExists(name))
                {
                    primaryNamespaceManager.CreateTopic(name);
                    Console.WriteLine("Created topic {0} in namespace {1}", name, primaryNamespace);
                }
                else
                {
                    Console.WriteLine("Topic {0} already exists in namespace {1}", name, primaryNamespace);
                }

                if (!secondaryNamespaceManager.TopicExists(name))
                {
                    secondaryNamespaceManager.CreateTopic(name);
                    Console.WriteLine("Created topic {0} in namespace {1}", name, secondaryNamespace);
                }
                else
                {
                    Console.WriteLine("Topic {0} already exists in namespace {1}", name, secondaryNamespace);
                }
            }
        }

    }
}
