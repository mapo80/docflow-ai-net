/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CompileReq } from '../models/CompileReq';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class RuleBuilderService {
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulebuilderValidate({
        requestBody,
    }: {
        requestBody: CompileReq,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rulebuilder/validate',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulebuilderCompile({
        requestBody,
    }: {
        requestBody: CompileReq,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rulebuilder/compile',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
