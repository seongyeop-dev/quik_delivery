public enum DeliveryType
{
    Quick = 0,      // 퀵
    Parcel = 1,     // 택배
    Pickup = 2,     // 회수
    Other = 3       // 기타
}

public enum SettlementStatus
{
    Unsettled = 0,  // 미정산
    Scheduled = 1,  // 정산 예정
    Completed = 2   // 정산 완료
}

public enum WorkStatus
{
    BeforeStart = 0, // 근무 전
    Working = 1,     // 근무 중
    Finished = 2     // 근무 종료
}