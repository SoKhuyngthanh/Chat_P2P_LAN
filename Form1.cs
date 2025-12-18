using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D; 
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

        // 1. Cuốn sổ lưu nội dung chat
        private Dictionary<string, string> chatLogs = new Dictionary<string, string>();

        // 2. THAY ĐỔI: Lưu số lượng tin nhắn chưa đọc (Key: Tên, Value: Số lượng)
        private Dictionary<string, int> unreadCounts = new Dictionary<string, int>();

        public Form1()
        {
            InitializeComponent();

            // --- CẤU HÌNH LISTBOX 
            lstPeers.DrawMode = DrawMode.OwnerDrawFixed; // Cho phép tự vẽ
            lstPeers.ItemHeight = 40; // Tăng chiều cao dòng cho dễ nhìn
            lstPeers.DrawItem += LstPeers_DrawItem; // Đăng ký hàm vẽ
            // ------------------------------------------------

            string name = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên hiển thị của bạn:", "Cấu hình", "User" + new Random().Next(100, 999));
            if (string.IsNullOrEmpty(name)) name = "User" + new Random().Next(100, 999);
            myName = name;
            this.Text += $" - {myName}";

            netManager = new NetworkManager(myName);
            netManager.OnPeerFound += NetManager_OnPeerFound;
            netManager.OnMessageReceived += NetManager_OnMessageReceived;
            netManager.Start();

            System.Windows.Forms.Timer timerCheckOffline = new System.Windows.Forms.Timer();
            timerCheckOffline.Interval = 5000;
            timerCheckOffline.Tick += TimerCheckOffline_Tick;
            timerCheckOffline.Start();
        }

        // --- HÀM VẼ GIAO DIỆN (MA THUẬT Ở ĐÂY) ---
        private void LstPeers_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            // 1. Lấy tên người dùng tại dòng này
            string peerName = lstPeers.Items[e.Index].ToString();
            
            // 2. Kiểm tra xem có tin nhắn chưa đọc không
            int count = 0;
            if (unreadCounts.ContainsKey(peerName))
            {
                count = unreadCounts[peerName];
            }

            // 3. Vẽ nền (Background)
            e.DrawBackground();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            
            // Nếu được chọn thì nền xanh, ko thì nền theo giao diện
            Brush bgBrush = isSelected ? new SolidBrush(Color.FromArgb(0, 120, 215)) : new SolidBrush(lstPeers.BackColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            // 4. Xác định Font chữ (Có tin mới thì IN ĐẬM, không thì thường)
            Font nameFont;
            if (count > 0)
                nameFont = new Font(e.Font, FontStyle.Bold); // In đậm
            else
                nameFont = new Font(e.Font, FontStyle.Regular); // Bình thường

            Brush textBrush = isSelected ? Brushes.White : new SolidBrush(lstPeers.ForeColor);

            // 5. Vẽ Tên người dùng (Canh lề trái)
            e.Graphics.DrawString(peerName, nameFont, textBrush, e.Bounds.X + 10, e.Bounds.Y + 10);

            // 6. VẼ CHẤM ĐỎ VÀ SỐ (Nếu có tin nhắn mới) 🔴
            if (count > 0)
            {
                string countText = count > 99 ? "99+" : count.ToString();
                
                // Kích thước chấm đỏ
                int circleSize = 24; 
                int circleX = e.Bounds.Right - circleSize - 10; // Vẽ sát lề phải
                int circleY = e.Bounds.Y + (e.Bounds.Height - circleSize) / 2; // Căn giữa chiều dọc

                // Vẽ hình tròn đỏ
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // Khử răng cưa cho tròn đẹp
                e.Graphics.FillEllipse(Brushes.Red, circleX, circleY, circleSize, circleSize);

                // Vẽ số màu trắng ở giữa hình tròn
                Font numberFont = new Font("Arial", 9, FontStyle.Bold);
                SizeF textSize = e.Graphics.MeasureString(countText, numberFont);
                float textX = circleX + (circleSize - textSize.Width) / 2;
                float textY = circleY + (circleSize - textSize.Height) / 2;
                
                e.Graphics.DrawString(countText, numberFont, Brushes.White, textX, textY);
            }

            // Vẽ viền focus nếu cần
            e.DrawFocusRectangle();
        }

        // --- SỰ KIỆN MẠNG ---

        private void NetManager_OnPeerFound(PeerInfo peer)
        {
            this.Invoke(new Action(() =>
            {
                var existing = peerList.Find(p => p.Name == peer.Name);
                if (existing == null)
                {
                    peerList.Add(peer);
                    lstPeers.Items.Add(peer.Name); // Chỉ cần add tên gốc, hàm vẽ tự lo phần hiển thị
                }
                else
                {
                    existing.LastSeen = DateTime.Now;
                    existing.IP = peer.IP;
                    existing.TcpPort = peer.TcpPort;
                }
            }));
        }

        private void NetManager_OnMessageReceived(string sender, string content)
        {
            this.Invoke(new Action(() =>
            {
                // Lưu lịch sử
                string logLine = $"[{DateTime.Now:HH:mm}] {sender}: {content}\r\n";
                if (!chatLogs.ContainsKey(sender)) chatLogs[sender] = "";
                chatLogs[sender] += logLine;

                // Xử lý thông báo
                if (selectedPeer != null && selectedPeer.Name == sender)
                {
                    // Đang chat -> Hiện tin nhắn
                    AppendMessage(sender, content, Color.Black);
                }
                else
                {
                    // KHÔNG đang chat -> Tăng số lượng tin chưa đọc
                    if (!unreadCounts.ContainsKey(sender)) unreadCounts[sender] = 0;
                    unreadCounts[sender]++; 

                    // Bắt ListBox vẽ lại để hiện chấm đỏ
                    lstPeers.Invalidate(); 
                }
            }));
        }

        // --- TIMER OFFLINE ---
        private void TimerCheckOffline_Tick(object sender, EventArgs e)
        {
            var offlinePeers = peerList.Where(p => (DateTime.Now - p.LastSeen).TotalSeconds > 15).ToList();
            if (offlinePeers.Count > 0)
            {
                foreach (var p in offlinePeers)
                {
                    peerList.Remove(p);
                    lstPeers.Items.Remove(p.Name);
                    if (unreadCounts.ContainsKey(p.Name)) unreadCounts.Remove(p.Name);
                }
            }
        }

        // --- GIAO DIỆN ---

        private void lstPeers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstPeers.SelectedIndex == -1) return;

            string selectedName = lstPeers.SelectedItem.ToString(); // Lấy tên gốc
            selectedPeer = peerList.Find(p => p.Name == selectedName);

            if (selectedPeer != null)
            {
                lblChatHeader.Text = $"  Đang chat với: {selectedPeer.Name}";
                btnSend.Enabled = true;
                txtMessage.Focus();

                // --- ĐÃ ĐỌC TIN NHẮN ---
                if (unreadCounts.ContainsKey(selectedName))
                {
                    unreadCounts.Remove(selectedName); // Xóa số lượng tin chưa đọc
                    lstPeers.Invalidate(); // Vẽ lại để mất chấm đỏ
                }

                rtbChatHistory.Clear();
                if (chatLogs.ContainsKey(selectedName))
                {
                    rtbChatHistory.Text = chatLogs[selectedName];
                    rtbChatHistory.SelectionStart = rtbChatHistory.Text.Length;
                    rtbChatHistory.ScrollToCaret();
                }
                else
                {
                    AppendMessage("System", $"Bắt đầu cuộc trò chuyện với {selectedPeer.Name}...", Color.Gray);
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        }

        private void SendMessage()
        {
            string msg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg) || selectedPeer == null) return;
            string safeMsg = msg.Replace("|", "¦");

            try
            {
                netManager.SendMessage(selectedPeer.IP, selectedPeer.TcpPort, safeMsg);
                string myLog = $"[{DateTime.Now:HH:mm}] Me: {safeMsg}\r\n";
                if (!chatLogs.ContainsKey(selectedPeer.Name)) chatLogs[selectedPeer.Name] = "";
                chatLogs[selectedPeer.Name] += myLog;
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            netManager.Stop();
            base.OnFormClosing(e);
        }
    }
}