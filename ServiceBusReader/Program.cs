using System;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceBusReader
{
    /// <summary>
    /// Work out the proper reading strategy for handling events. Answer the following questions:
    /// 1. When processing messages in parallel, can a first message fail the first time while the second message succeeds?
    /// 2. What is the proper error handling strategy with retries etc?
    /// </summary>
    class Program
    {
        private readonly string _topicName;
        private readonly string _subscriptionName1;
        private readonly string _subscriptionName2;
        private SubscriptionClient _subscriptionClient1;
        private SubscriptionClient _subscriptionClient2;
        private int _einsteinFailureCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">Pass the connection string as first argument.</param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            try
            {
                var connectionString = args[0];

                var app = new Program();
                using (var cts = new CancellationTokenSource())
                {
                    app.Run(connectionString, cts).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 1;
            }

            return 0;
        }

        public Program()
        {
            _topicName = ConfigurationManager.AppSettings["TopicName"];
            _subscriptionName1 = ConfigurationManager.AppSettings["SubscriptionName1"];
            _subscriptionName2 = ConfigurationManager.AppSettings["SubscriptionName2"];
        }

        public async Task Run(string connectionString, CancellationTokenSource cts)
        {
            var sendTask = SendMessagesAsync(connectionString, _topicName);

            var retryPolicy = new RetryExponential(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), 6);
            _subscriptionClient1 = new SubscriptionClient(connectionString, _topicName, _subscriptionName1, retryPolicy: retryPolicy);
            _subscriptionClient2 = new SubscriptionClient(connectionString, _topicName, _subscriptionName2, retryPolicy: retryPolicy);


            ReceiveMessagesAsync(_subscriptionClient1, ProcessMessages1Async, cts.Token);
            ReceiveMessagesAsync(_subscriptionClient2, ProcessMessages2Async, cts.Token);

            await Task.WhenAll(
                Task.WhenAny(
                    Task.Run(Console.ReadKey, cts.Token),
                    Task.Delay(TimeSpan.FromSeconds(10), cts.Token)
                ).ContinueWith((t) => cts.Cancel(), cts.Token),
                sendTask
                //receiveTask1,
                //receiveTask2
                );
        }

        async Task SendMessagesAsync(string connectionString, string topicName)
        {
            var topicClient = new TopicClient(connectionString, topicName);
            
            dynamic data = new[]
            {
                new {name = "Einstein", firstName = "Albert"},
                new {name = "Heisenberg", firstName = "Werner"},
                new {name = "Curie", firstName = "Marie"},
                new {name = "Hawking", firstName = "Steven"},
                new {name = "Newton", firstName = "Isaac"},
                new {name = "Bohr", firstName = "Niels"},
                new {name = "Faraday", firstName = "Michael"},
                new {name = "Galilei", firstName = "Galileo"},
                new {name = "Kepler", firstName = "Johannes"},
                new {name = "Kopernikus", firstName = "Nikolaus"}
            };

            for (var i = 0; i < data.Length; i++)
            {
                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data[i])))
                {
                    ContentType = "application/json",
                    Label = "Scientist",
                    MessageId = i.ToString(),
                    TimeToLive = TimeSpan.FromMinutes(2)
                };
                var destination = i % 2 == 0 ? "pande" : "preview";
                message.UserProperties.Add("destination", destination);

                await topicClient.SendAsync(message);
                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Message sent: Id = {0}, {1}, dest: {2}", message.MessageId, data[i].name, destination);
                    Console.ResetColor();
                }
            }
        }

        private void ReceiveMessagesAsync(SubscriptionClient subscriptionClient,
            Func<Message, CancellationToken, Task> messageCallback,
            CancellationToken cancellationToken)
        {
            var doneReceiving = new TaskCompletionSource<bool>();
            // close the receiver and factory when the CancellationToken fires 
            cancellationToken.Register(
                async () =>
                {
                    await subscriptionClient.CloseAsync();
                    doneReceiving.SetResult(true);
                });

            // register the RegisterMessageHandler callback
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                AutoComplete = false
            };

            // Register the function that processes messages.
            subscriptionClient.RegisterMessageHandler(messageCallback, messageHandlerOptions);
        }

        // Use this handler to examine the exceptions received on the message pump.
        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        async Task ProcessMessages1Async(Message message, CancellationToken token)
        {
            await ProcessMessagesAsync("1", _subscriptionClient1, message, token);
        }

        async Task ProcessMessages2Async(Message message, CancellationToken token)
        {
            await ProcessMessagesAsync("2", _subscriptionClient2, message, token);
        }

        async Task ProcessMessagesAsync(string subscriptionName, SubscriptionClient subscriptionClient, Message message, CancellationToken token)
        {
            // Process the message.
            var body = Encoding.UTF8.GetString(message.Body);
            var o = (JObject)JsonConvert.DeserializeObject(body);
            if (o["name"].ToString() == "Einstein")
            {
                if (++_einsteinFailureCount < 4)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"{subscriptionName}: Failing Einstein");
                    Console.ResetColor();
                    throw new Exception("Failing Einstein");
                }
            }
            Console.WriteLine($"{subscriptionName}: Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{body}");

            // Complete the message so that it is not received again.
            // This can be done only if the subscriptionClient is created in ReceiveMode.PeekLock mode (which is the default).
            await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);

            // Note: Use the cancellationToken passed as necessary to determine if the subscriptionClient has already been closed.
            // If subscriptionClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
            // to avoid unnecessary exceptions.
        }

    }
}
