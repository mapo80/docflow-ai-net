/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { MetricsInfo } from './MetricsInfo';
import type { PathInfo } from './PathInfo';
/**
 * Detailed job information.
 */
export type JobDetailResponse = {
    id?: string;
    status?: string | null;
    derivedStatus?: string | null;
    progress?: number;
    attempts?: number;
    createdAt?: string;
    updatedAt?: string;
    metrics?: MetricsInfo;
    paths?: PathInfo;
    errorMessage?: string | null;
    model?: string | null;
    templateToken?: string | null;
    language?: string | null;
    engine?: string | null;
};

