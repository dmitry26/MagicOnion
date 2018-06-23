using System;
using MagicOnion;

namespace Samples.SimpleServer
{
	/// <summary>
	/// Simple service example
	/// </summary>
	public interface ISimpleService : IService<ISimpleService>
	{
		/// <summary>
		/// Add two integers
		/// </summary>
		/// <param name="x">first integer</param>
		/// <param name="y">second integer</param>
		/// <returns>MagicOnion.UnaryResult&lt;int></returns>
		UnaryResult<int> SumAsync(int x,int y);
	}
}
