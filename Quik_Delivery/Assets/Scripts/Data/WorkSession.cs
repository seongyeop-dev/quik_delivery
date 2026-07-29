using System;

[Serializable]
public class WorkSession
{
    public string Id = Guid.NewGuid().ToString();

    public string Date = string.Empty;       // yyyy-MM-dd
    public string StartTime = string.Empty;  // HH:mm
    public string EndTime = string.Empty;    // HH:mm

    public WorkStatus WorkStatus = WorkStatus.BeforeStart;
    public bool IsWorking = false;
}