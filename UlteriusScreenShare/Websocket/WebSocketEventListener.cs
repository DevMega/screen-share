﻿#region

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

#endregion

namespace UlteriusScreenShare.Websocket
{
    public delegate void WebSocketEventListenerOnConnect(WebSocket webSocket);

    public delegate void WebSocketEventListenerOnDisconnect(WebSocket webSocket);

    public delegate void WebSocketEventListenerOnMessage(WebSocket webSocket, string message);

    public delegate void WebSocketEventListenerOnError(WebSocket webSocket, Exception error);

    public class WebSocketEventListener : IDisposable
    {
        private readonly WebSocketListener _listener;

        public WebSocketEventListener(IPEndPoint endpoint)
            : this(endpoint, new WebSocketListenerOptions())
        {
        }

        public WebSocketEventListener(IPEndPoint endpoint, WebSocketListenerOptions options)
        {
            _listener = new WebSocketListener(endpoint, options);
            _listener.Standards.RegisterStandard(new WebSocketFactoryRfc6455(_listener));
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        public event WebSocketEventListenerOnConnect OnConnect;
        public event WebSocketEventListenerOnDisconnect OnDisconnect;
        public event WebSocketEventListenerOnMessage OnMessage;
        public event WebSocketEventListenerOnError OnError;

        public void Start()
        {
            _listener.Start();
            Task.Run(ListenAsync);
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private async Task ListenAsync()
        {
            while (_listener.IsStarted)
            {
                try
                {
                    var websocket = await _listener.AcceptWebSocketAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    if (websocket != null)
                        Task.Run(() => HandleWebSocketAsync(websocket));
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(null, ex);
                }
            }
        }

        private async Task HandleWebSocketAsync(WebSocket websocket)
        {
            try
            {
                OnConnect?.Invoke(websocket);

                while (websocket.IsConnected)
                {
                    var message = await websocket.ReadStringAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    if (message != null)
                        OnMessage?.Invoke(websocket, message);
                }

                OnDisconnect?.Invoke(websocket);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(websocket, ex);
            }
            finally
            {
                websocket.Dispose();
            }
        }
    }
}