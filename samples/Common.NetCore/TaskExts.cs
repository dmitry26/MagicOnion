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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dmo.Extensions.Threading
{
    public static class TaskExts
    {		
		public static Task ForEachAsync<T>(this IEnumerable<T> source,Func<T,Task> asyncFunc,bool wrapIntoTask=false)
		{
			if (!wrapIntoTask)
				return Task.WhenAll(source.Select(item => asyncFunc(item)));

			return Task.WhenAll(source.Select(item => Task.Run(() => asyncFunc(item))));			
		}

		public static Task ForEachAsync<T>(this IEnumerable<T> source,Action<T> action)
		{
			return Task.WhenAll(source.Select(item => Task.Run(() => action(item))));
		}

		public static Task ForEachLimitAsync<T>(this IEnumerable<T> source,Func<T,Task> asyncFunc,int degreeOfParal = 0)
		{
			if (degreeOfParal <= 0)
				degreeOfParal = Environment.ProcessorCount;

			return Task.WhenAll(Partitioner.Create(source).GetPartitions(degreeOfParal).Select(p =>
				Task.Run(async () =>
				{
					using (p)
					{
						while (p.MoveNext())
						{
							await asyncFunc(p.Current);
						}
					}
				})));
		}
	}
}
