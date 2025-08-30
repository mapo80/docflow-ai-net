/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CreateModelRequest } from '../models/CreateModelRequest';
import type { ModelDto } from '../models/ModelDto';
import type { UpdateModelRequest } from '../models/UpdateModelRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ModelsService {
    /**
     * List models
     * @returns ModelDto OK
     * @throws ApiError
     */
    public static modelsList(): CancelablePromise<Array<ModelDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/models',
        });
    }
    /**
     * Create model
     * @returns ModelDto Created
     * @throws ApiError
     */
    public static modelsCreate({
        requestBody,
    }: {
        requestBody: CreateModelRequest,
    }): CancelablePromise<ModelDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/models',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                409: `Conflict`,
            },
        });
    }
    /**
     * Model details
     * @returns ModelDto OK
     * @throws ApiError
     */
    public static modelsGet({
        id,
    }: {
        id: string,
    }): CancelablePromise<ModelDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/models/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
    /**
     * Update model
     * @returns ModelDto OK
     * @throws ApiError
     */
    public static modelsUpdate({
        id,
        requestBody,
    }: {
        id: string,
        requestBody: UpdateModelRequest,
    }): CancelablePromise<ModelDto> {
        return __request(OpenAPI, {
            method: 'PATCH',
            url: '/api/v1/models/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
            },
        });
    }
    /**
     * Delete model
     * @returns void
     * @throws ApiError
     */
    public static modelsDelete({
        id,
    }: {
        id: string,
    }): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/v1/models/{id}',
            path: {
                'id': id,
            },
        });
    }
    /**
     * Start model download
     * @returns void
     * @throws ApiError
     */
    public static modelsStartDownload({
        id,
    }: {
        id: string,
    }): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/models/{id}/download',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
    /**
     * Get download log
     * @returns string OK
     * @throws ApiError
     */
    public static modelsDownloadLog({
        id,
    }: {
        id: string,
    }): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/models/{id}/download-log',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
}
