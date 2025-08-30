/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CloneTestRequest } from '../models/CloneTestRequest';
import type { RunSelectedRequest } from '../models/RunSelectedRequest';
import type { TestUpsert } from '../models/TestUpsert';
import type { UpdateMeta } from '../models/UpdateMeta';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class RuleTestsService {
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static getApiV1RulesTests({
        ruleId,
        search,
        suite,
        tag,
        sortBy,
        sortDir,
        page,
        pageSize,
    }: {
        ruleId: string,
        search?: string,
        suite?: string,
        tag?: string,
        sortBy?: string,
        sortDir?: string,
        page?: number,
        pageSize?: number,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/rules/{ruleId}/tests',
            path: {
                'ruleId': ruleId,
            },
            query: {
                'search': search,
                'suite': suite,
                'tag': tag,
                'sortBy': sortBy,
                'sortDir': sortDir,
                'page': page,
                'pageSize': pageSize,
            },
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesTests({
        ruleId,
        requestBody,
    }: {
        ruleId: string,
        requestBody: TestUpsert,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/{ruleId}/tests',
            path: {
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
    public static putApiV1RulesTests({
        ruleId,
        testId,
        requestBody,
    }: {
        ruleId: string,
        testId: string,
        requestBody: UpdateMeta,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/v1/rules/{ruleId}/tests/{testId}',
            path: {
                'ruleId': ruleId,
                'testId': testId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesTestsClone({
        ruleId,
        testId,
        requestBody,
    }: {
        ruleId: string,
        testId: string,
        requestBody: CloneTestRequest,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/{ruleId}/tests/{testId}/clone',
            path: {
                'ruleId': ruleId,
                'testId': testId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesTestsRun({
        ruleId,
    }: {
        ruleId: string,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/{ruleId}/tests/run',
            path: {
                'ruleId': ruleId,
            },
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1RulesTestsRunSelected({
        ruleId,
        requestBody,
    }: {
        ruleId: string,
        requestBody: RunSelectedRequest,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/rules/{ruleId}/tests/run-selected',
            path: {
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
    public static getApiV1RulesTestsCoverage({
        ruleId,
    }: {
        ruleId: string,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/rules/{ruleId}/tests/coverage',
            path: {
                'ruleId': ruleId,
            },
        });
    }
}
