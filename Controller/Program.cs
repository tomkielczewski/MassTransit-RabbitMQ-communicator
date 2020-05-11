using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Serialization;
using Messages;

namespace Controller
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
    class Switch : ISwitch  //Ustaw
    {
        public bool IsOn { get; set; }    //dziala
    }
    class Program
    {
        static void Main(string[] args)
        {
            var addr = "amqp://pwpnrbye:GMBxr4gWlaGe9w2lMT9MquPJfutT4F9r@kangaroo.rmq.cloudamqp.com/pwpnrbye";
            var userName = "pwpnrbye";
            var password = "GMBxr4gWlaGe9w2lMT9MquPJfutT4F9r";

            bool state = false;

            var bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(addr),
                h => { h.Username(userName); h.Password(password); });
                sbc.UseEncryptedSerializer(
                    new AesCryptoStreamProvider(
                        new Provider("16562516562516562516562516562516"), "1656251656251656"));
            });
            bus.Start();
            Console.WriteLine("Kontroler wystartował");

            var tsk = bus.GetSendEndpoint(new Uri("amqp://pwpnrbye:GMBxr4gWlaGe9w2lMT9MquPJfutT4F9r@kangaroo.rmq.cloudamqp.com/pwpnrbye/switcher"));
            tsk.Wait();
            var sendEp = tsk.Result;
            var keyPressed = Console.ReadKey().Key;
            bool shouldSend = false;
            while (keyPressed != ConsoleKey.Escape)//Ustaw { dziala = true } 
            {
                if(keyPressed == ConsoleKey.S)
                {
                    state = true;
                    shouldSend = true;
                }
                else if (keyPressed == ConsoleKey.T)
                {
                    state = false;
                    shouldSend = true;
                }

                if (shouldSend)
                {
                    sendEp.Send(new Switch() { IsOn = state },
                        ctx => { ctx.Headers.Set(EncryptedMessageSerializer.EncryptionKeyHeader, Guid.NewGuid().ToString()); });
                    Console.WriteLine($"\nZmiana stanu na {(state ? "Wlaczony" : "Wylaczony")}");
                }

                shouldSend = false;
                keyPressed = Console.ReadKey().Key;
            }

            bus.Stop();
        }
    }
}
