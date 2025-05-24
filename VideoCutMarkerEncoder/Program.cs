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
        /// 애플리케이션 메인 진입점
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // 애플리케이션 경로
                string appPath = Application.StartupPath;

                // 필요한 폴더 생성
                CreateRequiredFolders(appPath);

                // FFmpeg 확인
                CheckFFmpeg(appPath);

                // 메인 폼 실행
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"애플리케이션 시작 중 오류가 발생했습니다:\n\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// 필요한 폴더 생성
        /// </summary>
        private static void CreateRequiredFolders(string appPath)
        {
            // 필수 폴더 생성
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
        /// FFmpeg 확인 및 안내
        /// </summary>
        private static void CheckFFmpeg(string appPath)
        {
            string ffmpegPath = Path.Combine(appPath, "FFmpeg", "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                DialogResult result = MessageBox.Show(
                    "비디오 처리를 위해 FFmpeg가 필요합니다.\n\n" +
                    "다음 단계에 따라 FFmpeg를 설치하세요:\n\n" +
                    "1. https://ffmpeg.org/download.html 에서 FFmpeg 다운로드\n" +
                    "2. 다운로드한 파일의 압축을 풀기\n" +
                    "3. ffmpeg.exe 파일을 찾아 다음 경로에 복사:\n" +
                    $"   {ffmpegPath}\n\n" +
                    "FFmpeg 공식 웹사이트를 지금 방문하시겠습니까?",
                    "FFmpeg 필요",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    // 웹 브라우저로 FFmpeg 다운로드 페이지 열기
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