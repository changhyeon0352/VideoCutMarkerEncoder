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
        /// 서비스 시작 시 기존 파일들 확인 및 처리 (새로 추가)
        /// </summary>
        public void CheckExistingFilesOnStartup()
        {
            try
            {
                // JSON 파일들 검색
                string[] jsonFiles = Directory.GetFiles(_settingsManager.Settings.ShareFolder, "*.json");

                if (jsonFiles.Length > 0)
                {
                    // 사용자 확인
                    string message = $"Share 폴더에 {jsonFiles.Length}개의 대기 중인 파일이 있습니다.\n지금 모든 파일을 인코딩하시겠습니까?";

                    DialogResult result = MessageBox.Show(message, "대기 중인 파일 발견",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // ⭐ 기존 ProcessNewFile() 메서드 재사용!
                        foreach (string jsonFile in jsonFiles)
                        {
                            ProcessNewFile(jsonFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"기존 파일 확인 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 서비스 중지
        /// </summary>
        public void StopService(bool isStopShare = true)
        {
            try
            {
                // 파일 감시 중지
                _watcher.EnableRaisingEvents = false;
                

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

        /// <summary>
        /// Windows 공유가 활성화되어 있는지 확인 (앱 외부에서 설정한 경우도 포함)
        /// </summary>
        public bool IsShareActive()
        {
            try
            {
                // net share 명령으로 현재 공유 목록 조회
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "net";
                    process.StartInfo.Arguments = "share";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // 출력에서 설정된 ShareName이 있는지 확인
                    // net share 출력 형식: "ShareName   C:\path   Remark"
                    return output.Contains(_settingsManager.Settings.ShareName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"공유 상태 확인 오류: {ex.Message}");
                return false;
            }
        }
    }
}