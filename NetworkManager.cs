using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LanP2PChat
{
    // Class lưu thông tin của một người dùng trong mạng
    public class PeerInfo
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public int TcpPort { get; set; } // Cổng để chat riêng
        public DateTime LastSeen { get; set; } // Để kiểm tra xem họ còn online không
    }

    public class NetworkManager
    {
        // Cấu hình
        private const int UDP_PORT = 15000; // Cổng chung để "hú" nhau
        private int myTcpPort;              // Cổng chat riêng của mình
        private string myName;

        // Socket
        private UdpClient udpBroadcaster;
        private UdpClient udpListener;
        private TcpListener tcpListener;

        // Sự kiện để báo cho Giao diện biết
        public event Action<PeerInfo> OnPeerFound; // Khi tìm thấy bạn mới
        public event Action<string, string> OnMessageReceived; // Khi có tin nhắn đến (User, Content)

        // Trạng thái
        private bool isRunning = false;

        public NetworkManager(string name)
        {
            myName = name;
            // Chọn một cổng TCP ngẫu nhiên còn trống cho mình
            TcpListener l = new TcpListener(IPAddress.Any, 0);
            l.Start();
            myTcpPort = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
        }

        public void Start()
        {
            isRunning = true;
            
            // 1. Bắt đầu lắng nghe TCP (Để nhận tin nhắn chat)
            Thread tcpThread = new Thread(RunTcpServer);
            tcpThread.IsBackground = true;
            tcpThread.Start();

            // 2. Bắt đầu lắng nghe UDP (Để biết ai đang online)
            Thread udpReceiveThread = new Thread(RunUdpReceiver);
            udpReceiveThread.IsBackground = true;
            udpReceiveThread.Start();

            // 3. Bắt đầu gửi UDP Broadcast (Để báo mình đang online)
            Thread udpBroadcastThread = new Thread(RunUdpBroadcaster);
            udpBroadcastThread.IsBackground = true;
            udpBroadcastThread.Start();
        }

        // --- PHẦN 1: QUẢNG BÁ BẢN THÂN (UDP SENDER) ---
        private void RunUdpBroadcaster()
        {
            udpBroadcaster = new UdpClient();
            udpBroadcaster.EnableBroadcast = true;
            
            // Địa chỉ Broadcast toàn mạng
            IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Broadcast, UDP_PORT);

            // Gói tin: "HELLO|Tên_Của_Tôi|Cổng_TCP_Của_Tôi"
            string message = $"HELLO|{myName}|{myTcpPort}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            while (isRunning)
            {
                try
                {
                    udpBroadcaster.Send(data, data.Length, broadcastEP);
                    Thread.Sleep(3000); // Cứ 3 giây "hú" lên 1 lần
                }
                catch { }
            }
        }

        // --- PHẦN 2: LẮNG NGHE NGƯỜI KHÁC (UDP RECEIVER) ---
        private void RunUdpReceiver()
        {
            try
            {
                udpListener = new UdpClient();
                // Thủ thuật để nhiều máy cùng lắng nghe trên 1 cổng UDP (Reuse Address)
                udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));

                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                while (isRunning)
                {
                    byte[] data = udpListener.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    
                    // Lọc tin nhắn của chính mình (dựa trên Port TCP gửi kèm)
                    string[] parts = message.Split('|');
                    if (parts.Length == 3 && parts[0] == "HELLO")
                    {
                        string name = parts[1];
                        int port = int.Parse(parts[2]);

                        // Nếu không phải là mình thì báo ra ngoài
                        // (Ở đây lọc đơn giản bằng tên, thực tế nên dùng IP + Port)
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
            catch (Exception ex) 
            { 
                // Xử lý lỗi nếu cần
            }
        }

        // --- PHẦN 3: NHẬN TIN NHẮN CHAT (TCP SERVER) ---
        private void RunTcpServer()
        {
            tcpListener = new TcpListener(IPAddress.Any, myTcpPort);
            tcpListener.Start();

            while (isRunning)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    // Xử lý mỗi tin nhắn đến trong 1 task riêng để không chặn server
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

                    // Định dạng tin nhắn: "SENDER_NAME|CONTENT"
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

        // --- PHẦN 4: GỬI TIN NHẮN (TCP CLIENT) ---
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

            try
            {
                netManager.SendMessage(selectedPeer.IP, selectedPeer.TcpPort, msg);

                // Lưu tin mình vừa gửi vào sổ luôn
                string myLog = $"[{DateTime.Now:HH:mm}] Me: {msg}\r\n";
                
                if (!chatLogs.ContainsKey(selectedPeer.Name))
                {
                    chatLogs[selectedPeer.Name] = "";
                }
                chatLogs[selectedPeer.Name] += myLog;
                // ---------------------

                AppendMessage("Me", msg, Color.Blue);
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        public void Stop()
        {
            isRunning = false;
            udpBroadcaster?.Close();
            udpListener?.Close();
            tcpListener?.Stop();
        }
    }
}
