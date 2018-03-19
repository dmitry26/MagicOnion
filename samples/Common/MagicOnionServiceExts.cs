using System;
using System.Collections.Generic;
using System.Linq;

namespace MagicOnion.Server.Extensions
{
	/// <summary>
	/// Marks method which can't be handled by HttpGateway
	/// </summary>
	[AttributeUsage(AttributeTargets.Method,Inherited = true,AllowMultiple = true)]
	public class WebApiIgnoreAttributeAttribute : Attribute
	{
	}

	public static class MagicOnionServiceExts
	{
		public static IReadOnlyList<MethodHandler> WebApiHandlers(this MagicOnionServiceDefinition svc) =>
			svc?.MethodHandlers.Where(x => x.MethodType == Grpc.Core.MethodType.Unary
				&& !x.AttributeLookup.Contains(typeof(WebApiIgnoreAttributeAttribute))).ToArray()
				?? throw new ArgumentNullException("svc");
	}
}
