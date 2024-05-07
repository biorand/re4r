export const LocalStorageKeys = {
    ApiUrl: 'apiUrl',
    Stats: 'stats',
    UserManager: 'userManager'
};

class LocalStorageManager {
    getString(key: string): string | undefined {
        const result = localStorage.getItem(key);
        return typeof result === 'string' ? result : undefined;
    }

    get<T>(key: string): T | undefined {
        try {
            const j = localStorage.getItem(key);
            if (j) {
                return JSON.parse(j);
            }
        } catch {
        }
        return undefined;
    }

    set<T extends object>(key: string, value: T) {
        try {
            const j = JSON.stringify(value);
            localStorage.setItem(key, j);
        } catch {
        }
    }
}

let localStorageManager: LocalStorageManager | undefined;
export function getLocalStorageManager() {
    if (!localStorageManager) {
        localStorageManager = new LocalStorageManager();
    }
    return localStorageManager;
}
