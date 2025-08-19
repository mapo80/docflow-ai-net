/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { JobSummary } from './JobSummary';
/**
 * Paged list of jobs.
 */
export type PagedJobsResponse = {
    page?: number;
    pageSize?: number;
    total?: number;
    items?: Array<JobSummary> | null;
};

