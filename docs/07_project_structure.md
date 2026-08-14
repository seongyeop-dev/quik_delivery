# 07. 프로젝트 구조

## 저장소 구조

```text
quik_delivery/
├─ .gitattributes
├─ .gitignore
├─ README.md
├─ docs/
│  ├─ README.md
│  ├─ 01_overview.md
│  ├─ 02_architecture.md
│  ├─ 03_features.md
│  ├─ 04_data_flow.md
│  ├─ 05_validation.md
│  ├─ 06_project_scope.md
│  ├─ 07_project_structure.md
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
   ├─ Packages/
   └─ ProjectSettings/
```

루트 `README.md`는 프로젝트 요약과 대표 검증 자료를 제공하고, `docs`에는 기능·구조·검증 상세 문서를 분리했습니다. Unity 프로젝트는 `Quik_Delivery` 폴더에 포함되어 있습니다.

## Unity 프로젝트 구조

```text
Quik_Delivery/
├─ Assets/
│  ├─ Editor/
│  │  └─ SettlementPanelEditModeBuilder.cs
│  ├─ Scenes/
│  │  └─ MainScene.unity
│  ├─ Scripts/
│  │  ├─ Data/
│  │  ├─ Managers/
│  │  ├─ Repositories/
│  │  ├─ Services/
│  │  └─ UI/
│  ├─ Plugins/
│  │  └─ Android/
│  └─ TextMesh Pro/
├─ Packages/
│  └─ manifest.json
└─ ProjectSettings/
   └─ ProjectVersion.txt
```

프로젝트 기능은 `MainScene.unity`를 중심으로 구성했습니다. 검색된 다른 Scene과 Prefab은 TextMesh Pro 예제 자료이며, Quik Delivery 전용 기능 Scene은 `MainScene`입니다.

## 주요 Scene과 UI 구성

| 구분 | 경로 또는 구성 | 역할 |
|:---|:---|:---|
| 메인 Scene | `Assets/Scenes/MainScene.unity` | 홈, 배송 입력·목록, 통계, 캘린더, 정산과 설정 UI 구성 |
| 홈 화면 | `HomePanelUI` | 오늘·이번 달·전체 요약과 최근 배송 기록 표시 |
| 배송 입력 | `DeliveryInputPanelUI` | 배송 정보 입력, 계산 결과 표시와 생성·수정 |
| 배송 목록 | `DeliveryListPanelUI` | 기간별 기록, 부가세 대상 기록과 삭제 처리 |
| 통계 | `StatsPanelUI` | 기간·금액 조건별 배송 통계 |
| 캘린더 | `CalendarPanelUI` | 날짜별 요약, 주간 차트와 기록 목록 이동 |
| 월 정산 | `SettlementPanelUI`, `MonthlySummaryCardUI` | 연도 이동, 12개월 정산 카드와 백업 패널 |
| 설정 | `SettingsPanelUI` | 위치 자동 입력, 글자 크기와 삭제 확인 설정 |

Quik Delivery 전용 Prefab은 저장소에서 확인되지 않았습니다. 주요 UI는 `MainScene` 내부 오브젝트와 스크립트 참조로 구성되어 있습니다.

## 주요 스크립트

| 경로 | 역할 |
|:---|:---|
| `Assets/Scripts/Data/DeliveryRecord.cs` | 배송 정보와 `IsDailyFixedExpense` 상태 정의 |
| `Assets/Scripts/Data/WorkSession.cs` | 근무 시작·종료 데이터 정의 |
| `Assets/Scripts/Data/SettlementRecord.cs` | 월별 정산 데이터 정의 |
| `Assets/Scripts/Managers/DeliveryManager.cs` | 배송 CRUD, 기간 조회, 요약과 누락 고정비 생성 |
| `Assets/Scripts/Managers/WorkSessionManager.cs` | 근무 세션 저장과 근무시간 계산 |
| `Assets/Scripts/Managers/SettlementManager.cs` | 선택 연도의 월별 정산 집계 |
| `Assets/Scripts/Managers/BackupDataManager.cs` | 전체 백업 생성·복원과 다운로드 내보내기 |
| `Assets/Scripts/Repositories/DeliveryRepository.cs` | 배송 기록과 고정비 JSON 저장 |
| `Assets/Scripts/Repositories/WorkSessionRepository.cs` | 근무 세션 JSON 저장 |
| `Assets/Scripts/Repositories/SettingsRepository.cs` | 앱 설정 JSON 저장 |
| `Assets/Scripts/Services/JsonFileService.cs` | 저장 경로 구성과 JSON 직렬화·역직렬화 |
| `Assets/Scripts/Services/RevenueCalculator.cs` | 수입·비용·부가세·실수령 계산 |
| `Assets/Scripts/Services/LocationAutoFillService.cs` | Android 위치 권한과 주소 자동 입력 |
| `Assets/Scripts/UI/CalendarPanelUI.cs` | 캘린더와 주간 차트 표시 |
| `Assets/Scripts/UI/SettlementPanelUI.cs` | 연도 이동, 정산 갱신과 백업 UI 제어 |
| `Assets/Scripts/UI/MonthlySummaryCardUI.cs` | 월별 정산 카드 표시 |
| `Assets/Editor/SettlementPanelEditModeBuilder.cs` | 정산 화면 레이아웃과 12개 카드 참조 구성 |

## 데이터·백업 관련 파일

| 파일 | 저장 위치 | 역할 |
|:---|:---|:---|
| `delivery_records.json` | `Application.persistentDataPath` | 배송 기록과 일일 고정비 저장 |
| `work_sessions.json` | `Application.persistentDataPath` | 근무 시작·종료와 수정 기록 저장 |
| `app_settings.json` | `Application.persistentDataPath` | 앱 설정 저장 |
| `backup_*.json` | 앱 내부 저장소 | 배송 기록과 근무 세션 전체 백업 |
| 내보낸 백업 파일 | Android 다운로드 폴더 | 앱 외부 보관과 파일 선택 복원 |

실제 사용자 JSON, 배송 주소, 금액과 개인 메모는 저장소에 포함하지 않습니다.

## 주요 패키지

| 패키지 | 버전 | 용도 |
|:---|---:|:---|
| Unity | 6000.3.15f1 | 프로젝트 실행과 Android 빌드 |
| `com.unity.ugui` | 2.0.0 | 모바일 UI 구성 |
| TextMesh Pro | Unity Package | 텍스트 표시와 입력 |
| Unity Native File Picker | Git Package | Android 백업 파일 선택 |
| `com.unity.modules.androidjni` | 1.0.0 | Android 네이티브 연동 기반 |
| `com.unity.modules.jsonserialize` | 1.0.0 | JSON 직렬화 기반 |

## Android 빌드 관련 구조

```text
Assets/Plugins/Android/
├─ gradleTemplate.properties
├─ mainTemplate.gradle
└─ settingsTemplate.gradle
```

| 경로 | 역할 |
|:---|:---|
| `Assets/Plugins/Android/gradleTemplate.properties` | Android Gradle 속성 설정 |
| `Assets/Plugins/Android/mainTemplate.gradle` | 앱 모듈 Gradle 구성 |
| `Assets/Plugins/Android/settingsTemplate.gradle` | Gradle 프로젝트 설정 |
| `Packages/manifest.json` | Unity 패키지와 Native File Picker 의존성 |
| `ProjectSettings/ProjectVersion.txt` | Unity 편집기 버전 기록 |

APK 파일, Android 서명 파일과 실제 백업 JSON은 공개 저장소에서 제외합니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
