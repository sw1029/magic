import type { RecognitionStatus } from "../recognizer/types";
import { dashboardFamilyName, dashboardQualityName, dashboardStatusLabel } from "./dashboard-copy";
import type { DashboardBatchSummary } from "./dashboard-batch";

export interface DashboardBarDatum {
  id: string;
  label: string;
  value: number;
  ratio: number;
}

export interface DashboardHeatCell {
  expected: string;
  actual: string;
  count: number;
  intensity: number;
}

export interface DashboardScatterPoint {
  x: number;
  y: number;
  label: string;
  status: RecognitionStatus;
}

export interface DashboardPlotModel {
  statusBars: DashboardBarDatum[];
  familyBars: DashboardBarDatum[];
  qualityBars: DashboardBarDatum[];
  heatmap: DashboardHeatCell[];
  scorePoints: DashboardScatterPoint[];
}

export function buildDashboardPlotModel(summary: DashboardBatchSummary): DashboardPlotModel {
  return {
    statusBars: buildStatusBars(summary),
    familyBars: buildFamilyBars(summary),
    qualityBars: buildQualityBars(summary),
    heatmap: buildHeatmap(summary),
    scorePoints: summary.samples.slice(0, 240).map((sample) => ({
      x: sample.closure,
      y: sample.topScore,
      label: `${dashboardStatusLabel(sample.status)} ${(sample.topScore * 100).toFixed(0)}점`,
      status: sample.status
    }))
  };
}

function buildStatusBars(summary: DashboardBatchSummary): DashboardBarDatum[] {
  const order: RecognitionStatus[] = ["recognized", "ambiguous", "incomplete", "invalid"];
  return order.map((status) => ({
    id: status,
    label: dashboardStatusLabel(status),
    value: summary.statusCounts[status] ?? 0,
    ratio: ratio(summary.statusCounts[status] ?? 0, summary.total)
  }));
}

function buildFamilyBars(summary: DashboardBatchSummary): DashboardBarDatum[] {
  return Object.entries(summary.familyCounts)
    .sort((left, right) => right[1] - left[1])
    .map(([family, value]) => ({
      id: family,
      label: family === "none" ? "아직 없음" : dashboardFamilyName(family),
      value,
      ratio: ratio(value, summary.total)
    }));
}

function buildQualityBars(summary: DashboardBatchSummary): DashboardBarDatum[] {
  return Object.entries(summary.qualityAverages).map(([key, value]) => ({
    id: key,
    label: dashboardQualityName(key),
    value,
    ratio: Math.max(0, Math.min(1, value))
  }));
}

function buildHeatmap(summary: DashboardBatchSummary): DashboardHeatCell[] {
  const max = Math.max(...summary.confusionRows.map((row) => row.count), 1);
  return summary.confusionRows.map((row) => ({
    ...row,
    intensity: ratio(row.count, max)
  }));
}

function ratio(value: number, total: number): number {
  return total > 0 ? Number((value / total).toFixed(4)) : 0;
}
