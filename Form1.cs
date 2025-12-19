using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LanP2PChat
{
    // Class lưu cấu trúc tin nhắn
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public bool IsNew { get; set; } // Biến này quyết định việc in đậm
        public DateTime Time { get; set; }
    }

    public partial class Form1 : Form
    {
        private NetworkManager netManager;
        private List<PeerInfo> peerList = new List<PeerInfo>();
        private PeerInfo selectedPeer = null;
        private string myName;

        // 1. SỬA ĐỔI: Lưu lịch sử chat dạng Danh sách (List) thay vì chuỗi dài
        private Dictionary<string, List<ChatMessage>> chatLogs = new Dictionary<string, List<ChatMessage>>();

        // 2. Lưu số lượng tin nhắn chưa đọc
        private Dictionary<string, int> unreadCounts = new Dictionary<string, int>();

        public Form1()
        {
            InitializeComponent();

            // --- CẤU HÌNH LISTBOX 
            lstPeers.DrawMode = DrawMode.OwnerDrawFixed;
            lstPeers.ItemHeight = 40;
            lstPeers.DrawItem += LstPeers_DrawItem;

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

        // --- HÀM VẼ GIAO DIỆN ---
        private void LstPeers_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            string peerName = lstPeers.Items[e.Index].ToString();
            int count = 0;
            if (unreadCounts.ContainsKey(peerName))
            {
                count = unreadCounts[peerName];
            }

            e.DrawBackground();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (Brush bgBrush = isSelected ? new SolidBrush(Color.FromArgb(0, 120, 215)) : new SolidBrush(lstPeers.BackColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            Font nameFont;
            Color textColor;

            if (count > 0)
            {
                nameFont = new Font("Segoe UI", 11, FontStyle.Bold);
                textColor = isSelected ? Color.White : lstPeers.ForeColor;
            }
            else
            {
                nameFont = new Font("Segoe UI", 10, FontStyle.Regular);
                textColor = isSelected ? Color.White : lstPeers.ForeColor;
            }

            using (Brush textBrush = new SolidBrush(textColor))
            {
                float textY = e.Bounds.Y + (e.Bounds.Height - nameFont.Height) / 2;
                e.Graphics.DrawString(peerName, nameFont, textBrush, e.Bounds.X + 10, textY);
            }

            if (count > 0)
            {
                string countText = count > 99 ? "99+" : count.ToString();
                int circleSize = 22;
                int circleX = e.Bounds.Right - circleSize - 10;
                int circleY = e.Bounds.Y + (e.Bounds.Height - circleSize) / 2;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Brush redBrush = new SolidBrush(Color.Red))
                {
                    e.Graphics.FillEllipse(redBrush, circleX, circleY, circleSize, circleSize);
                }

                using (Font numberFont = new Font("Segoe UI", 9, FontStyle.Bold))
                {
                    SizeF textSize = e.Graphics.MeasureString(countText, numberFont);
                    float textX = circleX + (circleSize - textSize.Width) / 2;
                    float textY = circleY + (circleSize - textSize.Height) / 2;
                    e.Graphics.DrawString(countText, numberFont, Brushes.White, textX, textY);
                }
            }
            nameFont.Dispose();
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
                    lstPeers.Items.Add(peer.Name);
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
                // 1. Tạo đối tượng tin nhắn MỚI (IsNew = true)
                ChatMessage newMsg = new ChatMessage
                {
                    Sender = sender,
                    Content = content,
                    Time = DateTime.Now,
                    IsNew = true // Quan trọng: Đánh dấu tin chưa đọc
                };

                // 2. Thêm vào List thay vì cộng chuỗi
                if (!chatLogs.ContainsKey(sender)) chatLogs[sender] = new List<ChatMessage>();
                chatLogs[sender].Add(newMsg);

                // 3. Xử lý hiển thị
                if (selectedPeer != null && selectedPeer.Name == sender)
                {
                    // Đang mở chat -> Hiện luôn và đánh dấu đã đọc
                    AppendMessage(sender, content, Color.Black, true);
                    newMsg.IsNew = false; 
                }
                else
                {
                    // Không mở chat -> Tăng số tin chưa đọc
                    if (!unreadCounts.ContainsKey(sender)) unreadCounts[sender] = 0;
                    unreadCounts[sender]++;
                    lstPeers.Invalidate();
                }
            }));
        }

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

        // --- GIAO DIỆN (ĐÃ SỬA LỖI LOGIC) ---
        private void lstPeers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstPeers.SelectedIndex == -1) return;

            string selectedName = lstPeers.SelectedItem.ToString();
            selectedPeer = peerList.Find(p => p.Name == selectedName);

            if (selectedPeer != null)
            {
                lblChatHeader.Text = $"Đang chat với: {selectedPeer.Name}";
                btnSend.Enabled = true;
                txtMessage.Focus();

                // Xóa chấm đỏ thông báo
                if (unreadCounts.ContainsKey(selectedName))
                {
                    unreadCounts.Remove(selectedName);
                    lstPeers.Invalidate();
                }

                // LOAD LẠI LỊCH SỬ CHAT TỪ LIST
                rtbChatHistory.Clear();

                if (chatLogs.ContainsKey(selectedName))
                {
                    var messages = chatLogs[selectedName]; // Lấy danh sách tin nhắn

                    // Duyệt qua từng tin nhắn để vẽ
                    foreach (var msg in messages)
                    {
                        Color color = (msg.Sender == "Me") ? Color.Blue : Color.Black;
                        
                        // Nếu msg.IsNew là true -> Hàm AppendMessage sẽ in đậm
                        AppendMessage(msg.Sender, msg.Content, color, msg.IsNew);

                        // Sau khi in xong, đánh dấu là đã đọc
                        msg.IsNew = false;
                    }
                }
                else
                {
                    AppendMessage("System", $"Bắt đầu cuộc trò chuyện với {selectedPeer.Name}...", Color.Gray, false);
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

                // Tạo tin nhắn của mình (IsNew = false vì mình tự gửi)
                ChatMessage myMsg = new ChatMessage
                {
                    Sender = "Me",
                    Content = safeMsg,
                    Time = DateTime.Now,
                    IsNew = false
                };

                // Lưu vào List
                if (!chatLogs.ContainsKey(selectedPeer.Name)) chatLogs[selectedPeer.Name] = new List<ChatMessage>();
                chatLogs[selectedPeer.Name].Add(myMsg);

                // Hiển thị lên
                AppendMessage("Me", safeMsg, Color.Blue, false);
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi tin: " + ex.Message);
            }
        }

        // --- HÀM IN TIN NHẮN (ĐÃ THÊM THAM SỐ IN ĐẬM) ---
        private void AppendMessage(string sender, string content, Color color, bool isBold)
        {
            rtbChatHistory.SelectionStart = rtbChatHistory.TextLength;
            rtbChatHistory.SelectionLength = 0;
            
            // 1. In tên người gửi (Luôn đậm)
            rtbChatHistory.SelectionColor = color;
            rtbChatHistory.SelectionFont = new Font(rtbChatHistory.Font, FontStyle.Bold);
            rtbChatHistory.AppendText($"[{DateTime.Now:HH:mm}] {sender}: ");

            // 2. In nội dung (Đậm nếu chưa đọc, Thường nếu đã đọc)
            rtbChatHistory.SelectionColor = Color.Black;
            if (isBold)
            {
                rtbChatHistory.SelectionFont = new Font(rtbChatHistory.Font, FontStyle.Bold); // In đậm nội dung
            }
            else
            {
                rtbChatHistory.SelectionFont = new Font(rtbChatHistory.Font, FontStyle.Regular); // Chữ thường
            }
            
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