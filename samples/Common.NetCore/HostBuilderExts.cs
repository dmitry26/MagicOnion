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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dmo.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Dmo.Extensions.Hosting
{
	public static class HostingHostBuilderExts
	{
		public static IConfigurationBuilder AddAppSettings(this IConfigurationBuilder bldr,IHostingEnvironment env,string[] args = null,bool optional = true,bool reloadOnChange = false)
		{
			var envName = env.EnvironmentName;

			bldr.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json",optional,reloadOnChange)
				.AddJsonFile($"appsettings.{envName}.json",optional,reloadOnChange);

			if (env.IsDevelopment())
				bldr.AddUserSecrets(System.Reflection.Assembly.GetEntryAssembly(),optional: true);

			args = CmdArgHelper.WithoutShortSwitches(args,false);

			if (args != null && args.Length > 0)
				bldr.AddCommandLine(args);

			return bldr;
		}
		
		public static Task RunConsoleAsync(this IHostBuilder hostBuilder,Action<IServiceProvider> cfgApp,CancellationToken cancellationToken = default)
		{
			if (hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));

			var host = (hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder)))
				.UseConsoleLifetime().Build();

			cfgApp?.Invoke(host.Services);

			return host.RunAsync(cancellationToken);
		}

		public static IHostBuilder UseEnvironment(this IHostBuilder hostBuilder,string[] args)
		{
			return hostBuilder.ConfigureHostConfiguration(configBuilder =>
			{
				configBuilder.AddEnvironmentVariables("DOTNETCORE_");

				args = CmdArgHelper.WithoutShortSwitches(args);

				if (args != null && args.Length > 0)
					configBuilder.AddCommandLine(args);
			});
		}
	}
}
