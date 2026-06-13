#!/usr/bin/env python3
"""플레이테스트 attempts.csv 분석 — RQ1/RQ2/RQ3 핵심 지표 산출.

입력: ExamLogger가 남기는 attempts.csv (세션 폴더 또는 파일 경로, 복수 가능)
출력: 마크다운 요약 + 지표별 CSV (--out 디렉터리)

산출 지표
  RQ1 (무설명 학습 가능성)
    - 세션 내 학습 곡선: 시도 순서 rolling window 성공률 (기본 창 10)
    - 층별 진입 후 첫 성공까지 시도 수
  RQ2 (실패 피드백의 효과)
    - 목표(goal/family) 단위로 첫 힌트 노출 이전 vs 이후 성공률 비교
    - assist level별 직후 3시도 성공률
  RQ3 (필체 공정성)
    - base family별 첫 시도 성공률 분포

사용 예
  python scripts/playtest-attempts-analysis.py <세션폴더|attempts.csv> [...] --out outputs/playtest-analysis
  python scripts/playtest-attempts-analysis.py --self-test
"""

from __future__ import annotations

import argparse
import csv
import io
import math
import random
import sys
from collections import defaultdict
from pathlib import Path

WINDOW = 10
POST_HINT_WINDOW = 3


def parse_bool(value: str) -> bool:
    return str(value).strip().lower() == "true"


def load_attempts(path: Path) -> list[dict]:
    """attempts.csv 한 파일을 읽어 dict 리스트로 반환. 세션 폴더를 주면 내부 파일을 찾는다."""
    if path.is_dir():
        candidate = path / "attempts.csv"
        if not candidate.exists():
            raise FileNotFoundError(f"attempts.csv not found in {path}")
        path = candidate
    rows: list[dict] = []
    with open(path, encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            rows.append(
                {
                    "sessionId": row.get("sessionId", ""),
                    "floorId": row.get("floorId", ""),
                    "phase": row.get("phase", ""),
                    "family": row.get("targetFamily") or row.get("baseFamily") or "",
                    "goalId": row.get("intentGoalId", ""),
                    "status": row.get("status", ""),
                    "attemptIndex": int(float(row.get("attemptIndex") or 0)),
                    "elapsedMs": int(float(row.get("elapsedMs") or 0)),
                    "success": parse_bool(row.get("success", "")),
                    "hintShown": parse_bool(row.get("hintShown", "")),
                    "assistLevel": int(float(row.get("assistLevel") or 0)),
                }
            )
    rows.sort(key=lambda r: (r["sessionId"], r["attemptIndex"]))
    return rows


def by_session(rows: list[dict]) -> dict[str, list[dict]]:
    sessions: dict[str, list[dict]] = defaultdict(list)
    for row in rows:
        sessions[row["sessionId"]].append(row)
    return dict(sessions)


# ---------------------------------------------------------------- RQ1

def learning_curve(rows: list[dict], window: int = WINDOW) -> list[tuple[int, float]]:
    """시도 순서 기준 rolling 성공률. (창 중심 시도 번호, 성공률) 목록."""
    points = []
    successes = [1 if r["success"] else 0 for r in rows]
    for end in range(window, len(successes) + 1):
        chunk = successes[end - window : end]
        points.append((end, sum(chunk) / window))
    return points


def curve_slope(points: list[tuple[int, float]]) -> float:
    """학습 곡선의 단순 선형 기울기 (양수면 세션 내 향상)."""
    n = len(points)
    if n < 2:
        return 0.0
    xs = [p[0] for p in points]
    ys = [p[1] for p in points]
    mx, my = sum(xs) / n, sum(ys) / n
    denom = sum((x - mx) ** 2 for x in xs)
    if denom == 0:
        return 0.0
    return sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / denom


def attempts_to_first_success_per_floor(rows: list[dict]) -> dict[str, int]:
    result: dict[str, int] = {}
    counts: dict[str, int] = defaultdict(int)
    for row in rows:
        floor = row["floorId"]
        if floor in result:
            continue
        counts[floor] += 1
        if row["success"]:
            result[floor] = counts[floor]
    return result


# ---------------------------------------------------------------- RQ2

def goal_key(row: dict) -> str:
    return row["goalId"] or f"{row['floorId']}:{row['family']}"


def hint_before_after(rows: list[dict]) -> dict[str, dict]:
    """목표 단위로 첫 힌트 노출 이전/이후 성공률을 비교한다.

    힌트 노출 = hintShown 또는 assistLevel >= 1 인 시도.
    이전/이후 양쪽에 시도가 있는 목표만 비교에 포함한다.
    """
    grouped: dict[str, list[dict]] = defaultdict(list)
    for row in rows:
        grouped[goal_key(row)].append(row)

    comparisons = {}
    for key, attempts in grouped.items():
        hint_at = next(
            (i for i, a in enumerate(attempts) if a["hintShown"] or a["assistLevel"] >= 1),
            None,
        )
        if hint_at is None or hint_at == 0 or hint_at == len(attempts) - 1:
            continue
        before = attempts[:hint_at]
        after = attempts[hint_at + 1 :]
        if not before or not after:
            continue
        comparisons[key] = {
            "before_n": len(before),
            "before_rate": sum(a["success"] for a in before) / len(before),
            "after_n": len(after),
            "after_rate": sum(a["success"] for a in after) / len(after),
        }
    return comparisons


def post_assist_success(rows: list[dict], window: int = POST_HINT_WINDOW) -> dict[int, tuple[int, float]]:
    """assist level별로, 해당 레벨 힌트가 보인 시도 직후 window회의 성공률."""
    buckets: dict[int, list[int]] = defaultdict(list)
    for i, row in enumerate(rows):
        if row["assistLevel"] >= 1 and (row["hintShown"] or not row["success"]):
            following = rows[i + 1 : i + 1 + window]
            for a in following:
                buckets[row["assistLevel"]].append(1 if a["success"] else 0)
    return {
        level: (len(vals), sum(vals) / len(vals))
        for level, vals in sorted(buckets.items())
        if vals
    }


# ---------------------------------------------------------------- RQ3

def family_first_attempt_rate(rows: list[dict]) -> dict[str, tuple[int, float]]:
    """family별 '그 family에 대한 세션 내 첫 시도' 성공률. (표본 수, 성공률)"""
    first_attempts: dict[tuple[str, str], dict] = {}
    for row in rows:
        if not row["family"]:
            continue
        key = (row["sessionId"], row["family"])
        if key not in first_attempts:
            first_attempts[key] = row
    rates: dict[str, list[int]] = defaultdict(list)
    for (_, family), row in first_attempts.items():
        rates[family].append(1 if row["success"] else 0)
    return {
        family: (len(vals), sum(vals) / len(vals))
        for family, vals in sorted(rates.items())
    }


# ---------------------------------------------------------------- 리포트

def write_report(all_rows: list[dict], out_dir: Path) -> str:
    out_dir.mkdir(parents=True, exist_ok=True)
    sessions = by_session(all_rows)
    md = io.StringIO()
    md.write("# Playtest attempts 분석\n\n")
    md.write(f"세션 {len(sessions)}개, 시도 {len(all_rows)}건\n\n")

    md.write("## RQ1 — 세션 내 학습 곡선\n\n")
    md.write(f"| 세션 | 시도 수 | 전체 성공률 | rolling({WINDOW}) 기울기 | 해석 |\n|---|---|---|---|---|\n")
    curve_csv = [("sessionId", "attemptEnd", "rollingSuccessRate")]
    for sid, rows in sessions.items():
        rate = sum(r["success"] for r in rows) / len(rows)
        points = learning_curve(rows)
        slope = curve_slope(points)
        for end, value in points:
            curve_csv.append((sid, end, f"{value:.3f}"))
        verdict = "세션 내 향상" if slope > 0 else "향상 없음/감소"
        md.write(f"| {sid} | {len(rows)} | {rate:.2f} | {slope:+.4f} | {verdict} |\n")
    with open(out_dir / "learning-curve.csv", "w", encoding="utf-8", newline="") as f:
        csv.writer(f).writerows(curve_csv)

    md.write("\n층별 첫 성공까지 시도 수:\n\n| 세션 | " )
    floors = sorted({r["floorId"] for r in all_rows if r["floorId"]})
    md.write(" | ".join(floors) + " |\n|" + "---|" * (len(floors) + 1) + "\n")
    for sid, rows in sessions.items():
        per_floor = attempts_to_first_success_per_floor(rows)
        md.write(f"| {sid} | " + " | ".join(str(per_floor.get(f, "-")) for f in floors) + " |\n")

    md.write("\n## RQ2 — 힌트 노출 전후 성공률 (목표 단위)\n\n")
    pooled_before, pooled_after = [], []
    comp_csv = [("sessionId", "goal", "beforeN", "beforeRate", "afterN", "afterRate")]
    for sid, rows in sessions.items():
        for key, c in hint_before_after(rows).items():
            comp_csv.append((sid, key, c["before_n"], f"{c['before_rate']:.3f}", c["after_n"], f"{c['after_rate']:.3f}"))
            pooled_before.append(c["before_rate"])
            pooled_after.append(c["after_rate"])
    with open(out_dir / "hint-before-after.csv", "w", encoding="utf-8", newline="") as f:
        csv.writer(f).writerows(comp_csv)
    if pooled_before:
        mb = sum(pooled_before) / len(pooled_before)
        ma = sum(pooled_after) / len(pooled_after)
        md.write(f"비교 가능한 목표 {len(pooled_before)}개 — 힌트 이전 평균 성공률 **{mb:.2f}** → 이후 **{ma:.2f}** (변화 {ma - mb:+.2f})\n\n")
    else:
        md.write("비교 가능한 목표 없음 (힌트 전후 양쪽에 시도가 있는 목표가 없음)\n\n")

    md.write(f"assist level별 직후 {POST_HINT_WINDOW}시도 성공률:\n\n| level | 표본 | 성공률 |\n|---|---|---|\n")
    for level, (n, rate) in post_assist_success(all_rows).items():
        md.write(f"| {level} | {n} | {rate:.2f} |\n")

    md.write("\n## RQ3 — family별 첫 시도 성공률\n\n| family | 표본(세션 수) | 성공률 |\n|---|---|---|\n")
    fam_csv = [("family", "n", "firstAttemptSuccessRate")]
    rates = family_first_attempt_rate(all_rows)
    for family, (n, rate) in rates.items():
        md.write(f"| {family} | {n} | {rate:.2f} |\n")
        fam_csv.append((family, n, f"{rate:.3f}"))
    with open(out_dir / "family-first-attempt.csv", "w", encoding="utf-8", newline="") as f:
        csv.writer(f).writerows(fam_csv)
    if rates:
        values = [rate for _, rate in rates.values()]
        spread = max(values) - min(values)
        md.write(f"\nfamily 간 첫 시도 성공률 편차(최대-최소): **{spread:.2f}** — 0.3 이상이면 해당 family 인식 보정 검토.\n")

    report = md.getvalue()
    (out_dir / "report.md").write_text(report, encoding="utf-8")
    return report


# ---------------------------------------------------------------- self test

def make_synthetic(out_path: Path, seed: int = 7) -> None:
    """알려진 성질의 합성 데이터: 학습할수록 성공률 상승, 힌트 후 성공률 상승, water만 첫 시도 성공률 낮음."""
    rng = random.Random(seed)
    header = "sessionId,trialId,targetFamily,recognizedFamily,phase,baseFamily,overlayStack,sealId,floorId,targetObject,worldEffect,customShapeId,customShapeLabel,customShapeToken,mappedFamily,customEventId,customEventLabel,customEventKind,customEventRole,customEventUsesDirection,customEventOperatorOnly,customEventBlocks,customEventBlocked,customEventBlockedBy,customEventOriginX,customEventOriginY,customEventDirectionX,customEventDirectionY,status,confidence,customScore,defaultSimilarityScore,intentFamily,intentGoalId,intentSource,intentStrength,intentSimilarityScore,intentWeakConsiderationApplied,intentTutorialCaptureCount,intentStrongConsiderationEnabled,preIntentFamily,preIntentConfidence,intentStrongConsiderationApplied,intentScoreLift,closure,smoothness,tempo,stability,rotationBias,worldX,worldY,bufferStrokeCount,attemptIndex,elapsedMs,feedbackViewed,success,hintShown,assistLevel,assisted"
    lines = [header]
    families = ["fire", "water", "wind", "earth", "life"]
    for s in range(3):
        sid = f"synthetic-{s}"
        idx = 0
        elapsed = 0
        for goal_n in range(10):
            family = families[goal_n % 5]
            goal = f"goal-{goal_n}"
            floor = f"floor-{goal_n // 2 + 1}"
            fails_before_hint = rng.randint(2, 3)
            for k in range(fails_before_hint + 4):
                idx += 1
                elapsed += rng.randint(2000, 9000)
                base_skill = min(0.15 + idx * 0.015, 0.85)  # 학습 곡선
                hint_seen = k >= fails_before_hint
                p = base_skill + (0.35 if hint_seen else 0.0)
                if family == "water" and k == 0:
                    p = 0.05  # water 첫 시도만 불리
                success = rng.random() < p
                hint_flag = "true" if (k == fails_before_hint) else "false"
                assist = 1 if hint_seen else 0
                lines.append(
                    f'"{sid}","t{idx}","{family}","{family if success else ""}","base","{family}","","","{floor}","","","","","","","","","","",false,false,false,false,"",0,0,0,0,"{ "recognized" if success else "invalid" }",0.5,0,0,"","{goal}","",0,0,false,0,false,"",0,false,0,0.5,0.5,0.5,0.5,0,0,0,1,{idx},{elapsed},true,{"true" if success else "false"},{hint_flag},{assist},false'
                )
                if success and k >= 1:
                    break
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def self_test(tmp_dir: Path) -> int:
    tmp_dir.mkdir(parents=True, exist_ok=True)
    csv_path = tmp_dir / "attempts.csv"
    make_synthetic(csv_path)
    rows = load_attempts(csv_path)
    sessions = by_session(rows)
    failures = []

    slopes = [curve_slope(learning_curve(r)) for r in sessions.values()]
    if not all(s > 0 for s in slopes):
        failures.append(f"학습 곡선 기울기가 양수가 아님: {slopes}")

    pooled = []
    for r in sessions.values():
        for c in hint_before_after(r).values():
            pooled.append(c["after_rate"] - c["before_rate"])
    if not pooled or sum(pooled) / len(pooled) <= 0:
        failures.append(f"힌트 전후 개선이 검출되지 않음 (n={len(pooled)})")

    rates = family_first_attempt_rate(rows)
    if rates and not (rates.get("water", (0, 1.0))[1] <= min(v for k, (_, v) in rates.items() if k != "water")):
        failures.append(f"water 첫 시도 불리 조건이 재현되지 않음: {rates}")

    write_report(rows, tmp_dir / "report")

    if failures:
        print("SELF-TEST FAILED")
        for f in failures:
            print(" -", f)
        return 1
    print(f"SELF-TEST OK — 세션 {len(sessions)}, 시도 {len(rows)}, 힌트 비교쌍 {len(pooled)}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("inputs", nargs="*", help="세션 폴더 또는 attempts.csv 경로 (복수 가능)")
    parser.add_argument("--out", default="outputs/playtest-analysis", help="결과 출력 폴더")
    parser.add_argument("--self-test", action="store_true", help="합성 데이터로 자가 검증")
    args = parser.parse_args()

    if args.self_test:
        return self_test(Path("outputs/playtest-analysis-selftest"))

    if not args.inputs:
        parser.error("입력 경로가 없습니다 (또는 --self-test 사용)")

    all_rows: list[dict] = []
    for raw in args.inputs:
        all_rows.extend(load_attempts(Path(raw)))
    if not all_rows:
        print("시도 데이터가 없습니다", file=sys.stderr)
        return 1
    report = write_report(all_rows, Path(args.out))
    print(report)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
