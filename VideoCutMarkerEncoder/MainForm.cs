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

            // ù ���� ���� Ȯ�� �� �ʱ� ���� ���̵� ǥ��
            if (settingsManager.IsFirstRun)
            {
                ShowWelcomeGuide();
                settingsManager.IsFirstRun = false;
                settingsManager.SaveSettings();
            }
        }

        private void InitializeServices()
        {
            // ���� �Ŵ��� �ʱ�ȭ (���ø����̼� ��� ���)
            settingsManager = new SettingsManager(Application.StartupPath);

            // ���� ���μ��� �ʱ�ȭ
            videoProcessor = new VideoProcessor(settingsManager);
            videoProcessor.ProcessingProgress += OnProcessingProgress;
            videoProcessor.ProcessingCompleted += OnProcessingCompleted;

            // SMB ���� �ʱ�ȭ
            smbService = new SmbService(settingsManager);
            smbService.FileReceived += OnFileReceived;

            // UI ������Ʈ
            UpdateServiceStatus();
        }

        private void SetupTrayIcon()
        {
            // Ʈ���� ������ ����
            trayIcon = new NotifyIcon
            {
                Icon = this.Icon,
                Visible = true,
                Text = "VideoCutMarker"
            };

            // ���ؽ�Ʈ �޴� ����
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("����", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            contextMenu.Items.Add("����", null, (s, e) => ShowSettings());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("����", null, (s, e) => Application.Exit());

            trayIcon.ContextMenuStrip = contextMenu;
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };
        }

        private void ShowWelcomeGuide()
        {
            MessageBox.Show(
                "VideoCutMarker PC �ۿ� ���� ���� ȯ���մϴ�!\n\n" +
                "�� ���� ����� VideoCutMarker���� ������ ������ �ڵ����� ó���մϴ�.\n\n" +
                "1. ������ ����� '����' ��ư�� Ŭ���Ͽ� ���񽺸� �����ϼ���.\n" +
                "2. ����� �ۿ��� PC ������ ������ ������ �� �ֽ��ϴ�.\n" +
                "3. ���ڵ��� �ڵ����� ����ǰ� ���¸� Ȯ���� �� �ֽ��ϴ�.\n\n" +
                "���� �޴����� �� ���� �ɼ��� Ȯ���ϼ���.",
                "VideoCutMarker �����ϱ�",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void UpdateServiceStatus()
        {
            bool isRunning = smbService.IsRunning;

            // ���� ǥ��
            lblStatus.Text = isRunning ? "���� ��" : "������";
            lblStatus.ForeColor = isRunning ? Color.Green : Color.Red;

            // ��ư �ؽ�Ʈ ����
            btnToggleService.Text = isRunning ? "����" : "����";

            // ���� ���� ǥ��
            if (isRunning)
            {
                txtShareInfo.Text = $"SMB ���� �ּ�: \\\\{smbService.GetComputerName()}\\{settingsManager.Settings.ShareName}\r\n" +
                                   $"��� ����: {settingsManager.Settings.OutputFolder}";
            }
            else
            {
                txtShareInfo.Text = "���񽺰� �����Ǿ����ϴ�. '����' ��ư�� ���� ���񽺸� �����ϼ���.";
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
                        $"���񽺸� �����ϴ� �� ������ �߻��߽��ϴ�:\n\n{ex.Message}",
                        "����",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }

            UpdateServiceStatus();
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
                    // ������ ����Ǿ����� ���� �����
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
            // UI �����忡�� ����
            this.Invoke(new Action(() =>
            {
                try
                {
                    // ��Ÿ������ ���� �Ľ�
                    var metadata = MetadataParser.ParseMetadataFile(e.FilePath);

                    // �� �۾� ����
                    var task = new ProcessingTask
                    {
                        Metadata = metadata,
                        FilePath = e.FilePath,
                        Status = "��� ��",
                        Progress = 0
                    };

                    // �۾� ��Ͽ� �߰�
                    processingTasks.Add(task);

                    // UI ������Ʈ
                    AddTaskToListView(task);

                    // �۾� ó�� ����
                    videoProcessor.EnqueueTask(task);

                    // �˸� ǥ��
                    if (this.WindowState == FormWindowState.Minimized || !this.Visible)
                    {
                        trayIcon.ShowBalloonTip(
                            3000,
                            "�� ���� ����",
                            $"����: {Path.GetFileName(e.FilePath)}\nó���� ���۵Ǿ����ϴ�.",
                            ToolTipIcon.Info
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"���� ó�� �� ������ �߻��߽��ϴ�:\n\n{ex.Message}",
                        "����",
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
            // UI �����忡�� ����
            this.Invoke(new Action(() =>
            {
                // �۾� ã��
                var task = processingTasks.Find(t => t.TaskId == e.TaskId);
                if (task != null)
                {
                    // �۾� ���� ������Ʈ
                    task.Status = e.Status;
                    task.Progress = e.Progress;

                    // UI ������Ʈ
                    UpdateTaskInListView(task);
                }
            }));
        }

        private void OnProcessingCompleted(object sender, ProcessingCompletedEventArgs e)
        {
            // UI �����忡�� ����
            this.Invoke(new Action(() =>
            {
                // �۾� ã��
                var task = processingTasks.Find(t => t.TaskId == e.TaskId);
                if (task != null)
                {
                    // �۾� ���� ������Ʈ
                    task.Status = e.Success ? "�Ϸ�" : "����";
                    task.Progress = e.Success ? 100 : 0;
                    task.OutputPath = e.OutputFilePath;

                    // UI ������Ʈ
                    UpdateTaskInListView(task);

                    // �˸� ǥ��
                    if (settingsManager.Settings.NotifyOnComplete &&
                        (this.WindowState == FormWindowState.Minimized || !this.Visible))
                    {
                        trayIcon.ShowBalloonTip(
                            3000,
                            e.Success ? "ó�� �Ϸ�" : "ó�� ����",
                            $"����: {Path.GetFileName(task.Metadata.VideoFileName)}\n" +
                            $"{(e.Success ? "���������� ó���Ǿ����ϴ�." : "ó�� �� ������ �߻��߽��ϴ�: " + e.ErrorMessage)}",
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

                    // ���� ����
                    if (task.Status == "�Ϸ�")
                    {
                        item.ForeColor = Color.Green;
                    }
                    else if (task.Status == "����")
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
                    // ��� ���� ����
                    Process.Start("explorer.exe", $"/select,\"{task.OutputPath}\"");
                }
                else
                {
                    MessageBox.Show(
                        "��� ������ ã�� �� �����ϴ�.",
                        "����",
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
                    "��� ������ �������� �ʽ��ϴ�.",
                    "����",
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
                    "���� Ʈ���̷� �ּ�ȭ�Ǿ����ϴ�. ����Ŭ���Ͽ� �� �� �ֽ��ϴ�.",
                    ToolTipIcon.Info
                );

                return;
            }

            base.OnFormClosing(e);

            // ���� �� ���� ����
            if (smbService.IsRunning)
            {
                smbService.StopService();
            }

            // Ʈ���� ������ ����
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        }
    }
}