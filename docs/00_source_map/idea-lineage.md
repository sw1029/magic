# 아이디어 계보

이 문서는 `chat/`의 논의를 네 단계로 묶어, “무엇이 어떻게 바뀌었는가”를 상위 관점에서 정리합니다.

---

## 1단계. 원본 아이디어

관련 문서:

* [`request-answer01`](../../chat/request-answer01.md)
* [`request-answer02`](../../chat/request-answer02.md)

이 단계에서 정리된 핵심:

* 화면에 마법진을 그리면 결과가 나오는 게임 아이디어
* 속도, 각도, 품질, 환경이 결과를 바꾸는 구조
* 여러 마법진의 상호작용
* 2D 우선, 3D 확장
* 말처럼 읽는 체계가 아니라 공간적으로 읽는 시각 언어

이 단계의 의의:

* 프로젝트의 출발점
* “같은 모양은 같은 종류, 세부 손맛은 결과 차이”라는 방향의 씨앗

---

## 2단계. 언어 체계 형성

관련 문서:

* [`request-answer03`](../../chat/request-answer03.md)
* [`request-answer04`](../../chat/request-answer04.md)

이 단계에서 정리된 핵심:

* 5개 기본 속성과 10개 파생 속성
* core primitive와 시각 층위
* 여러 계층으로 읽는 구조
* 오류 정정과 컴파일 흐름
* 3D는 독립 언어가 아니라 상위 확장

이 단계의 의의:

* 마법진을 “그림”이 아니라 “구조”로 다루기 시작한 시점
* 이후 모든 문서의 기본 문법 틀이 여기서 나옴

---

## 3단계. 물리·개인화·UX 확장

관련 문서:

* [`request-answer05`](../../chat/request-answer05.md)
* [`request-answer06`](../../chat/request-answer06.md)
* [`request-answer07`](../../chat/request-answer07.md)
* [`request-answer08`](../../chat/request-answer08.md)
* [`request-answer09`](../../chat/request-answer09.md)

이 단계에서 정리된 핵심:

* 언어만으로는 창발성이 완성되지 않음
* 공통 결과 규칙과 반응 구조 필요
* 속도, 각도, 개인차는 결과 변화에만 반영
* 공방, 로그, 비교, 다시 보기 같은 실험 UX 필요
* 2D/3D, combat/forge, 결과 고정/손맛 차이 구분

이 단계의 의의:

* 구현 난이도와 UX 필요 요소가 함께 드러남
* 이후 작업 큐를 만들 때 “무엇을 먼저 만들고 무엇을 보류할지”를 판단하는 기준이 됨

---

## 4단계. 단순화와 클라이언트 확정

관련 문서:

* [`request-answer10`](../../chat/request-answer10.md)
* [`request-answer11`](../../chat/request-answer11.md)
* [`request-answer12`](../../chat/request-answer12.md)
* [`request-answer13`](../../chat/request-answer13.md)
* [`request-answer14`](../../chat/request-answer14.md)
* [`request-answer15`](../../chat/request-answer15.md)
* [`request-answer16`](../../chat/request-answer16.md)
* [`request-answer17`](../../chat/request-answer17.md)
* [`request-answer18`](../../chat/request-answer18.md)

이 단계에서 정리된 핵심:

* 기본 심볼 단순화
* 프로토타입용 기본 문양 5종 방향
* AI는 가벼운 보조 도구로 제한
* 2D 우선, 3D 후속
* 온라인 배제
* 로그 우선, 실패 연출과 실험 스테이지는 후속
* 성질 변형은 여러 번 허용
* 無와 武는 세계관 차이로 재정리

이 단계의 의의:

* 실제 개발 기준이 확정된 단계
* 현재 `docs/`와 작업 큐는 이 단계를 기준으로 구성됨

---

## 현재 기준에서 가장 중요한 변화

초기에는 “복잡한 문양 자체”가 강한 체계로 갔다면, 현재는 아래 방향으로 이동했습니다.

* 기본 문양은 단순화
* 구조와 조합은 확장
* 같은 모양은 같은 종류 유지
* 속도와 각도는 결과 차이 반영
* 창발성은 규칙과 상호작용에서 회수

즉, 현재의 목표는 “복잡한 문양을 많이 외우는 게임”이 아니라
**“간단한 문양을 바탕으로 실험과 조합에서 깊이가 생기는 마법 시스템”**입니다.
