using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject deliveryInputPanel;
    [SerializeField] private GameObject deliveryListPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject settlementPanel;
    [SerializeField] private GameObject calendarPanel;

    /// <summary>
    /// 지정한 패널만 활성화
    /// </summary>
    public void SetOnlyPanelActive(GameObject targetPanel)
    {
        if (homePanel != null)
        {
            homePanel.SetActive(targetPanel == homePanel);
        }

        if (deliveryInputPanel != null)
        {
            deliveryInputPanel.SetActive(targetPanel == deliveryInputPanel);
        }

        if (deliveryListPanel != null)
        {
            deliveryListPanel.SetActive(targetPanel == deliveryListPanel);
        }

        if (statsPanel != null)
        {
            statsPanel.SetActive(targetPanel == statsPanel);
        }

        if (settlementPanel != null)
        {
            settlementPanel.SetActive(targetPanel == settlementPanel);
        }

        if (calendarPanel != null)
        {
            calendarPanel.SetActive(targetPanel == calendarPanel);
        }
    }

    /// <summary>
    /// 홈 화면 열기
    /// </summary>
    public void OpenHomePanel()
    {
        SetOnlyPanelActive(homePanel);
    }

    /// <summary>
    /// 기록 목록 화면 열기
    /// </summary>
    public void OpenDeliveryListPanel()
    {
        SetOnlyPanelActive(deliveryListPanel);
    }

    /// <summary>
    /// 통계 화면 열기
    /// </summary>
    public void OpenStatsPanel()
    {
        SetOnlyPanelActive(statsPanel);
    }

    /// <summary>
    /// 정산 화면 열기
    /// </summary>
    public void OpenSettlementPanel()
    {
        SetOnlyPanelActive(settlementPanel);
    }

    /// <summary>
    /// 달력 분석 화면 열기
    /// </summary>
    public void OpenCalendarPanel()
    {
        SetOnlyPanelActive(calendarPanel);
    }
}