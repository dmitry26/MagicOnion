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
using MagicOnion.Server;

namespace Dmo.Extensions.MagicOnion
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
