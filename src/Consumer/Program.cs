using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Domain.Services; 
using System.Threading.Tasks;

namespace Consumer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MatchingEngine>();

                    // Register the consumer worker
                    services.AddHostedService<OrderConsumerWorker>();
                    
                })
                .Build();

            await host.RunAsync();
        }
    }
}
