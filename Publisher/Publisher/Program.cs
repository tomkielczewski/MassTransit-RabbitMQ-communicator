using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.Serialization;
using Messages;

namespace Publisher
{
    public class PassKey : SymmetricKey
    {
        public byte[] IV { get; set; }
        public byte[] Key { get; set; }
    }

    public class Provider : ISymmetricKeyProvider
    {
        private string k;
        public Provider(string _k) { k = _k; }
        public bool TryGetKey(string keyId, out SymmetricKey key)
        {
            var sk = new PassKey();
            sk.IV = Encoding.ASCII.GetBytes(keyId.Substring(0, 16));
            sk.Key = Encoding.ASCII.GetBytes(k);
            key = sk;
            return true;
        }
    }

    class PublishObserver : IPublishObserver
    {
        public int counterPubl = 0;

        public Task PrePublish<T>(PublishContext<T> context) where T : class
        {
            return Task.Run(() => { });
        }

        public Task PostPublish<T>(PublishContext<T> context) where T : class
        {
            return Task.Run(() => { counterPubl += 1; });
        }

        public Task PublishFault<T>(PublishContext<T> context, Exception exception) where T : class
        {
            return Task.Run(() => { });
        }
    }

    class ReceiveObserver : IReceiveObserver
    {
        public int counterAll = 0;
        public int counterHandled = 0;
        public int counterExceptions = 0;

        public Task PreReceive(ReceiveContext context)
        {
            return Task.Run(() => { counterAll += 1; });
        }

        public Task PostReceive(ReceiveContext context)
        {
            return Task.Run(() => { });
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class
        {
            return Task.Run(() => { counterHandled += 1; });
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
        {
            return Task.Run(() => { counterExceptions += 1; });
        }

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            return Task.Run(() => { });
        }
    }

    class Inbox : IConsumer<IAnswerA>, IConsumer<IAnswerB>
    {
        public Task Consume(ConsumeContext<IAnswerA> ctx)
        {
            return Task.Run(() =>
            {
                var shouldThrowException = new Random();
                if (shouldThrowException.Next(0, 100) < 34)
                    throw new Exception();
                Console.WriteLine($"Otrzymano odpowiedz od: {ctx.Message.Who}");
            });
        }
        public Task Consume(ConsumeContext<IAnswerB> ctx)
        {
            return Task.Run(() =>
            {
                var shouldThrowException = new Random();
                if (shouldThrowException.Next(0, 100) < 34)
                    throw new Exception();
                Console.WriteLine($"Otrzymano odpowiedz od: {ctx.Message.Who}");
            });
        }
    }

    class Switch : IConsumer<ISwitch>
    {
        public bool isOn { get; private set; } = true;

        public Task Consume(ConsumeContext<ISwitch> ctx)
        {
            isOn = ctx.Message.IsOn;
            return Task.Run(() =>
            {
                Console.WriteLine($"Kontroler każe {(isOn ? "Włączyć" : "Wyłączyć")} ");
            });
        }
    }
    class Publ : IPubl
    {
        public int Number { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {

            var addr = "amqp://pwpnrbye:GMBxr4gWlaGe9w2lMT9MquPJfutT4F9r@kangaroo.rmq.cloudamqp.com/pwpnrbye";
            var userName = "pwpnrbye";
            var password = "GMBxr4gWlaGe9w2lMT9MquPJfutT4F9r";

            var switcher = new Switch();
            var inbox = new Inbox();

            var rcvObserver = new ReceiveObserver();
            var publObserver = new PublishObserver();

            var busSwitch = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(addr),
                    h => { h.Username(userName); h.Password(password); });
                sbc.UseEncryptedSerializer(
                    new AesCryptoStreamProvider(
                        new Provider("16562516562516562516562516562516"), "1656251656251656"));
                sbc.ReceiveEndpoint(host, "switcher", ep => {
                    ep.Instance(switcher);
                });
            });

            var busPubl = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(addr),
                    h => { h.Username(userName); h.Password(password); });
                sbc.ReceiveEndpoint(host, "inbox", ep => {
                    ep.Instance(inbox);
                    ep.UseRetry(r => { r.Immediate(5); });
                });
            });

            busPubl.ConnectReceiveObserver(rcvObserver);
            busPubl.ConnectPublishObserver(publObserver);
            busPubl.Start();
            busSwitch.Start();
            Console.WriteLine("Wydawca wystartował");
            int counter = 1;
            bool endProgram = false;

            Task.Run(() =>
            {
                while(!endProgram)
                {
                    while (switcher.isOn)
                    {
                        busPubl.Publish(new Publ() { Number = counter });
                        Console.WriteLine($"Message: {counter} type: Publ");
                        counter += 1;
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            });

            ConsoleKey key;
            while ((key = Console.ReadKey().Key) != ConsoleKey.Escape)
            {
                if (key == ConsoleKey.S)
                {
                    Console.WriteLine($"\nStatyskytki odebranych wiadomości: " +
                        $"\n \t  liczba prób obsłużenia komunikatów każdego typu: {rcvObserver.counterAll}" +
                        $"\n \t  liczba pomyślnie obsłużonych komunikatów każdego typu: {rcvObserver.counterHandled}" +
                        $"\n \t  liczba rzuconych wyjątków: {rcvObserver.counterExceptions}" +
                        $"\n \t  liczba opublikowanych komunikatów: {publObserver.counterPubl}");

                }
            }
            if (key == ConsoleKey.Escape) endProgram = true;

            busPubl.Stop();
            busSwitch.Stop();
        }
    }
}