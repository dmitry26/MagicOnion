﻿// Copyright (c) DMO Consulting LLC. All rights reserved.
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
using System.IO;
using Dmo.Extensions.Configuration;
using Dmo.Extensions.Logging;
using Dmo.Extensions.MagicOnion;
using Grpc.Core;
using MagicOnion;
using MagicOnion.HttpGateway.Swagger;
using MagicOnion.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
			services.AddMagicOnion(Configuration);			
		}

		public void Configure(IApplicationBuilder app)
		{
			app.UseMagicOnion();			
		}
	}
}
