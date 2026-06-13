#!/usr/bin/env python3
"""Playtest attempts.csv 분석 도구.

RESEARCH_PROTOCOL.md의 측정 지표를 attempts.csv에서 자동 산출한다.
플레이테스트 후 세션 로그 폴더들을 지정하면 분석 요약(markdown + csv)을 만든다.

산출 분석 (FINAL_REPORT.md §5.2 대응):
  RQ1  학습 곡선        — 세션 내 시도 순서에 따른 성공률 이동 평균
  RQ2  힌트 전후 비교    — 힌트 노출 직전 vs 직후 성공률 변화 (준-인과 증거)
  RQ3  family 공정성    — targetFamily별 첫 시도 성공률 + 혼동 행렬
  공통  실패 유형 분포, assist level 도달률, 층별 막힘 지점, 첫 시전까지 시간

사용:
  python scripts/analyze-playtest-attempts.py <세션폴더 또는 attempts.csv> [...] -o outputs/playtest-1/analysis
  # 세션 폴더: Application.persistentDataPath/MagicExamHallLogs/<sessionId>/

SUS 채점 (선택):
  python scripts/analyze-playtest-attempts.py --sus outputs/playtest-1/sus.csv
  # sus.csv 형식: participantId,q1,...,q10 (각 1~5)

의존성: 표준 라이브러리만 사용.
"""

from __future__ import annotations

import argparse
import csv
import statistics
import sys
from collections import Counter, defaultdict
from pathlib import Path

TRUE_VALUES = {"true", "1", "yes"}
ROLLING_WINDOW = 10  # 학습 곡선 이동 평균 창
HINT_WINDOW = 5      # 힌트 전후 비교 창 (시도 수)
STUCK_FAILS = 3      # 막힘 판정: 동일 목표 연속 실패 수


def parse_bool(value: str) -> bool:
    return value.strip().lower() in TRUE_VALUES


def load_attempts(path: Path) -> list[dict]:
    """attempts.csv 한 개를 읽어 시도 dict 리스트로 반환."""
    rows: list[dict] = []
    with path.open(encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            try:
                rows.append({
                    "sessionId": row.get("sessionId", ""),
                    "floorId": row.get("floorId", ""),
                    "targetFamily": row.get("targetFamily", ""),
                    "recognizedFamily": row.get("recognizedFamily", ""),
                    "phase": row.get("phase", ""),
                    "status": row.get("status", ""),
                    "targetObject": row.get("targetObject", ""),
                    "attemptIndex": int(float(row.get("attemptIndex") or 0)),
                    "elapsedMs": int(float(row.get("elapsedMs") or 0)),
                    "success": parse_bool(row.get("success", "")),
                    "hintShown": parse_bool(row.get("hintShown", "")),
                    "assistLevel": int(float(row.get("assistLevel") or 0)),
                    "assisted": parse_bool(row.get("assisted", "")),
                })
            except (ValueError, TypeError):
                continue  # 손상 행은 건너뜀
    rows.sort(key=lambda r: r["attemptIndex"])
    return rows


def find_attempt_files(inputs: list[str]) -> list[Path]:
    files: list[Path] = []
    for raw in inputs:
        p = Path(raw)
        if p.is_dir():
            files.extend(sorted(p.rglob("attempts.csv")))
        elif p.is_file():
            files.append(p)
        else:
            print(f"warning: not found, skipping: {p}", file=sys.stderr)
    return files


def rate(success_count: int, total: int) -> float:
    return success_count / total if total else 0.0


# ---------------------------------------------------------------- RQ1 학습 곡선

def learning_curve(rows: list[dict]) -> list[tuple[int, float]]:
    """(attemptIndex, 직전 ROLLING_WINDOW개 성공률) 시퀀스."""
    points = []
    window: list[bool] = []
    for r in rows:
        window.append(r["success"])
        if len(window) > ROLLING_WINDOW:
            window.pop(0)
        points.append((r["attemptIndex"], rate(sum(window), len(window))))
    return points


def learning_summary(rows: list[dict]) -> dict:
    """세션 전반부 vs 후반부 성공률 — 학습의 한 줄 요약."""
    if len(rows) < 4:
        return {"firstHalf": None, "secondHalf": None, "delta": None}
    mid = len(rows) // 2
    first = rate(sum(r["success"] for r in rows[:mid]), mid)
    second = rate(sum(r["success"] for r in rows[mid:]), len(rows) - mid)
    return {"firstHalf": first, "secondHalf": second, "delta": second - first}


# ---------------------------------------------------------------- RQ2 힌트 전후

def hint_effect(rows: list[dict]) -> dict:
    """첫 힌트 노출(hintShown 또는 assistLevel>0) 전후 HINT_WINDOW개 시도의 성공률 비교.

    또한 조건부 비교: 직전 시도에서 힌트를 본 경우 vs 못 본 경우의 성공률.
    """
    first_hint = next((i for i, r in enumerate(rows) if r["hintShown"] or r["assistLevel"] > 0), None)
    around = {"before": None, "after": None, "delta": None}
    if first_hint is not None and first_hint >= 1:
        before = rows[max(0, first_hint - HINT_WINDOW):first_hint]
        after = rows[first_hint:first_hint + HINT_WINDOW]
        if before and after:
            b = rate(sum(r["success"] for r in before), len(before))
            a = rate(sum(r["success"] for r in after), len(after))
            around = {"before": b, "after": a, "delta": a - b}

    after_hint, after_hint_n = 0, 0
    after_nohint, after_nohint_n = 0, 0
    for prev, cur in zip(rows, rows[1:]):
        if prev["hintShown"]:
            after_hint += cur["success"]
            after_hint_n += 1
        else:
            after_nohint += cur["success"]
            after_nohint_n += 1
    conditional = {
        "pSuccessAfterHint": rate(after_hint, after_hint_n) if after_hint_n else None,
        "nAfterHint": after_hint_n,
        "pSuccessAfterNoHint": rate(after_nohint, after_nohint_n) if after_nohint_n else None,
        "nAfterNoHint": after_nohint_n,
    }
    return {"aroundFirstHint": around, "conditional": conditional}


# ---------------------------------------------------------------- RQ3 공정성

def family_fairness(rows: list[dict]) -> dict:
    """targetFamily별 첫 시도 성공률 + 혼동 행렬."""
    first_attempt: dict[tuple[str, str], dict] = {}
    for r in rows:
        if not r["targetFamily"]:
            continue
        key = (r["sessionId"], r["targetFamily"])
        if key not in first_attempt:
            first_attempt[key] = r

    per_family: dict[str, list[bool]] = defaultdict(list)
    for (_, family), r in first_attempt.items():
        per_family[family].append(r["success"])

    confusion: Counter = Counter()
    for r in rows:
        if r["targetFamily"] and r["recognizedFamily"]:
            confusion[(r["targetFamily"], r["recognizedFamily"])] += 1

    return {
        "firstAttemptSuccess": {f: rate(sum(v), len(v)) for f, v in sorted(per_family.items())},
        "firstAttemptN": {f: len(v) for f, v in sorted(per_family.items())},
        "confusion": confusion,
    }


# ---------------------------------------------------------------- 공통 지표

def common_metrics(rows: list[dict]) -> dict:
    status_dist = Counter(r["status"] for r in rows if r["status"])
    assist_reach = Counter()
    for level in (1, 2, 3):
        sessions = {r["sessionId"] for r in rows if r["assistLevel"] >= level}
        assist_reach[level] = len(sessions)

    stuck: list[tuple[str, str, int]] = []
    streak: dict[tuple[str, str], int] = defaultdict(int)
    for r in rows:
        key = (r["floorId"], r["targetObject"] or r["targetFamily"])
        if r["success"]:
            streak[key] = 0
        else:
            streak[key] += 1
            if streak[key] == STUCK_FAILS:
                stuck.append((key[0], key[1], r["attemptIndex"]))

    first_cast_ms = min((r["elapsedMs"] for r in rows), default=None)
    per_floor_attempts = Counter(r["floorId"] for r in rows if r["floorId"])

    return {
        "totalAttempts": len(rows),
        "overallSuccessRate": rate(sum(r["success"] for r in rows), len(rows)),
        "statusDistribution": status_dist,
        "assistReachSessions": dict(assist_reach),
        "stuckPoints": stuck,
        "firstCastMs": first_cast_ms,
        "attemptsPerFloor": dict(per_floor_attempts),
    }


# ---------------------------------------------------------------- SUS

def score_sus(path: Path) -> list[tuple[str, float]]:
    """SUS 표준 채점: 홀수 문항 (응답-1), 짝수 문항 (5-응답), 합계 x 2.5."""
    scores = []
    with path.open(encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            pid = row.get("participantId", "?")
            try:
                total = 0
                for q in range(1, 11):
                    v = int(row[f"q{q}"])
                    total += (v - 1) if q % 2 == 1 else (5 - v)
                scores.append((pid, total * 2.5))
            except (KeyError, ValueError):
                print(f"warning: invalid SUS row for {pid}", file=sys.stderr)
    return scores


# ---------------------------------------------------------------- 출력

def fmt_pct(v) -> str:
    return "-" if v is None else f"{v * 100:.1f}%"


def write_report(out_dir: Path, sessions: dict[str, list[dict]]) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    all_rows = [r for rows in sessions.values() for r in rows]
    lines = ["# Playtest attempts 분석", "", f"세션 {len(sessions)}개, 시도 {len(all_rows)}건", ""]

    lines += ["## RQ1 — 학습 곡선 (세션 전/후반 성공률)", "",
              "| 세션 | 전반부 | 후반부 | 변화 |", "| --- | --- | --- | --- |"]
    for sid, rows in sessions.items():
        s = learning_summary(rows)
        lines.append(f"| {sid} | {fmt_pct(s['firstHalf'])} | {fmt_pct(s['secondHalf'])} | {fmt_pct(s['delta'])} |")
    lines.append("")

    curve_path = out_dir / "learning-curve.csv"
    with curve_path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        w.writerow(["sessionId", "attemptIndex", f"rollingSuccessRate(window={ROLLING_WINDOW})"])
        for sid, rows in sessions.items():
            for idx, val in learning_curve(rows):
                w.writerow([sid, idx, f"{val:.3f}"])
    lines += [f"이동 평균 곡선 원자료: `{curve_path.name}` (그래프용)", ""]

    lines += ["## RQ2 — 힌트 노출 전후 성공률", "",
              "| 세션 | 첫 힌트 전 | 첫 힌트 후 | 변화 | P(성공\\|직전 힌트) | P(성공\\|직전 힌트 없음) |",
              "| --- | --- | --- | --- | --- | --- |"]
    for sid, rows in sessions.items():
        h = hint_effect(rows)
        a, c = h["aroundFirstHint"], h["conditional"]
        lines.append(
            f"| {sid} | {fmt_pct(a['before'])} | {fmt_pct(a['after'])} | {fmt_pct(a['delta'])} "
            f"| {fmt_pct(c['pSuccessAfterHint'])} (n={c['nAfterHint']}) "
            f"| {fmt_pct(c['pSuccessAfterNoHint'])} (n={c['nAfterNoHint']}) |")
    lines += ["", f"창 크기: 전후 {HINT_WINDOW}회. 관찰 연구이므로 인과가 아니라 경향으로 해석한다.", ""]

    fairness = family_fairness(all_rows)
    lines += ["## RQ3 — family 공정성 (첫 시도 성공률)", "",
              "| family | 첫 시도 성공률 | n(세션) |", "| --- | --- | --- |"]
    for fam, sr in fairness["firstAttemptSuccess"].items():
        lines.append(f"| {fam} | {fmt_pct(sr)} | {fairness['firstAttemptN'][fam]} |")
    lines.append("")

    conf_path = out_dir / "confusion-matrix.csv"
    families = sorted({k[0] for k in fairness["confusion"]} | {k[1] for k in fairness["confusion"]})
    with conf_path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        w.writerow(["target\\recognized"] + families)
        for t in families:
            w.writerow([t] + [fairness["confusion"].get((t, rec), 0) for rec in families])
    lines += [f"혼동 행렬: `{conf_path.name}`", ""]

    m = common_metrics(all_rows)
    lines += ["## 공통 지표", "",
              f"- 전체 시도 {m['totalAttempts']}건, 전체 성공률 {fmt_pct(m['overallSuccessRate'])}",
              f"- 첫 시전까지 시간(최소 elapsedMs): {m['firstCastMs']} ms",
              f"- assist level 도달 세션 수: {m['assistReachSessions']}",
              f"- 층별 시도 수: {m['attemptsPerFloor']}", "",
              "### 실패 유형 분포", "", "| status | 건수 |", "| --- | --- |"]
    for status, n in m["statusDistribution"].most_common():
        lines.append(f"| {status} | {n} |")
    lines += ["", f"### 막힘 지점 (동일 목표 연속 {STUCK_FAILS}회 실패)", ""]
    if m["stuckPoints"]:
        lines += ["| 층 | 목표 | 발생 시도 index |", "| --- | --- | --- |"]
        lines += [f"| {f} | {g} | {i} |" for f, g, i in m["stuckPoints"]]
    else:
        lines.append("(없음)")
    lines.append("")

    report_path = out_dir / "analysis.md"
    report_path.write_text("\n".join(lines), encoding="utf-8")
    print(f"written: {report_path}")
    print(f"written: {curve_path}")
    print(f"written: {conf_path}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("inputs", nargs="*", help="세션 폴더 또는 attempts.csv 경로 (복수 가능)")
    parser.add_argument("-o", "--out", default="outputs/playtest-analysis", help="분석 결과 출력 폴더")
    parser.add_argument("--sus", help="SUS 응답 csv 채점 (participantId,q1..q10)")
    args = parser.parse_args()

    if args.sus:
        scores = score_sus(Path(args.sus))
        print("| participant | SUS |")
        print("| --- | --- |")
        for pid, s in scores:
            print(f"| {pid} | {s:.1f} |")
        if scores:
            vals = [s for _, s in scores]
            print(f"\n평균 {statistics.mean(vals):.1f} / 중앙값 {statistics.median(vals):.1f} (기준: 68 이상이면 평균 이상)")
        if not args.inputs:
            return 0

    files = find_attempt_files(args.inputs)
    if not files:
        parser.error("attempts.csv를 찾지 못했습니다. 세션 폴더 또는 파일 경로를 지정하세요.")

    sessions: dict[str, list[dict]] = {}
    for path in files:
        rows = load_attempts(path)
        if not rows:
            print(f"warning: empty attempts file: {path}", file=sys.stderr)
            continue
        sessions[rows[0]["sessionId"] or path.parent.name] = rows

    if not sessions:
        print("error: no usable attempt rows", file=sys.stderr)
        return 1

    write_report(Path(args.out), sessions)
    return 0


if __name__ == "__main__":
    sys.exit(main())
