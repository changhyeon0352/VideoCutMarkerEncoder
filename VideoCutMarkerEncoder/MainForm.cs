using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel;
using VideoCutMarkerEncoder.Services;
using VideoCutMarkerEncoder.Models;

namespace VideoCutMarkerEncoder
{
    public partial class MainForm : Form
    {
        private SmbService smbService;
        private VideoProcessor videoProcessor;
        private SettingsManager settingsManager;
        private NotifyIcon trayIcon;
        private List<ProcessingTask> processingTasks = new List<ProcessingTask>();

        public MainForm()
        {

            InitializeComponent();
            InitializeServices();
            SetupTrayIcon();
            // ⭐ 폼 활성화 시 상태 갱신
            this.Activated += (s, e) => UpdateServiceStatus();
            this.Shown += MainForm_Shown;
            // 첫 실행 여부 확인 및 초기 설정 가이드 표시
            if (settingsManager.IsFirstRun)
            {
                ShowWelcomeGuide();
                settingsManager.IsFirstRun = false;
                settingsManager.SaveSettings();
            }
            
        }
        private void MainForm_Shown(object sender, EventArgs e)
        {
            // ⭐ 창이 완전히 표시된 후 기존 파일 체크
            smbService.CheckExistingFilesOnStartup();
        }

        private void InitializeServices()
        {
            // 설정 매니저 초기화 (애플리케이션 경로 기반)
            settingsManager = new SettingsManager(Application.StartupPath);

            // 비디오 프로세서 초기화
            videoProcessor = new VideoProcessor(settingsManager);
            videoProcessor.ProcessingProgress += OnProcessingProgress;
            videoProcessor.ProcessingCompleted += OnProcessingCompleted;

            // SMB 서비스 초기화
            smbService = new SmbService(settingsManager);
            smbService.FileReceived += OnFileReceived;
            

            // UI 업데이트
            UpdateServiceStatus();
        }

        private void SetupTrayIcon()
        {
            // 트레이 아이콘 설정
            trayIcon = new NotifyIcon
            {
                Icon = this.Icon,
                Visible = true,
                Text = "VideoCutMarker"
            };

            // 컨텍스트 메뉴 설정
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("열기", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            contextMenu.Items.Add("설정", null, (s, e) => ShowSettings());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("종료", null, (s, e) => Application.Exit());

            trayIcon.ContextMenuStrip = contextMenu;
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };
        }

        private void ShowWelcomeGuide()
        {
            MessageBox.Show(
                "VideoCutMarker PC 앱에 오신 것을 환영합니다!\n\n" +
                "이 앱은 모바일 VideoCutMarker에서 편집한 비디오를 자동으로 처리합니다.\n\n" +
                "1. 오른쪽 상단의 '시작' 버튼을 클릭하여 서비스를 시작하세요.\n" +
                "2. 모바일 앱에서 PC 앱으로 파일을 전송할 수 있습니다.\n" +
                "3. 인코딩이 자동으로 진행되고 상태를 확인할 수 있습니다.\n\n" +
                "설정 메뉴에서 더 많은 옵션을 확인하세요.",
                "VideoCutMarker 시작하기",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void UpdateServiceStatus()
        {
            bool isRunning = smbService.IsRunning;
            bool isShareActive = smbService.IsShareActive();

            // File monitoring status
            lblStatus.Text = isRunning ? "File Monitoring: Running" : "File Monitoring: Stopped";
            lblStatus.ForeColor = isRunning ? Color.Green : Color.Gray;


            // Share information display
            if (isShareActive)
            {
                txtShareInfo.Text = $"✅ SMB Share Status: Active\r\n" +
                                   $"SMB Share Address: \\\\{smbService.GetComputerName()}\\{settingsManager.Settings.ShareName}\r\n" +
                                   $"Share Folder: {settingsManager.Settings.ShareFolder}\r\n" +
                                   $"Output Folder: {settingsManager.Settings.OutputFolder}";
                txtShareInfo.ForeColor = Color.Green;

                // Hide help button when share is active
                btnShareHelp.Visible = false;
            }
            else
            {
                txtShareInfo.Text = $"❌ SMB Share Status: Inactive\r\n\r\n" +
                                   $"SMB share is not configured.\r\n" +
                                   $"Click the '?' button on the right to see how to set up sharing.\r\n\r\n" +
                                   $"Folder to share: {settingsManager.Settings.ShareFolder}\r\n" +
                                   $"Share name: {settingsManager.Settings.ShareName}";
                txtShareInfo.ForeColor = Color.Red;

                // Show help button when share is inactive
                btnShareHelp.Visible = true;
            }
        }

        private void btnToggleService_Click(object sender, EventArgs e)
        {
            if (smbService.IsRunning)
            {
                smbService.StopService();
            }
            else
            {
                try
                {
                    smbService.StartService();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"서비스를 시작하는 중 오류가 발생했습니다:\n\n{ex.Message}",
                        "오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }

            UpdateServiceStatus();
        }

        private void btnShareHelp_Click(object sender, EventArgs e)
        {
            string helpMessage =
                "📁 How to Set Up SMB Share\n\n" +
                "1. Open Windows Explorer and navigate to:\n" +
                $"   {settingsManager.Settings.ShareFolder}\n\n" +
                "2. Right-click the folder → Select 'Properties'\n\n" +
                "3. Go to 'Sharing' tab → Click 'Advanced Sharing' button\n\n" +
                "4. Check 'Share this folder'\n\n" +
                $"5. Share name: {settingsManager.Settings.ShareName}\n\n" +
                "6. Click 'Permissions' → Grant 'Full Control' to Everyone\n\n" +
                "7. Click OK → OK\n\n" +
                "✅ After setup, the share status will automatically change to 'Active'.\n\n" +
                "📌 Would you like to copy the folder path to clipboard?";

            DialogResult result = MessageBox.Show(
                helpMessage,
                "SMB Share Setup Guide",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    Clipboard.SetText(settingsManager.Settings.ShareFolder);
                    MessageBox.Show(
                        "Folder path copied to clipboard!",
                        "Copy Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to copy to clipboard: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            ShowSettings();
        }

        private void ShowSettings()
        {
            using (var settingsForm = new SettingsForm(settingsManager))
            {
                var result = settingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    // 설정이 변경되었으면 서비스 재시작
                    if (smbService.IsRunning)
                    {
                        smbService.StopService();
                        smbService.StartService();
                    }

                    UpdateServiceStatus();
                }
            }
        }

        private void OnFileReceived(object sender, FileReceivedEventArgs e)
        {
            // UI 스레드에서 실행
            this.Invoke(new Action(() =>
            {
                try
                {
                    // 메타데이터 파일 파싱
                    var metadata = MetadataParser.ParseMetadataFile(e.FilePath);

                    // 새 작업 생성
                    var task = new ProcessingTask
                    {
                        Metadata = metadata,
                        FilePath = e.FilePath,
                        Status = "대기 중",
                        Progress = 0
                    };

                    // 작업 목록에 추가
                    processingTasks.Add(task);

                    // UI 업데이트
                    AddTaskToListView(task);

                    // 작업 처리 시작
                    videoProcessor.EnqueueTask(task);

                    // 알림 표시
                    if (this.WindowState == FormWindowState.Minimized || !this.Visible)
                    {
                        trayIcon.ShowBalloonTip(
                            3000,
                            "새 파일 수신",
                            $"파일: {Path.GetFileName(e.FilePath)}\n처리가 시작되었습니다.",
                            ToolTipIcon.Info
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"파일 처리 중 오류가 발생했습니다:\n\n{ex.Message}",
                        "오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }));
        }

        private void AddTaskToListView(ProcessingTask task)
        {
            var item = new ListViewItem(Path.GetFileName(task.Metadata.VideoFileName));
            item.SubItems.Add(task.Status);
            item.SubItems.Add($"{task.Progress}%");
            item.Tag = task.TaskId;

            listTasks.Items.Add(item);
        }

        private void OnProcessingProgress(object sender, ProcessingProgressEventArgs e)
        {
            // UI 스레드에서 실행
            this.Invoke(new Action(() =>
            {
                // 작업 찾기
                var task = processingTasks.Find(t => t.TaskId == e.TaskId);
                if (task != null)
                {
                    // 작업 상태 업데이트
                    task.Status = e.Status;
                    task.Progress = e.Progress;

                    // UI 업데이트
                    UpdateTaskInListView(task);
                }
            }));
        }

        private void OnProcessingCompleted(object sender, ProcessingCompletedEventArgs e)
        {
            // UI 스레드에서 실행
            this.Invoke(new Action(() =>
            {
                // 작업 찾기
                var task = processingTasks.Find(t => t.TaskId == e.TaskId);
                if (task != null)
                {
                    // 작업 상태 업데이트
                    task.Status = e.Success ? "완료" : "실패";
                    task.Progress = e.Success ? 100 : 0;
                    task.OutputPath = e.OutputFilePath;

                    // UI 업데이트
                    UpdateTaskInListView(task);

                    // 알림 표시
                    if (settingsManager.Settings.NotifyOnComplete &&
                        (this.WindowState == FormWindowState.Minimized || !this.Visible))
                    {
                        trayIcon.ShowBalloonTip(
                            3000,
                            e.Success ? "처리 완료" : "처리 실패",
                            $"파일: {Path.GetFileName(task.Metadata.VideoFileName)}\n" +
                            $"{(e.Success ? "성공적으로 처리되었습니다." : "처리 중 오류가 발생했습니다: " + e.ErrorMessage)}",
                            e.Success ? ToolTipIcon.Info : ToolTipIcon.Error
                        );
                    }
                }
            }));
        }

        private void UpdateTaskInListView(ProcessingTask task)
        {
            foreach (ListViewItem item in listTasks.Items)
            {
                if ((string)item.Tag == task.TaskId)
                {
                    item.SubItems[1].Text = task.Status;
                    item.SubItems[2].Text = $"{task.Progress}%";

                    // 색상 설정
                    if (task.Status == "완료")
                    {
                        item.ForeColor = Color.Green;
                    }
                    else if (task.Status == "실패")
                    {
                        item.ForeColor = Color.Red;
                    }

                    return;
                }
            }
        }

        private void listTasks_DoubleClick(object sender, EventArgs e)
        {
            if (listTasks.SelectedItems.Count > 0)
            {
                string taskId = (string)listTasks.SelectedItems[0].Tag;
                var task = processingTasks.Find(t => t.TaskId == taskId);

                if (task != null && !string.IsNullOrEmpty(task.OutputPath) && File.Exists(task.OutputPath))
                {
                    // 출력 폴더 열기
                    Process.Start("explorer.exe", $"/select,\"{task.OutputPath}\"");
                }
                else
                {
                    MessageBox.Show(
                        "출력 파일을 찾을 수 없습니다.",
                        "정보",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
        }

        private void btnOpenOutput_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(settingsManager.Settings.OutputFolder))
            {
                Process.Start("explorer.exe", settingsManager.Settings.OutputFolder);
            }
            else
            {
                MessageBox.Show(
                    "출력 폴더가 존재하지 않습니다.",
                    "정보",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && settingsManager.Settings.MinimizeToTray)
            {
                e.Cancel = true;
                this.Hide();

                trayIcon.ShowBalloonTip(
                    2000,
                    "VideoCutMarker",
                    "앱이 트레이로 최소화되었습니다. 더블클릭하여 열 수 있습니다.",
                    ToolTipIcon.Info
                );

                return;
            }

            base.OnFormClosing(e);

            // 종료 시 서비스 중지
            if (smbService.IsRunning)
            {
                smbService.StopService(false);
            }

            // 트레이 아이콘 제거
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        }
    }
}