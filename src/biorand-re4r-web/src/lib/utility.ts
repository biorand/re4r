import { readable, type Readable } from "svelte/store";

export function idleTimeout(
    idleTime: number,
    init: (whenIdle: (idleAction: () => void) => void) => void
) {
    let cookie = 0;
    init((idleAction) => {
        const thisCookie = ++cookie;
        setTimeout(() => {
            if (thisCookie === cookie) {
                idleAction();
            }
        }, idleTime);
    });
}

export function buildUrl(base: string, queryParams: { [key: string]: any }) {
    let url = base;
    let first = true;
    if (queryParams) {
        url += '?';
        for (const key in queryParams) {
            const value = queryParams[key];
            if (typeof value !== 'undefined' && value !== null) {
                if (first) first = false;
                else url += '&';
                url += `${key}=${encodeURIComponent(value)}`;
            }
        }
    }
    return url;
}

let locationReadable: Readable<Location> | undefined;
export function getLocation() {
    if (!locationReadable) {
        let currentUrl = window.location.href;
        locationReadable = readable(window.location, (set) => {
            const cookie = setInterval(() => {
                if (window.location.href !== currentUrl) {
                    currentUrl = window.location.href;
                    set(window.location);
                }
            }, 50);
            return () => {
                clearInterval(cookie);
            };
        });
    }
    return locationReadable;
}

export function tryParseInt(input: any): number | undefined {
    if (typeof input !== 'string') return undefined;
    const result = parseInt(input);
    if (isNaN(result)) return undefined;
    return result;
}

export function objectEquals(obj1: any, obj2: any) {
    // Check if both objects are objects
    if (!obj1 || !obj2 || typeof obj1 !== 'object' || typeof obj2 !== 'object') {
        return false;
    }

    // Get the keys of the objects
    const keys1 = Object.keys(obj1);
    const keys2 = Object.keys(obj2);

    // Check if the number of keys is the same
    if (keys1.length !== keys2.length) {
        return false;
    }

    // Check if all keys and values are equal
    for (let key of keys1) {
        if (!keys2.includes(key) || obj1[key] !== obj2[key]) {
            return false;
        }

        // Recursively check nested objects
        if (typeof obj1[key] === 'object' && typeof obj2[key] === 'object') {
            if (!objectEquals(obj1[key], obj2[key])) {
                return false;
            }
        }
    }

    return true;
}

export function rng(low: number, high: number) {
    const range = high - low;
    return low + Math.round(Math.random() * range);
}

export function groupBy<T>(array: T[], keyFn: (obj: T) => string) {
    return array.reduce((acc: { [key: string]: T[] }, obj) => {
        const k = keyFn(obj);
        acc[k] = acc[k] || [];
        acc[k].push(obj);
        return acc;
    }, {});
}

export function replaceBy<T>(collection: T[], predicate: (item: T) => boolean, newValue: T) {
    return collection.map(x => {
        return predicate(x) ? newValue : x;
    });
}
