import { getApi, getGameMoniker, type User, type UserAuthInfo } from "./api";
import { LocalStorageKeys, getLocalStorageManager } from "./localStorage";

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
        const lsManager = getLocalStorageManager();
        this._info = lsManager.get(LocalStorageKeys.UserManager);
    }

    saveToLocalStorage() {
        const lsManager = getLocalStorageManager();
        lsManager.set(LocalStorageKeys.UserManager, this._info);
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
                try {
                    const user = await api.getUser(this._info.user.id);
                    this.setSignedIn({
                        ...this._info,
                        user: user
                    });
                } catch (err: any) {
                    if (err?.statusCode === 401) {
                        this.signOut();
                    }
                }
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

export function containsUserTag(tags: string[], tag: string) {
    if (tag.indexOf("$GAME") != -1) {
        const gameMoniker = getGameMoniker();
        tag = tag.replace("$GAME", gameMoniker);
    }

    const index = tags.findIndex(x => {
        if (x.startsWith(tag)) {
            if (x.length <= tag.length || x[x.length] == '/') {
                return true;
            }
        }
        return false;
    });
    return index != -1;
}

export function hasUserTag(user: User | undefined, tag: string) {
    const tags = user?.tags || [];
    return containsUserTag(tags, tag);
}
