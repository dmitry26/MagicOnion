using MagicOnion;
using MagicOnion.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591

namespace Samples.ChatServer
{
    public interface IChatRoomStreaming
    {
        Task<ServerStreamingResult<RoomMember>> OnJoin();
        Task<ServerStreamingResult<RoomMember>> OnLeave();
        Task<ServerStreamingResult<ChatMessage>> OnMessageReceived();

		UnaryResult<bool> CompleteOnJoin();
		UnaryResult<bool> CompleteOnLeave();
		UnaryResult<bool> CompleteOnMsgReceived();
	}

    public interface IChatRoomCommand
    {
        /// <summary>Join or Create a room.</summary>
        UnaryResult<ChatRoomResponse> JoinOrCreateRoom(string roomName, string nickName);

        UnaryResult<ChatRoomResponse[]> GetRooms();

		UnaryResult<RoomMember[]> GetMembers(string roomId);

		UnaryResult<bool> Leave(string roomId);

		UnaryResult<bool> SendMessage(string roomId, string message);
    }

    public interface IChatRoomService : IService<IChatRoomService>, IChatRoomCommand, IChatRoomStreaming
    {
    }
}

#pragma warning restore CS1591