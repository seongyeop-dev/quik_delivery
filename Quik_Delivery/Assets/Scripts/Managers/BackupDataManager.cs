using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class BackupDataManager : MonoBehaviour
{
    [Serializable]
    public class FullBackupData
    {
        public string BackupCreatedAt = string.Empty;
        public string BackupFileName = string.Empty;
        public List<DeliveryRecord> DeliveryRecords = new List<DeliveryRecord>();
        public List<WorkSession> WorkSessions = new List<WorkSession>();
    }

    [Header("Dependencies")]
    [SerializeField] private JsonFileService jsonFileService;
    [SerializeField] private DeliveryRepository deliveryRepository;
    [SerializeField] private WorkSessionRepository workSessionRepository;
    [SerializeField] private DeliveryManager deliveryManager;

    private void Awake()
    {
        TryInitializeDependencies();
    }

    /// <summary>
    /// 비어 있는 참조 자동 탐색
    /// </summary>
    private void TryInitializeDependencies()
    {
        if (jsonFileService == null)
        {
            jsonFileService = FindFirstObjectByType<JsonFileService>();
        }

        if (deliveryRepository == null)
        {
            deliveryRepository = FindFirstObjectByType<DeliveryRepository>();
        }

        if (workSessionRepository == null)
        {
            workSessionRepository = FindFirstObjectByType<WorkSessionRepository>();
        }
        if (deliveryManager == null)
        {
            deliveryManager = FindFirstObjectByType<DeliveryManager>();
        }
    }

    /// <summary>
    /// 전체 데이터 백업 생성
    /// 파일명 예:
    /// backup_20260510-0512.json
    /// </summary>
    public bool CreateFullBackup(out string backupFileName, out string backupFullPath, out string resultMessage)
    {
        backupFileName = string.Empty;
        backupFullPath = string.Empty;
        resultMessage = string.Empty;

        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            resultMessage = "JsonFileService 참조가 없습니다.";
            return false;
        }

        List<DeliveryRecord> deliveryRecords = deliveryRepository != null
            ? deliveryRepository.LoadAllRecords()
            : new List<DeliveryRecord>();

        List<WorkSession> workSessions = workSessionRepository != null
            ? workSessionRepository.LoadAllSessions()
            : new List<WorkSession>();

        DateTime backupDate = DateTime.Now.Date;
        DateTime firstDataDate = GetEarliestDataDate(deliveryRecords, workSessions, backupDate);

        backupFileName = BuildBackupFileName(firstDataDate, backupDate);
        backupFullPath = jsonFileService.GetFullPath(backupFileName);

        FullBackupData backupData = new FullBackupData
        {
            BackupCreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            BackupFileName = backupFileName,
            DeliveryRecords = new List<DeliveryRecord>(deliveryRecords),
            WorkSessions = new List<WorkSession>(workSessions)
        };

        jsonFileService.SaveToJson(backupFileName, backupData, true);

        resultMessage = $"백업 완료: {backupFileName}";
        Debug.Log($"[BackupDataManager] {resultMessage} / 경로: {backupFullPath}");
        return true;
    }

    /// <summary>
    /// persistentDataPath 안에서 가장 최근 백업 파일을 찾아 전체 데이터를 덮어쓴다.
    /// </summary>
    public bool ImportLatestBackup(out string importedFileName, out string importedFullPath, out string resultMessage)
    {
        importedFileName = string.Empty;
        importedFullPath = string.Empty;
        resultMessage = string.Empty;

        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            resultMessage = "JsonFileService 참조가 없습니다.";
            return false;
        }

        if (deliveryRepository == null)
        {
            resultMessage = "DeliveryRepository 참조가 없습니다.";
            return false;
        }

        if (workSessionRepository == null)
        {
            resultMessage = "WorkSessionRepository 참조가 없습니다.";
            return false;
        }

        if (!TryFindLatestBackupFile(out importedFileName, out importedFullPath))
        {
            resultMessage = "불러올 백업 파일이 없습니다.";
            return false;
        }

        FullBackupData backupData = LoadBackupDataFromFullPath(importedFullPath);

        if (backupData == null)
        {
            resultMessage = "백업 파일을 읽지 못했습니다.";
            return false;
        }

        if (backupData.DeliveryRecords == null)
        {
            backupData.DeliveryRecords = new List<DeliveryRecord>();
        }

        if (backupData.WorkSessions == null)
        {
            backupData.WorkSessions = new List<WorkSession>();
        }

        deliveryRepository.SaveAllRecords(backupData.DeliveryRecords);
        workSessionRepository.SaveAllSessions(backupData.WorkSessions);
        if (deliveryManager != null)
        {
            deliveryManager.EnsureMissingDailyFixedExpenses(DateTime.Today);
        }

        resultMessage = $"백업 불러오기 완료: {importedFileName}";
        Debug.Log($"[BackupDataManager] {resultMessage} / 경로: {importedFullPath}");
        return true;
    }

    /// <summary>
    /// 사용자가 파일 선택창에서 직접 선택한 백업 JSON 파일을 불러온다.
    /// NativeFilePicker가 반환한 파일 경로를 기준으로 복원한다.
    /// </summary>
    public bool ImportBackupFromFilePath(string backupFilePath, out string importedFileName, out string importedFullPath, out string resultMessage)
    {
        importedFileName = string.Empty;
        importedFullPath = string.Empty;
        resultMessage = string.Empty;

        TryInitializeDependencies();

        if (deliveryRepository == null)
        {
            resultMessage = "DeliveryRepository 참조가 없습니다.";
            return false;
        }

        if (workSessionRepository == null)
        {
            resultMessage = "WorkSessionRepository 참조가 없습니다.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            resultMessage = "선택된 백업 파일이 없습니다.";
            return false;
        }

        if (!File.Exists(backupFilePath))
        {
            resultMessage = "선택한 백업 파일을 찾을 수 없습니다.";
            return false;
        }

        importedFullPath = backupFilePath;
        importedFileName = Path.GetFileName(backupFilePath);

        if (!importedFileName.StartsWith("backup_", StringComparison.OrdinalIgnoreCase) ||
            !importedFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            resultMessage = "backup_*.json 형식의 백업 파일을 선택해 주세요.";
            return false;
        }

        FullBackupData backupData = LoadBackupDataFromFullPath(backupFilePath);

        if (backupData == null)
        {
            resultMessage = "백업 파일을 읽지 못했습니다.";
            return false;
        }

        if (backupData.DeliveryRecords == null)
        {
            backupData.DeliveryRecords = new List<DeliveryRecord>();
        }

        if (backupData.WorkSessions == null)
        {
            backupData.WorkSessions = new List<WorkSession>();
        }

        deliveryRepository.SaveAllRecords(backupData.DeliveryRecords);
        workSessionRepository.SaveAllSessions(backupData.WorkSessions);
        if (deliveryManager != null)
        {
            deliveryManager.EnsureMissingDailyFixedExpenses(DateTime.Today);
        }

        resultMessage = $"백업 불러오기 완료: {importedFileName}";
        Debug.Log($"[BackupDataManager] {resultMessage} / 경로: {importedFullPath}");
        return true;
    }

    /// <summary>
    /// 앱 내부 최신 백업 파일을 Download 폴더로 내보낸다.
    /// </summary>
    public bool ExportLatestBackupToDownloads(out string exportedFileName, out string exportedFullPath, out string resultMessage)
    {
        exportedFileName = string.Empty;
        exportedFullPath = string.Empty;
        resultMessage = string.Empty;

        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            resultMessage = "JsonFileService 참조가 없습니다.";
            return false;
        }

        if (!TryFindLatestInternalBackupFile(out string sourceFileName, out string sourceFullPath))
        {
            resultMessage = "내보낼 백업 파일이 없습니다. 먼저 전체 데이터 백업을 실행하세요.";
            return false;
        }

        exportedFileName = sourceFileName;

        if (TryCopyFileToDownloads(sourceFullPath, exportedFileName, out exportedFullPath, out string copyError))
        {
            resultMessage = $"백업 내보내기 완료: {exportedFileName}";
            Debug.Log($"[BackupDataManager] {resultMessage} / 경로: {exportedFullPath}");
            return true;
        }

        resultMessage = $"백업 내보내기 실패: {copyError}";
        Debug.LogWarning($"[BackupDataManager] {resultMessage}");
        return false;
    }

    /// <summary>
    /// 앱 내부 persistentDataPath 안의 backup_*.json 중 가장 최근 수정 파일을 찾는다.
    /// 내보내기 원본은 반드시 앱 내부 백업 파일이어야 한다.
    /// </summary>
    private bool TryFindLatestInternalBackupFile(out string latestFileName, out string latestFullPath)
    {
        latestFileName = string.Empty;
        latestFullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(Application.persistentDataPath))
        {
            return false;
        }

        return TryFindLatestBackupFileInDirectory(Application.persistentDataPath, out latestFileName, out latestFullPath);
    }

    /// <summary>
    /// 앱 내부와 Download 폴더의 backup_*.json 중 가장 최근 수정 파일을 찾는다.
    /// </summary>
    private bool TryFindLatestBackupFile(out string latestFileName, out string latestFullPath)
    {
        latestFileName = string.Empty;
        latestFullPath = string.Empty;

        List<string> searchDirectories = new List<string>
        {
            Application.persistentDataPath,
            GetDownloadDirectoryPath()
        };

        bool hasFile = false;
        DateTime newestWriteTime = DateTime.MinValue;

        for (int i = 0; i < searchDirectories.Count; i++)
        {
            string directory = searchDirectories[i];

            if (!TryFindLatestBackupFileInDirectory(directory, out string fileName, out string fullPath))
            {
                continue;
            }

            DateTime writeTime = File.GetLastWriteTime(fullPath);

            if (!hasFile || writeTime > newestWriteTime)
            {
                hasFile = true;
                newestWriteTime = writeTime;
                latestFileName = fileName;
                latestFullPath = fullPath;
            }
        }

        return hasFile;
    }

    /// <summary>
    /// 지정 폴더 안의 backup_*.json 중 가장 최근 수정 파일을 찾는다.
    /// </summary>
    private bool TryFindLatestBackupFileInDirectory(string directoryPath, out string latestFileName, out string latestFullPath)
    {
        latestFileName = string.Empty;
        latestFullPath = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return false;
            }

            string[] files = Directory.GetFiles(directoryPath, "backup_*.json", SearchOption.TopDirectoryOnly);

            if (files == null || files.Length == 0)
            {
                return false;
            }

            string newestPath = files[0];
            DateTime newestWriteTime = File.GetLastWriteTime(files[0]);

            for (int i = 1; i < files.Length; i++)
            {
                DateTime writeTime = File.GetLastWriteTime(files[i]);

                if (writeTime > newestWriteTime)
                {
                    newestWriteTime = writeTime;
                    newestPath = files[i];
                }
            }

            latestFullPath = newestPath;
            latestFileName = Path.GetFileName(newestPath);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BackupDataManager] 백업 파일 탐색 실패: {directoryPath} / {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 백업 JSON 파일을 전체 경로 기준으로 읽는다.
    /// </summary>
    private FullBackupData LoadBackupDataFromFullPath(string fullPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                return null;
            }

            string json = File.ReadAllText(fullPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<FullBackupData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BackupDataManager] 백업 파일 읽기 실패: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 백업 파일을 Download 폴더로 복사한다.
    /// Android에서는 일반 파일 복사 실패 시 MediaStore 방식으로 한 번 더 시도한다.
    /// </summary>
    private bool TryCopyFileToDownloads(string sourceFullPath, string fileName, out string destinationFullPath, out string errorMessage)
    {
        destinationFullPath = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(sourceFullPath) || !File.Exists(sourceFullPath))
        {
            errorMessage = "원본 백업 파일이 없습니다.";
            return false;
        }

        string downloadDirectory = GetDownloadDirectoryPath();
        string directDestinationPath = Path.Combine(downloadDirectory, fileName);

        if (TryCopyFileDirectly(sourceFullPath, directDestinationPath, out string directError))
        {
            destinationFullPath = directDestinationPath;
            return true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        byte[] fileBytes = File.ReadAllBytes(sourceFullPath);

        if (TrySaveBytesToAndroidDownloads(fileName, fileBytes, out string mediaStorePath, out string mediaStoreError))
        {
            destinationFullPath = mediaStorePath;
            return true;
        }

        errorMessage = $"{directError} / MediaStore 실패: {mediaStoreError}";
        return false;
#else
        errorMessage = directError;
        return false;
#endif
    }

    /// <summary>
    /// 일반 파일 복사 방식으로 Download 폴더에 복사한다.
    /// </summary>
    private bool TryCopyFileDirectly(string sourceFullPath, string destinationFullPath, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            string targetDirectory = Path.GetDirectoryName(destinationFullPath);

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(sourceFullPath, destinationFullPath, true);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Android MediaStore를 사용해서 Download 폴더에 저장한다.
    /// Android 10 이상에서 일반 파일 복사가 막힐 때 사용한다.
    /// </summary>
    private bool TrySaveBytesToAndroidDownloads(string fileName, byte[] fileBytes, out string savedPath, out string errorMessage)
    {
        savedPath = string.Empty;
        errorMessage = string.Empty;

        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (AndroidJavaClass mediaStoreDownloads = new AndroidJavaClass("android.provider.MediaStore$Downloads"))
            using (AndroidJavaObject downloadsUri = mediaStoreDownloads.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
            using (AndroidJavaObject contentValues = new AndroidJavaObject("android.content.ContentValues"))
            using (AndroidJavaClass mediaColumns = new AndroidJavaClass("android.provider.MediaStore$MediaColumns"))
            {
                string displayNameKey = mediaColumns.GetStatic<string>("DISPLAY_NAME");
                string mimeTypeKey = mediaColumns.GetStatic<string>("MIME_TYPE");
                string relativePathKey = mediaColumns.GetStatic<string>("RELATIVE_PATH");

                contentValues.Call("put", displayNameKey, fileName);
                contentValues.Call("put", mimeTypeKey, "application/json");
                contentValues.Call("put", relativePathKey, "Download/");

                using (AndroidJavaObject uri = resolver.Call<AndroidJavaObject>("insert", downloadsUri, contentValues))
                {
                    if (uri == null)
                    {
                        errorMessage = "Download URI 생성 실패";
                        return false;
                    }

                    using (AndroidJavaObject outputStream = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
                    {
                        if (outputStream == null)
                        {
                            errorMessage = "OutputStream 생성 실패";
                            return false;
                        }

                        outputStream.Call("write", fileBytes);
                        outputStream.Call("flush");
                        outputStream.Call("close");
                    }
                }

                savedPath = $"Download/{fileName}";
                return true;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
#endif

    /// <summary>
    /// 플랫폼별 Download 폴더 경로 반환
    /// </summary>
    private string GetDownloadDirectoryPath()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return "/storage/emulated/0/Download";
#else
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
#endif
    }

    /// <summary>
    /// 배송기록/근무기록 전체에서 가장 빠른 날짜를 찾는다.
    /// 데이터가 없으면 오늘 날짜를 사용한다.
    /// </summary>
    private DateTime GetEarliestDataDate(
        List<DeliveryRecord> deliveryRecords,
        List<WorkSession> workSessions,
        DateTime fallbackDate)
    {
        DateTime earliestDate = fallbackDate;
        bool hasDate = false;

        if (deliveryRecords != null)
        {
            for (int i = 0; i < deliveryRecords.Count; i++)
            {
                DeliveryRecord record = deliveryRecords[i];

                if (record == null || string.IsNullOrWhiteSpace(record.Date))
                {
                    continue;
                }

                if (TryParseDate(record.Date, out DateTime parsedDate))
                {
                    if (!hasDate || parsedDate < earliestDate)
                    {
                        earliestDate = parsedDate;
                        hasDate = true;
                    }
                }
            }
        }

        if (workSessions != null)
        {
            for (int i = 0; i < workSessions.Count; i++)
            {
                WorkSession session = workSessions[i];

                if (session == null || string.IsNullOrWhiteSpace(session.Date))
                {
                    continue;
                }

                if (TryParseDate(session.Date, out DateTime parsedDate))
                {
                    if (!hasDate || parsedDate < earliestDate)
                    {
                        earliestDate = parsedDate;
                        hasDate = true;
                    }
                }
            }
        }

        return hasDate ? earliestDate : fallbackDate;
    }

    /// <summary>
    /// yyyy-MM-dd 형식 날짜 파싱
    /// </summary>
    private bool TryParseDate(string dateText, out DateTime parsedDate)
    {
        return DateTime.TryParseExact(
            dateText,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsedDate);
    }

    /// <summary>
    /// 파일명 생성
    /// 같은 해이면 뒤쪽은 MMdd만 사용
    /// 예:
    /// backup_20260510-0512.json
    /// </summary>
    private string BuildBackupFileName(DateTime firstDataDate, DateTime backupDate)
    {
        if (firstDataDate.Year == backupDate.Year)
        {
            return $"backup_{firstDataDate:yyyyMMdd}-{backupDate:MMdd}.json";
        }

        return $"backup_{firstDataDate:yyyyMMdd}-{backupDate:yyyyMMdd}.json";
    }
}
