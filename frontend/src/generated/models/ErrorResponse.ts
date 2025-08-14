/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
/**
 * Uniform error payload returned by the API.
 */
export type ErrorResponse = {
    /**
     * Short machine readable error code.
     */
    error?: string | null;
    /**
     * Human readable message.
     */
    message?: string | null;
    /**
     * Retry hint in seconds when applicable.
     */
    retryAfterSeconds?: number | null;
};

