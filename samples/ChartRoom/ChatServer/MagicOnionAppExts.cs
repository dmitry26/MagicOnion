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
using Dmo.Extensions.Configuration;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Server;
using MagicOnion.HttpGateway.Swagger;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dmo.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using System.IO;
using Dmo.Extensions.MagicOnion;

namespace Samples.ChatServer
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

			services.AddSingleton(MagicOnionEngine.BuildServerServiceDefinition(new MagicOnionOptions(true)
			{
				// MagicOnionLogger = new MagicOnionLogToGrpcLogger(),
				MagicOnionLogger = new MagicOnionLogToGrpcLoggerWithNamedDataDump(),
				GlobalFilters = new MagicOnionFilterAttribute[]
				{
				},
				EnableCurrentContext = true
			}));

			services.AddSingleton(svcProv =>
			{
				var options = svcProv.GetOptions<MagicOnionSettings>();
				var magicOnionSvc = svcProv.GetService<MagicOnionServiceDefinition>();

				return new Server
				{
					Services = { magicOnionSvc },
					Ports = { new ServerPort(options.GrpcServerHost,options.GrpcServerPort,ServerCredentials.Insecure) }
				};
			});
		}

		public static IServiceProvider UseMagicOnion(this IApplicationBuilder app)
		{
			var services = (app?? throw new ArgumentNullException(nameof(app))).ApplicationServices;

			var loggerFactory = services.GetService<ILoggerFactory>();
			GrpcEnvironment.SetLogger(new GrpcNetCoreLogger(loggerFactory));

			var options = services.GetOptions<MagicOnionSettings>();
			Environment.SetEnvironmentVariable("SETTINGS_MAX_HEADER_LIST_SIZE",options.MaxHeaderListSize);

			var magicOnionSvc = services.GetService<MagicOnionServiceDefinition>();
			var swaggerOpts = options?.Swagger ?? new MagicOnionSettings.SwaggerSettings();
			var xmlPath = Path.Combine(AppContext.BaseDirectory,swaggerOpts.XmlServiceDefDoc ?? "");

			if (!File.Exists(xmlPath))
				xmlPath = null;

			var handlers = magicOnionSvc.WebApiHandlers();

			app.UseMagicOnionSwagger(handlers,new SwaggerOptions(swaggerOpts.Title,swaggerOpts.Description,swaggerOpts.ApiBasePath)
			{
				XmlDocumentPath = xmlPath
			});

			app.UseMagicOnionHttpGateway(handlers,new Channel(options.GrpcServerHost,options.GrpcServerPort,ChannelCredentials.Insecure));

			var server = services.GetRequiredService<Server>();
			var appLifetime = services.GetRequiredService<IApplicationLifetime>();

			appLifetime.ApplicationStarted.Register(() => server.Start());

			appLifetime.ApplicationStopping.Register(() =>
			{
				server.ShutdownAsync().GetAwaiter().GetResult();
			});

			return services;
		}
	}
}
