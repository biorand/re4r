export const LocalStorageKeys = {
    ApiUrl: 'apiUrl',
    Stats: 'stats',
    UserManager: 'userManager',
    UserProfileManager: 'userProfileManager'
};

class LocalStorageManager {
    getNumber(key: string): number | undefined {
        const result = localStorage.getItem(key);
        return typeof result === 'string' ? parseFloat(result) : undefined;
    }

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

    set<T>(key: string, value: T | undefined) {
        try {
            if (typeof value === 'object') {
                const j = JSON.stringify(value);
                localStorage.setItem(key, j);
            } else if (value === null || typeof value === 'undefined') {
                localStorage.removeItem(key);
            } else {
                localStorage.setItem(key, value.toString());
            }
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
