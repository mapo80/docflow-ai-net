/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
export type Job = {
    id: string;
    status: string;
    derivedStatus?: string | null;
    progress?: number | null;
    attempts?: number | null;
    createdAt: string;
    updatedAt: string;
    durationMs?: number | null;
    paths?: {
        manifest?: string | null;
        prompt?: string | null;
        fields?: string | null;
        output?: string | null;
        error?: string | null;
    } | null;
};

