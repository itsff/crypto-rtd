using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoRtd
{
    public class WebSocketMessageEventArgs : EventArgs
    {
        public WebSocketMessageEventArgs(JToken message)
        {
            this.Message = message;
        }

        public JToken Message { get; private set; }
    }

    public abstract class WebSocketClientBase
    {
        static readonly Random _random = new Random();
        readonly EventWaitHandle _waitHandle;
        protected readonly Uri _endpoint;
        protected ClientWebSocket _socket;
        protected readonly CancellationTokenSource _cancellationTokenSource;
        Thread _receiveThread;

        protected WebSocketClientBase(Uri endpoint)
        {
            _waitHandle = new AutoResetEvent(false);
            _endpoint = endpoint;
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            lock (_cancellationTokenSource)
            {
                if (_receiveThread == null)
                {
                    _receiveThread = new Thread(() =>
                    {
                        var cancellationToken = _cancellationTokenSource.Token;

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            _waitHandle.Reset();
                            try
                            {
                                ReceiveLoop().Wait(cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                // User requested a stop. Do nothing.
                                break;
                            }
                            catch
                            {
                                // Keep spinning  
                                Thread.Sleep(TimeSpan.FromSeconds(2 * _random.Next(1, 5)));
                            }
                        }
                    });
                }
                _receiveThread.IsBackground = true;
                _receiveThread.Start();
            }
            _waitHandle.WaitOne();
        }

        async Task ReceiveLoop()
        {
            await EnsureConnection();
            _waitHandle.Set();

            using (var memStream = new MemoryStream(1024 * 1024))
            {
                var buffer = new ArraySegment<byte>(new byte[100 * 1024]);

                while (_socket.State == WebSocketState.Open)
                {
                    var result = await _socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);

                    if (result.CloseStatus.HasValue)
                    {
                        break;
                    }

                    memStream.Write(buffer.Array, buffer.Offset, result.Count);
                    if (result.EndOfMessage)
                    {
                        long pos = memStream.Position;
                        memStream.Seek(0, SeekOrigin.Begin);
                        var str = Encoding.UTF8.GetString(memStream.ToArray(), 0, (int)pos);
                        OnMessage(str);
                    }
                }
            }
        }

        protected virtual async Task EnsureConnection()
        {
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(_endpoint, _cancellationTokenSource.Token);
            await ResubscribeInstruments();
            FireSocketReconnected();
        }

        protected abstract Task ResubscribeInstruments();

        public void Disconnect()
        {
            _cancellationTokenSource.Cancel();
            lock (_cancellationTokenSource)
            {
                if (_receiveThread != null)
                {
                    _receiveThread.Join();
                }
                _receiveThread = null;
            }
        }

        public event EventHandler<WebSocketMessageEventArgs> MessageReceived;
        public event EventHandler SocketReconnected;

        protected void FireMessageReceived(WebSocketMessageEventArgs e)
        {
            EventHandler<WebSocketMessageEventArgs> h = this.MessageReceived;
            if (h != null)
            {
                h.Invoke(this, e);
            }
        }

        protected void FireSocketReconnected()
        {
            EventHandler h = this.SocketReconnected;
            if (h != null)
            {
                h.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void OnMessage(string message)
        {
            if (!String.IsNullOrWhiteSpace(message))
            {
                JToken o = null;
                try
                {
                    o = JToken.Parse(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine(message);
                }

                if (o != null)
                {
                    FireMessageReceived(new WebSocketMessageEventArgs(o));
                }
            }
        }

        protected static Task SendStringAsync(
            WebSocket socket,
            string message,
            CancellationToken token)
        {
            byte[] sendBytes = Encoding.UTF8.GetBytes(message);
            var sendBuffer = new ArraySegment<byte>(sendBytes);
            return socket.SendAsync(
                sendBuffer,
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: token);
        }

        protected static Task SendStringAsync(
            WebSocket socket,
            string message)
        {
            return SendStringAsync(socket, message, CancellationToken.None);
        }

        public Task SendStringAsync(string message)
        {
            return SendStringAsync(_socket, message);
        }
    }

    public class GdaxWebSocketClient : WebSocketClientBase
    {
        readonly HashSet<string> _subscribedInstruments;

        public GdaxWebSocketClient(Uri endpoint) : base(endpoint)
        {
            _subscribedInstruments = new HashSet<string>();
        }

        protected override Task ResubscribeInstruments()
        {
            string[] subscribeThese;

            lock (_subscribedInstruments)
            {
                subscribeThese = _subscribedInstruments.ToArray();
            }

            return SubscribeTickers(subscribeThese);
        }

        public Task SubscribeTickers(params string[] instruments)
        {
            string[] subscribeTheseInstruments = null;

            lock (_subscribedInstruments)
            {
                foreach (var i in instruments)
                {
                    _subscribedInstruments.Add(i.ToUpperInvariant());
                }
                subscribeTheseInstruments = _subscribedInstruments.ToArray();
            }

            var msg = new JObject();
            msg["type"] = "subscribe";
            msg["channels"] = new JArray("ticker");
            msg["product_ids"] = new JArray(instruments);

            return SendStringAsync(msg.ToString());
        }
    }
}
