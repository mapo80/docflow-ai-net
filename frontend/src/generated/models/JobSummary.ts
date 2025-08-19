/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
/**
 * Summary information for a job.
 */
export type JobSummary = {
    id?: string;
    status?: string | null;
    derivedStatus?: string | null;
    progress?: number;
    attempts?: number;
    createdAt?: string;
    updatedAt?: string;
    model?: string | null;
    templateToken?: string | null;
};

