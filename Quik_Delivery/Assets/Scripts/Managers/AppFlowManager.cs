using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AppFlowManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private UIManager uiManager;

    [Header("Bottom Navigation Buttons")]
    [SerializeField] private Button btnHome;
    [SerializeField] private Button btnRecord;
    [SerializeField] private Button btnStats;
    [SerializeField] private Button btnSettlement;
    [SerializeField] private Button btnCalendar;

    private void Awake()
    {
        TryInitializeDependencies();
        RegisterEvents();
    }

    /// <summary>
    /// UIManager 자동 탐색
    /// </summary>
    private void TryInitializeDependencies()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    /// <summary>
    /// 하단 네비 버튼 이벤트 연결
    /// </summary>
    private void RegisterEvents()
    {
        RegisterButton(btnHome, OnClickHome);
        RegisterButton(btnRecord, OnClickRecord);
        RegisterButton(btnStats, OnClickStats);
        RegisterButton(btnSettlement, OnClickSettlement);
        RegisterButton(btnCalendar, OnClickCalendar);
    }

    /// <summary>
    /// 버튼 공통 연결
    /// </summary>
    private void RegisterButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    /// <summary>
    /// 홈 버튼
    /// </summary>
    private void OnClickHome()
    {
        if (uiManager != null)
        {
            uiManager.OpenHomePanel();
        }
    }

    /// <summary>
    /// 기록 버튼
    /// </summary>
    private void OnClickRecord()
    {
        if (uiManager != null)
        {
            uiManager.OpenDeliveryListPanel();
        }
    }

    /// <summary>
    /// 통계 버튼
    /// </summary>
    private void OnClickStats()
    {
        if (uiManager != null)
        {
            uiManager.OpenStatsPanel();
        }
    }

    /// <summary>
    /// 정산 버튼
    /// </summary>
    private void OnClickSettlement()
    {
        if (uiManager != null)
        {
            uiManager.OpenSettlementPanel();
        }
    }

    /// <summary>
    /// 달력 버튼
    /// </summary>
    private void OnClickCalendar()
    {
        if (uiManager != null)
        {
            uiManager.OpenCalendarPanel();
        }
    }
}