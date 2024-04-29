import type { ConfigDefinition } from "./ConfigDefinition";
import { getUserManager } from "./userManager";
import { buildUrl } from "./utility";

export interface RegisterResult {
    success: boolean;
    email: string;
    name: string;
    message: string;
}

export interface SignInResult {
    success: boolean;
    id: number;
    email: string;
    name: string;
    token?: string;
    message: string;
}

export interface ProfileQueryOptions {
    q?: string;
    user?: string;
    page?: number;
}

export interface Profile {
    id: number;
    name: string;
    description: string;
    userId: number;
    userName: string;
    starCount: number;
    seedCount: number;
    isStarred: boolean;
    configId: number;
    config?: Config;
}

export interface ProfileQueryResult {
    page: number;
    pageCount: number;
    pageResults: Profile[];
}

export interface ConfigOption {
    id: string;
    label: string;
    description?: string;
    type: string;
    size?: number;
    min?: number;
    max?: number;
    step?: number;
    options?: string[];
    default?: boolean | number | string;
}

export interface ConfigGroup {
    label: string;
    warning?: string;
    items: ConfigOption[];
}

export interface ConfigPage {
    label: string;
    groups: ConfigGroup[];
}

export interface ConfigDefinition {
    pages: ConfigPage[];
}

export type Config = { [key: string]: any };

export class BioRandApi {
    private urlBase = "https://api-re4r.biorand.net";
    private authToken?: string;

    constructor(urlBase?: string) {
        if (urlBase) {
            this.urlBase = urlBase;
        }
    }

    setAuthToken(authToken: string) {
        this.authToken = authToken;
    }

    async register(email: string, name: string) {
        return await this.post<RegisterResult>("auth/register", {
            email,
            name
        });
    }

    async signIn(email: string, code?: string) {
        return await this.post<SignInResult>("auth/signin", {
            email,
            code
        });
    }

    async getProfiles() {
        return await this.get<Profile[]>("profile");
    }

    async searchProfiles(query: ProfileQueryOptions) {
        return await this.get<ProfileQueryResult>("profile/search", query);
    }

    async setProfileStar(profileId: number, value: boolean) {
        return await this.fetch(value ? 'POST' : 'DELETE', `profile/${profileId}/star`);
    }

    async updateTempConfig(profileId: number, config: Config) {
        return await this.put(`profile/temp`, {
            profileId,
            config
        });
    }

    async getConfigDefinition() {
        return await this.get<ConfigDefinition>("profile/definition");
    }

    private async get<T>(query: string, body?: any) {
        return this.fetch<T>('GET', query, body);
    }

    private async post<T>(query: string, body?: any) {
        return this.fetch<T>('POST', query, body);
    }

    private async put<T>(query: string, body?: any) {
        return this.fetch<T>('PUT', query, body);
    }

    private async fetch<T>(method: string, query: string, body?: any): Promise<T> {
        const isGet = method === 'GET';
        const headers: any = {};
        const fetchOptions: any = { method, headers }
        if (this.authToken)
            headers['Authorization'] = `Bearer ${this.authToken}`;
        if (!isGet)
            headers['Content-Type'] = 'application/json';
        if (!isGet)
            fetchOptions.body = JSON.stringify(body);
        const baseUrl = this.getUrl(query);
        const url = isGet ? buildUrl(baseUrl, body) : baseUrl;
        const req = await fetch(url, fetchOptions);
        return await req.json();
    }

    private getUrl(query: string) {
        return `${this.urlBase}/${query}`;
    }
}

export function getApi() {
    const hostName = document.location.hostname;
    let apiUrl = 'https://api-re4r.biorand.net';
    if (hostName === 'localhost') {
        apiUrl = 'http://localhost:10285';
    } else if (hostName.indexOf('beta') !== -1) {
        apiUrl = 'https://beta-api-re4r.biorand.net';
    }

    const api = new BioRandApi(apiUrl);
    const userManager = getUserManager();
    if (userManager.isSignedIn()) {
        api.setAuthToken(userManager.info!.token);
    }
    return api;
}
