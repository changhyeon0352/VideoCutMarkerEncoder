namespace VideoCutMarkerEncoder
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panelHeader = new Panel();
            lblStatus = new Label();
            btnSettings = new Button();
            btnShareHelp = new Button();
            lblStatusTitle = new Label();
            panelInfo = new Panel();
            txtShareInfo = new TextBox();
            lblInfo = new Label();
            panelTasks = new Panel();
            btnOpenOutput = new Button();
            listTasks = new ListView();
            columnFileName = new ColumnHeader();
            columnStatus = new ColumnHeader();
            columnProgress = new ColumnHeader();
            lblTasks = new Label();
            
            panelHeader.SuspendLayout();
            panelInfo.SuspendLayout();
            panelTasks.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(240, 240, 240);
            panelHeader.Controls.Add(lblStatus);
            panelHeader.Controls.Add(btnShareHelp);
            panelHeader.Controls.Add(btnSettings);
            panelHeader.Controls.Add(lblStatusTitle);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(684, 60);
            // 
            // lblStatusTitle
            // 
            lblStatusTitle.AutoSize = true;
            lblStatusTitle.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatusTitle.Location = new Point(12, 20);
            lblStatusTitle.Name = "lblStatusTitle";
            lblStatusTitle.Text = "VideoCutMarker Encoder";
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(250, 20);
            lblStatus.AutoSize = true;
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "중지됨";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnSettings
            // 
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Location = new Point(470, 20);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(75, 23);
            btnSettings.Text = "Setting";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += btnSettings_Click;
            // 
            // btnShareHelp
            // 
            btnShareHelp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnShareHelp.Location = new Point(570, 20);
            btnShareHelp.Name = "btnShareHelp";
            btnShareHelp.Size = new Size(75, 23);
            btnShareHelp.Text = "?";
            btnShareHelp.UseVisualStyleBackColor = true;
            btnShareHelp.Click += btnShareHelp_Click;
            
            // 
            // panelInfo
            // 
            panelInfo.Controls.Add(txtShareInfo);
            panelInfo.Controls.Add(lblInfo);
            panelInfo.Dock = DockStyle.Top;
            panelInfo.Location = new Point(0, 60);
            panelInfo.Name = "panelInfo";
            panelInfo.Padding = new Padding(12);
            panelInfo.Size = new Size(684, 150);
            panelInfo.TabIndex = 1;
            // 
            // txtShareInfo
            // 
            txtShareInfo.BackColor = SystemColors.Window;
            txtShareInfo.Dock = DockStyle.Fill;
            txtShareInfo.Font = new Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtShareInfo.Location = new Point(12, 33);
            txtShareInfo.Multiline = true;
            txtShareInfo.Name = "txtShareInfo";
            txtShareInfo.ReadOnly = true;
            txtShareInfo.Size = new Size(660, 75);
            txtShareInfo.TabIndex = 1;
            txtShareInfo.Text = "서비스가 중지되었습니다. '시작' 버튼을 눌러 서비스를 시작하세요.";
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Dock = DockStyle.Top;
            lblInfo.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblInfo.Location = new Point(12, 12);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(78, 21);
            lblInfo.TabIndex = 0;
            lblInfo.Text = "공유 정보";
            // 
            // panelTasks
            // 
            panelTasks.Controls.Add(btnOpenOutput);
            panelTasks.Controls.Add(listTasks);
            panelTasks.Controls.Add(lblTasks);
            panelTasks.Dock = DockStyle.Fill;
            panelTasks.Location = new Point(0, 180);
            panelTasks.Name = "panelTasks";
            panelTasks.Padding = new Padding(12);
            panelTasks.Size = new Size(684, 281);
            panelTasks.TabIndex = 2;
            // 
            // btnOpenOutput
            // 
            btnOpenOutput.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOpenOutput.Location = new Point(570, 246);
            btnOpenOutput.Name = "btnOpenOutput";
            btnOpenOutput.Size = new Size(102, 23);
            btnOpenOutput.TabIndex = 2;
            btnOpenOutput.Text = "출력 폴더 열기";
            btnOpenOutput.UseVisualStyleBackColor = true;
            btnOpenOutput.Click += btnOpenOutput_Click;
            // 
            // listTasks
            // 
            listTasks.Columns.AddRange(new ColumnHeader[] { columnFileName, columnStatus, columnProgress });
            listTasks.Dock = DockStyle.Fill;
            listTasks.FullRowSelect = true;
            listTasks.GridLines = true;
            listTasks.Location = new Point(12, 33);
            listTasks.Name = "listTasks";
            listTasks.Size = new Size(660, 236);
            listTasks.TabIndex = 1;
            listTasks.UseCompatibleStateImageBehavior = false;
            listTasks.View = View.Details;
            listTasks.DoubleClick += listTasks_DoubleClick;
            // 
            // columnFileName
            // 
            columnFileName.Text = "파일명";
            columnFileName.Width = 300;
            // 
            // columnStatus
            // 
            columnStatus.Text = "상태";
            columnStatus.Width = 200;
            // 
            // columnProgress
            // 
            columnProgress.Text = "진행률";
            columnProgress.Width = 100;
            // 
            // lblTasks
            // 
            lblTasks.AutoSize = true;
            lblTasks.Dock = DockStyle.Top;
            lblTasks.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTasks.Location = new Point(12, 12);
            lblTasks.Name = "lblTasks";
            lblTasks.Size = new Size(78, 21);
            lblTasks.TabIndex = 0;
            lblTasks.Text = "처리 작업";
            
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(684, 461);
            Controls.Add(panelTasks);
            Controls.Add(panelInfo);
            Controls.Add(panelHeader);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(700, 500);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "VideoCutMarker";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelInfo.ResumeLayout(false);
            panelInfo.PerformLayout();
            panelTasks.ResumeLayout(false);
            panelTasks.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblStatusTitle;
        private System.Windows.Forms.Button btnShareHelp;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Panel panelInfo;
        private System.Windows.Forms.TextBox txtShareInfo;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Panel panelTasks;
        private System.Windows.Forms.Label lblTasks;
        private System.Windows.Forms.ListView listTasks;
        private System.Windows.Forms.ColumnHeader columnFileName;
        private System.Windows.Forms.ColumnHeader columnStatus;
        private System.Windows.Forms.ColumnHeader columnProgress;
        private System.Windows.Forms.Button btnOpenOutput;
    }
}