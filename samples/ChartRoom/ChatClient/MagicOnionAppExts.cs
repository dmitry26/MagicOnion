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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dmo.Extensions.Configuration;
using Grpc.Core;
using Grpc.Core.Logging;
using MagicOnion.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MagicOnion.Client;
using Microsoft.Extensions.Logging;
using Dmo.Extensions.Logging;

namespace Samples.ChatClient
{
	public static class MagicOnionAppExts
	{
		public static void AddMagicOnion(this IServiceCollection services,IConfiguration appConfig)
		{
			if(services == null)
				throw new ArgumentNullException("services");

			if (appConfig == null)
				throw new ArgumentNullException("appConfig");

			services.ConfigureSection<MagicOnionSettings>(appConfig,"MagicOnion");

			services.AddSingleton(svcProv =>
			{
				var opts = svcProv.GetOptions<MagicOnionSettings>();
				return new Channel(opts.GrpcServerHost,opts.GrpcServerPort,ChannelCredentials.Insecure);
			});
		}

		public static IServiceProvider UseMagicOnion(this IServiceProvider services)
		{
			var loggerFactory = services.GetService<ILoggerFactory>();
			GrpcEnvironment.SetLogger(new GrpcNetCoreLogger(loggerFactory));

			// add using Microsoft.Extensions.Hosting;
			var appLifetime = (services ?? throw new ArgumentNullException(nameof(services))).GetRequiredService<IApplicationLifetime>();

			var channel = services.GetRequiredService<Channel>();
			_ = channel.ConnectAsync();

			appLifetime.ApplicationStopping.Register(() =>
			{
				channel.ShutdownAsync().GetAwaiter().GetResult();
			});

			return services;
		}		
	}
}
