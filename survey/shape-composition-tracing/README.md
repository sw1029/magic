# Shape Composition Tracing Survey

정적 단독 모듈입니다. 이 디렉토리만 압축해서 배포할 수 있고, 서버/API 없이 브라우저에서 실행됩니다.

## 실행

- `index.html`을 브라우저에서 열거나, 이 디렉토리에서 정적 서버를 띄워 접속합니다.
- 한 세션은 도형 조합 2개를 순서대로 수집합니다.
- 각 조합은 `3`회 마음대로, `7`회 똑바로, `7`회 편하게, `7`회 빠르게, 최종 `3`회 똑바로 따라그리기로 구성됩니다.

## 수집 데이터

`JSON 저장`은 재현 가능한 원자료를 저장합니다.

- `compositions`: 사용자가 만든 두 도형 조합의 도형 타입, 위치, 크기, 회전, 굵기, 편집 이벤트
- `trials`: 각 따라그리기 시도의 block, trial index, elapsed time, target shape snapshot
- `strokes`: pointer stroke별 raw sample (`x`, `y`, `x1000`, `y1000`, `tMs`, `pressure`)
- `shapeTrace`: 기존 마법진 수집과 유사한 정규화 좌표 형식 (`[x1000, y1000, tMs]`)

`CSV 요약 저장`은 trial 단위 확인용 요약만 저장합니다. 분석 원자료는 JSON을 사용하세요.
