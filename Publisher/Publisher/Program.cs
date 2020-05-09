using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;

namespace Publisher
{
    class Publ//:IPubl
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
            var bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(addr),
                h => { h.Username(userName); h.Password(password); });
            });
            bus.Start();
            Console.WriteLine("Wydawca wystartował");
            int counter = 1;

            Task.Run(() =>
            {
                while (true)
                {
                    bus.Publish(new Publ() { Number = counter });
                    Console.WriteLine($"Message: {counter} type: Publ");
                    counter += 1;
                    System.Threading.Thread.Sleep(1000);
                }
            });
        }
    }
}
