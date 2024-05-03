import { getApi, type UserAuthInfo } from "./api";
import { LocalStorageKeys } from "./localStorage";

class Notifier<T extends (...args: any[]) => void> {
    subscribers: T[];

    constructor() {
        this.subscribers = [];
    }

    subscribe(callback: T) {
        this.subscribers.push(callback);
    }

    raise(...args: Parameters<T>) {
        this.subscribers.forEach(sub => {
            sub(args);
        });
    }
}

class UserManager {
    private _info: UserAuthInfo | undefined;
    private notifications = new Notifier<() => void>();

    get info(): UserAuthInfo | undefined {
        return this._info;
    }

    constructor() {
        try {
            const j = localStorage.getItem(LocalStorageKeys.UserManager);
            if (j) {
                this._info = JSON.parse(j);
            }
        } catch {
            this._info = undefined;
        }
    }

    saveToLocalStorage() {
        if (this._info)
            localStorage.setItem(LocalStorageKeys.UserManager, JSON.stringify(this._info));
        else
            localStorage.removeItem(LocalStorageKeys.UserManager);
    }

    isSignedIn() {
        return !!this._info;
    }

    setSignedIn(userAuthInfo: UserAuthInfo) {
        this._info = userAuthInfo;
        this.notifications.raise();
        this.saveToLocalStorage();
    }

    signOut() {
        this._info = undefined;
        this.notifications.raise();
        this.saveToLocalStorage();
    }

    async refresh() {
        try {
            if (this._info) {
                const api = getApi();
                const user = await api.getUser(this._info.user.id);
                this.setSignedIn({
                    ...this._info,
                    user: user
                });
            }
        }
        catch { }
    }

    subscribe(cb: () => void) {
        this.notifications.subscribe(cb);
    }
}

let userManager: UserManager | undefined = undefined;

export function getUserManager() {
    if (!userManager) {
        userManager = new UserManager();
    }
    return userManager;
}
