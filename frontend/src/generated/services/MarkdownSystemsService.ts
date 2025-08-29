/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CreateMarkdownSystemRequest } from '../models/CreateMarkdownSystemRequest';
import type { MarkdownSystemDto } from '../models/MarkdownSystemDto';
import type { UpdateMarkdownSystemRequest } from '../models/UpdateMarkdownSystemRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class MarkdownSystemsService {
    /**
     * List markdown systems
     * @returns MarkdownSystemDto OK
     * @throws ApiError
     */
    public static markdownSystemsList(): CancelablePromise<Array<MarkdownSystemDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/markdown-systems',
        });
    }
    /**
     * Create markdown system
     * @returns MarkdownSystemDto Created
     * @throws ApiError
     */
    public static markdownSystemsCreate({
        requestBody,
    }: {
        requestBody: CreateMarkdownSystemRequest,
    }): CancelablePromise<MarkdownSystemDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/markdown-systems',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                409: `Conflict`,
            },
        });
    }
    /**
     * Markdown system details
     * @returns MarkdownSystemDto OK
     * @throws ApiError
     */
    public static markdownSystemsGet({
        id,
    }: {
        id: string,
    }): CancelablePromise<MarkdownSystemDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/markdown-systems/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
    /**
     * Update markdown system
     * @returns MarkdownSystemDto OK
     * @throws ApiError
     */
    public static markdownSystemsUpdate({
        id,
        requestBody,
    }: {
        id: string,
        requestBody: UpdateMarkdownSystemRequest,
    }): CancelablePromise<MarkdownSystemDto> {
        return __request(OpenAPI, {
            method: 'PATCH',
            url: '/api/markdown-systems/{id}',
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
     * Delete markdown system
     * @returns void
     * @throws ApiError
     */
    public static markdownSystemsDelete({
        id,
    }: {
        id: string,
    }): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/markdown-systems/{id}',
            path: {
                'id': id,
            },
        });
    }
}
