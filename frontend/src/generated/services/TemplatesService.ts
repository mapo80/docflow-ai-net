/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CreateTemplateRequest } from '../models/CreateTemplateRequest';
import type { TemplateDto } from '../models/TemplateDto';
import type { TemplateSummaryPagedResult } from '../models/TemplateSummaryPagedResult';
import type { UpdateTemplateRequest } from '../models/UpdateTemplateRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class TemplatesService {
    /**
     * List templates
     * @returns TemplateSummaryPagedResult OK
     * @throws ApiError
     */
    public static templatesList({
        q,
        page,
        pageSize,
        sort,
    }: {
        q?: string,
        page?: number,
        pageSize?: number,
        sort?: string,
    }): CancelablePromise<TemplateSummaryPagedResult> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/templates',
            query: {
                'q': q,
                'page': page,
                'pageSize': pageSize,
                'sort': sort,
            },
        });
    }
    /**
     * Create template
     * @returns TemplateDto Created
     * @throws ApiError
     */
    public static templatesCreate({
        requestBody,
    }: {
        requestBody: CreateTemplateRequest,
    }): CancelablePromise<TemplateDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/templates',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                409: `Conflict`,
                422: `Unprocessable Content`,
            },
        });
    }
    /**
     * Template details
     * @returns TemplateDto OK
     * @throws ApiError
     */
    public static templatesGet({
        id,
    }: {
        id: string,
    }): CancelablePromise<TemplateDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/templates/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
    /**
     * Update template
     * @returns TemplateDto OK
     * @throws ApiError
     */
    public static templatesUpdate({
        id,
        requestBody,
    }: {
        id: string,
        requestBody: UpdateTemplateRequest,
    }): CancelablePromise<TemplateDto> {
        return __request(OpenAPI, {
            method: 'PATCH',
            url: '/api/templates/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                404: `Not Found`,
                409: `Conflict`,
                422: `Unprocessable Content`,
            },
        });
    }
    /**
     * Delete template
     * @returns void
     * @throws ApiError
     */
    public static templatesDelete({
        id,
    }: {
        id: string,
    }): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/templates/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
}
