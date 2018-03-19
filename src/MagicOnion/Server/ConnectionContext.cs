﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MagicOnion.Server
{
    public class ConnectionContext
    {
        #region instance

        ConcurrentDictionary<string, object> items;

        /// <summary>Object storage per invoke.</summary>
        public ConcurrentDictionary<string, object> Items
        {
            get
            {
                if (items == null) LazyInitializer.EnsureInitialized(ref items,() => new ConcurrentDictionary<string, object>());
                return items;
            }
        }

        public string ConnectionId { get; }

        public CancellationToken ConnectionStatus { get; }

        public ConnectionContext(string connectionId, CancellationToken connectionStatus)
        {
            this.ConnectionId = connectionId;
            this.ConnectionStatus = connectionStatus;
        }

        #endregion

        #region manager

        // Factory and Cache.

        public const string HeaderKey = "connection_id";
        static ConcurrentDictionary<string, ConnectionContext> manager = new ConcurrentDictionary<string, ConnectionContext>();

        public static int GetCurrentManagingConnectionCount()
        {
            return manager.Count;
        }

        public static string GetConnectionId(ServiceContext context)
        {
            return TryGetConnectionId(context, out var id) ? id : null;
        }

        public static bool TryGetConnectionId(ServiceContext context, out string id)
        {
            var connectionId = context.CallContext.RequestHeaders.Get(HeaderKey);
            if (connectionId == null || connectionId.IsBinary)
            {
                id = null;
                return false;
            }

            id = connectionId.Value;
            return true;
        }

        public static CancellationTokenSource Register(string connectionId)
        {
            var cts = new CancellationTokenSource();
            manager[connectionId] = new ConnectionContext(connectionId, cts.Token);
            return cts;
        }

        public static void Unregister(string connectionId)
        {
            ConnectionContext _;
            manager.TryRemove(connectionId, out _);
        }

        public static ConnectionContext GetContext(string connectionId)
        {
            if (manager.TryGetValue(connectionId, out ConnectionContext source))
            {
                return source;
            }
            else
            {
                throw new Exception("Heartbeat connection doesn't registered.");
            }
        }

        public static bool TryGetContext(string connectionId, out ConnectionContext context)
        {
            if (manager.TryGetValue(connectionId, out ConnectionContext source))
            {
                context = source;
                return true;
            }
            else
            {
                context = null;
                return false;
            }
        }

        #endregion
    }

    public static class ConnectionContextExtensions
    {
        public static ConnectionContext GetConnectionContext<T>(this ServiceBase<T> service)
        {
            return service.Context.GetConnectionContext();
        }

        public static ConnectionContext GetConnectionContext(this ServiceContext context)
        {
            if (!ConnectionContext.TryGetConnectionId(context, out var id))
            {
                return null;
            }
            return ConnectionContext.TryGetContext(id, out var ctx) ? ctx : null;
        }
    }
}
