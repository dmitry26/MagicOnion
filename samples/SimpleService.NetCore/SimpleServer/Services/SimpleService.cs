using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MagicOnion;
using MagicOnion.Server;
using Samples.SimpleServer;

namespace Samples.SimpleServer.Services
{
	public class SimpleService : ServiceBase<ISimpleService>, ISimpleService
	{
		public UnaryResult<int> SumAsync(int x,int y)
		{
			Logger.Debug($"Received request: x = {x}, y = {y}");

			return UnaryResult(x + y);
		}
	}
}
