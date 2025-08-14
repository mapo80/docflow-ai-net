/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { HealthResponse } from '../models/HealthResponse';
import type { Job } from '../models/Job';
import type { PagedListJob } from '../models/PagedListJob';
import type { ModelInfo } from '../models/ModelInfo';
import type { ModelStatus } from '../models/ModelStatus';
import type { ModelSwitchRequest } from '../models/ModelSwitchRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class DefaultService {
    /**
     * @returns HealthResponse Health
     * @throws ApiError
     */
    public static getHealth(): CancelablePromise<HealthResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/health/ready',
        });
    }
    /**
     * @returns HealthResponse Health
     * @throws ApiError
     */
    public static getHealthLive(): CancelablePromise<HealthResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/health/live',
        });
    }
    /**
     * @returns PagedListJob Paged Jobs
     * @throws ApiError
     */
    public static getJobs({
        page = 1,
        pageSize = 10,
        status,
    }: {
        page?: number,
        pageSize?: number,
        status?: string | null,
    }): CancelablePromise<PagedListJob> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/jobs',
            query: {
                'page': page,
                'pageSize': pageSize,
                'status': status,
            },
            errors: {
                429: `Queue full`,
            },
        });
    }
    /**
     * @returns Job Job created
     * @throws ApiError
     */
    public static createJob({
        mode,
    }: {
        mode?: string,
    }): CancelablePromise<Job> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/jobs',
            query: {
                'mode': mode,
            },
        });
    }
    /**
     * @returns Job Job
     * @throws ApiError
     */
    public static getJob({
        id,
    }: {
        id: string,
    }): CancelablePromise<Job> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/jobs/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not found`,
            },
        });
    }
    /**
     * @returns any Accepted
     * @throws ApiError
     */
    public static cancelJob({
        id,
    }: {
        id: string,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/jobs/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not found`,
                409: `Conflict`,
            },
        });
    }
    /**
     * @returns any Accepted
     * @throws ApiError
     */
    public static switchModel({
        requestBody,
    }: {
        requestBody: ModelSwitchRequest,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/model/switch',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns ModelStatus Status
     * @throws ApiError
     */
    public static getModelStatus(): CancelablePromise<ModelStatus> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/model/status',
        });
    }
    /**
     * @returns ModelInfo Model info
     * @throws ApiError
     */
    public static getModelInfo(): CancelablePromise<ModelInfo> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/model/info',
        });
    }
    /**
     * @returns any Accepted
     * @throws ApiError
     */
    public static cancelModelSwitch(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/model/switch',
        });
    }
}
