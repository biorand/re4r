import { LocalStorageKeys } from "./localStorage";

interface UserInfo {
    id: number;
    email: string;
    name: string;
    token: string;
}

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
    private _info: UserInfo | undefined;
    private notifications = new Notifier<() => void>();

    get info(): UserInfo | undefined {
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

    setSignedIn(id: number, email: string, name: string, token: string) {
        this._info = {
            id, email, name, token
        }
        this.notifications.raise();
        this.saveToLocalStorage();
    }

    signOut() {
        this._info = undefined;
        this.notifications.raise();
        this.saveToLocalStorage();
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
