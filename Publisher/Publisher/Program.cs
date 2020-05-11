using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Messages;

namespace Publisher
{
    class Switch : IConsumer<ISwitch>
    {
        public bool isOn { get; private set; } = true;

        public Task Consume(ConsumeContext<ISwitch> ctx)
        {
            //ctx.RespondAsync<Messages.IAnswer>(new Messages.IAnswer(){});
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

            var busSwitch = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(addr),
                    h => { h.Username(userName); h.Password(password); });
                sbc.ReceiveEndpoint(host, "switcher", ep =>
                {
                    ep.Instance(switcher);
                });
            });

            var busPubl = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(addr),
                    h => { h.Username(userName); h.Password(password); });
            });
            busPubl.Start();
            busSwitch.Start();
            Console.WriteLine("Wydawca wystartował");
            int counter = 1;
            bool endProgram = false;

            var t = Task.Run(() =>
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
                    /*
                    if(!switcher.isOn)
                    {
                        Console.WriteLine("Aby zamknąć naciśnij Esc.");
                        if(Console.ReadKey().Key == ConsoleKey.Escape)
                        {
                            endProgram = true;
                        }
                    }
                    */
                }
            });
            t.Wait();

            busPubl.Stop();
            busSwitch.Stop();
        }
    }
}