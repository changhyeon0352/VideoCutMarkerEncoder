using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using VideoCutMarkerEncoder;
using VideoCutMarkerEncoder.Services;

namespace VideoCutMarker.Desktop
{
    static class Program
    {
        /// <summary>
        /// ���ø����̼� ���� ������
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // ���ø����̼� ���
                string appPath = Application.StartupPath;

                // �ʿ��� ���� ����
                CreateRequiredFolders(appPath);

                // FFmpeg Ȯ��
                CheckFFmpeg(appPath);

                // ���� �� ����
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"���ø����̼� ���� �� ������ �߻��߽��ϴ�:\n\n{ex.Message}",
                    "����",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// �ʿ��� ���� ����
        /// </summary>
        private static void CreateRequiredFolders(string appPath)
        {
            // �ʼ� ���� ����
            string[] folders = {
                Path.Combine(appPath, "Share"),
                Path.Combine(appPath, "Output"),
                Path.Combine(appPath, "Config"),
                Path.Combine(appPath, "FFmpeg")
            };

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        /// <summary>
        /// FFmpeg Ȯ�� �� �ȳ�
        /// </summary>
        private static void CheckFFmpeg(string appPath)
        {
            string ffmpegPath = Path.Combine(appPath, "FFmpeg", "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                DialogResult result = MessageBox.Show(
                    "���� ó���� ���� FFmpeg�� �ʿ��մϴ�.\n\n" +
                    "���� �ܰ迡 ���� FFmpeg�� ��ġ�ϼ���:\n\n" +
                    "1. https://ffmpeg.org/download.html ���� FFmpeg �ٿ�ε�\n" +
                    "2. �ٿ�ε��� ������ ������ Ǯ��\n" +
                    "3. ffmpeg.exe ������ ã�� ���� ��ο� ����:\n" +
                    $"   {ffmpegPath}\n\n" +
                    "FFmpeg ���� ������Ʈ�� ���� �湮�Ͻðڽ��ϱ�?",
                    "FFmpeg �ʿ�",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    // �� �������� FFmpeg �ٿ�ε� ������ ����
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://ffmpeg.org/download.html",
                        UseShellExecute = true
                    });
                }
            }
        }
    }
}