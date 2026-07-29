using System;
using System.Collections.Generic;
using UnityEngine;

public class SettlementManager : MonoBehaviour
{
    [Serializable]
    public class SettlementSummary
    {
        public int TotalRecordCount = 0;
        public int TotalNetRevenue = 0;
        public int UnsettledAmount = 0;
        public int CompletedAmount = 0;
        public int UnsettledCount = 0;
        public int CompletedCount = 0;
    }

    [Header("Dependencies")]
    [SerializeField] private DeliveryManager deliveryManager;

    private void Awake()
    {
        TryInitializeDependencies();
    }

    /// <summary>
    /// 외부 참조가 비어 있으면 자동 탐색
    /// </summary>
    private void TryInitializeDependencies()
    {
        if (deliveryManager == null)
        {
            deliveryManager = FindFirstObjectByType<DeliveryManager>();
        }
    }

    /// <summary>
    /// 전체 기록 기준 정산 요약
    /// </summary>
    public SettlementSummary GetAllSettlementSummary()
    {
        TryInitializeDependencies();

        if (deliveryManager == null)
        {
            Debug.LogWarning("[SettlementManager] DeliveryManager 참조가 없습니다.");
            return new SettlementSummary();
        }

        List<DeliveryRecord> allRecords = deliveryManager.GetAllRecords();
        return BuildSummary(allRecords);
    }

    /// <summary>
    /// 이번 달 기록 기준 정산 요약
    /// </summary>
    public SettlementSummary GetCurrentMonthSettlementSummary()
    {
        TryInitializeDependencies();

        if (deliveryManager == null)
        {
            Debug.LogWarning("[SettlementManager] DeliveryManager 참조가 없습니다.");
            return new SettlementSummary();
        }

        List<DeliveryRecord> monthRecords = deliveryManager.GetCurrentMonthRecords();
        return BuildSummary(monthRecords);
    }

    /// <summary>
    /// 공통 정산 요약 계산
    /// </summary>
    private SettlementSummary BuildSummary(List<DeliveryRecord> records)
    {
        SettlementSummary summary = new SettlementSummary();

        if (records == null)
        {
            return summary;
        }

        for (int i = 0; i < records.Count; i++)
        {
            DeliveryRecord record = records[i];

            if (record == null)
            {
                continue;
            }

            summary.TotalRecordCount += 1;
            summary.TotalNetRevenue += record.NetRevenue;

            if (record.IsSettled)
            {
                summary.CompletedAmount += record.NetRevenue;
                summary.CompletedCount += 1;
            }
            else
            {
                summary.UnsettledAmount += record.NetRevenue;
                summary.UnsettledCount += 1;
            }
        }

        return summary;
    }
}