﻿
namespace NMSPublisher
{
    using System;
    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.Util;

    using MyMessages;
    using MyMessages.Subscriber1;
    using MyMessages.SubscriberNMS;

    class Program
    {
        private static IQueue responseQueue;

        static void Main(string[] args)
        {
            var connectionFactory = new NetTxConnectionFactory("activemq:tcp://localhost:61616")
            {
                AcknowledgementMode = AcknowledgementMode.Transactional,
                PrefetchPolicy = { QueuePrefetch = 1, TopicPrefetch = 1, DurableTopicPrefetch = 1 }
            };

            using (var connection = connectionFactory.CreateNetTxConnection())
            {
                connection.Start();
                using (var session = connection.CreateSession())
                using (var consumer = CreateResponseConsumer(session))
                {
                    RunProducer(connection);
                }

                connection.Stop();
            }
        }

        private static IMessageConsumer CreateResponseConsumer(ISession session)
        {
            responseQueue = session.CreateTemporaryQueue();
            
            var consumer = session.CreateConsumer(responseQueue);
            consumer.Listener += OnMessage;

            return consumer;
        }

        private static void OnMessage(IMessage message)
        {
            Console.WriteLine("Received Response to request {0}: {1}", message.NMSCorrelationID, "blah");
            Console.WriteLine("==========================================================================");
        }

        private static void RunProducer(INetTxConnection connection)
        {
            Console.WriteLine("Press 'e' to publish an IEvent, EventMessage, and AnotherEventMessage alternately.");
            Console.WriteLine("Press 's' to send a command to Subscriber1");
            Console.WriteLine("Press 'n' to send a command to SubscriberNMS");
            Console.WriteLine("Press 'q' to exit");

            while (true)
            {
                var key = Console.ReadKey();
                using (var session = connection.CreateNetTxSession())
                {
                    switch (key.KeyChar)
                    {
                        case 'q':
                            return;
                        case 'e':
                            PublishEvent(session);
                            break;
                        case 'n':
                            SendCommand(session, "queue://subscribernms");
                            break;
                        case 's':
                            SendCommand(session, "queue://subscriber1");
                            break;
                    }
                }
            }
        }

        private static void SendCommand(ISession session, string queue)
        {
            var destination = SessionUtil.GetDestination(session, queue);
            using (var producer = session.CreateProducer())
            {
                var message =
                    session.CreateXmlMessage(
                        new MyRequest1 { Time = DateTime.Now, Duration = TimeSpan.FromMinutes(5), CommandId = Guid.NewGuid() });
                message.NMSReplyTo = responseQueue;
                producer.Send(destination, message);
            }
        }

        private static void PublishEvent(ISession session)
        {
            var destination = SessionUtil.GetDestination(session, "topic://VirtualTopic.EventMessage");
            using (var producer = session.CreateProducer())
            {
                var message =
                    session.CreateXmlMessage(
                        new EventMessage
                            { Time = DateTime.Now, Duration = TimeSpan.FromMinutes(5), EventId = Guid.NewGuid() });
                producer.Send(destination, message);
            }
        }
    }
}