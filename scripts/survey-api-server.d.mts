import type { Server } from "node:http";

export const SURVEY_SCHEMA_VERSION: string;
export const SURVEY_EXPERIMENT_GROUPS: string[];
export const MAX_BODY_BYTES: number;
export function assignExperimentGroup(seed: string): string;
export function assignBalancedExperimentGroup(groupCounts: Record<string, number> | Map<string, number>, seed: string): string;
export function countExperimentGroupsFromResponseLog(text: string): Record<string, number>;
export function validateSurveyResponsePayload(payload: unknown): string[];
export function validateSurveyRaffleContactPayload(payload: unknown): string[];
export function createSurveyApiServer(options?: {
  dataDir?: string;
  responsePath?: string;
  raffleContactPath?: string;
  initialExperimentGroupCounts?: Record<string, number> | Map<string, number>;
  allowedOrigins?: string[];
  now?: () => number;
}): {
  server: Server;
  sessions: Map<string, unknown>;
  responsePath: string;
  raffleContactPath: string;
  experimentGroupCounts: {
    completed: Map<string, number>;
    active: Map<string, number>;
  };
};
