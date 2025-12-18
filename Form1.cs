using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq; 
using System.Windows.Forms;

namespace LanP2PChat
{
    public partial class Form1 : Form
    {
        private NetworkManager netManager;
        private List<PeerInfo> peerList = new List<PeerInfo>();
        private PeerInfo selectedPeer = null;
        private string myName;

        // 1. Cuốn sổ lưu tin nhắn (Lịch sử chat)
        private Dictionary<string, string> chatLogs = new Dictionary<string, string>();

        // 2. Danh sách những người có tin nhắn chưa đọc (Để hiện chuông 🔔)
        private HashSet<string> unreadPeers = new HashSet<string>();

        public Form1()
        {
            InitializeComponent();

            // --- CẤU HÌNH BAN ĐẦU ---
            
            // Hỏi tên người dùng
            string name = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên hiển thị của bạn:", "Cấu hình", "User" + new Random().Next(100, 999));
            if (string.IsNullOrEmpty(name)) name = "User" + new Random().Next(100, 999);
            myName = name;
            this.Text += $" - {myName}";

            // Khởi tạo mạng
            netManager = new NetworkManager(myName);
            netManager.OnPeerFound += NetManager_OnPeerFound;
            netManager.OnMessageReceived += NetManager_OnMessageReceived;

            // Bắt đầu chạy ngầm
            netManager.Start();

            // --- TÍNH NĂNG TỰ ĐỘNG XÓA OFFLINE ---
            System.Windows.Forms.Timer timerCheckOffline = new System.Windows.Forms.Timer();
            timerCheckOffline.Interval = 5000; // Quét mỗi 5 giây
            timerCheckOffline.Tick += TimerCheckOffline_Tick;
            timerCheckOffline.Start();
        }

        // --- SỰ KIỆN TỪ NETWORK (CHẠY TRÊN LUỒNG KHÁC) ---

        private void NetManager_OnPeerFound(PeerInfo peer)
        {
            this.Invoke(new Action(() =>
            {
                var existing = peerList.Find(p => p.Name == peer.Name);
                if (existing == null)
                {
                    peerList.Add(peer);
                    lstPeers.Items.Add(peer.Name);
                }
                else
                {
                    // Cập nhật thời gian nhìn thấy lần cuối
                    existing.LastSeen = DateTime.Now;
                    
                    // Cập nhật lại IP/Port đề phòng họ khởi động lại app
                    existing.IP = peer.IP;
                    existing.TcpPort = peer.TcpPort;
                }
            }));
        }

        private void NetManager_OnMessageReceived(string sender, string content)
        {
            this.Invoke(new Action(() =>
            {
                // 1. Lưu vào sổ lịch sử trước
                string logLine = $"[{DateTime.Now:HH:mm}] {sender}: {content}\r\n";
                if (!chatLogs.ContainsKey(sender)) chatLogs[sender] = "";
                chatLogs[sender] += logLine;

                // 2. Kiểm tra xem có đang chat với người này không?
                if (selectedPeer != null && selectedPeer.Name == sender)
                {
                    // Đang chat -> Hiện tin nhắn lên luôn
                    AppendMessage(sender, content, Color.Black);
                }
                else
                {
                    // KHÔNG đang chat -> Đánh dấu là TIN MỚI 🔔
                    if (!unreadPeers.Contains(sender))
                    {
                        unreadPeers.Add(sender);
                        UpdatePeerNameInList(sender, true); // Thêm chuông
                    }
                }
            }));
        }

        // --- TIMER XÓA NGƯỜI OFFLINE ---

        private void TimerCheckOffline_Tick(object sender, EventArgs e)
        {
            // Tìm những người đã quá 15 giây không thấy tăm hơi
            var offlinePeers = peerList.Where(p => (DateTime.Now - p.LastSeen).TotalSeconds > 15).ToList();

            if (offlinePeers.Count > 0)
            {
                foreach (var p in offlinePeers)
                {
                    peerList.Remove(p);
                    
                    // Xóa tên khỏi ListBox (cần xử lý cả trường hợp tên đang có chuông)
                    RemovePeerFromListBox(p.Name);
                }
            }
        }

        // --- SỰ KIỆN GIAO DIỆN ---

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Chặn tiếng 'beep'
                SendMessage();
            }
        }

        private void lstPeers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstPeers.SelectedIndex == -1) return;

            // Lấy tên đang hiển thị (có thể dính chữ 🔔)
            string rawName = lstPeers.SelectedItem.ToString();
            string realName = rawName.Replace(" 🔔", ""); // Lọc bỏ chuông để lấy tên thật

            selectedPeer = peerList.Find(p => p.Name == realName);

            if (selectedPeer != null)
            {
                lblChatHeader.Text = $"  Đang chat với: {selectedPeer.Name}";
                btnSend.Enabled = true;
                txtMessage.Focus();

                // --- XỬ LÝ ĐÃ ĐỌC ---
                if (unreadPeers.Contains(realName))
                {
                    unreadPeers.Remove(realName);
                    UpdatePeerNameInList(realName, false); // Xóa chuông
                }

                // --- HIỂN THỊ LỊCH SỬ CHAT ---
                rtbChatHistory.Clear();
                if (chatLogs.ContainsKey(realName))
                {
                    rtbChatHistory.Text = chatLogs[realName];
                    rtbChatHistory.SelectionStart = rtbChatHistory.Text.Length;
                    rtbChatHistory.ScrollToCaret();
                }
                else
                {
                    AppendMessage("System", $"Bắt đầu cuộc trò chuyện với {selectedPeer.Name}...", Color.Gray);
                }
            }
        }

        // --- CÁC HÀM HỖ TRỢ LOGIC ---

        private void SendMessage()
        {
            string msg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg) || selectedPeer == null) return;

            // SỬA LỖI: Thay thế ký tự | để tránh hỏng giao thức
            string safeMsg = msg.Replace("|", "¦");

            try
            {
                // 1. Gửi qua mạng
                netManager.SendMessage(selectedPeer.IP, selectedPeer.TcpPort, safeMsg);

                // 2. Lưu tin nhắn của MÌNH vào lịch sử luôn (để chuyển qua lại vẫn còn)
                string myLog = $"[{DateTime.Now:HH:mm}] Me: {safeMsg}\r\n";
                if (!chatLogs.ContainsKey(selectedPeer.Name)) chatLogs[selectedPeer.Name] = "";
                chatLogs[selectedPeer.Name] += myLog;

                // 3. Hiển thị lên màn hình
                AppendMessage("Me", safeMsg, Color.Blue);
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi tin: " + ex.Message);
            }
        }

        private void AppendMessage(string sender, string content, Color color)
        {
            rtbChatHistory.SelectionStart = rtbChatHistory.TextLength;
            rtbChatHistory.SelectionLength = 0;

            rtbChatHistory.SelectionColor = color;
            rtbChatHistory.SelectionFont = new Font(rtbChatHistory.Font, FontStyle.Bold);
            rtbChatHistory.AppendText($"[{DateTime.Now:HH:mm}] {sender}: ");

            rtbChatHistory.SelectionColor = Color.Black;
            rtbChatHistory.SelectionFont = new Font(rtbChatHistory.Font, FontStyle.Regular);
            rtbChatHistory.AppendText(content + "\n");

            rtbChatHistory.ScrollToCaret();
        }

        // Hàm hỗ trợ thêm/xóa chuông 🔔
        private void UpdatePeerNameInList(string peerName, bool hasNewMessage)
        {
            for (int i = 0; i < lstPeers.Items.Count; i++)
            {
                string itemText = lstPeers.Items[i].ToString();
                if (itemText == peerName || itemText == peerName + " 🔔")
                {
                    lstPeers.Items[i] = hasNewMessage ? peerName + " 🔔" : peerName;
                    break;
                }
            }
        }

        // Hàm hỗ trợ xóa tên khỏi ListBox (xử lý cả trường hợp có chuông)
        private void RemovePeerFromListBox(string peerName)
        {
            // Xóa tên thường
            lstPeers.Items.Remove(peerName);
            // Xóa tên có chuông (nếu có)
            lstPeers.Items.Remove(peerName + " 🔔");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            netManager.Stop();
            base.OnFormClosing(e);
        }
    }
}
