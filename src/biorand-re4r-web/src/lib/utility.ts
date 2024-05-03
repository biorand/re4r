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
