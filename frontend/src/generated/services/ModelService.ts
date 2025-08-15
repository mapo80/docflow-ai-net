/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DownloadModelRequest } from '../models/DownloadModelRequest';
import type { ModelDownloadStatus } from '../models/ModelDownloadStatus';
import type { ModelInfo } from '../models/ModelInfo';
import type { SwitchModelRequest } from '../models/SwitchModelRequest';
import type { Void } from '../models/Void';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ModelService {
    /**
     * Current model
     * Gets current model info
     * @returns ModelInfo OK
     * @throws ApiError
     */
    public static modelCurrent(): CancelablePromise<ModelInfo> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/model',
        });
    }
    /**
     * Available models
     * Lists available GGUF models
     * @returns string OK
     * @throws ApiError
     */
    public static modelAvailable(): CancelablePromise<Array<string>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/model/available',
        });
    }
    /**
     * Download model
     * Starts downloading the specified model
     * @returns Void OK
     * @throws ApiError
     */
    public static modelDownload({
        requestBody,
    }: {
        requestBody?: DownloadModelRequest,
    }): CancelablePromise<Void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/model/download',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
                409: `Conflict`,
            },
        });
    }
    /**
     * Switch model
     * Activates a downloaded model
     * @returns any OK
     * @throws ApiError
     */
    public static modelSwitch({
        requestBody,
    }: {
        requestBody: SwitchModelRequest,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/model/switch',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
            },
        });
    }
    /**
     * Model switch status
     * Gets progress for the current model download
     * @returns ModelDownloadStatus OK
     * @throws ApiError
     */
    public static modelStatus(): CancelablePromise<ModelDownloadStatus> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/model/status',
        });
    }
}
