using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;

namespace Echo.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = Stormancer.ClientConfiguration.ForAccount("test", "echo");
            //config.ServerEndpoint = "http://localhost:8081";

            var client = new Stormancer.Client(config);
            client.GetPublicScene("test-scene", "hello").ContinueWith(
                t =>
                {
                    var scene = t.Result;
                    scene.AddRoute("echo.out", p =>
                    {
                        Console.WriteLine(p.ReadObject<string>());
                    }, null);

                    scene.Connect().ContinueWith(t2 =>
                    {
                        if (t2.IsCompleted)
                        {
                            scene.SendPacket("echo.in", s =>
                            {
                                //var serializer = scene.GetComponent<ISerializer>();
                                //serializer.Serialize("hello", s);
                                scene.Host.Serializer().Serialize("hello", s);
                            });
                        }
                        else
                        {
                            Console.WriteLine("Bad stuff happened...");
                        }
                    });
                });
        }
    }
}
