using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class WorkSessionManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private WorkSessionRepository workSessionRepository;

    private void Awake()
    {
        TryInitializeDependencies();
    }

    private void TryInitializeDependencies()
    {
        if (workSessionRepository == null)
        {
            workSessionRepository = FindFirstObjectByType<WorkSessionRepository>();
        }
    }

    public bool StartWork()
    {
        TryInitializeDependencies();

        if (workSessionRepository == null)
        {
            Debug.LogWarning("[WorkSessionManager] WorkSessionRepository 참조가 없습니다.");
            return false;
        }

        WorkSession currentSession = workSessionRepository.GetWorkingSession();

        if (currentSession != null)
        {
            return false;
        }

        DateTime now = DateTime.Now;

        WorkSession newSession = new WorkSession
        {
            Date = now.ToString("yyyy-MM-dd"),
            StartTime = now.ToString("HH:mm"),
            EndTime = string.Empty,
            WorkStatus = WorkStatus.Working,
            IsWorking = true
        };

        workSessionRepository.AddSession(newSession);
        Debug.Log("[WorkSessionManager] 근무 시작 저장 완료");
        return true;
    }

    public bool EndWork()
    {
        TryInitializeDependencies();

        if (workSessionRepository == null)
        {
            Debug.LogWarning("[WorkSessionManager] WorkSessionRepository 참조가 없습니다.");
            return false;
        }

        WorkSession currentSession = workSessionRepository.GetWorkingSession();

        if (currentSession == null)
        {
            return false;
        }

        currentSession.EndTime = DateTime.Now.ToString("HH:mm");
        currentSession.WorkStatus = WorkStatus.Finished;
        currentSession.IsWorking = false;

        bool isUpdated = workSessionRepository.UpdateSession(currentSession);

        if (isUpdated)
        {
            Debug.Log("[WorkSessionManager] 근무 종료 저장 완료");
        }

        return isUpdated;
    }

    public bool SaveWorkSessionByDate(string dateText, string startTimeText, string endTimeText, out string resultMessage)
    {
        resultMessage = string.Empty;
        TryInitializeDependencies();

        if (workSessionRepository == null)
        {
            resultMessage = "WorkSessionRepository 참조가 없습니다.";
            return false;
        }

        bool isDateParsed = DateTime.TryParseExact(
            dateText,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate);

        if (!isDateParsed)
        {
            resultMessage = "날짜 형식은 yyyy-MM-dd 입니다.";
            return false;
        }

        bool isStartParsed = DateTime.TryParseExact(
            startTimeText,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedStartTime);

        if (!isStartParsed)
        {
            resultMessage = "시작시간 형식은 HH:mm 입니다.";
            return false;
        }

        bool isEndParsed = DateTime.TryParseExact(
            endTimeText,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedEndTime);

        if (!isEndParsed)
        {
            resultMessage = "종료시간 형식은 HH:mm 입니다.";
            return false;
        }

        DateTime startDateTime = parsedDate.Date.AddHours(parsedStartTime.Hour).AddMinutes(parsedStartTime.Minute);
        DateTime endDateTime = parsedDate.Date.AddHours(parsedEndTime.Hour).AddMinutes(parsedEndTime.Minute);

        if (endDateTime < startDateTime)
        {
            resultMessage = "종료시간은 시작시간보다 빠를 수 없습니다.";
            return false;
        }

        WorkSession targetSession = GetSessionByDate(parsedDate);

        if (targetSession == null)
        {
            targetSession = new WorkSession();
            targetSession.Date = parsedDate.ToString("yyyy-MM-dd");
        }

        targetSession.StartTime = startDateTime.ToString("HH:mm");
        targetSession.EndTime = endDateTime.ToString("HH:mm");
        targetSession.IsWorking = false;
        targetSession.WorkStatus = WorkStatus.Finished;

        bool isSaved;

        if (string.IsNullOrWhiteSpace(targetSession.Id) || GetSessionById(targetSession.Id) == null)
        {
            workSessionRepository.AddSession(targetSession);
            isSaved = true;
        }
        else
        {
            isSaved = workSessionRepository.UpdateSession(targetSession);
        }

        resultMessage = isSaved ? "근무시간 저장 완료" : "근무시간 저장 실패";
        return isSaved;
    }

    public WorkSession GetSessionByDate(DateTime targetDate)
    {
        List<WorkSession> allSessions = GetAllSessions();
        string dateText = targetDate.ToString("yyyy-MM-dd");
        WorkSession latestSession = null;

        for (int i = 0; i < allSessions.Count; i++)
        {
            WorkSession session = allSessions[i];

            if (session == null)
            {
                continue;
            }

            if (session.Date == dateText)
            {
                latestSession = session;
            }
        }

        return latestSession;
    }

    private WorkSession GetSessionById(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        List<WorkSession> allSessions = GetAllSessions();

        for (int i = 0; i < allSessions.Count; i++)
        {
            WorkSession session = allSessions[i];

            if (session == null)
            {
                continue;
            }

            if (session.Id == sessionId)
            {
                return session;
            }
        }

        return null;
    }

    public string GetCurrentWorkingSessionId()
    {
        WorkSession session = GetCurrentWorkingSession();
        return session != null ? session.Id : string.Empty;
    }

    public WorkSession GetCurrentWorkingSession()
    {
        TryInitializeDependencies();

        if (workSessionRepository == null)
        {
            return null;
        }

        return workSessionRepository.GetWorkingSession();
    }

    public List<WorkSession> GetAllSessions()
    {
        TryInitializeDependencies();

        if (workSessionRepository == null)
        {
            return new List<WorkSession>();
        }

        return workSessionRepository.LoadAllSessions();
    }

    public List<WorkSession> GetTodaySessions()
    {
        List<WorkSession> allSessions = GetAllSessions();
        List<WorkSession> todaySessions = new List<WorkSession>();
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        for (int i = 0; i < allSessions.Count; i++)
        {
            WorkSession session = allSessions[i];

            if (session == null)
            {
                continue;
            }

            if (session.Date == today)
            {
                todaySessions.Add(session);
            }
        }

        return todaySessions;
    }

    public List<WorkSession> GetCurrentMonthSessions()
    {
        List<WorkSession> allSessions = GetAllSessions();
        List<WorkSession> monthSessions = new List<WorkSession>();
        string currentMonthPrefix = DateTime.Now.ToString("yyyy-MM");

        for (int i = 0; i < allSessions.Count; i++)
        {
            WorkSession session = allSessions[i];

            if (session == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(session.Date) && session.Date.StartsWith(currentMonthPrefix))
            {
                monthSessions.Add(session);
            }
        }

        return monthSessions;
    }

    public int GetTodayWorkedMinutes()
    {
        DateTime today = DateTime.Now.Date;
        return GetWorkedMinutesByDateRange(today, today);
    }

    public int GetCurrentMonthWorkedMinutes()
    {
        DateTime today = DateTime.Now.Date;
        DateTime startDate = new DateTime(today.Year, today.Month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        return GetWorkedMinutesByDateRange(startDate, endDate);
    }

    public WorkStatus GetCurrentWorkStatus()
    {
        WorkSession currentSession = GetCurrentWorkingSession();

        if (currentSession != null)
        {
            return WorkStatus.Working;
        }

        List<WorkSession> todaySessions = GetTodaySessions();

        for (int i = 0; i < todaySessions.Count; i++)
        {
            WorkSession session = todaySessions[i];

            if (session != null && session.WorkStatus == WorkStatus.Finished)
            {
                return WorkStatus.Finished;
            }
        }

        return WorkStatus.BeforeStart;
    }

    public string GetStatusText()
    {
        WorkStatus currentStatus = GetCurrentWorkStatus();
        int todayMinutes = GetTodayWorkedMinutes();

        switch (currentStatus)
        {
            case WorkStatus.Working:
                return $"현재 상태: 근무 중 / 오늘 {FormatMinutes(todayMinutes)}";

            case WorkStatus.Finished:
                return $"현재 상태: 근무 완료 / 오늘 {FormatMinutes(todayMinutes)}";

            default:
                return "현재 상태: 근무 전 / 오늘 0분";
        }
    }

    public string GetHeaderStatusText()
    {
        WorkStatus currentStatus = GetCurrentWorkStatus();
        int todayMinutes = GetTodayWorkedMinutes();
        string workedText = FormatMinutes(todayMinutes);

        switch (currentStatus)
        {
            case WorkStatus.Working:
                return $"근무 중 / {workedText}";

            case WorkStatus.Finished:
                return $"근무 완료 / {workedText}";

            default:
                return "근무 전 / 0분";
        }
    }

    public string FormatMinutes(int totalMinutes)
    {
        int safeMinutes = Mathf.Max(0, totalMinutes);
        int hour = safeMinutes / 60;
        int minute = safeMinutes % 60;

        if (hour <= 0)
        {
            return $"{minute}분";
        }

        if (minute == 0)
        {
            return $"{hour}시간";
        }

        if (minute >= 59)
        {
            return $"{hour + 1}시간";
        }

        return $"{hour}시간 {minute}분";
    }

    public int GetWorkedMinutesByDate(DateTime targetDate)
    {
        return GetWorkedMinutesByDateRange(targetDate.Date, targetDate.Date);
    }

    public int GetWorkedMinutesByDateRange(DateTime startDate, DateTime endDate)
    {
        DateTime safeStartDate = startDate.Date;
        DateTime safeEndDate = endDate.Date;

        if (safeStartDate > safeEndDate)
        {
            DateTime temp = safeStartDate;
            safeStartDate = safeEndDate;
            safeEndDate = temp;
        }

        List<WorkSession> allSessions = GetAllSessions();
        Dictionary<string, WorkSession> latestSessionByDate = new Dictionary<string, WorkSession>();

        for (int i = 0; i < allSessions.Count; i++)
        {
            WorkSession session = allSessions[i];

            if (session == null || string.IsNullOrWhiteSpace(session.Date))
            {
                continue;
            }

            bool isParsed = DateTime.TryParseExact(
                session.Date,
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
                latestSessionByDate[session.Date] = session;
            }
        }

        List<WorkSession> rangeSessions = new List<WorkSession>(latestSessionByDate.Values);
        return CalculateWorkedMinutes(rangeSessions);
    }

    private int CalculateWorkedMinutes(List<WorkSession> sessions)
    {
        int totalMinutes = 0;

        if (sessions == null)
        {
            return 0;
        }

        for (int i = 0; i < sessions.Count; i++)
        {
            WorkSession session = sessions[i];

            if (session == null)
            {
                continue;
            }

            totalMinutes += GetWorkedMinutes(session);
        }

        return totalMinutes;
    }

    private int GetWorkedMinutes(WorkSession session)
    {
        if (session == null)
        {
            return 0;
        }

        bool isStartParsed = DateTime.TryParseExact(
            $"{session.Date} {session.StartTime}",
            "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime startDateTime);

        if (!isStartParsed)
        {
            return 0;
        }

        DateTime endDateTime;

        if (session.IsWorking || string.IsNullOrWhiteSpace(session.EndTime))
        {
            endDateTime = DateTime.Now;
        }
        else
        {
            bool isEndParsed = DateTime.TryParseExact(
                $"{session.Date} {session.EndTime}",
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out endDateTime);

            if (!isEndParsed)
            {
                return 0;
            }
        }

        if (endDateTime < startDateTime)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.RoundToInt((float)(endDateTime - startDateTime).TotalMinutes));
    }
}
