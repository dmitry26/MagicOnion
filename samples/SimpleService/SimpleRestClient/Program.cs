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
using System.Diagnostics;
using RestSharp;

namespace Samples.SimpleRestClient
{
    class Program
    {
        static void Main(string[] args)
        {
			try
			{
				var url = "http://localhost:5432/ISimpleService";
				var client = new RestClient(url);

				var r = new Random(Environment.TickCount);

				for (int i = 0; i < 10; ++i)
				{
					SendReq(client,r.Next(0,100),r.Next(-200,200));
				}
			}
			catch (Exception x)
			{
				Console.WriteLine(x);
			}

			Console.WriteLine("\nPress Enter to exit.");
			Console.ReadLine();
		}

		private static void SendReq(RestClient client,int x,int y)
		{
			var req = new RestRequest("SumAsync",Method.POST);
			req.AddHeader("Cache-Control","no-cache")
				.AddHeader("Accept","application/json")
				.AddHeader("Content-Type","application/x-www-form-urlencoded")
				.AddParameter("x",x)
				.AddParameter("y",y);

			Console.WriteLine($"Sending request: x = {x}, y = {y}");
			var sw = Stopwatch.StartNew();
			var response = client.Execute(req);
			Console.WriteLine($"Received response: result = {response.Content}, duration = {sw.ElapsedMilliseconds}ms");
		}
    }
}
