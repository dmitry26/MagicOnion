using MagicOnion;
using MagicOnion.Server;
using MagicOnion.Server.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.ChatServer.Services
{
	// Room is "not" serializable
	public class ChatRoom
	{
		StreamingContextGroup<string,RoomMember,IChatRoomStreaming> group;

		public string Id { get; }
		public string Name { get; }

		public int MemberCount => group.Count;

		public ChatRoom(string id,string name)
		{
			this.Id = id;
			this.Name = name;
			this.group = new StreamingContextGroup<string,RoomMember,IChatRoomStreaming>();
		}

		public void AddMember(RoomMember member,StreamingContextRepository<IChatRoomStreaming> streaming)
		{
			this.group.Add(member.Id,member,streaming);
		}

		public void RemoveMember(string memberId)
		{
			this.group.Remove(memberId);
		}

		public RoomMember? GetMember(string memberId)
		{
			var v = this.group.Get(memberId);
			return v?.Item1;
		}

		public RoomMember[] GetMembers()
		{
			return this.group.All().Select(x => x.Item1).ToArray();
		}

		public Task BroadcastJoinAsync(RoomMember joinMember)
		{
			return this.group.BroadcastAllExceptAsync(x => x.OnJoin,joinMember,joinMember.Id);
		}

		public Task BroadcastLeaveAsync(RoomMember leaveMember)
		{
			return this.group.BroadcastAllExceptAsync(x => x.OnLeave,leaveMember,leaveMember.Id);
		}

		public Task BroadcastMessageAsync(RoomMember sendMember,string message)
		{
			return this.group.BroadcastAllExceptAsync(x => x.OnMessageReceived,new ChatMessage { Sender = sendMember,Message = message },sendMember.Id);
		}

		public ChatRoomResponse ToChatRoomResponse()
		{
			return new ChatRoomResponse { Id = Id,Name = Name };
		}
	}

	// In-Memory Room Repository.
	public class RoomRepository
	{
		public static RoomRepository Default = new RoomRepository();

		ConcurrentDictionary<string,ChatRoom> rooms = new ConcurrentDictionary<string,ChatRoom>();
		ConcurrentDictionary<string,ChatRoom> roomByNameDict = new ConcurrentDictionary<string,ChatRoom>();

		// use ddefault only...
		RoomRepository()
		{
		}

		public ChatRoom GetOrAddRoom(string name,Func<string,ChatRoom> roomFact)
		{
			ChatRoom newRoom = null;

			var room = roomByNameDict.GetOrAdd(name,rn =>
			{
				newRoom = roomFact(rn);
				return newRoom;
			});

			if (room == newRoom)
				rooms.GetOrAdd(newRoom.Id,newRoom);

			return room;
		}

		public ChatRoom GetRoom(string roomId)
		{
			return rooms.TryGetValue(roomId,out ChatRoom room) ? room : null;
		}

		public ChatRoom RemoveRoom(string roomId)
		{
			//return rooms.TryRemove(roomId, out ChatRoom room) ? room : null;

			if (rooms.TryRemove(roomId,out ChatRoom room))
			{
				roomByNameDict.TryRemove(room.Name,out ChatRoom dummy);
				return room;
			}

			return null;
		}

		public ICollection<ChatRoom> GetRooms()
		{
			return rooms.Values;
		}
	}

	// This class requires Heartbeat.Connect
	public class ChatRoomService : ServiceBase<IChatRoomService>, IChatRoomService
	{
		// Helper Common Methods
		static string GetMyId(ConnectionContext context,ChatRoom room)
		{
			context.Items.TryGetValue($"RoomService{room.Id}.MyId",out object o);
			return o as string;
		}

		static void SetMyId(ConnectionContext context,ChatRoom room,string id)
		{
			context.Items[$"RoomService{room.Id}.MyId"] = id;
		}

		static void SetRoomItem(ConnectionContext context,string roomId,object o)
		{
			context.Items[$"RoomService{roomId}.{o.GetType().Name}"] = o;
		}

		static TRoomItem GetRoomItem<TRoomItem>(ConnectionContext context,string roomId) where TRoomItem : class
		{
			context.Items.TryGetValue($"RoomService{roomId}.{typeof(TRoomItem).Name}",out object o);

			if (o != null && o is TRoomItem)
				return (TRoomItem)o;

			return default(TRoomItem);
		}

		static TRoomItem? GetRoomValue<TRoomItem>(ConnectionContext context,string roomId) where TRoomItem : struct
		{
			context.Items.TryGetValue($"RoomService{roomId}.{typeof(TRoomItem).Name}",out object o);

			if (o != null && o is TRoomItem)
				return (TRoomItem)o;

			return null;
		}

		// Room Commands

		[WebApiIgnoreAttribute]
		public async UnaryResult<ChatRoomResponse> JoinOrCreateRoom(string roomName,string nickName)
		{
			var newMember = new RoomMember();
			ChatRoom newRoom = null;

			var room = RoomRepository.Default.GetOrAddRoom(roomName,name =>
			{
				newRoom = new ChatRoom(Guid.NewGuid().ToString(),name);
				newMember = new RoomMember(Guid.NewGuid().ToString(),nickName);
				newRoom.AddMember(newMember,GetStreamingContextRepository());
				return newRoom;
			});

			var connectionContext = this.GetConnectionContext();

			if (room != newRoom)
			{
				if (GetMyId(connectionContext,room) != null)
					return null;

				// Join	
				newMember = new RoomMember(Guid.NewGuid().ToString(),nickName);
				room.AddMember(newMember,GetStreamingContextRepository());
				await room.BroadcastJoinAsync(newMember);
			}

			SetMyId(connectionContext,room,newMember.Id);

			SetRoomItem(connectionContext,room.Id,connectionContext.ConnectionStatus.Register(state =>
			{
				var t = (Tuple<string,string>)state;
				LeaveCore(t.Item1,t.Item2).Wait();
			},Tuple.Create(room.Id,newMember.Id)));

			return room.ToChatRoomResponse();
		}

		public UnaryResult<ChatRoomResponse[]> GetRooms()
		{
			return UnaryResult(RoomRepository.Default.GetRooms().Select(x => x.ToChatRoomResponse()).ToArray());
		}

		public UnaryResult<RoomMember[]> GetMembers(string roomId)
		{
			var connectionContext = this.GetConnectionContext();
			var room = RoomRepository.Default.GetRoom(roomId);
			if (room == null) return UnaryResult(new RoomMember[0]);
			return UnaryResult(room.GetMembers());
		}

		[WebApiIgnoreAttribute]
		public async UnaryResult<bool> Leave(string roomId)
		{
			var connectionContext = this.GetConnectionContext();
			var room = RoomRepository.Default.GetRoom(roomId);
			if (room == null) return false;

			GetRoomValue<System.Threading.CancellationTokenRegistration>(connectionContext,room.Id)?.Dispose();
			await LeaveCore(roomId,GetMyId(connectionContext,room));
			return true;
		}

		// called from ConnectionStatus.Register so should be static.
		static async Task LeaveCore(string roomId,string myId)
		{
			var room = RoomRepository.Default.GetRoom(roomId);
			if (room == null) return;

			var self = room.GetMember(myId);
			if (self == null) return;

			room.RemoveMember(myId);
			if (room.MemberCount == 0)
			{
				RoomRepository.Default.RemoveRoom(roomId);
			}
			else
			{
				await room.BroadcastLeaveAsync(self.Value);
			}
		}

		[WebApiIgnoreAttribute]
		public async UnaryResult<bool> SendMessage(string roomId,string message)
		{
			var room = RoomRepository.Default.GetRoom(roomId);
			if (room == null) return false;
			var myId = GetMyId(this.GetConnectionContext(),room);
			var self = room.GetMember(myId);
			if (self == null) return false;

			await RoomRepository.Default.GetRoom(roomId).BroadcastMessageAsync(self.Value,message);
			return true;
		}

		// RoomStreaming

		StreamingContextRepository<IChatRoomStreaming> GetStreamingContextRepository()
		{
			var connection = this.GetConnectionContext();
			var item = connection.Items.GetOrAdd("RoomStreamingStreamingContextRepository",_ => new Lazy<StreamingContextRepository<IChatRoomStreaming>>(() =>
			{				
				return new StreamingContextRepository<IChatRoomStreaming>(connection,this);
			}));
			return (item as Lazy<StreamingContextRepository<IChatRoomStreaming>>).Value;
		}

		public async Task<ServerStreamingResult<RoomMember>> OnJoin()
		{
			return await GetStreamingContextRepository().RegisterStreamingMethod(this,OnJoin);
		}

		public async Task<ServerStreamingResult<RoomMember>> OnLeave()
		{
			return await GetStreamingContextRepository().RegisterStreamingMethod(this,OnLeave);
		}

		public async Task<ServerStreamingResult<ChatMessage>> OnMessageReceived()
		{
			return await GetStreamingContextRepository().RegisterStreamingMethod(this,OnMessageReceived);
		}

		// Complete Commands

		[WebApiIgnoreAttribute]
		public async UnaryResult<bool> CompleteOnJoin()
		{
			await GetStreamingContextRepository().Complete<RoomMember>(x => x.OnJoin);
			return true;
		}

		[WebApiIgnoreAttribute]
		public async UnaryResult<bool> CompleteOnLeave()
		{
			await GetStreamingContextRepository().Complete<RoomMember>(x => x.OnLeave);
			return true;
		}

		[WebApiIgnoreAttribute]
		public async UnaryResult<bool> CompleteOnMsgReceived()
		{
			await GetStreamingContextRepository().Complete<ChatMessage>(x => x.OnMessageReceived);
			return true;
		}
	}
}
