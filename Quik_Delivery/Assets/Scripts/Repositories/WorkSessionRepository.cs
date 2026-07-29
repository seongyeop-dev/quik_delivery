using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkSessionRepository : MonoBehaviour
{
    [Serializable]
    private class WorkSessionCollection
    {
        public List<WorkSession> Sessions = new List<WorkSession>();
    }

    [Header("Dependencies")]
    [SerializeField] private JsonFileService jsonFileService;

    [Header("Storage Settings")]
    [SerializeField] private string fileName = "work_sessions.json";

    private void Awake()
    {
        TryInitializeDependencies();
    }

    private void TryInitializeDependencies()
    {
        if (jsonFileService == null)
        {
            jsonFileService = FindFirstObjectByType<JsonFileService>();
        }
    }

    public List<WorkSession> LoadAllSessions()
    {
        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            Debug.LogWarning("[WorkSessionRepository] JsonFileService 참조가 없습니다.");
            return new List<WorkSession>();
        }

        WorkSessionCollection collection = jsonFileService.LoadFromJson<WorkSessionCollection>(fileName);

        if (collection == null || collection.Sessions == null)
        {
            return new List<WorkSession>();
        }

        return new List<WorkSession>(collection.Sessions);
    }

    public void SaveAllSessions(List<WorkSession> sessions)
    {
        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            Debug.LogWarning("[WorkSessionRepository] JsonFileService 참조가 없습니다.");
            return;
        }

        WorkSessionCollection collection = new WorkSessionCollection();

        if (sessions != null)
        {
            collection.Sessions = new List<WorkSession>(sessions);
        }

        jsonFileService.SaveToJson(fileName, collection);
    }

    public void AddSession(WorkSession session)
    {
        if (session == null)
        {
            Debug.LogWarning("[WorkSessionRepository] 저장할 session이 null입니다.");
            return;
        }

        List<WorkSession> sessions = LoadAllSessions();
        sessions.Add(session);
        SaveAllSessions(sessions);
    }

    public bool UpdateSession(WorkSession updatedSession)
    {
        if (updatedSession == null || string.IsNullOrWhiteSpace(updatedSession.Id))
        {
            Debug.LogWarning("[WorkSessionRepository] 수정할 session 정보가 올바르지 않습니다.");
            return false;
        }

        List<WorkSession> sessions = LoadAllSessions();

        for (int i = 0; i < sessions.Count; i++)
        {
            WorkSession session = sessions[i];

            if (session == null)
            {
                continue;
            }

            if (session.Id == updatedSession.Id)
            {
                sessions[i] = updatedSession;
                SaveAllSessions(sessions);
                return true;
            }
        }

        return false;
    }

    public WorkSession GetWorkingSession()
    {
        List<WorkSession> sessions = LoadAllSessions();

        for (int i = sessions.Count - 1; i >= 0; i--)
        {
            WorkSession session = sessions[i];

            if (session == null)
            {
                continue;
            }

            if (session.IsWorking)
            {
                return session;
            }
        }

        return null;
    }
}
