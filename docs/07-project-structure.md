# 프로젝트 구조

```text
quik-delivery/
├─ README.md
├─ LICENSE.md
├─ docs/
│  ├─ README.md
│  ├─ 01-overview.md
│  ├─ 02-architecture.md
│  ├─ 03-features.md
│  ├─ 04-data-flow.md
│  ├─ 05-validation.md
│  ├─ 06-known-issues.md
│  ├─ 07-project-structure.md
│  ├─ demo/
│  │  └─ README.md
│  └─ images/
│     ├─ overview/
│     ├─ features/
│     ├─ settlement/
│     ├─ backup/
│     ├─ mobile/
│     └─ validation/
└─ Quik_Delivery/
   ├─ Assets/
   │  ├─ Editor/
   │  ├─ Scenes/
   │  │  └─ MainScene.unity
   │  └─ Scripts/
   │     ├─ Data/
   │     ├─ Managers/
   │     ├─ Repositories/
   │     ├─ Services/
   │     └─ UI/
   ├─ Packages/
   └─ ProjectSettings/
```

## 핵심 스크립트

| 경로 | 역할 |
|---|---|
| `Assets/Scripts/Managers/DeliveryManager.cs` | 배송 CRUD, 기간 조회, 요약, 누락 고정비 |
| `Assets/Scripts/Managers/WorkSessionManager.cs` | 근무 세션과 근무시간 |
| `Assets/Scripts/Managers/BackupDataManager.cs` | 전체 백업·복원·다운로드 |
| `Assets/Scripts/Repositories/DeliveryRepository.cs` | 배송 JSON 저장 |
| `Assets/Scripts/Repositories/WorkSessionRepository.cs` | 근무 JSON 저장 |
| `Assets/Scripts/Services/JsonFileService.cs` | 파일 직렬화·역직렬화 |
| `Assets/Scripts/Services/RevenueCalculator.cs` | 수익·비용·부가세 계산 |
| `Assets/Scripts/UI/CalendarPanelUI.cs` | 캘린더와 주간 차트 |
| `Assets/Scripts/UI/SettlementPanelUI.cs` | 연도별 정산과 백업 UI |
| `Assets/Editor/SettlementPanelEditModeBuilder.cs` | 정산 패널 Edit Mode 구성 |
