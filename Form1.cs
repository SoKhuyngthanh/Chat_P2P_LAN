using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LanP2PChat
{
    public partial class Form1 : Form
    {
        private NetworkManager netManager;
        private List<PeerInfo> peerList = new List<PeerInfo>();
        private PeerInfo selectedPeer = null;
        private string myName;

        public Form1()
        {
            InitializeComponent();
            
            // Hỏi tên người dùng khi mở ứng dụng
            // (Trong thực tế bạn có thể làm Form đăng nhập riêng)
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
        }

        // --- SỰ KIỆN TỪ NETWORK ---

        private void NetManager_OnPeerFound(PeerInfo peer)
        {
            // Cập nhật giao diện phải dùng Invoke vì sự kiện này đến từ luồng khác
            this.Invoke(new Action(() =>
            {
                // Kiểm tra xem đã có trong danh sách chưa để tránh trùng lặp
                var existing = peerList.Find(p => p.Name == peer.Name);
                if (existing == null)
                {
                    peerList.Add(peer);
                    lstPeers.Items.Add(peer.Name); // Chỉ hiện tên
                }
                else
                {
                    // Cập nhật lại thời gian last seen (có thể dùng để lọc offline sau này)
                    existing.LastSeen = DateTime.Now;
                }
            }));
        }

        private void NetManager_OnMessageReceived(string sender, string content)
        {
            this.Invoke(new Action(() =>
            {
                AppendMessage(sender, content, Color.Black);
                
                // Nếu đang không chat với người này, có thể hiện thông báo nhỏ hoặc đổi màu (tùy chọn)
            }));
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

            string selectedName = lstPeers.SelectedItem.ToString();
            selectedPeer = peerList.Find(p => p.Name == selectedName);

            if (selectedPeer != null)
            {
                lblChatHeader.Text = $"  Đang chat với: {selectedPeer.Name} ({selectedPeer.IP})";
                btnSend.Enabled = true;
                txtMessage.Focus();
                
                // Xóa lịch sử chat cũ hoặc tải lại (ở đây mình xóa cho đơn giản)
                rtbChatHistory.Clear();
                AppendMessage("System", $"Bắt đầu cuộc trò chuyện với {selectedPeer.Name}...", Color.Gray);
            }
        }

        // --- HÀM HỖ TRỢ ---

        private void SendMessage()
        {
            string msg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg) || selectedPeer == null) return;

            try
            {
                // 1. Gửi qua mạng
                netManager.SendMessage(selectedPeer.IP, selectedPeer.TcpPort, msg);

                // 2. Hiển thị lên màn hình của mình
                AppendMessage("Me", msg, Color.Blue);
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

        // Đóng ứng dụng an toàn
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            netManager.Stop();
            base.OnFormClosing(e);
        }
    }
}