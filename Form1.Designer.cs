namespace LanP2PChat
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // Khai báo các control giao diện
        private System.Windows.Forms.Panel panelSidebar;
        private System.Windows.Forms.Label lblHeaderUsers;
        private System.Windows.Forms.ListBox lstPeers;
        private System.Windows.Forms.Panel panelChatArea;
        private System.Windows.Forms.Panel panelInput;
        private System.Windows.Forms.RichTextBox rtbChatHistory;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label lblChatHeader;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelSidebar = new System.Windows.Forms.Panel();
            this.lstPeers = new System.Windows.Forms.ListBox();
            this.lblHeaderUsers = new System.Windows.Forms.Label();
            this.panelChatArea = new System.Windows.Forms.Panel();
            this.rtbChatHistory = new System.Windows.Forms.RichTextBox();
            this.lblChatHeader = new System.Windows.Forms.Label();
            this.panelInput = new System.Windows.Forms.Panel();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.panelSidebar.SuspendLayout();
            this.panelChatArea.SuspendLayout();
            this.panelInput.SuspendLayout();
            this.SuspendLayout();

            // --- 1. SIDEBAR (DANH SÁCH BẠN BÈ) ---
            this.panelSidebar.BackColor = System.Drawing.Color.FromArgb(45, 52, 71); // Màu xanh đen hiện đại
            this.panelSidebar.Controls.Add(this.lstPeers);
            this.panelSidebar.Controls.Add(this.lblHeaderUsers);
            this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelSidebar.Width = 200;

            // Label tiêu đề danh sách
            this.lblHeaderUsers.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblHeaderUsers.Height = 50;
            this.lblHeaderUsers.Text = "  ONLINE PEERS";
            this.lblHeaderUsers.ForeColor = System.Drawing.Color.White;
            this.lblHeaderUsers.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblHeaderUsers.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ListBox hiển thị tên người dùng
            this.lstPeers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstPeers.BackColor = System.Drawing.Color.FromArgb(45, 52, 71);
            this.lstPeers.ForeColor = System.Drawing.Color.LightGray;
            this.lstPeers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstPeers.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lstPeers.ItemHeight = 30;
            this.lstPeers.SelectedIndexChanged += new System.EventHandler(this.lstPeers_SelectedIndexChanged);

            // --- 2. KHUNG CHAT CHÍNH ---
            this.panelChatArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelChatArea.BackColor = System.Drawing.Color.FromArgb(240, 242, 245); // Màu xám sáng dịu mắt
            this.panelChatArea.Controls.Add(this.rtbChatHistory);
            this.panelChatArea.Controls.Add(this.lblChatHeader);
            this.panelChatArea.Controls.Add(this.panelInput);

            // Header khung chat (Đang chat với ai)
            this.lblChatHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblChatHeader.Height = 50;
            this.lblChatHeader.BackColor = System.Drawing.Color.White;
            this.lblChatHeader.Text = "  Chưa chọn người chat";
            this.lblChatHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblChatHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblChatHeader.ForeColor = System.Drawing.Color.FromArgb(64, 64, 64);

            // Lịch sử chat (RichTextBox)
            this.rtbChatHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbChatHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbChatHistory.BackColor = System.Drawing.Color.FromArgb(240, 242, 245);
            this.rtbChatHistory.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.rtbChatHistory.ReadOnly = true;
            this.rtbChatHistory.Padding = new System.Windows.Forms.Padding(10);

            // --- 3. KHUNG NHẬP LIỆU (DƯỚI CÙNG) ---
            this.panelInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelInput.Height = 60;
            this.panelInput.BackColor = System.Drawing.Color.White;
            this.panelInput.Padding = new System.Windows.Forms.Padding(10);
            this.panelInput.Controls.Add(this.btnSend);
            this.panelInput.Controls.Add(this.txtMessage);

            // Nút Gửi (Button)
            this.btnSend.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSend.Width = 80;
            this.btnSend.Text = "Gửi";
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.BackColor = System.Drawing.Color.FromArgb(0, 123, 255); // Màu xanh dương chuẩn
            this.btnSend.ForeColor = System.Drawing.Color.White;
            this.btnSend.FlatAppearance.BorderSize = 0;
            this.btnSend.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSend.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);

            // Ô nhập tin nhắn (TextBox)
            this.txtMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMessage.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.txtMessage.Multiline = true; // Để canh giữa text theo chiều dọc dễ hơn
            this.txtMessage.Margin = new System.Windows.Forms.Padding(0, 5, 10, 5);
            this.txtMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtMessage_KeyDown);

            // --- CẤU HÌNH FORM CHÍNH ---
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.panelChatArea);
            this.Controls.Add(this.panelSidebar);
            this.Name = "Form1";
            this.Text = "LAN P2P Chat (Modern UI)";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.panelSidebar.ResumeLayout(false);
            this.panelChatArea.ResumeLayout(false);
            this.panelInput.ResumeLayout(false);
            this.panelInput.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}