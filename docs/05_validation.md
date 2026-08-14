# 05. 검증 결과

## 테스트 환경

| 항목 | 내용 |
|:---|:---|
| Unity 버전 | Unity 6000.3.15f1 |
| 주요 Scene | `Assets/Scenes/MainScene.unity` |
| 실행 환경 | Unity Play Mode, Android 실기기 |
| 저장 환경 | `Application.persistentDataPath`, Android 다운로드 폴더 |
| 검증 방식 | 주요 사용 흐름을 기준으로 한 시나리오 검증 |
| 공개 자료 | 개인정보가 없는 화면 캡처와 Android 시연 영상 |

## 검증 항목

| 영역 | 검증 항목 | 결과 |
|:---|:---|:---:|
| 앱 실행 | `MainScene` 실행과 홈·하단 메뉴·주요 패널 전환 | PASS |
| 배송 기록 | 입력값 계산과 새 기록 JSON 저장 | PASS |
| 배송 수정 | 기존 식별자를 유지한 상태의 값 변경과 화면 갱신 | PASS |
| 배송 삭제 | 기록 삭제 후 홈·목록·통계·캘린더·정산 갱신 | PASS |
| 근무 세션 | 근무 시작·종료와 날짜·시간 수동 보정 | PASS |
| 기간 통계 | 오늘·주간·월간·연간·전체·직접 기간 집계 | PASS |
| 금액 필터 | 최소·최대 금액 조건 적용 | PASS |
| 캘린더 | 날짜별 요약, 주간 차트와 선택 날짜 기록 이동 | PASS |
| 부가세 | 대상 기록 조회와 월별 합계 표시 | PASS |
| 월별 정산 | 이전·다음 연도 이동과 1월부터 12월까지의 카드 갱신 | PASS |
| 일일 고정비 | 기준 시작일부터 오늘까지 누락된 고정비 레코드 생성 | PASS |
| 배송 건수 분리 | 일일 고정비를 비용에는 포함하고 배송 건수에서는 제외 | PASS |
| 전체 백업 | 배송 기록과 근무 세션을 `backup_*.json`으로 생성 | PASS |
| 백업 복원 | 선택한 백업 파일 저장, 누락 고정비 재확인과 화면 갱신 | PASS |
| 다운로드 내보내기 | 최신 백업 파일을 Android 다운로드 폴더에서 확인 | PASS |
| Android 실행 | APK 설치 후 주요 기능 흐름 실행 | PASS |

## 검증 결과

배송 기록 생성·조회·수정·삭제 이후 관련 화면이 같은 데이터 기준으로 갱신되는 것을 확인했습니다. 기간과 금액 조건에 따른 통계, 월간 캘린더, 부가세 목록과 연도별 12개월 정산도 저장된 배송·근무 데이터를 기준으로 반영됐습니다.

누락된 일일 고정비는 기준 시작일부터 오늘까지 생성되며, 비용과 실수령 계산에는 포함되지만 배송 건수에서는 제외되는 것을 확인했습니다.

전체 백업은 배송 기록과 근무 세션을 하나의 `backup_*.json` 파일로 생성합니다. Android 실기기에서 백업 파일 생성, 다운로드 폴더 내보내기와 선택한 파일 복원 이후 데이터 및 화면 갱신을 확인했습니다.

## 검증 화면

<table>
  <tr>
    <td width="50%" align="center"><img src="images/mobile/01_실기기_홈_대시보드.png" alt="Android 실기기 홈 화면" width="100%"></td>
    <td width="50%" align="center"><img src="images/mobile/03_실기기_배송_기록_입력.jpg" alt="Android 실기기 배송 기록 입력" width="100%"></td>
  </tr>
  <tr>
    <td align="center">홈 대시보드</td>
    <td align="center">배송 기록 입력</td>
  </tr>
</table>

<table>
  <tr>
    <td width="50%" align="center"><img src="images/mobile/06_실기기_통계_분석.jpg" alt="Android 실기기 통계 분석" width="100%"></td>
    <td width="50%" align="center"><img src="images/mobile/09_실기기_캘린더_분석.jpg" alt="Android 실기기 캘린더 분석" width="100%"></td>
  </tr>
  <tr>
    <td align="center">기간·금액 통계</td>
    <td align="center">월간 캘린더 분석</td>
  </tr>
</table>

<table>
  <tr>
    <td width="50%" align="center"><img src="images/mobile/07_실기기_연도별_월_정산.jpg" alt="Android 실기기 연도별 월 정산" width="100%"></td>
    <td width="50%" align="center"><img src="images/mobile/08_실기기_부가세_기록.png" alt="Android 실기기 부가세 기록" width="100%"></td>
  </tr>
  <tr>
    <td align="center">연도별 월 정산</td>
    <td align="center">부가세 대상 기록</td>
  </tr>
</table>

## Android 빌드·실행 검증

Android 기기에 APK를 설치한 뒤 홈 대시보드, 근무시간 설정, 배송 기록 생성·수정·삭제, 통계, 캘린더, 부가세와 연도별 월 정산 화면을 순서대로 확인했습니다.

실기기 시연 영상과 전체 화면 자료는 [Android 실기기 시연 문서](demo/README.md)에 정리했습니다.

## 백업·복원 검증

<p align="center">
  <img src="images/validation/10_안드로이드_apk_백업_파일.jpg" alt="Android APK와 백업 파일 검증" width="90%">
</p>

Android 실기기에서 전체 백업 생성 후 앱 내부 저장소와 다운로드 폴더의 백업 파일을 확인했습니다. 선택한 백업 파일을 복원한 뒤 배송·근무 데이터 저장, 누락 고정비 재확인과 연결 화면 갱신을 검증했습니다.

## 월별 정산 검증

<p align="center">
  <img src="images/settlement/연도별_월_정산.png" alt="1월부터 12월까지의 월별 정산 화면" width="360">
</p>

이전·다음 연도로 이동할 때 연도 제목과 월별 데이터가 함께 갱신되며, 1월부터 12월까지 고정된 12개 카드에 배송 건수, 총수입, 비용 항목과 실수령이 표시되는 것을 확인했습니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
