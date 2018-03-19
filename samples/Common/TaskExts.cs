using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Threading.Extensions
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
