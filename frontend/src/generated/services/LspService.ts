/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { LspSyncReq } from '../models/LspSyncReq';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class LspService {
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiV1LspWorkspaceSync({
        workspaceId,
        requestBody,
    }: {
        workspaceId: string,
        requestBody: LspSyncReq,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/lsp/workspace/sync',
            query: {
                'workspaceId': workspaceId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static getApiV1LspCsharp({
        workspaceId,
    }: {
        workspaceId?: string,
    }): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/v1/lsp/csharp',
            query: {
                'workspaceId': workspaceId,
            },
        });
    }
}
