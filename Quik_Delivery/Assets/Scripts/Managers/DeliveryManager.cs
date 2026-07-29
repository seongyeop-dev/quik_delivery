using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    private static readonly DateTime DailyFixedExpenseStartDate = new DateTime(2026, 5, 8);
    [Serializable]
    public class DeliverySummary
    {
        public int TotalCount = 0;

        // 수입/비용/실수령액
        public int TotalGrossRevenue = 0; // 총수입
        public int TotalExpense = 0;      // 총비용
        public int TotalNetRevenue = 0;   // 실수령액

        // 항목별 합계
        public int TotalBaseFee = 0;
        public int TotalBaseFeeVatSource = 0;
        public int TotalVatAmount = 0;
        public int TotalExtraFee = 0;
        public int TotalCommissionFee = 0;
        public int TotalDataUsageFee = 0;
        public int TotalOtherDeduction = 0;
        public int TotalElectricCharge = 0;
        public int TotalTollFee = 0;
    }

    [Header("Dependencies")]
    [SerializeField] private DeliveryRepository deliveryRepository;
    [SerializeField] private RevenueCalculator revenueCalculator;

    [Header("Daily Fixed Deductions")]
    [SerializeField] private bool autoCreateDailyFixedDeduction = true;
    [SerializeField] private int defaultCompanyDeposit = 41200;
    [SerializeField] private int defaultDataUsageFee = 4530;
    private bool isEnsuringDailyFixedExpenses = false;
    private bool hasStarted = false;
    private DateTime lastDailyFixedExpenseCheckDate = DateTime.MinValue;
    private Coroutine dailyFixedExpenseWatchCoroutine;

    private void Awake()
    {
        TryInitializeDependencies();
    }
    private void Start()
    {
        hasStarted = true;
        CheckAndEnsureDailyFixedExpenses(DateTime.Today, true);
        StartDailyFixedExpenseWatch();
    }

    private void OnEnable()
    {
        if (hasStarted)
        {
            StartDailyFixedExpenseWatch();
        }
    }

    private void OnDisable()
    {
        StopDailyFixedExpenseWatch();
    }

    private void OnDestroy()
    {
        StopDailyFixedExpenseWatch();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            CheckAndEnsureDailyFixedExpenses(DateTime.Today, false);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            CheckAndEnsureDailyFixedExpenses(DateTime.Today, false);
        }
    }

    /// <summary>
    /// 외부 참조가 비어 있으면 자동 탐색
    /// </summary>
    private void TryInitializeDependencies()
    {
        if (deliveryRepository == null)
        {
            deliveryRepository = FindFirstObjectByType<DeliveryRepository>();
        }

        if (revenueCalculator == null)
        {
            revenueCalculator = FindFirstObjectByType<RevenueCalculator>();
        }
    }

    /// <summary>
    /// 배송 기록 신규 저장
    /// 배송 기록 저장 후, 해당 날짜에 일일 고정비가 없으면 1회만 자동 생성한다.
    /// </summary>
    public void SaveDeliveryRecord(DeliveryRecord record)
    {
        TryInitializeDependencies();

        if (deliveryRepository == null)
        {
            Debug.LogWarning("[DeliveryManager] DeliveryRepository 참조가 없습니다.");
            return;
        }

        if (record == null)
        {
            Debug.LogWarning("[DeliveryManager] 저장할 record가 null입니다.");
            return;
        }

        DateTime now = DateTime.Now;

        if (string.IsNullOrWhiteSpace(record.Date))
        {
            record.Date = now.ToString("yyyy-MM-dd");
        }

        if (string.IsNullOrWhiteSpace(record.StartTime))
        {
            record.StartTime = now.ToString("HH:mm");
        }

        if (string.IsNullOrWhiteSpace(record.EndTime))
        {
            record.EndTime = now.ToString("HH:mm");
        }

        bool shouldCreateDailyFixedDeduction =
            autoCreateDailyFixedDeduction &&
            !record.IsDailyFixedExpense &&
            IsCountableDeliveryRecord(record);

        ApplyCalculation(record);
        deliveryRepository.AddRecord(record);
        Debug.Log("[DeliveryManager] 배송 기록 저장 완료");

        if (shouldCreateDailyFixedDeduction)
        {
            EnsureDailyFixedDeductionRecord(record.Date);
        }
    }

    /// <summary>
    /// 배송 기록 수정 저장
    /// 날짜를 바꿔 저장한 경우에도 새 날짜에 일일 고정비가 없으면 1회만 자동 생성한다.
    /// </summary>
    public bool UpdateDeliveryRecord(DeliveryRecord record)
    {
        TryInitializeDependencies();

        if (deliveryRepository == null)
        {
            Debug.LogWarning("[DeliveryManager] DeliveryRepository 참조가 없습니다.");
            return false;
        }

        if (record == null)
        {
            Debug.LogWarning("[DeliveryManager] 수정할 record가 null입니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(record.Id))
        {
            Debug.LogWarning("[DeliveryManager] 수정할 record의 Id가 비어 있습니다.");
            return false;
        }

        ApplyCalculation(record);

        bool isUpdated = deliveryRepository.UpdateRecord(record);

        if (isUpdated)
        {
            Debug.Log("[DeliveryManager] 배송 기록 수정 완료");

            if (autoCreateDailyFixedDeduction &&
                !record.IsDailyFixedExpense &&
                IsCountableDeliveryRecord(record))
            {
                EnsureDailyFixedDeductionRecord(record.Date);
            }
        }
        else
        {
            Debug.LogWarning("[DeliveryManager] 수정 대상 record를 찾지 못했습니다.");
        }

        return isUpdated;
    }

    /// <summary>
    /// 특정 날짜에 일일 고정비 기록이 없으면 자동 생성한다.
    /// 회사입금/데이터사용료는 하루에 한 번만 차감된다.
    /// </summary>
    private void EnsureDailyFixedDeductionRecord(string dateText)
    {
        TryInitializeDependencies();

        if (deliveryRepository == null || string.IsNullOrWhiteSpace(dateText) || isEnsuringDailyFixedExpenses)
        {
            return;
        }

        if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return;
        }

        isEnsuringDailyFixedExpenses = true;

        try
        {
            List<DeliveryRecord> records = deliveryRepository.LoadAllRecords();
            HashSet<string> fixedExpenseDates = CollectDailyFixedExpenseDates(records);

            if (TryAddDailyFixedExpenseRecord(records, fixedExpenseDates, date.Date))
            {
                deliveryRepository.SaveAllRecords(records);
            }
        }
        finally
        {
            isEnsuringDailyFixedExpenses = false;
        }
    }
    public int EnsureMissingDailyFixedExpenses(DateTime throughDate)
    {
        if (!autoCreateDailyFixedDeduction || throughDate.Date < DailyFixedExpenseStartDate)
        {
            return 0;
        }

        TryInitializeDependencies();

        if (deliveryRepository == null || isEnsuringDailyFixedExpenses)
        {
            return 0;
        }

        isEnsuringDailyFixedExpenses = true;

        try
        {
            List<DeliveryRecord> records = deliveryRepository.LoadAllRecords();
            HashSet<string> fixedExpenseDates = CollectDailyFixedExpenseDates(records);
            int addedCount = 0;

            for (DateTime date = DailyFixedExpenseStartDate; date <= throughDate.Date; date = date.AddDays(1))
            {
                if (TryAddDailyFixedExpenseRecord(records, fixedExpenseDates, date))
                {
                    addedCount += 1;
                }
            }

            if (addedCount > 0)
            {
                deliveryRepository.SaveAllRecords(records);
            }

            return addedCount;
        }
        finally
        {
            isEnsuringDailyFixedExpenses = false;
        }
    }

    private HashSet<string> CollectDailyFixedExpenseDates(List<DeliveryRecord> records)
    {
        HashSet<string> dates = new HashSet<string>(StringComparer.Ordinal);

        if (records == null)
        {
            return dates;
        }

        for (int i = 0; i < records.Count; i++)
        {
            DeliveryRecord record = records[i];

            if (record != null && record.IsDailyFixedExpense && !string.IsNullOrWhiteSpace(record.Date))
            {
                dates.Add(record.Date);
            }
        }

        return dates;
    }

    private bool TryAddDailyFixedExpenseRecord(List<DeliveryRecord> records, HashSet<string> fixedExpenseDates, DateTime date)
    {
        if (!autoCreateDailyFixedDeduction || records == null || fixedExpenseDates == null ||
            (defaultCompanyDeposit <= 0 && defaultDataUsageFee <= 0))
        {
            return false;
        }

        string dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (fixedExpenseDates.Contains(dateText))
        {
            return false;
        }

        DeliveryRecord fixedRecord = new DeliveryRecord
        {
            Date = dateText,
            StartTime = "00:00",
            EndTime = "00:00",
            StartAddress = "\uC77C\uC77C \uACE0\uC815\uBE44",
            EndAddress = "\uD68C\uC0AC\uC785\uAE08/\uB370\uC774\uD130 \uC0AC\uC6A9\uB8CC",
            BaseFee = 0,
            ExtraFee = 0,
            CommissionFee = Mathf.Max(0, defaultCompanyDeposit),
            DataUsageFee = Mathf.Max(0, defaultDataUsageFee),
            OtherDeduction = 0,
            ElectricCharge = 0,
            TollFee = 0,
            IsDailyFixedExpense = true,
            Memo = "\uC77C\uC77C \uACE0\uC815\uBE44 \uC790\uB3D9 \uC0DD\uC131"
        };

        ApplyCalculation(fixedRecord);
        records.Add(fixedRecord);
        fixedExpenseDates.Add(dateText);
        return true;
    }

    private void CheckAndEnsureDailyFixedExpenses(DateTime today, bool force)
    {
        if (!force && lastDailyFixedExpenseCheckDate == today.Date)
        {
            return;
        }

        EnsureMissingDailyFixedExpenses(today);
        lastDailyFixedExpenseCheckDate = today.Date;
    }

    private void StartDailyFixedExpenseWatch()
    {
        if (dailyFixedExpenseWatchCoroutine == null && isActiveAndEnabled)
        {
            dailyFixedExpenseWatchCoroutine = StartCoroutine(WatchDailyFixedExpenseDate());
        }
    }

    private void StopDailyFixedExpenseWatch()
    {
        if (dailyFixedExpenseWatchCoroutine != null)
        {
            StopCoroutine(dailyFixedExpenseWatchCoroutine);
            dailyFixedExpenseWatchCoroutine = null;
        }
    }

    private IEnumerator WatchDailyFixedExpenseDate()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(60f);

        while (isActiveAndEnabled)
        {
            yield return wait;
            CheckAndEnsureDailyFixedExpenses(DateTime.Today, false);
        }

        dailyFixedExpenseWatchCoroutine = null;
    }
    private void ApplyCalculation(DeliveryRecord record)
    {
        if (record == null)
        {
            return;
        }

        if (revenueCalculator != null)
        {
            revenueCalculator.ApplyCalculation(record);
            return;
        }

        record.BaseFee = Mathf.Max(0, record.BaseFee);
        record.ExtraFee = Mathf.Max(0, record.ExtraFee);
        record.CommissionFee = Mathf.Max(0, record.CommissionFee);
        record.DataUsageFee = Mathf.Max(0, record.DataUsageFee);
        record.OtherDeduction = Mathf.Max(0, record.OtherDeduction);
        record.ElectricCharge = Mathf.Max(0, record.ElectricCharge);
        record.TollFee = Mathf.Max(0, record.TollFee);

        record.GrossRevenue =
            record.BaseFee +
            record.BaseFeeVatSource +
            record.ExtraFee;
        record.TotalExpense =
            record.CommissionFee +
            record.DataUsageFee +
            record.OtherDeduction +
            record.ElectricCharge +
            record.TollFee;
        record.NetRevenue = record.GrossRevenue - record.TotalExpense;
    }

    /// <summary>
    /// 배송 기록 삭제
    /// </summary>
    public bool DeleteDeliveryRecord(string recordId)
    {
        TryInitializeDependencies();

        if (deliveryRepository == null)
        {
            Debug.LogWarning("[DeliveryManager] DeliveryRepository 참조가 없습니다.");
            return false;
        }

        bool isDeleted = deliveryRepository.DeleteRecordById(recordId);

        if (isDeleted)
        {
            Debug.Log("[DeliveryManager] 배송 기록 삭제 완료");
        }
        else
        {
            Debug.LogWarning("[DeliveryManager] 삭제 대상 record를 찾지 못했습니다.");
        }

        return isDeleted;
    }

    /// <summary>
    /// ID 기준 기록 1건 조회
    /// </summary>
    public DeliveryRecord GetRecordById(string recordId)
    {
        TryInitializeDependencies();

        if (deliveryRepository == null)
        {
            return null;
        }

        return deliveryRepository.GetRecordById(recordId);
    }

    /// <summary>
    /// 전체 기록 조회
    /// </summary>
    public List<DeliveryRecord> GetAllRecords()
    {
        TryInitializeDependencies();

        if (deliveryRepository == null)
        {
            return new List<DeliveryRecord>();
        }

        return deliveryRepository.LoadAllRecords();
    }

    /// <summary>
    /// 오늘 기록 조회
    /// </summary>
    public List<DeliveryRecord> GetTodayRecords()
    {
        TryInitializeDependencies();

        if (deliveryRepository == null)
        {
            return new List<DeliveryRecord>();
        }

        return deliveryRepository.GetTodayRecords();
    }

    /// <summary>
    /// 최근 7일 기록 조회
    /// </summary>
    public List<DeliveryRecord> GetCurrentWeekRecords()
    {
        List<DeliveryRecord> allRecords = GetAllRecords();
        List<DeliveryRecord> weekRecords = new List<DeliveryRecord>();

        DateTime today = DateTime.Now.Date;
        DateTime minDate = today.AddDays(-6);

        for (int i = 0; i < allRecords.Count; i++)
        {
            DeliveryRecord record = allRecords[i];

            if (record == null || string.IsNullOrWhiteSpace(record.Date))
            {
                continue;
            }

            bool isParsed = DateTime.TryParseExact(
                record.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedDate);

            if (!isParsed)
            {
                continue;
            }

            DateTime onlyDate = parsedDate.Date;

            if (onlyDate >= minDate && onlyDate <= today)
            {
                weekRecords.Add(record);
            }
        }

        return weekRecords;
    }

    public List<DeliveryRecord> GetRecordsForMonth(int year, int month)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }

        DateTime startDate = new DateTime(year, month, 1);
        return GetRecordsByDateRange(startDate, startDate.AddMonths(1).AddDays(-1));
    }

    public DeliverySummary GetSummaryForMonth(int year, int month)
    {
        return BuildSummary(GetRecordsForMonth(year, month));
    }

    public List<DeliverySummary> GetMonthlySummaries(int year)
    {
        List<DeliverySummary> summaries = new List<DeliverySummary>(12);

        for (int month = 1; month <= 12; month++)
        {
            summaries.Add(GetSummaryForMonth(year, month));
        }

        return summaries;
    }
    /// <summary>
    /// 이번 달 기록 조회
    /// </summary>
    public List<DeliveryRecord> GetCurrentMonthRecords()
    {
        TryInitializeDependencies();

        if (deliveryRepository == null)
        {
            return new List<DeliveryRecord>();
        }

        return deliveryRepository.GetCurrentMonthRecords();
    }

    /// <summary>
    /// 올해 기록 조회
    /// </summary>
    public List<DeliveryRecord> GetCurrentYearRecords()
    {
        List<DeliveryRecord> allRecords = GetAllRecords();
        List<DeliveryRecord> yearRecords = new List<DeliveryRecord>();

        string currentYearPrefix = DateTime.Now.ToString("yyyy");

        for (int i = 0; i < allRecords.Count; i++)
        {
            DeliveryRecord record = allRecords[i];

            if (record == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(record.Date) &&
                record.Date.StartsWith(currentYearPrefix))
            {
                yearRecords.Add(record);
            }
        }

        return yearRecords;
    }

    /// <summary>
    /// 사용자 지정 기간 기록 조회
    /// </summary>
    public List<DeliveryRecord> GetRecordsByDateRange(DateTime startDate, DateTime endDate)
    {
        List<DeliveryRecord> allRecords = GetAllRecords();
        List<DeliveryRecord> rangeRecords = new List<DeliveryRecord>();

        DateTime safeStartDate = startDate.Date;
        DateTime safeEndDate = endDate.Date;

        if (safeStartDate > safeEndDate)
        {
            DateTime temp = safeStartDate;
            safeStartDate = safeEndDate;
            safeEndDate = temp;
        }

        for (int i = 0; i < allRecords.Count; i++)
        {
            DeliveryRecord record = allRecords[i];

            if (record == null || string.IsNullOrWhiteSpace(record.Date))
            {
                continue;
            }

            bool isParsed = DateTime.TryParseExact(
                record.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedDate);

            if (!isParsed)
            {
                continue;
            }

            DateTime onlyDate = parsedDate.Date;

            if (onlyDate >= safeStartDate && onlyDate <= safeEndDate)
            {
                rangeRecords.Add(record);
            }
        }

        return rangeRecords;
    }

    /// <summary>
    /// 전체 누적 요약 계산
    /// </summary>
    public DeliverySummary GetAllSummary()
    {
        return BuildSummary(GetAllRecords());
    }

    /// <summary>
    /// 오늘 요약 계산
    /// </summary>
    public DeliverySummary GetTodaySummary()
    {
        return BuildSummary(GetTodayRecords());
    }

    /// <summary>
    /// 최근 7일 요약 계산
    /// </summary>
    public DeliverySummary GetCurrentWeekSummary()
    {
        return BuildSummary(GetCurrentWeekRecords());
    }

    /// <summary>
    /// 이번 달 요약 계산
    /// </summary>
    public DeliverySummary GetCurrentMonthSummary()
    {
        return BuildSummary(GetCurrentMonthRecords());
    }

    /// <summary>
    /// 올해 요약 계산
    /// </summary>
    public DeliverySummary GetCurrentYearSummary()
    {
        return BuildSummary(GetCurrentYearRecords());
    }

    /// <summary>
    /// 사용자 지정 기간 요약 계산
    /// </summary>
    public DeliverySummary GetSummaryByDateRange(DateTime startDate, DateTime endDate)
    {
        return BuildSummary(GetRecordsByDateRange(startDate, endDate));
    }

    /// <summary>
    /// 외부에서 전달한 기록 리스트를 요약
    /// </summary>
    public DeliverySummary GetSummaryByRecords(List<DeliveryRecord> records)
    {
        return BuildSummary(records);
    }

    /// <summary>
    /// 배송 건수로 셀 수 있는 기록인지 확인
    /// 기본요금, 기본요금+부가세 또는 추가요금이 있으면 배송 1건으로 본다.
    /// 일일 고정비 기록은 배송 건수에서 제외한다.
    /// </summary>
    private bool IsCountableDeliveryRecord(DeliveryRecord record)
    {
        if (record == null)
        {
            return false;
        }

        if (record.IsDailyFixedExpense)
        {
            return false;
        }

        return record.BaseFee > 0 || record.BaseFeeVatSource > 0 || record.ExtraFee > 0;
    }

    /// <summary>
    /// 공통 요약 계산
    /// </summary>
    private DeliverySummary BuildSummary(List<DeliveryRecord> records)
    {
        DeliverySummary summary = new DeliverySummary();

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

            int safeBaseFee = Mathf.Max(0, record.BaseFee);
            int safeBaseFeeVatSource = Mathf.Max(0, record.BaseFeeVatSource);
            int safeExtraFee = Mathf.Max(0, record.ExtraFee);
            int safeCommissionFee = Mathf.Max(0, record.CommissionFee);
            int safeDataUsageFee = Mathf.Max(0, record.DataUsageFee);
            int safeOtherDeduction = Mathf.Max(0, record.OtherDeduction);
            int safeElectricCharge = Mathf.Max(0, record.ElectricCharge);
            int safeTollFee = Mathf.Max(0, record.TollFee);
            int safeGrossRevenue = safeBaseFee + safeBaseFeeVatSource + safeExtraFee;
            int safeTotalExpense = safeCommissionFee + safeDataUsageFee + safeOtherDeduction + safeElectricCharge + safeTollFee;

            if (IsCountableDeliveryRecord(record))
            {
                summary.TotalCount += 1;
            }

            summary.TotalGrossRevenue += safeGrossRevenue;
            summary.TotalExpense += safeTotalExpense;
            summary.TotalNetRevenue += safeGrossRevenue - safeTotalExpense;
            summary.TotalBaseFee += safeBaseFee;
            summary.TotalBaseFeeVatSource += safeBaseFeeVatSource;
            summary.TotalVatAmount += Mathf.RoundToInt(safeBaseFeeVatSource * 0.1f);
            summary.TotalExtraFee += safeExtraFee;
            summary.TotalCommissionFee += safeCommissionFee;
            summary.TotalDataUsageFee += safeDataUsageFee;
            summary.TotalOtherDeduction += safeOtherDeduction;
            summary.TotalElectricCharge += safeElectricCharge;
            summary.TotalTollFee += safeTollFee;
        }

        return summary;
    }
}
