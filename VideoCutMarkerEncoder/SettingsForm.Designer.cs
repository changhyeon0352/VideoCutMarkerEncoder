using System;
using System.IO;
using System.Windows.Forms;
using VideoCutMarkerEncoder.Models;

namespace VideoCutMarkerEncoder
{

    public partial class SettingsForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private AppSettings _originalSettings;

        public SettingsForm(SettingsManager settingsManager)
        {
            InitializeComponent();

            _settingsManager = settingsManager;

            // 원본 설정 복사
            _originalSettings = _settingsManager.Settings;

            // UI에 설정 표시
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            // 폴더 설정
            txtShareFolder.Text = _originalSettings.ShareFolder;
            txtOutputFolder.Text = _originalSettings.OutputFolder;

            // 일반 설정
            txtShareName.Text = _originalSettings.ShareName;
            chkMinimizeToTray.Checked = _originalSettings.MinimizeToTray;
            chkNotifyOnComplete.Checked = _originalSettings.NotifyOnComplete;

            // 인코딩 설정
            cboVideoCodec.SelectedItem = _originalSettings.VideoCodec;
            cboAudioCodec.SelectedItem = _originalSettings.AudioCodec;
            trkVideoQuality.Value = _originalSettings.VideoQuality;
            lblQualityValue.Text = _originalSettings.VideoQuality.ToString();
            cboEncodingSpeed.SelectedItem = _originalSettings.EncodingSpeed;
        }

        private void btnBrowseShareFolder_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = txtShareFolder.Text;
                folderDialog.Description = "공유 폴더 선택";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtShareFolder.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void btnFFmpegHelp_Click(object sender, EventArgs e)
        {
            string appPath = Application.StartupPath;
            string ffmpegPath = Path.Combine(appPath, "FFmpeg", "ffmpeg.exe");

            MessageBox.Show(
                "FFmpeg 설치 방법:\n\n" +
                "1. https://ffmpeg.org/download.html 에서 FFmpeg 다운로드\n" +
                "   - Windows 사용자: 'Windows builds from gyan.dev' 링크 클릭\n" +
                "   - 'ffmpeg-release-essentials.zip' 파일 다운로드\n\n" +
                "2. 다운로드한 ZIP 파일 압축 풀기\n\n" +
                "3. bin 폴더 안의 ffmpeg.exe 파일을 찾아\n" +
                $"   {ffmpegPath} 경로에 복사\n\n" +
                "FFmpeg는 GPL 및 LGPL 라이선스 하에 제공됩니다.\n" +
                "자세한 정보: https://ffmpeg.org/legal.html",
                "FFmpeg 설치 안내",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void btnBrowseOutputFolder_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = txtOutputFolder.Text;
                folderDialog.Description = "출력 폴더 선택";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputFolder.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void trkVideoQuality_Scroll(object sender, EventArgs e)
        {
            lblQualityValue.Text = trkVideoQuality.Value.ToString();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                // 폴더 유효성 검사
                if (!Directory.Exists(txtShareFolder.Text))
                {
                    Directory.CreateDirectory(txtShareFolder.Text);
                }

                if (!Directory.Exists(txtOutputFolder.Text))
                {
                    Directory.CreateDirectory(txtOutputFolder.Text);
                }

                // 설정 저장
                _settingsManager.Settings.ShareFolder = txtShareFolder.Text;
                _settingsManager.Settings.OutputFolder = txtOutputFolder.Text;
                _settingsManager.Settings.ShareName = txtShareName.Text;
                _settingsManager.Settings.MinimizeToTray = chkMinimizeToTray.Checked;
                _settingsManager.Settings.NotifyOnComplete = chkNotifyOnComplete.Checked;
                _settingsManager.Settings.VideoCodec = cboVideoCodec.SelectedItem.ToString();
                _settingsManager.Settings.AudioCodec = cboAudioCodec.SelectedItem.ToString();
                _settingsManager.Settings.VideoQuality = trkVideoQuality.Value;
                _settingsManager.Settings.EncodingSpeed = cboEncodingSpeed.SelectedItem.ToString();

                _settingsManager.SaveSettings();

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 저장 중 오류가 발생했습니다:\n\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnResetDefault_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "모든 설정을 기본값으로 초기화하시겠습니까?",
                "설정 초기화",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // 기본 설정값 (앱 폴더 기준)
                string appFolder = Application.StartupPath;

                txtShareFolder.Text = Path.Combine(appFolder, "Share");
                txtOutputFolder.Text = Path.Combine(appFolder, "Output");
                txtShareName.Text = "VideoCutMarker";
                chkMinimizeToTray.Checked = true;
                chkNotifyOnComplete.Checked = true;
                cboVideoCodec.SelectedItem = "libx264";
                cboAudioCodec.SelectedItem = "aac";
                trkVideoQuality.Value = 23;
                lblQualityValue.Text = "23";
                cboEncodingSpeed.SelectedItem = "medium";
            }
        }

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.grpFolders = new System.Windows.Forms.GroupBox();
            this.btnBrowseOutputFolder = new System.Windows.Forms.Button();
            this.txtOutputFolder = new System.Windows.Forms.TextBox();
            this.lblOutputFolder = new System.Windows.Forms.Label();
            this.btnBrowseShareFolder = new System.Windows.Forms.Button();
            this.txtShareFolder = new System.Windows.Forms.TextBox();
            this.lblShareFolder = new System.Windows.Forms.Label();
            this.grpGeneral = new System.Windows.Forms.GroupBox();
            this.chkNotifyOnComplete = new System.Windows.Forms.CheckBox();
            this.chkMinimizeToTray = new System.Windows.Forms.CheckBox();
            this.txtShareName = new System.Windows.Forms.TextBox();
            this.lblShareName = new System.Windows.Forms.Label();
            this.tabEncoding = new System.Windows.Forms.TabPage();
            this.grpEncodingSettings = new System.Windows.Forms.GroupBox();
            this.lblQualityValue = new System.Windows.Forms.Label();
            this.lblQualityInfo = new System.Windows.Forms.Label();
            this.trkVideoQuality = new System.Windows.Forms.TrackBar();
            this.lblQuality = new System.Windows.Forms.Label();
            this.cboEncodingSpeed = new System.Windows.Forms.ComboBox();
            this.lblEncodingSpeed = new System.Windows.Forms.Label();
            this.cboAudioCodec = new System.Windows.Forms.ComboBox();
            this.lblAudioCodec = new System.Windows.Forms.Label();
            this.cboVideoCodec = new System.Windows.Forms.ComboBox();
            this.lblVideoCodec = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnResetDefault = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.grpFolders.SuspendLayout();
            this.grpGeneral.SuspendLayout();
            this.tabEncoding.SuspendLayout();
            this.grpEncodingSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkVideoQuality)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabGeneral);
            this.tabControl.Controls.Add(this.tabEncoding);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(460, 315);
            this.tabControl.TabIndex = 0;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.grpFolders);
            this.tabGeneral.Controls.Add(this.grpGeneral);
            this.tabGeneral.Location = new System.Drawing.Point(4, 24);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeneral.Size = new System.Drawing.Size(452, 287);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "일반 설정";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // grpFolders
            // 
            this.grpFolders.Controls.Add(this.btnBrowseOutputFolder);
            this.grpFolders.Controls.Add(this.txtOutputFolder);
            this.grpFolders.Controls.Add(this.lblOutputFolder);
            this.grpFolders.Controls.Add(this.btnBrowseShareFolder);
            this.grpFolders.Controls.Add(this.txtShareFolder);
            this.grpFolders.Controls.Add(this.lblShareFolder);
            this.grpFolders.Location = new System.Drawing.Point(17, 125);
            this.grpFolders.Name = "grpFolders";
            this.grpFolders.Size = new System.Drawing.Size(418, 146);
            this.grpFolders.TabIndex = 1;
            this.grpFolders.TabStop = false;
            this.grpFolders.Text = "폴더 설정";
            // 
            // btnBrowseOutputFolder
            // 
            this.btnBrowseOutputFolder.Location = new System.Drawing.Point(375, 103);
            this.btnBrowseOutputFolder.Name = "btnBrowseOutputFolder";
            this.btnBrowseOutputFolder.Size = new System.Drawing.Size(37, 23);
            this.btnBrowseOutputFolder.TabIndex = 5;
            this.btnBrowseOutputFolder.Text = "...";
            this.btnBrowseOutputFolder.UseVisualStyleBackColor = true;
            this.btnBrowseOutputFolder.Click += new System.EventHandler(this.btnBrowseOutputFolder_Click);
            // 
            // txtOutputFolder
            // 
            this.txtOutputFolder.Location = new System.Drawing.Point(15, 103);
            this.txtOutputFolder.Name = "txtOutputFolder";
            this.txtOutputFolder.Size = new System.Drawing.Size(354, 23);
            this.txtOutputFolder.TabIndex = 4;
            // 
            // lblOutputFolder
            // 
            this.lblOutputFolder.AutoSize = true;
            this.lblOutputFolder.Location = new System.Drawing.Point(15, 85);
            this.lblOutputFolder.Name = "lblOutputFolder";
            this.lblOutputFolder.Size = new System.Drawing.Size(59, 15);
            this.lblOutputFolder.TabIndex = 3;
            this.lblOutputFolder.Text = "출력 폴더";
            // 
            // btnBrowseShareFolder
            // 
            this.btnBrowseShareFolder.Location = new System.Drawing.Point(375, 47);
            this.btnBrowseShareFolder.Name = "btnBrowseShareFolder";
            this.btnBrowseShareFolder.Size = new System.Drawing.Size(37, 23);
            this.btnBrowseShareFolder.TabIndex = 2;
            this.btnBrowseShareFolder.Text = "...";
            this.btnBrowseShareFolder.UseVisualStyleBackColor = true;
            this.btnBrowseShareFolder.Click += new System.EventHandler(this.btnBrowseShareFolder_Click);
            // 
            // txtShareFolder
            // 
            this.txtShareFolder.Location = new System.Drawing.Point(15, 47);
            this.txtShareFolder.Name = "txtShareFolder";
            this.txtShareFolder.Size = new System.Drawing.Size(354, 23);
            this.txtShareFolder.TabIndex = 1;
            // 
            // lblShareFolder
            // 
            this.lblShareFolder.AutoSize = true;
            this.lblShareFolder.Location = new System.Drawing.Point(15, 29);
            this.lblShareFolder.Name = "lblShareFolder";
            this.lblShareFolder.Size = new System.Drawing.Size(59, 15);
            this.lblShareFolder.TabIndex = 0;
            this.lblShareFolder.Text = "공유 폴더";
            // 
            // grpGeneral
            // 
            this.grpGeneral.Controls.Add(this.chkNotifyOnComplete);
            this.grpGeneral.Controls.Add(this.chkMinimizeToTray);
            this.grpGeneral.Controls.Add(this.txtShareName);
            this.grpGeneral.Controls.Add(this.lblShareName);
            this.grpGeneral.Location = new System.Drawing.Point(17, 16);
            this.grpGeneral.Name = "grpGeneral";
            this.grpGeneral.Size = new System.Drawing.Size(418, 103);
            this.grpGeneral.TabIndex = 0;
            this.grpGeneral.TabStop = false;
            this.grpGeneral.Text = "일반 설정";
            // 
            // chkNotifyOnComplete
            // 
            this.chkNotifyOnComplete.AutoSize = true;
            this.chkNotifyOnComplete.Location = new System.Drawing.Point(179, 72);
            this.chkNotifyOnComplete.Name = "chkNotifyOnComplete";
            this.chkNotifyOnComplete.Size = new System.Drawing.Size(106, 19);
            this.chkNotifyOnComplete.TabIndex = 3;
            this.chkNotifyOnComplete.Text = "완료 시 알림";
            this.chkNotifyOnComplete.UseVisualStyleBackColor = true;
            // 
            // chkMinimizeToTray
            // 
            this.chkMinimizeToTray.AutoSize = true;
            this.chkMinimizeToTray.Location = new System.Drawing.Point(15, 72);
            this.chkMinimizeToTray.Name = "chkMinimizeToTray";
            this.chkMinimizeToTray.Size = new System.Drawing.Size(158, 19);
            this.chkMinimizeToTray.TabIndex = 2;
            this.chkMinimizeToTray.Text = "트레이로 최소화";
            this.chkMinimizeToTray.UseVisualStyleBackColor = true;
            // 
            // txtShareName
            // 
            this.txtShareName.Location = new System.Drawing.Point(155, 29);
            this.txtShareName.Name = "txtShareName";
            this.txtShareName.Size = new System.Drawing.Size(214, 23);
            this.txtShareName.TabIndex = 1;
            // 
            // lblShareName
            // 
            this.lblShareName.AutoSize = true;
            this.lblShareName.Location = new System.Drawing.Point(15, 32);
            this.lblShareName.Name = "lblShareName";
            this.lblShareName.Size = new System.Drawing.Size(134, 15);
            this.lblShareName.TabIndex = 0;
            this.lblShareName.Text = "SMB 공유 이름:";
            // 
            // tabEncoding
            // 
            this.tabEncoding.Controls.Add(this.grpEncodingSettings);
            this.tabEncoding.Location = new System.Drawing.Point(4, 24);
            this.tabEncoding.Name = "tabEncoding";
            this.tabEncoding.Padding = new System.Windows.Forms.Padding(3);
            this.tabEncoding.Size = new System.Drawing.Size(452, 287);
            this.tabEncoding.TabIndex = 1;
            this.tabEncoding.Text = "인코딩 설정";
            this.tabEncoding.UseVisualStyleBackColor = true;
            // 
            // grpEncodingSettings
            // 
            this.grpEncodingSettings.Controls.Add(this.lblQualityValue);
            this.grpEncodingSettings.Controls.Add(this.lblQualityInfo);
            this.grpEncodingSettings.Controls.Add(this.trkVideoQuality);
            this.grpEncodingSettings.Controls.Add(this.lblQuality);
            this.grpEncodingSettings.Controls.Add(this.cboEncodingSpeed);
            this.grpEncodingSettings.Controls.Add(this.lblEncodingSpeed);
            this.grpEncodingSettings.Controls.Add(this.cboAudioCodec);
            this.grpEncodingSettings.Controls.Add(this.lblAudioCodec);
            this.grpEncodingSettings.Controls.Add(this.cboVideoCodec);
            this.grpEncodingSettings.Controls.Add(this.lblVideoCodec);
            this.grpEncodingSettings.Location = new System.Drawing.Point(17, 16);
            this.grpEncodingSettings.Name = "grpEncodingSettings";
            this.grpEncodingSettings.Size = new System.Drawing.Size(418, 255);
            this.grpEncodingSettings.TabIndex = 0;
            this.grpEncodingSettings.TabStop = false;
            this.grpEncodingSettings.Text = "인코딩 설정";
            // 
            // lblQualityValue
            // 
            this.lblQualityValue.AutoSize = true;
            this.lblQualityValue.Location = new System.Drawing.Point(340, 159);
            this.lblQualityValue.Name = "lblQualityValue";
            this.lblQualityValue.Size = new System.Drawing.Size(29, 15);
            this.lblQualityValue.TabIndex = 9;
            this.lblQualityValue.Text = "23";
            // 
            // lblQualityInfo
            // 
            this.lblQualityInfo.AutoSize = true;
            this.lblQualityInfo.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblQualityInfo.Location = new System.Drawing.Point(155, 194);
            this.lblQualityInfo.Name = "lblQualityInfo";
            this.lblQualityInfo.Size = new System.Drawing.Size(223, 15);
            this.lblQualityInfo.TabIndex = 8;
            this.lblQualityInfo.Text = "낮을수록 고품질, 높을수록 저용량";
            // 
            // trkVideoQuality
            // 
            this.trkVideoQuality.Location = new System.Drawing.Point(155, 159);
            this.trkVideoQuality.Maximum = 51;
            this.trkVideoQuality.Minimum = 15;
            this.trkVideoQuality.Name = "trkVideoQuality";
            this.trkVideoQuality.Size = new System.Drawing.Size(179, 45);
            this.trkVideoQuality.TabIndex = 7;
            this.trkVideoQuality.TickFrequency = 2;
            this.trkVideoQuality.Value = 23;
            this.trkVideoQuality.Scroll += new System.EventHandler(this.trkVideoQuality_Scroll);
            // 
            // lblQuality
            // 
            this.lblQuality.AutoSize = true;
            this.lblQuality.Location = new System.Drawing.Point(15, 159);
            this.lblQuality.Name = "lblQuality";
            this.lblQuality.Size = new System.Drawing.Size(80, 15);
            this.lblQuality.TabIndex = 6;
            this.lblQuality.Text = "품질 (CRF):";
            // 
            // cboEncodingSpeed
            // 
            this.cboEncodingSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEncodingSpeed.FormattingEnabled = true;
            this.cboEncodingSpeed.Items.AddRange(new object[] {
            "ultrafast",
            "superfast",
            "veryfast",
            "faster",
            "fast",
            "medium",
            "slow",
            "slower",
            "veryslow"});
            this.cboEncodingSpeed.Location = new System.Drawing.Point(155, 115);
            this.cboEncodingSpeed.Name = "cboEncodingSpeed";
            this.cboEncodingSpeed.Size = new System.Drawing.Size(214, 23);
            this.cboEncodingSpeed.TabIndex = 5;
            // 
            // lblEncodingSpeed
            // 
            this.lblEncodingSpeed.AutoSize = true;
            this.lblEncodingSpeed.Location = new System.Drawing.Point(15, 118);
            this.lblEncodingSpeed.Name = "lblEncodingSpeed";
            this.lblEncodingSpeed.Size = new System.Drawing.Size(94, 15);
            this.lblEncodingSpeed.TabIndex = 4;
            this.lblEncodingSpeed.Text = "인코딩 속도:";
            // 
            // cboAudioCodec
            // 
            this.cboAudioCodec.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAudioCodec.FormattingEnabled = true;
            this.cboAudioCodec.Items.AddRange(new object[] {
            "aac",
            "mp3",
            "copy"});
            this.cboAudioCodec.Location = new System.Drawing.Point(155, 74);
            this.cboAudioCodec.Name = "cboAudioCodec";
            this.cboAudioCodec.Size = new System.Drawing.Size(214, 23);
            this.cboAudioCodec.TabIndex = 3;
            // 
            // lblAudioCodec
            // 
            this.lblAudioCodec.AutoSize = true;
            this.lblAudioCodec.Location = new System.Drawing.Point(15, 77);
            this.lblAudioCodec.Name = "lblAudioCodec";
            this.lblAudioCodec.Size = new System.Drawing.Size(109, 15);
            this.lblAudioCodec.TabIndex = 2;
            this.lblAudioCodec.Text = "오디오 코덱:";
            // 
            // cboVideoCodec
            // 
            this.cboVideoCodec.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboVideoCodec.FormattingEnabled = true;
            this.cboVideoCodec.Items.AddRange(new object[] {
            "libx264",
            "libx265",
            "h264_nvenc",
            "hevc_nvenc"});
            this.cboVideoCodec.Location = new System.Drawing.Point(155, 32);
            this.cboVideoCodec.Name = "cboVideoCodec";
            this.cboVideoCodec.Size = new System.Drawing.Size(214, 23);
            this.cboVideoCodec.TabIndex = 1;
            // 
            // lblVideoCodec
            // 
            this.lblVideoCodec.AutoSize = true;
            this.lblVideoCodec.Location = new System.Drawing.Point(15, 35);
            this.lblVideoCodec.Name = "lblVideoCodec";
            this.lblVideoCodec.Size = new System.Drawing.Size(94, 15);
            this.lblVideoCodec.TabIndex = 0;
            this.lblVideoCodec.Text = "비디오 코덱:";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(235, 341);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "확인";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(316, 341);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnResetDefault
            // 
            this.btnResetDefault.Location = new System.Drawing.Point(397, 341);
            this.btnResetDefault.Name = "btnResetDefault";
            this.btnResetDefault.Size = new System.Drawing.Size(75, 23);
            this.btnResetDefault.TabIndex = 3;
            this.btnResetDefault.Text = "기본값";
            this.btnResetDefault.UseVisualStyleBackColor = true;
            this.btnResetDefault.Click += new System.EventHandler(this.btnResetDefault_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 376);
            this.Controls.Add(this.btnResetDefault);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tabControl);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "설정";
            this.tabControl.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.grpFolders.ResumeLayout(false);
            this.grpFolders.PerformLayout();
            this.grpGeneral.ResumeLayout(false);
            this.grpGeneral.PerformLayout();
            this.tabEncoding.ResumeLayout(false);
            this.grpEncodingSettings.ResumeLayout(false);
            this.grpEncodingSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkVideoQuality)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.GroupBox grpGeneral;
        private System.Windows.Forms.Label lblShareName;
        private System.Windows.Forms.TextBox txtShareName;
        private System.Windows.Forms.TabPage tabEncoding;
        private System.Windows.Forms.GroupBox grpFolders;
        private System.Windows.Forms.Button btnBrowseShareFolder;
        private System.Windows.Forms.TextBox txtShareFolder;
        private System.Windows.Forms.Label lblShareFolder;
        private System.Windows.Forms.Button btnBrowseOutputFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.GroupBox grpEncodingSettings;
        private System.Windows.Forms.Label lblVideoCodec;
        private System.Windows.Forms.ComboBox cboVideoCodec;
        private System.Windows.Forms.Label lblAudioCodec;
        private System.Windows.Forms.ComboBox cboAudioCodec;
        private System.Windows.Forms.Label lblQuality;
        private System.Windows.Forms.ComboBox cboEncodingSpeed;
        private System.Windows.Forms.Label lblEncodingSpeed;
        private System.Windows.Forms.TrackBar trkVideoQuality;
        private System.Windows.Forms.Label lblQualityInfo;
        private System.Windows.Forms.Label lblQualityValue;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnResetDefault;
        private System.Windows.Forms.CheckBox chkMinimizeToTray;
        private System.Windows.Forms.CheckBox chkNotifyOnComplete;
    }
}