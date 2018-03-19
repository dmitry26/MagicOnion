using Microsoft.AspNetCore.Hosting;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore;
using Samples.Hosting.Extensions;

namespace Samples.ChatServer
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.Title = "Server";

			BuildWebHost(args).Run();
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost.CreateDefaultBuilder(HostBuilderExts.ExceptEnvArgs(args))
				.UseConfiguration(HostBuilderExts.BuildEnvArgConfig(args))
				.UseStartup<Startup>()
				.Build();		
	}	
}