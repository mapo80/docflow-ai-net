/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ModelDownloadStatus } from '../models/ModelDownloadStatus';
import type { SwitchModelRequest } from '../models/SwitchModelRequest';
import type { Void } from '../models/Void';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ModelService {
    /**
     * Switch model
     * Starts downloading and activating the specified model
     * @returns Void OK
     * @throws ApiError
     */
    public static modelSwitch({
        requestBody,
    }: {
        requestBody?: SwitchModelRequest,
    }): CancelablePromise<Void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/model/switch',
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
     * Model switch status
     * Gets progress for the current model switch
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
