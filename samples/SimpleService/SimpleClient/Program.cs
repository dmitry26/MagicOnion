// Copyright (c) DMO Consulting LLC. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading.Tasks;
using Dmo.Extensions.Configuration;
using Dmo.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Samples.SimpleClient
{
	class Program
	{
		static async Task Main(string[] args)
		{
			try
			{
				Console.Title = System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);				
				
				await RunAsync(args);
			}
			catch (Exception x)
			{
				Console.WriteLine(x);
				Console.ReadLine();
			}
		}

		static async Task RunAsync(string[] args)
		{
			var builder = new HostBuilder()
				.UseEnvironment(args)
				.ConfigureAppConfiguration((hostContext,config) =>
				{
					config.AddAppSettings(hostContext.HostingEnvironment,args);
				})
				.ConfigureServices((hostContext,services) =>
				{
					services.AddScoped<IHostedService,SimpleService>();
					services.AddMagicOnion(hostContext.Configuration);
				})
				.UseSerilog((hostContext,config) => config.ReadFrom.Configuration(hostContext.Configuration));

			await builder.RunConsoleAsync(services => services.UseMagicOnion());
		}
	}
}
