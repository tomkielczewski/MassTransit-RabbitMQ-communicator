using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Messages;

namespace SubscriberB
{
    class Program
    {
        class AnswerB : IAnswerB
        {
            public string Who { get; set; }
        }
        class Inbox : IConsumer<IPubl>
        {
            public Task Consume(ConsumeContext<IPubl> ctx)
            {
                return Task.Run(() =>
                {
                    if (ctx.Message.Number % 3 == 0)
                    {
                        ctx.RespondAsync(new AnswerB() { Who = "abonent B" });

                    }
                    Console.WriteLine($"Otrzymano wiadomosc: {ctx.Message.Number} {(ctx.Message.Number % 3 == 0 ? "odpowiedziano" : "")}");
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
                sbc.ReceiveEndpoint(host, "subscriberB", ep =>
                {
                    ep.Instance(inbox);
                });
            });
            bus.Start();
            Console.WriteLine("Abonent B wystartował");
            Console.ReadKey();
            bus.Stop();
        }
    }
}
