/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { BlocksReq } from '../models/BlocksReq';
import type { ImportFailuresReq } from '../models/ImportFailuresReq';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PropertiesService {
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesPropertiesRunFromBlocks({
        requestBody,
    }: {
        requestBody: BlocksReq,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/properties/run-from-blocks',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesPropertiesRun({
        ruleId,
        trials,
        seed,
    }: {
        ruleId: string,
        trials?: number,
        seed?: number,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/{ruleId}/properties/run',
            path: {
                'ruleId': ruleId,
            },
            query: {
                'trials': trials,
                'seed': seed,
            },
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesPropertiesImportFailures({
        ruleId,
        requestBody,
    }: {
        ruleId: string,
        requestBody: ImportFailuresReq,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/{ruleId}/properties/import-failures',
            path: {
                'ruleId': ruleId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
