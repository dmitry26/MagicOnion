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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dmo.Extensions.Configuration
{
	public static class ConfigurationExts
	{
		public static T GetSettings<T>(this IConfiguration config,string sectionKey = null)
		{
			if (config == null)
				throw new ArgumentNullException("config");

			return string.IsNullOrEmpty(sectionKey) ? config.Get<T>() : config.GetSection(sectionKey).Get<T>();
		}

		public static object GetSettings(this IConfiguration config,Type type,string sectionKey = null)
		{
			if (config == null)
				throw new ArgumentNullException("config");

			if (type == null)
				throw new ArgumentNullException("type");

			return string.IsNullOrEmpty(sectionKey) ? config.Get(type) : config.GetSection(sectionKey).Get(type);
		}

		public static IServiceCollection ConfigureSection<TOptions>(this IServiceCollection services,IConfiguration config,bool optional = false)
			where TOptions : class, new() =>
			services.ConfigureSection<TOptions>(config,typeof(TOptions).Name,optional);

		public static IServiceCollection ConfigureSection<TOptions>(this IServiceCollection services,IConfiguration config,string sectionKey,bool optional = false)
			where TOptions : class, new()
		{
			if (services == null)
				throw new ArgumentNullException("services");

			if (string.IsNullOrEmpty(sectionKey))
				throw new ArgumentException("'sectionKey' is null or empty");

			var cfgSection = config.GetSection(sectionKey);

			if (!optional && !cfgSection.Exists())
				throw new InvalidOperationException($"Configuration: the section {sectionKey} was not found");

			services.Configure<TOptions>(cfgSection);
			return services;
		}

		public static TOptions GetOptions<TOptions>(this IServiceProvider services)
			where TOptions : class, new()
		{
			if (services == null)
				throw new ArgumentNullException("services");

			return services.GetService<IOptions<TOptions>>().Value;
		}
	}
}
