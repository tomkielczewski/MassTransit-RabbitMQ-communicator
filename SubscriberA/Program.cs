using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Messages;

namespace SubscriberA
{
    class Program
    {
        class AnswerA : IAnswerA
        {
            public string Who { get; set; }
        }
        class Inbox : IConsumer<IPubl>, IConsumer<Fault<IAnswerA>>
        {
            public Task Consume(ConsumeContext<IPubl> ctx)
            {
                return Task.Run(() =>
                {
                    if(ctx.Message.Number % 2 == 0)
                    {
                        ctx.RespondAsync(new AnswerA() { Who = "abonent A" });

                    }
                    Console.WriteLine($"Otrzymano wiadomosc: {ctx.Message.Number} {(ctx.Message.Number % 2 == 0 ? "odpowiedziano" : "")}");
                });
            }
            public Task Consume(ConsumeContext<Fault<IAnswerA>> ctx)
            {
                return Task.Run(() =>
                {
                    foreach (var e in ctx.Message.Exceptions)
                    {
                        Console.WriteLine($"\n----------Wyjątek od {e.Message} {ctx.Message.Message.Who}\n");
                    }
                });
            }
        }

        static void Main(string[] args)
        {
            var addr = "amqp://pwpnrbye:GMBxr4gWlaGe9w2lMT9MquPJfutT4F9r@kangaroo.rmq.cloudamqp.com/pwpnrbye";
            var userName = "pwpnrbye";
            var password = "GMBxr4gWlaGe9w2lMT9MquPJfutT4F9r";

            var inbox = new Inbox();
            var bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(addr),
                h => { h.Username(userName); h.Password(password); });
                sbc.ReceiveEndpoint(host, "subscriberA", ep =>
                {
                    ep.Instance(inbox);
                });
            });
            bus.Start();
            Console.WriteLine("Abonent A wystartował");
            Console.ReadKey();
            bus.Stop();
        }
    }
}
