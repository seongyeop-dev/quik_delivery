using System;
using System.IO;
using UnityEngine;

public class JsonFileService : MonoBehaviour
{
    /// <summary>
    /// 파일명을 실제 저장 경로로 변환한다.
    /// </summary>
    public string GetFullPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    /// <summary>
    /// JSON 파일 저장
    /// </summary>
    public void SaveToJson<T>(string fileName, T data, bool prettyPrint = true)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogWarning("[JsonFileService] fileName이 비어 있습니다.");
            return;
        }

        if (data == null)
        {
            Debug.LogWarning("[JsonFileService] 저장할 data가 null입니다.");
            return;
        }

        try
        {
            string fullPath = GetFullPath(fileName);
            string json = JsonUtility.ToJson(data, prettyPrint);

            File.WriteAllText(fullPath, json);
            Debug.Log($"[JsonFileService] 저장 완료: {fullPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JsonFileService] 저장 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// JSON 파일 불러오기
    /// 파일이 없거나 실패하면 new T() 반환
    /// </summary>
    public T LoadFromJson<T>(string fileName) where T : new()
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogWarning("[JsonFileService] fileName이 비어 있습니다.");
            return new T();
        }

        try
        {
            string fullPath = GetFullPath(fileName);

            if (!File.Exists(fullPath))
            {
                return new T();
            }

            string json = File.ReadAllText(fullPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new T();
            }

            T loadedData = JsonUtility.FromJson<T>(json);

            if (loadedData == null)
            {
                return new T();
            }

            return loadedData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JsonFileService] 불러오기 실패: {ex.Message}");
            return new T();
        }
    }

    /// <summary>
    /// 파일 존재 여부 확인
    /// </summary>
    public bool Exists(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        string fullPath = GetFullPath(fileName);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// 파일 삭제
    /// </summary>
    public void DeleteFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        try
        {
            string fullPath = GetFullPath(fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log($"[JsonFileService] 파일 삭제 완료: {fullPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JsonFileService] 파일 삭제 실패: {ex.Message}");
        }
    }
}