using System;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryRepository : MonoBehaviour
{
    [Serializable]
    private class DeliveryRecordCollection
    {
        public List<DeliveryRecord> Records = new List<DeliveryRecord>();
    }

    [Header("Dependencies")]
    [SerializeField] private JsonFileService jsonFileService;

    [Header("Storage Settings")]
    [SerializeField] private string fileName = "delivery_records.json";

    private void Awake()
    {
        TryInitializeDependencies();
    }

    /// <summary>
    /// 외부 참조가 비어 있으면 자동 탐색
    /// </summary>
    private void TryInitializeDependencies()
    {
        if (jsonFileService == null)
        {
            jsonFileService = FindFirstObjectByType<JsonFileService>();
        }
    }

    /// <summary>
    /// 전체 기록 불러오기
    /// </summary>
    public List<DeliveryRecord> LoadAllRecords()
    {
        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            Debug.LogWarning("[DeliveryRepository] JsonFileService 참조가 없습니다.");
            return new List<DeliveryRecord>();
        }

        DeliveryRecordCollection collection =
            jsonFileService.LoadFromJson<DeliveryRecordCollection>(fileName);

        if (collection == null || collection.Records == null)
        {
            return new List<DeliveryRecord>();
        }

        return new List<DeliveryRecord>(collection.Records);
    }

    /// <summary>
    /// 전체 기록 저장
    /// </summary>
    public void SaveAllRecords(List<DeliveryRecord> records)
    {
        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            Debug.LogWarning("[DeliveryRepository] JsonFileService 참조가 없습니다.");
            return;
        }

        DeliveryRecordCollection collection = new DeliveryRecordCollection();

        if (records != null)
        {
            collection.Records = new List<DeliveryRecord>(records);
        }

        jsonFileService.SaveToJson(fileName, collection);
    }

    /// <summary>
    /// 기록 1건 추가 저장
    /// </summary>
    public void AddRecord(DeliveryRecord record)
    {
        if (record == null)
        {
            Debug.LogWarning("[DeliveryRepository] 저장할 record가 null입니다.");
            return;
        }

        List<DeliveryRecord> records = LoadAllRecords();
        records.Add(record);
        SaveAllRecords(records);
    }

    /// <summary>
    /// ID로 기록 1건 조회
    /// </summary>
    public DeliveryRecord GetRecordById(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return null;
        }

        List<DeliveryRecord> records = LoadAllRecords();

        for (int i = 0; i < records.Count; i++)
        {
            DeliveryRecord record = records[i];

            if (record == null)
            {
                continue;
            }

            if (record.Id == recordId)
            {
                return record;
            }
        }

        return null;
    }

    /// <summary>
    /// 기록 1건 수정 저장
    /// </summary>
    public bool UpdateRecord(DeliveryRecord updatedRecord)
    {
        if (updatedRecord == null)
        {
            Debug.LogWarning("[DeliveryRepository] 수정할 record가 null입니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(updatedRecord.Id))
        {
            Debug.LogWarning("[DeliveryRepository] 수정할 record의 Id가 비어 있습니다.");
            return false;
        }

        List<DeliveryRecord> records = LoadAllRecords();

        for (int i = 0; i < records.Count; i++)
        {
            DeliveryRecord record = records[i];

            if (record == null)
            {
                continue;
            }

            if (record.Id == updatedRecord.Id)
            {
                records[i] = updatedRecord;
                SaveAllRecords(records);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ID 기준으로 기록 1건 삭제
    /// </summary>
    public bool DeleteRecordById(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            Debug.LogWarning("[DeliveryRepository] 삭제할 recordId가 비어 있습니다.");
            return false;
        }

        List<DeliveryRecord> records = LoadAllRecords();

        for (int i = 0; i < records.Count; i++)
        {
            DeliveryRecord record = records[i];

            if (record == null)
            {
                continue;
            }

            if (record.Id == recordId)
            {
                records.RemoveAt(i);
                SaveAllRecords(records);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 오늘 기록만 가져오기
    /// </summary>
    public List<DeliveryRecord> GetTodayRecords()
    {
        List<DeliveryRecord> allRecords = LoadAllRecords();
        List<DeliveryRecord> todayRecords = new List<DeliveryRecord>();

        string today = DateTime.Now.ToString("yyyy-MM-dd");

        for (int i = 0; i < allRecords.Count; i++)
        {
            DeliveryRecord record = allRecords[i];

            if (record == null)
            {
                continue;
            }

            if (record.Date == today)
            {
                todayRecords.Add(record);
            }
        }

        return todayRecords;
    }

    /// <summary>
    /// 현재 월 기록만 가져오기
    /// </summary>
    public List<DeliveryRecord> GetCurrentMonthRecords()
    {
        List<DeliveryRecord> allRecords = LoadAllRecords();
        List<DeliveryRecord> monthRecords = new List<DeliveryRecord>();

        string currentMonthPrefix = DateTime.Now.ToString("yyyy-MM");

        for (int i = 0; i < allRecords.Count; i++)
        {
            DeliveryRecord record = allRecords[i];

            if (record == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(record.Date) &&
                record.Date.StartsWith(currentMonthPrefix))
            {
                monthRecords.Add(record);
            }
        }

        return monthRecords;
    }
}