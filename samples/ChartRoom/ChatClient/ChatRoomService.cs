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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dmo.Extensions.Threading;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.ChatServer;


namespace Samples.ChatClient
{   
	public class ChatRoomService : BackgroundService
	{
		public ChatRoomService(Channel channel,ILogger<ChatRoomService> logger)
		{
			_channel = channel;
			_logger = logger;
		}

		private readonly ILogger _logger;

		private readonly Channel _channel;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("ChatRoomService started.");

			await RunChat();

			_logger.LogInformation("ChatRoomService is exiting");
			Console.WriteLine("Press Ctrl+C to shut down.");			
		}

		private async Task RunChat()
		{			
			var roomName = "test";

			await Task.Delay(1000);
			
			await Console.Out.WriteLineAsync("\nPlease enter a room name, then press Enter.").ConfigureAwait(false);
			var text = (await Console.In.ReadLineAsync().ConfigureAwait(false)).Trim();

			if (text.Length > 0)
				roomName = text;			

			var memberNames = new List<string>();

			while (true)
			{				
				await Console.Out.WriteLineAsync("Enter a member name, then press Enter. Empty name will end this step.");								
				text = (await Console.In.ReadLineAsync().ConfigureAwait(false)).Trim();				

				if (text.Length == 0)
				{
					if (memberNames.Count > 0)
						break;
				}
				else if (!memberNames.Contains(text))
					memberNames.Add(text);
			}
			
			await memberNames.ForEachAsync(async mn =>
			{
				using (var ctx = new ChannelContext(_channel))
				{
					await RunChat(ctx,roomName,mn).ConfigureAwait(false);
				}
			});			
		}

		private async Task RunChat(ChannelContext ctx,string roomName,string nickName)
		{
			await ctx.WaitConnectComplete();

			// create room
			var client = ctx.CreateClient<IChatRoomService>();

			var onJoinTask = (await client.OnJoin()).ResponseStream.ForEachAsync(async x =>
			{
				await Console.Out.WriteLineAsync($"{DateTime.Now.ToString("HH:mm:ss.fff")} [OnJoin: '{nickName}'] '{x.Name}' joined room '{roomName}'").ConfigureAwait(false);
			});

			var onLeaveTask = (await client.OnLeave()).ResponseStream.ForEachAsync(async x =>
			{
				await Console.Out.WriteLineAsync($"{DateTime.Now.ToString("HH:mm:ss.fff")} [OnLeave: '{nickName}'] '{x.Name}' left room '{roomName}'").ConfigureAwait(false);
			});

			var onMsgTask = (await client.OnMessageReceived()).ResponseStream.ForEachAsync(async x =>
			{
				await Console.Out.WriteLineAsync($"{DateTime.Now.ToString("HH:mm:ss.fff")} [OnMessageReceived: '{nickName}'] received message from '{x.Sender.Name}': {x.Message}").ConfigureAwait(false);
			});

			await Task.Delay(1000);

			// we can only have one member per ChannelContext
			var room = await client.JoinOrCreateRoom(roomName,nickName).ConfigureAwait(false);

			await Console.Out.WriteLineAsync($"{DateTime.Now.ToString("HH:mm:ss.fff")} [JoinOrCreateRoom: '{nickName}'] entered room '{roomName}'").ConfigureAwait(false);

			if (!(await client.SendMessage(room.Id,$"Hello from '{nickName}'!")))
				await Console.Out.WriteLineAsync("'{nickName}' failed to send message").ConfigureAwait(false);

			var members = await client.GetMembers(room.Id).ConfigureAwait(false);
			await Console.Out.WriteLineAsync($"{DateTime.Now.ToString("HH:mm:ss.fff")} '{nickName}': members count = {members.Length}").ConfigureAwait(false);

			while (true)
			{
				await Console.Out.WriteLineAsync($"'{nickName}, enter a message, then press Enter. Empty message will exit the room.");
				
				var text = (await Console.In.ReadLineAsync().ConfigureAwait(false)).Trim();

				if (text.Length == 0)
					break;

				if (!(await client.SendMessage(room.Id,text)))
					await Console.Out.WriteLineAsync("'{nickName}' failed to send message").ConfigureAwait(false);
			}

			var t = client.CompleteOnJoin().ConfigureAwait(false);
			t = client.CompleteOnLeave().ConfigureAwait(false);
			t = client.CompleteOnMsgReceived().ConfigureAwait(false);

			await Task.WhenAll(onJoinTask,onLeaveTask,onMsgTask);
		}
	}
}
