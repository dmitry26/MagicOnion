﻿using MagicOnion;
using MagicOnion.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sandbox.ConsoleServer.Services
{
    // Room is "not" serializable
    public class ChatRoom
    {
        StreamingContextGroup<string, RoomMember, IChatRoomStreaming> group;

        public string Id { get; }
        public string Name { get; }

        public int MemberCount => group.Count;

        public ChatRoom(string id, string name)
        {
            this.Id = id;
            this.Name = name;
            this.group = new StreamingContextGroup<string, RoomMember, IChatRoomStreaming>();
        }

        public void AddMember(RoomMember member, StreamingContextRepository<IChatRoomStreaming> streaming)
        {
            this.group.Add(member.Id, member, streaming);
        }

        public void RemoveMember(string memberId)
        {
            this.group.Remove(memberId);
        }

        public RoomMember? GetMember(string memberId)
        {
            var v = this.group.Get(memberId);
            return (v == null) ? (RoomMember?)null : v.Item1;
        }

        public Task BroadcastJoinAsync(RoomMember joinMember)
        {
			return this.group.BroadcastAllExceptAsync(x => x.OnJoin, joinMember, joinMember.Id);
        }

        public Task BroadcastLeaveAsync(RoomMember leaveMember)
        {
			return this.group.BroadcastAllExceptAsync(x => x.OnLeave, leaveMember, leaveMember.Id);
        }

        public Task BroadcastMessageAsync(RoomMember sendMember, string message)
        {
			return this.group.BroadcastAllExceptAsync(x => x.OnMessageReceived, new ChatMessage { Sender = sendMember, Message = message }, sendMember.Id);
        }

        public ChatRoomResponse ToChatRoomResponse()
        {
            return new ChatRoomResponse { Id = Id, Name = Name };
        }
    }

    // In-Memory Room Repository.
    public class RoomRepository
    {
        public static RoomRepository Default = new RoomRepository();

        ConcurrentDictionary<string, ChatRoom> rooms = new ConcurrentDictionary<string, ChatRoom>();

        // use ddefault only...
        RoomRepository()
        {
        }

        public void AddRoom(ChatRoom room)
        {
            rooms[room.Id] = room;
        }

        public ChatRoom GetRoom(string roomId)
        {
            return rooms[roomId];
        }

        public ChatRoom RemoveRoom(string roomId)
        {
            ChatRoom room;
            return rooms.TryRemove(roomId, out room)
                ? room
                : null;
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
        static string GetMyId(ConnectionContext context, ChatRoom room)
        {
            return context.Items[$"RoomService{room.Id}.MyId"] as string;
        }

        static void SetMyId(ConnectionContext context, ChatRoom room, string id)
        {
            context.Items[$"RoomService{room.Id}.MyId"] = id;
        }


        // RoomCommand


        public UnaryResult<ChatRoomResponse> CreateNewRoom(string roomName, string nickName)
        {
            var room = new ChatRoom(Guid.NewGuid().ToString(), roomName);
            var member = new RoomMember(Guid.NewGuid().ToString(), nickName);
            room.AddMember(member, GetStreamingContextRepository());
            RoomRepository.Default.AddRoom(room);

            var connectionContext = this.GetConnectionContext();
            SetMyId(connectionContext, room, member.Id);

            var roomId = room.Id;
            this.GetConnectionContext().ConnectionStatus.Register(state =>
            {
                var t = (Tuple<string, string>)state;
                LeaveCore(t.Item1, t.Item2).Wait();
            }, Tuple.Create(room.Id, member.Id));

            return UnaryResult(room.ToChatRoomResponse());
        }

        public UnaryResult<ChatRoomResponse[]> GetRooms()
        {
            return UnaryResult(RoomRepository.Default.GetRooms().Select(x => x.ToChatRoomResponse()).ToArray());
        }

        public async UnaryResult<ChatRoomResponse> Join(string roomId, string nickName)
        {
            var room = RoomRepository.Default.GetRoom(roomId);
            var newMember = new RoomMember(Guid.NewGuid().ToString(), nickName);
            room.AddMember(newMember, GetStreamingContextRepository());

            await room.BroadcastJoinAsync(newMember);

            return room.ToChatRoomResponse();
        }

        public async UnaryResult<bool> Leave(string roomId)
        {
            var connectionContext = this.GetConnectionContext();
            var room = RoomRepository.Default.GetRoom(roomId);
            if (room == null) return false;
            await LeaveCore(roomId, GetMyId(connectionContext, room));
            return true;
        }

        // called from ConnectionStatus.Register so should be static.
        static async Task LeaveCore(string roomId, string myId)
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

        public async UnaryResult<bool> SendMessage(string roomId, string message)
        {
            var room = RoomRepository.Default.GetRoom(roomId);
            var myId = GetMyId(this.GetConnectionContext(), room);
            var self = room.GetMember(myId);
            if (self == null) return false;

            await RoomRepository.Default.GetRoom(roomId).BroadcastMessageAsync(self.Value, message);
            return true;
        }

        // RoomStreaming

        StreamingContextRepository<IChatRoomStreaming> GetStreamingContextRepository()
        {
            var connection = this.GetConnectionContext();
            var item = connection.Items.GetOrAdd("RoomStreamingStreamingContextRepository", _ => new Lazy<StreamingContextRepository<IChatRoomStreaming>>(() =>
            {
                return new StreamingContextRepository<IChatRoomStreaming>(connection, this);
            }));
            return (item as Lazy<StreamingContextRepository<IChatRoomStreaming>>).Value;
        }

        public async Task<ServerStreamingResult<RoomMember>> OnJoin()
        {
            return await GetStreamingContextRepository().RegisterStreamingMethod(this, OnJoin);
        }

        public async Task<ServerStreamingResult<RoomMember>> OnLeave()
        {
            return await GetStreamingContextRepository().RegisterStreamingMethod(this, OnLeave);
        }

        public async Task<ServerStreamingResult<ChatMessage>> OnMessageReceived()
        {
            return await GetStreamingContextRepository().RegisterStreamingMethod(this, OnMessageReceived);
        }
    }
}
