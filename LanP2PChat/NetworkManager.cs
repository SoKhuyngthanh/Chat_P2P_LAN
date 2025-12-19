using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LanP2PChat
{
    // Class lưu thông tin người dùng
    public class PeerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public int TcpPort { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class NetworkManager
    {
        private const int UDP_PORT = 15000;
        private int myTcpPort;
        private string myName;

        private UdpClient? udpBroadcaster;
        private UdpClient? udpListener;
        private TcpListener? tcpListener;

        // Sự kiện báo ra ngoài
        public event Action<PeerInfo>? OnPeerFound;
        public event Action<string, string>? OnMessageReceived;

        private bool isRunning = false;

        public NetworkManager(string name)
        {
            myName = name;
            // Tìm cổng TCP trống ngẫu nhiên
            TcpListener l = new TcpListener(IPAddress.Any, 0);
            l.Start();
            myTcpPort = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
        }

        public void Start()
        {
            isRunning = true;
            
            // 1. Luồng TCP Server (Nhận tin nhắn)
            Thread tcpThread = new Thread(RunTcpServer);
            tcpThread.IsBackground = true;
            tcpThread.Start();

            // 2. Luồng UDP Receiver (Nghe tìm bạn)
            Thread udpReceiveThread = new Thread(RunUdpReceiver);
            udpReceiveThread.IsBackground = true;
            udpReceiveThread.Start();

            // 3. Luồng UDP Broadcaster (Hét tìm bạn)
            Thread udpBroadcastThread = new Thread(RunUdpBroadcaster);
            udpBroadcastThread.IsBackground = true;
            udpBroadcastThread.Start();
        }

        private void RunUdpBroadcaster()
        {
            udpBroadcaster = new UdpClient();
            udpBroadcaster.EnableBroadcast = true;
            IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Broadcast, UDP_PORT);
            
            string message = $"HELLO|{myName}|{myTcpPort}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            while (isRunning)
            {
                try
                {
                    udpBroadcaster.Send(data, data.Length, broadcastEP);
                    Thread.Sleep(3000); // 3 giây gửi 1 lần
                }
                catch { }
            }
        }

        private void RunUdpReceiver()
        {
            try
            {
                udpListener = new UdpClient();
                udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));

                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                while (isRunning)
                {
                    byte[] data = udpListener.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    
                    string[] parts = message.Split('|');
                    if (parts.Length == 3 && parts[0] == "HELLO")
                    {
                        string name = parts[1];
                        int port = int.Parse(parts[2]);

                        if (name != myName) 
                        {
                            OnPeerFound?.Invoke(new PeerInfo
                            {
                                Name = name,
                                IP = remoteEP.Address.ToString(),
                                TcpPort = port,
                                LastSeen = DateTime.Now
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void RunTcpServer()
        {
            tcpListener = new TcpListener(IPAddress.Any, myTcpPort);
            tcpListener.Start();

            while (isRunning)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Task.Run(() => HandleIncomingChat(client));
                }
                catch { }
            }
        }

        private void HandleIncomingChat(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string rawMsg = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    string[] parts = rawMsg.Split(new[] { '|' }, 2);
                    if (parts.Length == 2)
                    {
                        string sender = parts[0];
                        string content = parts[1];
                        OnMessageReceived?.Invoke(sender, content);
                    }
                }
                client.Close();
            }
            catch { }
        }

        // --- PHẦN GỬI TIN NHẮN (ĐÃ SỬA LẠI SẠCH SẼ) ---
        public void SendMessage(string peerIP, int peerPort, string content)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(peerIP, peerPort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Gói tin: "Tên_Tôi|Nội_dung_tin_nhắn"
                        string msg = $"{myName}|{content}";
                        byte[] data = Encoding.UTF8.GetBytes(msg);
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể gửi tin nhắn: " + ex.Message);
            }
         
        }

        public void Stop()
        {
            isRunning = false;
            try { udpBroadcaster?.Close(); } catch {}
            try { udpListener?.Close(); } catch {}
            try { tcpListener?.Stop(); } catch {}
        }
    }
}