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
            var config = Stormancer.ClientConfiguration.ForAccount("d81fc876-6094-3d92-a3d0-86d42d866b96", "hello-world-tutorial");
            //config.ServerEndpoint = "http://localhost:8081";

            var client = new Stormancer.Client(config);
            client.GetPublicScene("main", "hello").ContinueWith(
                t =>
                {
                    var scene = t.Result;
                    scene.AddRoute("msg", p =>
                    {
                        Console.WriteLine(p.ReadObject<string>());
                    }, null);

                    scene.Connect().ContinueWith(t2 =>
                    {
                        if (t2.IsCompleted)
                        {
                        }
                        else
                        {
                            Console.WriteLine("Bad stuff happened...");
                        }
                    });
                });

            Console.ReadLine();
        }
    }
}
