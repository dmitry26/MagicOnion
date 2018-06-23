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

namespace Dmo.Extensions.Configuration
{
    public static class CmdArgHelper
    {
		private static Dictionary<string,string> GetSwitchMappings() => new Dictionary<string,string>
		{			
			{"-u","--urls"},
			{"-e","--environment"},
		};

		public static string[] WithoutShortSwitches(string[] args,bool useMap=true)
		{
			if (args == null || args.Length == 0) return args;

			var map = useMap ? GetSwitchMappings() : null;

			return args.Select(x =>
			{
				var idx = x.IndexOf('=');

				if (x[0] == '/') x = (idx == 2 ? "-" : "--") + x.Substring(1);

				if (idx <= 0) return x;

				return (idx == 2 && map != null && map.TryGetValue(x.Substring(0,idx),out string sw))
					? sw + x.Substring(idx)
					: ((idx == 2 && x[0] == '-') ? null : x);
			}).Where(x => x != null).ToArray();
		}
	}
}
