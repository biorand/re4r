import { LocalStorageKeys, getLocalStorageManager } from "./localStorage";
import { getUserManager } from "./userManager";
import { buildUrl } from "./utility";

export interface QueryOptions {
    sort?: string;
    order?: undefined | "asc" | "desc";
    page?: number;
}

export type QueryResult<T> = {
    page: number;
    pageCount: number;
    totalResults: number;
    pageStart: number;
    pageEnd: number;
    pageResults: T[];
};

export type ValidationResult = {
    [key: string]: string;
};

export interface RegisterResult {
    success: boolean;
    email: string;
    name: string;
    validation?: ValidationResult;
}

export interface SignInResult extends UserAuthInfo {
    success: boolean;
    validation?: ValidationResult;
}

export interface UserAuthInfo {
    token: string;
    user: User;
}

export interface User {
    id: number;
    created: number;
    email: string;
    name: string;
    tags?: string[];
    avatarUrl: string;
    shareHistory: boolean;
    kofiEmail: string;
    kofiEmailVerified: boolean;
    kofiMember: boolean;
    twitch?: UserTwitchInfo;
}

export interface UserTwitchInfo {
    displayName: string;
    profileImageUrl: string;
    isSubscribed: boolean;
}

export interface UserQueryOptions {
    sort?: string;
    order?: undefined | "asc" | "desc";
    page?: number;
}

export type UserQueryResult = QueryResult<User>;

export interface UpdateUpdateRequest {
    email?: string;
    name?: string;
    kofiEmail?: string;
    tags?: string[];
    shareHistory?: boolean;
    twitchCode?: string;
    twitchRedirectUri?: string;
}

export interface UpdateUserResult {
    success: boolean;
    validation?: ValidationResult;
}

export interface ProfileQueryOptions {
    q?: string;
    game?: number;
    user?: string;
    page?: number;
}

export interface Profile {
    id: number;
    gameId: number;
    name: string;
    description: string;
    userId: number;
    userName: string;
    starCount: number;
    seedCount: number;
    isStarred: boolean;
    public: boolean;
    official: boolean;
    configId: number;
    config?: Config;
}

export type ProfileQueryResult = QueryResult<Profile>;

export interface ConfigOption {
    id: string;
    label: string;
    description?: string;
    category?: ConfigOptionCategory;
    type: string;
    size?: number;
    min?: number;
    max?: number;
    step?: number;
    options?: string[];
    default?: boolean | number | string;
}

export interface ConfigOptionCategory {
    label: string;
    backgroundColor: string;
    textColor: string;
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

export enum RandoStatus {
    Unknown,
    Unassigned,
    Processing,
    Completed,
    Failed,
    Expired,
    Discarded,
}

export interface GenerateRequest {
    gameId: number;
    seed: number;
    profileId: number;
    config: Config;
}

export interface Rando {
    id: number;
    seed: number;
    config: Config;
    status: RandoStatus;
    instructions: string;
    failReason: string;
    assets: RandoAsset[];
}

export interface RandoAsset {
    key: string;
    title: string;
    description: string;
    fileName: string;
    downloadUrl: string;
}

export interface RandoHistoryQueryOptions {
    user?: string;
    game?: number;
    sort?: string;
    order?: undefined | "asc" | "desc";
    page?: number;
}

export type RandoHistoryResult = QueryResult<RandoHistoryItem>;

export interface RandoHistoryItem {
    id: number;
    created: number;
    userId: number;
    userName: string;
    userTags: string[];
    userAvatarUrl: string;
    profileId: number;
    profileName: string;
    profileUserId: number;
    profileUserName: string;
    version: string;
    seed: number;
    status: RandoStatus;
    config: string;
}

export interface StatsResult {
    randoCount: number;
    profileCount: number;
    userCount: number;
}

export interface LightUserInfo {
    id: number;
    name: string;
    avatarUrl: string;
}

export interface AdminLightUserInfo extends LightUserInfo {
    email: string;
}

export interface PatronQueryOptions extends QueryOptions {
    gameId?: number;
    user?: string;
}

export type PatronDonationsResult = QueryResult<PatronDonationsItem>;
export interface PatronDonationsItem {
    id: number;
    gameId: number;
    messageId: string;
    timestamp: number;
    email: string;
    amount: number;
    tierName: string;
    payload: string;
    user: LightUserInfo;
}

export type PatronDailyResult = {
    day: string;
    donations: number;
    amount: number;
}[];

export type TokenModelResult = QueryResult<TokenModel>;
export interface TokenModel {
    id: number;
    created: number;
    lastUsed: number | null;
    code: string;
    token: string;
    user: AdminLightUserInfo;
}

export interface NewsItem {
    id: number;
    gameId: number;
    date: string;
    timestamp: number;
    title: string;
    body: string;
}

export interface NewsItemRequest {
    timestamp: number;
    title: string;
    body: string;
}

export interface DailyResult {
    day: string;
    value: number;
}

export interface MonthlyResult {
    month: string;
    value: number;
}

export interface HomeStatsResult {
    seeds: DailyResult[],
    totalUsers: MonthlyResult[]
};

export interface InfoResult {
    generators: RandoGenerator[];
    totalRandoMemory: number;
    generatedRandos: Rando[];
}

export interface RandoGenerator {
    id: number;
    gameId: number;
    status: string;
    registerTime: number;
    lastHeartbeatTime: number;
}

export class BioRandApiError extends Error {
    statusCode: number;

    constructor(statusCode: number, message?: string) {
        super(message);
        this.name = 'BioRandApiError';
        this.statusCode = statusCode;
    }
}

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

    async signOut() {
        await this.post("auth/signout");
    }

    async getUser(id: number | string) {
        return await this.get<User>(`user/${id}`);
    }

    async getUsers(query: UserQueryOptions) {
        return await this.get<UserQueryResult>(`user`, query);
    }

    async updateUser(id: number, request: UpdateUpdateRequest) {
        return await this.put<UpdateUserResult>(`user/${id}`, request);
    }

    async verifyEmail(token: string) {
        return await this.post(`user/verify`, { token });
    }

    async reverifyKofiEmail(userId: number) {
        return await this.post(`user/${userId}/reverifykofi`);
    }

    async getProfiles(game: number) {
        return await this.get<Profile[]>("profile", {
            game
        });
    }

    async searchProfiles(query: ProfileQueryOptions) {
        return await this.get<ProfileQueryResult>("profile/search", query);
    }

    async setProfileStar(profileId: number, value: boolean) {
        return await this.fetch(value ? 'POST' : 'DELETE', `profile/${profileId}/star`);
    }

    async createProfile(profile: Profile) {
        return await this.post<Profile>(`profile`, profile);
    }

    async updateProfile(profile: Profile) {
        return await this.put<Profile>(`profile/${profile.id}`, profile);
    }

    async deleteProfile(id: number) {
        return await this.delete<Profile>(`profile/${id}`);
    }

    async getConfigDefinition(game: number) {
        return await this.get<ConfigDefinition>("profile/definition", {
            game
        });
    }

    async generate(request: GenerateRequest) {
        return await this.post<Rando>("rando/generate", request);
    }

    async getRando(id: number) {
        return await this.get<Rando>(`rando/${id}`);
    }

    async getStats(gameId: number) {
        return await this.get<StatsResult>("rando/stats", {
            gameId
        });
    }

    async getRandoHistory(query: RandoHistoryQueryOptions) {
        return await this.get<RandoHistoryResult>("rando/history", query);
    }

    async getPatronDonations(query: PatronQueryOptions) {
        return await this.get<PatronDonationsResult>("patron/donations", query);
    }

    async getPatronDaily(gameId?: number) {
        return await this.get<PatronDailyResult>("patron/daily", {
            gameId
        });
    }

    async updatePatronUser(id: number, userName: string) {
        return await this.put("patron/match", {
            id, userName
        });
    }

    async getTokens(query: QueryOptions) {
        return await this.get<TokenModelResult>("auth/tokens", query);
    }

    async getNewsItems(game: number) {
        return await this.get<NewsItem[]>("home/news", {
            game
        });
    }

    async createNewsItem(req: NewsItemRequest) {
        return await this.post<NewsItem>("home/news", req);
    }

    async updateNewsItem(id: number, req: NewsItemRequest) {
        return await this.put(`home/news/${id}`, req);
    }

    async deleteNewsItem(id: number) {
        return await this.delete(`home/news/${id}`);
    }

    async getHomeStats(gameId: number) {
        return await this.get<HomeStatsResult>("home/stats", {
            gameId
        });
    }

    async getInfo() {
        return await this.get<InfoResult>("info");
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

    private async delete<T>(query: string, body?: any) {
        return this.fetch<T>('DELETE', query, body);
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
        if (!req.ok) {
            throw new BioRandApiError(req.status)
        }

        const contentType = req.headers.get('content-type');
        if (contentType?.startsWith('application/json')) {
            return await req.json();
        } else {
            return <any>undefined;
        }
    }

    private getUrl(query: string) {
        return `${this.urlBase}/${query}`;
    }
}

export function isLocalhost() {
    return document.location.hostname === 'localhost';
}

export function isBeta() {
    return document.location.hostname.indexOf('beta') != -1;
}

export function isCustomApi() {
    const lsManager = getLocalStorageManager();
    let apiUrl = lsManager.getString(LocalStorageKeys.ApiUrl);
    return !!apiUrl;
}

export function switchApi(useDefault: boolean) {
    const lsManager = getLocalStorageManager();
    if (useDefault) {
        lsManager.set(LocalStorageKeys.ApiUrl, undefined);
    } else {
        lsManager.set(LocalStorageKeys.ApiUrl, 'https://api.biorand.net');
    }
}

export function getGameId() {
    const lsManager = getLocalStorageManager();
    let gameId = lsManager.getNumber(LocalStorageKeys.GameId);
    if (gameId) {
        return gameId;
    }

    const hostname = document.location.hostname;
    const regexResult = hostname.match(/^(?:beta-)?([^.]+)\..+$/);
    if (regexResult) {
        const moniker = regexResult[1];
        switch (moniker) {
            case 're2r':
                return 2;
            case 're4r':
                return 1;
        }
    }
    return 1;
}

export function getGameMoniker() {
    const gameId = getGameId();
    if (gameId == 2)
        return "re2r";
    return "re4r";
}

export function getApi() {
    const lsManager = getLocalStorageManager();
    let apiUrl = lsManager.getString(LocalStorageKeys.ApiUrl);
    if (!apiUrl) {
        apiUrl = 'https://api.biorand.net';
        if (isLocalhost()) {
            apiUrl = 'http://localhost:10285';
        } else if (isBeta()) {
            apiUrl = 'https://beta-api.biorand.net';
        }
    }

    const api = new BioRandApi(apiUrl);
    const userManager = getUserManager();
    if (userManager.isSignedIn()) {
        api.setAuthToken(userManager.info!.token);
    }
    return api;
}

export function getWebsiteTitle(pageTitle?: string) {
    let mainTitle;
    switch (getGameId()) {
        default:
        case 1:
            mainTitle = "BioRand 4";
            break;
        case 2:
            mainTitle = "BioRand 2";
            break;
    }
    if (!pageTitle)
        return mainTitle;
    return `${pageTitle} - ${mainTitle}`;
}
