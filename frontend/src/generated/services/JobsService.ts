/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { JobDetailResponse } from '../models/JobDetailResponse';
import type { PagedJobsResponse } from '../models/PagedJobsResponse';
import type { SubmitAcceptedResponse } from '../models/SubmitAcceptedResponse';
import type { Void } from '../models/Void';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class JobsService {
    /**
     * @returns PagedJobsResponse OK
     * @throws ApiError
     */
    public static jobsList({
        page,
        pageSize,
    }: {
        page?: number,
        pageSize?: number,
    }): CancelablePromise<PagedJobsResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/jobs',
            query: {
                'page': page,
                'pageSize': pageSize,
            },
            errors: {
                400: `Bad Request`,
            },
        });
    }
    /**
     * @returns any OK
     * @returns SubmitAcceptedResponse Accepted
     * @throws ApiError
     */
    public static jobsCreate({
        idempotencyKey,
    }: {
        /**
         * Optional idempotency key
         */
        idempotencyKey?: string,
    }): CancelablePromise<any | SubmitAcceptedResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/jobs',
            headers: {
                'Idempotency-Key': idempotencyKey,
            },
            errors: {
                400: `Bad Request`,
                413: `Payload Too Large`,
                429: `Too Many Requests`,
                507: `Insufficient Storage`,
            },
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static jobsFile({
        id,
        file,
    }: {
        id: string,
        file: string,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/jobs/{id}/files/{file}',
            path: {
                'id': id,
                'file': file,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
    /**
     * @returns JobDetailResponse OK
     * @throws ApiError
     */
    public static jobsGetById({
        id,
    }: {
        id: string,
    }): CancelablePromise<JobDetailResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/jobs/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
    /**
     * @returns Void Accepted
     * @throws ApiError
     */
    public static jobsDelete({
        id,
    }: {
        id: string,
    }): CancelablePromise<Void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/v1/jobs/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
                409: `Conflict`,
            },
        });
    }
}
