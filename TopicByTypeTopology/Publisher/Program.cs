using System;
using System.Collections.Generic;
using System.Linq;
using Messages;
using Microsoft.ServiceBus.Messaging;

namespace Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to publish a message or x to quit");
            var key = Console.ReadLine();

            var primaryFactory = MessagingFactory.CreateFromConnectionString(ConnectionStrings.PrimaryNamespace);
            var secondaryFactory = MessagingFactory.CreateFromConnectionString(ConnectionStrings.SecondaryNamespace);

            var rand = new Random();

            while ("x" != key)
            {
                var factory = rand.Next(0, 100) > 50 ? primaryFactory : secondaryFactory;
                
                var evt = CreateRandomEvent(rand);

                var client = factory.CreateTopicClient(evt.GetType().Name);

                var brokeredMessage = new BrokeredMessage(evt);
                brokeredMessage.Properties["EnclosedMessageTypes"] = GetEnclosedMessageTypes(evt);

                client.Send(brokeredMessage);

                Console.WriteLine("Message published, press any key to publish another message or x to quit");
                key = Console.ReadLine();
            }
        }

        private static string GetEnclosedMessageTypes(BaseMessage evt)
        {
            var types = new List<Type>();
            var t = evt.GetType();
            while (t != null && t != typeof(object))
            {
                types.Add(t);
                t = t.BaseType;
            }
            return String.Join(",", types.Select(x => x.FullName));
        }

        private static BaseMessage CreateRandomEvent(Random rand)
        {
            var i = rand.Next(0, 100);
            if(i > 75)
                return new SecondLeafMessage();
            if(i > 50)
                return new LeafMessage();
            if(i > 25)
                return new MiddleMessage();
            else
                return new BaseMessage();
        }
    }
}
