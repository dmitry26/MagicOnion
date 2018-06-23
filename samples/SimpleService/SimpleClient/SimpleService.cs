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
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.SimpleServer;

namespace Samples.SimpleClient
{
	public class SimpleService : BackgroundService
	{
		public SimpleService(ISimpleService client,ILogger<SimpleService> logger)
		{
			_client = client;
			_logger = logger;
		}

		private readonly ILogger _logger;

		private readonly ISimpleService _client;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("SimpleService started.");

			var r = new Random(Environment.TickCount);

			for (int i = 0; i < 10; ++i)
			{
				await SumAsync(r.Next(0,100),r.Next(-200,200));
			}

			_logger.LogInformation("SimpleService is exiting.");

			Console.WriteLine("\nPress Ctrl+C to shut down.");
		}

		private async Task SumAsync(int x,int y)
		{
			_logger.LogInformation($"Sending request: x = {x}, y = {y}");
			var sw = Stopwatch.StartNew();
			var result = await _client.SumAsync(x,y).ConfigureAwait(false);
			_logger.LogInformation($"Received response: result = {result}, duration = {sw.ElapsedMilliseconds}ms");
		}
	}
}
