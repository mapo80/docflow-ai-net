/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ImportReq } from '../models/ImportReq';
import type { SuggestReq } from '../models/SuggestReq';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AiTestsService {
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1AiTestsSuggest({
        ruleId,
        requestBody,
    }: {
        ruleId: string,
        requestBody: SuggestReq,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/ai/tests/suggest',
            query: {
                'ruleId': ruleId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1AiTestsImport({
        ruleId,
        requestBody,
    }: {
        ruleId: string,
        requestBody: ImportReq,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/ai/tests/import',
            query: {
                'ruleId': ruleId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
