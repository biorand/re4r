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
    email: string;
    token?: string;
    message: string;
}

export interface ProfileQueryOptions {
    q?: string;
    user?: string;
    page?: number;
}

export interface ProfileQueryResult {
    page: number;
    pageCount: number;
    pageResults: {
        id: number;
        name: string;
        description: string;
        userName: string;
        starCount: number;
        seedCount: number;
        isStarred: boolean;
    }[];
}

class BioRandApi {
    private urlBase = "http://localhost:10285/api"
    private authToken?: string;

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

    async searchProfiles(query: ProfileQueryOptions) {
        return await this.get<ProfileQueryResult>("profile/search", query);
    }

    async setProfileStar(profileId: number, value: boolean) {
        return await this.fetch(value ? 'POST' : 'DELETE', `profile/${profileId}/star`);
    }

    private async get<T>(query: string, body?: any) {
        return this.fetch<T>('GET', query, body);
    }

    private async post<T>(query: string, body?: any) {
        return this.fetch<T>('POST', query, body);
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
    const api = new BioRandApi();
    const userManager = getUserManager();
    if (userManager.isSignedIn()) {
        api.setAuthToken(userManager.info!.token);
    }
    return api;
}
