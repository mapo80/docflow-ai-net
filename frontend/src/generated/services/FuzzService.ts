/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { FuzzImportReq } from '../models/FuzzImportReq';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class FuzzService {
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesFuzzPreview({
        ruleId,
        maxPerField,
    }: {
        ruleId: string,
        maxPerField?: number,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/{ruleId}/fuzz/preview',
            path: {
                'ruleId': ruleId,
            },
            query: {
                'maxPerField': maxPerField,
            },
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesFuzzImport({
        ruleId,
        requestBody,
    }: {
        ruleId: string,
        requestBody: FuzzImportReq,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/{ruleId}/fuzz/import',
            path: {
                'ruleId': ruleId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
