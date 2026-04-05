# 프로토타입 실구현 프롬프트 pack

이 문서는 `docs/20_queue/prototype-implementation-plan.md`를 실제 구현 작업으로 옮길 때 사용할 프롬프트 묶음입니다.

목표는 아래 둘 중 하나입니다.

* 한 번에 큰 범위를 맡기는 통합 프롬프트 사용
* 구현 파동을 나눠 단계별 프롬프트를 순서대로 사용

권장 방식은 **통합 프롬프트 1개 + 단계별 프롬프트 5개** 조합입니다.

---

## 1. 사용 원칙

모든 프롬프트는 아래 전제를 공유합니다.

* 작업 디렉토리는 현재 레포 루트다.
* 현재 Web UI의 기본 5문양 recognizer는 유지한다.
* 같은 모양은 같은 family라는 원칙을 깨면 안 된다.
* 품질 벡터 기반 보정은 semantics를 바꾸는 용도가 아니다.
* overlay는 base family를 바꾸지 않고 파생/실행 연산자로만 작동한다.
* draw 중에는 preview만, canonical 확정은 `seal`에서만 한다.
* 문서는 한국어를 유지한다.

구현 전에 반드시 읽을 파일:

* `README.md`
* `docs/10_direction/final-direction.md`
* `docs/10_direction/prototype-target.md`
* `docs/20_queue/prototype-implementation-plan.md`
* `src/app.ts`
* `src/recognizer/recognize.ts`
* `src/recognizer/quality.ts`
* `src/recognizer/types.ts`
* `tests/recognizer.test.ts`

---

## 2. 통합 프롬프트

아래 프롬프트는 한 번에 큰 작업 범위를 맡길 때 사용합니다.

```text
현재 레포의 Magic Recognizer V1을 V1.5로 확장하라.

목표:
1. 현재 Web UI의 기본 5문양 recognizer를 유지한다.
2. 유저 입력 누적을 반영하는 user input profile을 추가한다.
3. raw quality와 adjusted quality를 분리한다.
4. base seal 이후 같은 캔버스에 덧그리는 overlay operator 인식을 추가한다.
5. 최소 operator 6개를 구현한다: steel_brace, electric_fork, ice_bar, soul_dot, void_cut, martial_axis.
6. martial_axis는 void_cut가 먼저 있을 때만 recognized 가능하게 한다.
7. final seal 시 base family + overlay list + raw/adjusted quality + profile delta를 compile 결과로 보여 준다.
8. Web UI에서 phase를 분리하고 debug overlay와 log export를 유지/확장한다.
9. 기존 root recognizer 테스트를 깨지 않도록 새 테스트를 추가한다.

제약:
* 같은 모양은 같은 family 원칙을 유지한다.
* 개인화는 family 판정 권한을 가져가면 안 된다.
* overlay는 base seal 이후에만 해석한다.
* 원본 stroke는 ghost overlay로만 보조하고 대체하지 않는다.
* 문서와 코드 naming을 일관되게 맞춘다.

작업 순서:
1. 현재 구조를 빠르게 점검한다.
2. 필요한 타입과 모듈 경계를 먼저 설계한다.
3. user profile과 quality adjustment를 추가한다.
4. overlay recognizer와 compile 결과 모델을 추가한다.
5. Web UI를 base/overlay/final phase로 개편한다.
6. 테스트를 추가하고 npm test, npm run build로 검증한다.
7. 변경 내용을 간단히 정리한다.

우선 실제 코드 변경부터 진행하고, 막히면 그때만 가정을 짧게 적어라.
```

---

## 3. 단계별 프롬프트 A. 구조/타입 확장

```text
현재 레포에서 overlay operator와 user input profile을 수용할 수 있도록 타입과 모듈 구조를 먼저 정리하라.

필수 작업:
* src/recognizer/types.ts 확장
* src/recognizer/operator-spec.ts 추가
* src/recognizer/user-profile.ts 추가
* 필요한 compile 결과 타입 초안 추가

설계 기준:
* base recognizer와 overlay recognizer는 타입 상으로 분리한다.
* InputPhase는 base / overlay / compiled 3단계로 둔다.
* OverlayOperator는 최소 6개만 우선 포함한다.
* rawQuality와 adjustedQuality를 별도 필드로 둔다.
* 로그 스키마 확장 가능성을 남긴다.

변경 후에는 새 타입 구조가 이후 recognize.ts, app.ts, tests에서 바로 사용 가능해야 한다.
불필요한 UI 변경은 아직 하지 마라.
```

---

## 4. 단계별 프롬프트 B. 품질 벡터 보정

```text
현재 recognizer에 user input profile 기반 quality adjustment를 추가하라.

목표:
* raw quality는 현재 방식대로 계산한다.
* adjusted quality는 user comfort band를 반영해 다시 계산하거나 재해석한다.
* profile은 누적 샘플 수가 적을 때 baseline fallback을 사용한다.
* family 판정은 유지하고, quality interpretation만 더 안정적으로 만든다.

필수 제약:
* 개인화 때문에 invalid가 recognized로 뒤집히면 안 된다.
* tempo, stability, rotationBias 위주로 시작하고 closure는 보조 정도로만 쓴다.
* sampleCount가 충분하지 않으면 adjustment를 약하게 한다.

작업 대상:
* src/recognizer/quality.ts
* src/recognizer/user-profile.ts
* src/recognizer/recognize.ts
* tests에 관련 케이스 추가

작업 후에는 빠른 입력과 느린 입력이 같은 family로 유지되면서 adjusted quality만 달라지는 테스트가 있어야 한다.
```

---

## 5. 단계별 프롬프트 C. overlay recognizer

```text
base seal 이후 덧그리는 overlay operator recognizer를 추가하라.

이번 단계에서 구현할 operator:
* steel_brace
* electric_fork
* ice_bar
* soul_dot
* void_cut
* martial_axis

핵심 요구:
* overlay stroke는 base stroke와 별도 세션으로 관리한다.
* base silhouette 기준 anchor zone 또는 reference frame을 계산한다.
* operator마다 단순하지만 설명 가능한 scoring 함수를 둔다.
* recognized / ambiguous / incomplete / invalid 상태를 반환한다.
* martial_axis는 void_cut가 없으면 incomplete 또는 invalid 여야 한다.

작업 대상:
* src/recognizer/operators.ts
* src/recognizer/operator-templates.ts
* 필요 시 src/recognizer/geometry.ts
* src/recognizer/types.ts
* overlay 테스트 파일

중요:
* overlay가 base family를 바꾸면 안 된다.
* base recognizer 코드를 최대한 덜 건드리고 확장 모듈로 분리하라.
```

---

## 6. 단계별 프롬프트 D. compile 결과와 로그

```text
base family, overlay, raw/adjusted quality, profile delta를 하나의 compiled result로 묶고 로그 export를 확장하라.

필수 요구:
* CompiledSpellResult 또는 동등한 구조를 추가한다.
* final seal 시점에만 canonical compiled result를 만든다.
* log에는 raw strokes, base result, overlay result, profile snapshot, final compiled result를 남긴다.
* 기존 log export 버튼은 유지한다.

작업 대상:
* src/recognizer/compile.ts
* src/recognizer/types.ts
* src/app.ts

완료 기준:
* export한 JSON 하나만으로 한 번의 authoring 세션을 이해할 수 있어야 한다.
```

---

## 7. 단계별 프롬프트 E. Web UI 통합

```text
현재 Web UI를 V1.5 authoring UI로 확장하라.

목표:
* base 입력 단계와 overlay 입력 단계를 분리한다.
* 버튼 흐름을 명확하게 만든다.
* overlay preview panel을 추가한다.
* raw quality와 adjusted quality를 함께 보여 준다.
* debug overlay에서 anchor zone, operator ghost, phase 상태를 확인 가능하게 한다.

필수 UI 흐름:
1. 사용자가 base를 그림
2. Seal Base로 base family를 확정
3. Start Overlay 상태에서 overlay를 덧그림
4. Seal Final로 compiled result를 확정

제약:
* 원본 stroke는 그대로 보여 준다.
* beautify 선으로 대체하지 않는다.
* 현재 디자인 언어를 크게 깨지 않는다.

작업 대상:
* src/app.ts
* src/style.css

작업 후에는 브라우저에서 base root와 overlay operator를 순서대로 테스트할 수 있어야 한다.
```

---

## 8. 단계별 프롬프트 F. 테스트와 마감 검증

```text
이번 단계 변경 사항에 대한 테스트와 검증을 마무리하라.

필수 작업:
* 기존 base recognizer 테스트 유지
* user profile adjustment 테스트 추가
* overlay operator 6개 인식 테스트 추가
* martial_axis requires void_cut 규칙 테스트 추가
* invalid overlay가 base family를 흔들지 않는 회귀 테스트 추가
* npm test와 npm run build를 실행해 결과를 확인한다

보고 형식:
* 어떤 테스트를 추가했는지
* 무엇을 검증했는지
* 남은 리스크가 무엇인지

테스트 통과와 빌드 통과가 모두 확인될 때까지 멈추지 마라.
```

---

## 9. 문서 업데이트 프롬프트

코드 작업 뒤에 문서를 맞출 때는 아래 프롬프트를 사용합니다.

```text
방금 구현한 V1.5 확장 내용을 현재 문서 체계에 맞게 갱신하라.

필수 반영:
* README.md 사용 방법 업데이트
* docs/20_queue/prototype-implementation-plan.md의 진행 상태 반영
* 필요하면 docs/10_direction/prototype-target.md에 Web UI 검증 범위 문구 보강

문서 원칙:
* 한국어 유지
* 기존 결정 문서를 덮어쓰지 말고, 현재 구현 범위를 명확히 적는다
* 문서가 코드보다 앞서 나가지 않게 한다
```

---

## 10. 최종 검수 프롬프트

구현 마감 직전에 아래 프롬프트를 사용하는 것이 가장 안전합니다.

```text
현재 작업 트리를 기준으로 이번 단계 목표가 실제로 충족됐는지 점검하라.

체크 항목:
* 기본 5문양 recognizer가 유지되는가
* user input profile 기반 adjusted quality가 보이는가
* 최소 operator 6개를 Web UI에서 확인 가능한가
* same shape = same family 원칙이 깨지지 않는가
* overlay는 base seal 이후에만 읽히는가
* export log에 base / overlay / compiled 결과가 모두 남는가
* npm test와 npm run build가 통과하는가

출력 형식:
* 충족된 항목
* 미충족 항목
* 남은 리스크
* 다음 단계 권장 작업 3개 이내
```

---

## 11. 권장 실행 순서

가장 권장하는 실제 사용 순서는 아래입니다.

1. 통합 프롬프트로 전체 뼈대를 먼저 구현
2. 단계별 프롬프트 B, C, E로 품질 보정, overlay, UI를 집중 보강
3. 단계별 프롬프트 F로 테스트 고정
4. 문서 업데이트 프롬프트 수행
5. 최종 검수 프롬프트 수행

이 순서를 따르면, 현재 레포의 V1 recognizer를 깨지 않고 V1.5까지 확장하는 흐름을 가장 안정적으로 가져갈 수 있습니다.
