using Grpc.Core;
using Grpc.Core.Logging;
using MagicOnion;
using MagicOnion.Client;
using Samples.ChatServer;
using Samples.Threading.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.ChatClient
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Console.Title = "ChatClient";

				GrpcEnvironment.SetLogger(new ConsoleLogger());
				_appSettings = GetAppSettings();

				RunChat().GetAwaiter().GetResult();
			}
			catch (Exception x)
			{
				Console.WriteLine(x);
				Console.ReadLine();
			}
		}

		private static AppSettings _appSettings;

		static AppSettings GetAppSettings()
		{
			var reader = new AppSettingsReader();

			return new AppSettings
			{
				GrpcServerHost = reader.GetValue<string>("GrpcServerHost","localhost"),
				GrpcServerPort = reader.GetValue<int>("GrpcServerPort",12345),
				NewRoom = reader.GetValue<bool>("NewRoom",false)
			};
		}

		static async Task RunChat()
		{
			var channel = GetChannel();

			var roomName = "test";

			if (_appSettings.NewRoom)
			{
				var r = new Random(Environment.TickCount);
				roomName += r.Next(1,99).ToString();
			}

			await GetMemberNames().ForEachAsync(async mn =>
			{
				using (var ctx = new ChannelContext(channel))
				{
					await RunChat(ctx,roomName,mn).ConfigureAwait(false);
				}
			});

			await channel.ShutdownAsync().ConfigureAwait(false);
		}

		static Channel GetChannel()
		{
			return new Channel(_appSettings.GrpcServerHost,_appSettings.GrpcServerPort,ChannelCredentials.Insecure);
		}

		static IEnumerable<string> GetMemberNames()
		{
			var r = new Random(Environment.TickCount);
			return new string[] { new string((char)r.Next('A','Z'),1),new string((char)r.Next('A','Z'),1) };
		}

		static async Task RunChat(ChannelContext ctx,string roomName,string nickName)
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

			await Console.In.ReadLineAsync().ConfigureAwait(false);

			var t = client.CompleteOnJoin().ConfigureAwait(false);
			t = client.CompleteOnLeave().ConfigureAwait(false);
			t = client.CompleteOnMsgReceived().ConfigureAwait(false);

			await Task.WhenAll(onJoinTask,onLeaveTask,onMsgTask);
		}
	}

	internal static class AppSettingsExts
	{
		public static T GetValue<T>(this AppSettingsReader reader,string key,T def) where T : IConvertible
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			try
			{
				return (T)reader.GetValue(key,typeof(T));
			}
			catch
			{
			}

			return def;
		}
	}

	public class AppSettings
	{
		public string GrpcServerHost { get; set; } = "localhost";
		public int GrpcServerPort { get; set; } = 12345;
		public bool NewRoom {get; set; } = false;
	}
}
