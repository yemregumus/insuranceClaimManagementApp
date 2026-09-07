using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;

namespace InsuranceClaimManagement
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            if (args.Any(argument =>
                    string.Equals(argument, "--seed-admin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(argument, "--provision-organization", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    await IdentityDataSeeder.SeedAdministratorAsync(host.Services);
                    return 0;
                }
                catch (InvalidOperationException exception)
                {
                    Console.Error.WriteLine("Administrator account was not created.");
                    Console.Error.WriteLine(exception.Message);
                    return 1;
                }
            }

            host.Run();
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
