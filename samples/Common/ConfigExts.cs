using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Samples.Configuration.Extensions
{
	public static class ConfigExts
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

		public static IServiceCollection ConfigureSection<TOptions>(this IServiceCollection services,IConfiguration config)
			where TOptions : class, new()
		{
			if (services == null)
				throw new ArgumentNullException("services");

			services.Configure<TOptions>(config.GetSection(typeof(TOptions).Name));
			return services;
		}

		public static IServiceCollection ConfigureSection<TOptions>(this IServiceCollection services,string sectionKey,IConfiguration config)
			where TOptions : class, new()
		{
			if (services == null)
				throw new ArgumentNullException("services");

			if (string.IsNullOrEmpty(sectionKey))
				throw new ArgumentException("'sectionKey' is null or empty");

			services.Configure<TOptions>(config.GetSection(sectionKey));
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
