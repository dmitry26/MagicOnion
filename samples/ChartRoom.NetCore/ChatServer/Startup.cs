﻿using System;
using System.IO;
using Grpc.Core;
using Grpc.Core.Logging;
using MagicOnion;
using MagicOnion.HttpGateway.Swagger;
using MagicOnion.Server;
using MagicOnion.Server.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Samples.Configuration.Extensions;

namespace Samples.ChatServer
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			GrpcEnvironment.SetLogger(new ConsoleLogger());

			services.AddOptions();
			services.ConfigureSection<MagicOnionSettings>("MagicOnion",Configuration);

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

		public void Configure(IApplicationBuilder app)
		{
			var services = app.ApplicationServices;
			var options = services.GetOptions<MagicOnionSettings>();

			Environment.SetEnvironmentVariable("SETTINGS_MAX_HEADER_LIST_SIZE",options.MaxHeaderListSize);

			services.GetService<Server>().Start();

			var magicOnionSvc = services.GetService<MagicOnionServiceDefinition>();
			var xmlName = "ChatServerDefinition.xml";
			var xmlPath = Path.Combine(AppContext.BaseDirectory,xmlName);

			var handlers = magicOnionSvc.WebApiHandlers();

			app.UseMagicOnionSwagger(handlers,new SwaggerOptions("ChatServer","Swagger Integration","/")
			{
				XmlDocumentPath = xmlPath
			});

			app.UseMagicOnionHttpGateway(handlers,new Channel(options.GrpcServerHost,options.GrpcServerPort,ChannelCredentials.Insecure));
		}
	}

	public class MagicOnionSettings
	{
		public string GrpcServerHost { get; set; } = "localhost";
		public int GrpcServerPort { get; set; } = 12345;
		public string MaxHeaderListSize { get; set; } = "1000000";
	}	
}
