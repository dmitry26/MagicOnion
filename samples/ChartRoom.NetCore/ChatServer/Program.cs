using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Samples.ChatServer
{
    class Program
    {
		static void Main(string[] args)
        {
			Console.Title = "ChatServer";

			BuildWebHost(args).Run();
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost.CreateDefaultBuilder(args)				
				.UseStartup<Startup>()
				.Build();
	}
}
