/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { MarkdownResult } from '../models/MarkdownResult';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class MarkdownService {
    /**
     * @returns MarkdownResult OK
     * @throws ApiError
     */
    public static markdownConvert({
        language,
        engine,
        formData,
    }: {
        language: string,
        engine: string,
        formData?: {
            file?: Blob;
        },
    }): CancelablePromise<MarkdownResult> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/v1/markdown',
            query: {
                'language': language,
                'engine': engine,
            },
            formData: formData,
            mediaType: 'multipart/form-data',
            errors: {
                400: `Bad Request`,
                422: `Unprocessable Entity`,
                500: `Internal Server Error`,
            },
        });
    }
}
