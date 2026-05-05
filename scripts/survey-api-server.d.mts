import type { Server } from "node:http";

export const SURVEY_SCHEMA_VERSION: string;
export const SURVEY_EXPERIMENT_GROUPS: string[];
export const MAX_BODY_BYTES: number;
export function assignExperimentGroup(seed: string): string;
export function validateSurveyResponsePayload(payload: unknown): string[];
export function validateSurveyRaffleContactPayload(payload: unknown): string[];
export function createSurveyApiServer(options?: {
  dataDir?: string;
  responsePath?: string;
  raffleContactPath?: string;
  allowedOrigins?: string[];
  now?: () => number;
}): {
  server: Server;
  sessions: Map<string, unknown>;
  responsePath: string;
  raffleContactPath: string;
};
