using System;
using Grpc.Core;
using Grpc.Core.Logging;
using MagicOnion;
using MagicOnion.Client;
using Microsoft.Extensions.Configuration;
using Samples.Hosting.Extensions;
using Samples.Configuration.Extensions;
using System.Threading.Tasks;
using Samples.SimpleServer;

namespace Samples.NetCore.SimpleClient
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Console.Title = "SimpleClient";

				GrpcEnvironment.SetLogger(new ConsoleLogger());
				_appSettings = HostBuilderExts.GetAppSettings();

				Run().GetAwaiter().GetResult();
			}
			catch (Exception x)
			{
				Console.WriteLine(x);				
			}

			Console.ReadLine();
		}

		private static IConfigurationRoot _appSettings;

		static async Task Run()
		{
			var channel = GetChannel();

			// create MagicOnion dynamic client proxy
			var client = MagicOnionClient.Create<ISimpleService>(channel);

			var r = new Random(Environment.TickCount);

			// call method.
			var result = await client.SumAsync(r.Next(0,100),r.Next(-200,200)).ConfigureAwait(false);
			Console.WriteLine($"Received response: result = {result}");

			await channel.ShutdownAsync().ConfigureAwait(false);
		}

		static Channel GetChannel()
		{
			var opts = _appSettings.GetSettings<MagicOnionSettings>("MagicOnion") ?? new MagicOnionSettings();
			return new Channel(opts.GrpcServerHost,opts.GrpcServerPort,ChannelCredentials.Insecure);
		}
	}

	public class MagicOnionSettings
	{
		public string GrpcServerHost { get; set; } = "localhost";
		public int GrpcServerPort { get; set; } = 12345;
	}
}
