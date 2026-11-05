---
name: team-convention
description: OZGL3_4 팀(유니티 2D 메트로베니아) 공용 코드 컨벤션. 이 프로젝트에서 C# 스크립트를 작성·수정·리뷰하거나, 폴더/프리팹/씬을 만들거나, 커밋 메시지·브랜치·PR을 작성할 때 반드시 따른다.
---

# OZGL3_4 팀 코드 컨벤션

## 1. 개발 규칙
- **Addressables**와 **UniTask**를 사용한다. (비동기는 코루틴 대신 UniTask 우선, 에셋 로드는 Addressables)
- **씬 구성**: 씬의 모든 것은 **프리팹으로 구성**하며, 동적 생성할 예정이다.
- **UI 컴포넌트**
  - 텍스트: `TextMeshProUGUI`
  - 버튼: `Button - TextMeshPro`
- **람다 금지** (이벤트 구독, LINQ 등 포함. 이름 있는 메서드를 사용한다)

## 2. 명명 규칙
```csharp
[SerializeField] private Image Image_AAA;          // 타입명_이름
[SerializeField] private GameObject Prefab_AAA;    // GameObject 프리팹은 Prefab_
[SerializeField] private Transform Transform_AAA;
public string SoundName { get; private set; }      // 프로퍼티는 첫 글자 대문자 (PascalCase)

private float _speed = 5f;                         // private 필드는 _camelCase

// 이벤트 사용 시 반드시 On + 동사 + 대상
public event Action OnStartSomething;              // Start, End, Click 등

// 버튼 연결용 메서드는 OnClick + 대상
private void OnClickSomething() { }

// 일반 메서드는 동사 + 대상
private void MoveSomething() { }                   // Get, Set, Change, Move 등
```
- 다른 사람이 사용할 확률이 높은 메서드 등에는 **주석**을 단다.
- 가급적 **제네릭** 사용 (예: Array 대신 `List<T>`)
- 축약된 이름은 사용하지 않는다.
- 한 행에 하나의 변수만 선언한다.

## 3. 형식 규칙
### 들여쓰기 — Allman 스타일
여는 중괄호 `{`는 항상 새 줄에 둔다.
```csharp
void DisplayMouseCursor(bool showMouse)
{
    if (!showMouse)
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
```

### 중괄호
- 한 줄짜리 if/for 문도 **중괄호를 붙인다.**
- 예외: **얼리 return**은 중괄호 생략 가능.
```csharp
// 금지
for (int i = 0; i < 10; i++)
    for (int j = 0; j < 10; j++)
        hap += 1;

// 권장
for (int i = 0; i < 10; i++)
{
    for (int j = 0; j < 10; j++)
    {
        hap += 1;
    }
}
```

### 공백
| 규칙 | 권장 | 금지 |
|---|---|---|
| 인수 사이 쉼표 뒤 공백 1개 | `CollectItem(myObject, 0, 1);` | `CollectItem(myObject,0,1);` |
| 괄호 안쪽 공백 없음 | `DropPowerUp(myPrefab, 0, 1);` | `DropPowerUp( myPrefab, 0, 1 );` |
| 대괄호 안 공백 없음 | `x = dataArray[index];` | `x = dataArray[ index ];` |
| 흐름 제어 키워드와 괄호 사이 공백 | `while (x == y)` | `while(x==y)` |
| 비교 연산자 앞뒤 공백 | `if (x == y)` | `if (x==y)` |

## 4. 폴더 구조
```
Assets/
├── Prefabs/
│   ├── UI/
│   └── Objects/
├── Scripts/
│   ├── Manager/
│   ├── Data/
│   ├── UI/
│   └── Feature/
├── Resources/
│   ├── UI/
│   └── JsonOutput/
├── Scenes/
└── ThirdParty/
```

## 5. 공용 매니저 및 주요 클래스
프로젝트 전반에서 공용으로 쓰는 라인업. **생성·수정 시 팀원과 싱크를 맞춰야 하므로**, 이 클래스들을 바꾸는 작업이면 사용자에게 먼저 알린다.
- `GameManager` / `GameDataManager` / `GameData`
- `UIManager` / `UIExtension`
- `GameObjectManager` / `ResourceManager`
- `SoundManager` / `SaveManager`
- `PlayerModel` / `GameUtil`

**매니저 생성 규칙**: `MonoBehaviour` 대신 `SingletonBase<클래스명>`을 상속한다. 싱글톤을 따로 구현하지 않는다.
```csharp
public class SoundManager : SingletonBase<SoundManager>
{
}
```

## 6. Git & 브랜치
- `master`: 최종 배포용. **직접 Push 절대 금지.**
- `develop`: 기능 개발이 완료된 작업물이 모이는 메인 브랜치.
- `feature/(기능)`: 기능 개발 브랜치. **Projects 업무 이슈 하나당 브랜치 하나.**

### 커밋 메시지
형식: `[태그] 내용` (한국어)

| 태그 | 사용 상황 | 예시 |
|---|---|---|
| `[feat]` | 새로운 기능 개발 중이거나 완료 | `[feat] 플레이어 이동 시스템 구현` |
| `[fix]` | 버그 수정 | `[fix] 점프 시 간헐적으로 끼이는 현상 수정` |
| `[chore]` | 빌드 세팅, 패키지 설치, 파일 이동 등 코드 외적인 작업 | `[chore] UniTask 패키지 설치` |
| `[refactor]` | 버그는 없으나 가독성·구조 개선 | `[refactor] UI 매니저 초기화 로직 개선` |

## 7. 작업 및 PR 프로세스
### 작업 진행
1. 기능별로 본인이 작업할 **개별 씬을 하나 생성**한다. (씬 이름 = 기능 이름)
2. 작업은 반드시 해당 기능의 `feature/(기능)` 브랜치에서 한다.
3. 작업이 끝나면 Project 상태를 업데이트하고, **무조건 `develop` 브랜치를 향해** PR을 만든다.
4. **PR 전 필수 체크**: `develop`을 먼저 pull 받아 로컬에서 병합하고, 충돌 없이 잘 돌아가는지 확인한 뒤 push한다.

### PR 작성 및 머지
- **제목**: `[브랜치 이름(본인 이름)] 기능 요약`
- **내용**: 필요하면 어떤 방식으로 구현했는지 기술한다.
- PR 작성 후 **디스코드에 공유**한다. (사용자에게 공유하라고 안내)
- **본인이 직접 PR 승인/머지 금지.** 팀원 교차 검증 후 머지한다. → PR을 대신 만들더라도 머지·승인·auto-merge는 절대 하지 않는다.
