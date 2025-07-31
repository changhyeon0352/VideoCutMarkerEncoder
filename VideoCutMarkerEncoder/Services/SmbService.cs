using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using VideoCutMarkerEncoder.Models;

namespace VideoCutMarkerEncoder.Services
{
    /// <summary>
    /// 파일 수신 이벤트 인자
    /// </summary>
    public class FileReceivedEventArgs : EventArgs
    {
        public string FilePath { get; set; }
    }

    /// <summary>
    /// SMB 서비스 클래스 - Windows 파일 공유 설정 및 파일 감시
    /// </summary>
    public class SmbService
    {
        private readonly HashSet<string> _processedFiles = new HashSet<string>();
        private readonly SettingsManager _settingsManager;
        private FileSystemWatcher _watcher;
        private bool _isRunning;

        /// <summary>
        /// 서비스 실행 중 여부
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 파일 수신 이벤트
        /// </summary>
        public event EventHandler<FileReceivedEventArgs> FileReceived;

        /// <summary>
        /// 생성자
        /// </summary>
        public SmbService(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;

            // 와처 초기화
            InitializeWatcher();
        }

        /// <summary>
        /// 파일 시스템 와처 초기화
        /// </summary>
        private void InitializeWatcher()
        {
            try
            {
                // 공유 폴더가 없으면 생성
                if (!Directory.Exists(_settingsManager.Settings.ShareFolder))
                {
                    Directory.CreateDirectory(_settingsManager.Settings.ShareFolder);
                }

                // 파일 시스템 와처 설정
                _watcher = new FileSystemWatcher(_settingsManager.Settings.ShareFolder)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    Filter = "*.json", // VCM 메타데이터 파일만 감시
                    EnableRaisingEvents = false
                };
                _watcher.EnableRaisingEvents = true;
                _watcher.Created += OnFileCreated;
                _watcher.Changed += OnFileChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 와처 초기화 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 서비스 시작
        /// </summary>
        public void StartService()
        {
            try
            {

                // 공유 폴더 확인
                if (!Directory.Exists(_settingsManager.Settings.ShareFolder))
                {
                    Directory.CreateDirectory(_settingsManager.Settings.ShareFolder);
                }

                // 와처 재설정 (공유 폴더가 변경되었을 수 있음)
                if (_watcher.Path != _settingsManager.Settings.ShareFolder)
                {
                    _watcher.Dispose();
                    InitializeWatcher();
                }

                // Windows 내장 SMB 공유 설정
                SetupWindowsShare();

                // 파일 감시 시작
                _watcher.EnableRaisingEvents = true;

                _isRunning = true;

                Debug.WriteLine("SMB 서비스가 시작되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SMB 서비스 시작 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 서비스 중지
        /// </summary>
        public void StopService()
        {
            try
            {
                // 파일 감시 중지
                _watcher.EnableRaisingEvents = false;

                // Windows 내장 SMB 공유 해제
                RemoveWindowsShare();

                _isRunning = false;

                Debug.WriteLine("SMB 서비스가 중지되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SMB 서비스 중지 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 컴퓨터 이름 가져오기
        /// </summary>
        public string GetComputerName()
        {
            return Environment.MachineName;
        }

        /// <summary>
        /// IP 주소 가져오기
        /// </summary>
        public string GetIPAddress()
        {
            try
            {
                // 로컬 IP 주소 가져오기
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                foreach (IPAddress address in addresses)
                {
                    // IPv4 주소만 필터링
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return address.ToString();
                    }
                }

                return "127.0.0.1";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IP 주소 가져오기 오류: {ex.Message}");
                return "알 수 없음";
            }
        }

        /// <summary>
        /// Windows 공유 설정
        /// </summary>
        private void SetupWindowsShare()
        {
            try
            {
                // 기존 공유가 있으면 제거
                RemoveWindowsShare();

                // net share 명령으로 공유 설정
                string args = $"{_settingsManager.Settings.ShareName}=\"{_settingsManager.Settings.ShareFolder}\" /GRANT:Everyone,FULL";

                // net share 명령 실행
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "net";
                    process.StartInfo.Arguments = $"share {args}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        throw new Exception($"공유 설정 실패: {error}");
                    }
                }

                Debug.WriteLine($"SMB 공유 설정 완료: \\\\{GetComputerName()}\\{_settingsManager.Settings.ShareName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Windows 공유 설정 오류: {ex.Message}");

                // 관리자 권한 확인
                if (ex.Message.Contains("액세스가 거부되었습니다") ||
                    ex.Message.Contains("Access is denied"))
                {
                    MessageBox.Show(
                        "SMB 공유를 설정하려면 관리자 권한이 필요합니다.\n\n" +
                        "앱을 관리자 권한으로 실행하세요.",
                        "권한 오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }

                throw;
            }
        }

        /// <summary>
        /// Windows 공유 해제
        /// </summary>
        private void RemoveWindowsShare()
        {
            try
            {
                // net share 명령으로 공유 해제
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "net";
                    process.StartInfo.Arguments = $"share {_settingsManager.Settings.ShareName} /DELETE /Y";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    process.WaitForExit();
                }

                Debug.WriteLine($"SMB 공유 해제 완료: {_settingsManager.Settings.ShareName}");
            }
            catch (Exception ex)
            {
                // 기존 공유가 없는 경우 오류 무시
                Debug.WriteLine($"Windows 공유 해제 오류 (무시됨): {ex.Message}");
            }
        }

        /// <summary>
        /// 파일 생성 이벤트 처리
        /// </summary>
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            ProcessNewFile(e.FullPath);
        }

        /// <summary>
        /// 파일 변경 이벤트 처리
        /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            //ProcessNewFile(e.FullPath);
        }

        /// <summary>
        /// 새 파일 처리
        /// </summary>
        private void ProcessNewFile(string filePath)
        {
            // 중복 처리 방지
            if (_processedFiles.Contains(filePath))
                return;

            _processedFiles.Add(filePath);
            // 1초 후 제거 (다음 변경 감지를 위해)
            Task.Delay(2000).ContinueWith(_ => _processedFiles.Remove(filePath));

            // 파일 확장자 확인
            if (Path.GetExtension(filePath).ToLower() == ".json")
            {
                Debug.WriteLine($"새 메타데이터 파일 감지: {filePath}");

                // 잠시 대기 (파일 쓰기가 완료될 때까지)
                System.Threading.Thread.Sleep(1000);

                try
                {
                    // 파일이 존재하는지 확인
                    if (File.Exists(filePath))
                    {
                        // 이벤트 발생
                        FileReceived?.Invoke(this, new FileReceivedEventArgs { FilePath = filePath });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"파일 처리 오류: {ex.Message}");
                }
            }
        }
    }
}