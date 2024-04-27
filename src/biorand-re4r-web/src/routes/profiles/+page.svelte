<script lang="ts">
    import { page } from '$app/stores';
    import PageNav from '$lib/PageNav.svelte';
    import { getApi, type ProfileQueryOptions, type ProfileQueryResult } from '$lib/api';
    import { buildUrl, getLocation, idleTimeout } from '$lib/utility';
    import { readable, writable } from 'svelte/store';

    function tryParseInt(input: any): number | undefined {
        if (typeof input !== 'string') return undefined;
        const result = parseInt(input);
        if (isNaN(result)) return undefined;
        return result;
    }

    const queryParams = readable<ProfileQueryOptions>(undefined, (set) => {
        getLocation().subscribe((location) => {
            const searchParams = new URLSearchParams(location.search);
            set({
                q: searchParams.get('q') || undefined,
                user: searchParams.get('user') || undefined,
                page: tryParseInt(searchParams.get('page'))
            });
        });
    });

    let searchInput: ProfileQueryOptions;
    let searchResult: ProfileQueryResult | undefined = undefined;
    const api = getApi();
    queryParams.subscribe(async (params) => {
        searchInput = params;
        searchResult = await api.searchProfiles(searchInput);
    });

    const filter = writable($page.url.searchParams.get('q'));
    idleTimeout(500, (whenIdle) => {
        filter.subscribe((value) => {
            if (value == searchInput.q) return;
            whenIdle(() => {
                const url = getSearchUrl({ ...searchInput, q: value });
                window.history.replaceState({}, '', url);
            });
        });
    });

    async function setProfileStarred(profileId: number, value: boolean) {
        if (searchResult) {
            await api.setProfileStar(profileId, value);
            searchResult = {
                ...searchResult,
                pageResults: searchResult.pageResults.map((r) => {
                    if (r.id != profileId) {
                        return r;
                    } else {
                        return {
                            ...r,
                            starCount: r.starCount + (value ? 1 : -1),
                            isStarred: value
                        };
                    }
                })
            };
        }
    }

    function getSearchUrl(query: { [key: string]: any }) {
        return buildUrl('profiles', query);
    }

    function getByUserUrl(user: string) {
        return getSearchUrl({ ...searchInput, user, page: undefined });
    }

    function getPageUrl(page: number) {
        return getSearchUrl({ ...searchInput, page });
    }
</script>

<div class="container">
    <h1 class="mb-3">Community Profiles</h1>
    <div class="card p-3 mb-3">
        <form>
            <div class="mb-3 row">
                <label for="input-filter" class="col-sm-2 col-form-label">Filter</label>
                <div class="col-sm-10">
                    <input
                        type="text"
                        class="form-control"
                        id="input-filter"
                        bind:value={$filter}
                    />
                </div>
            </div>
        </form>
    </div>
    {#if searchResult}
        <ul class="list-group mb-3">
            {#each searchResult.pageResults as profile}
                <li class="list-group-item">
                    <div class="float-end">
                        <div class="form-check form-check-reverse form-switch">
                            <span class="badge text-bg-secondary">{profile.starCount}</span>
                            <input
                                class="form-check-input"
                                type="checkbox"
                                role="switch"
                                id="profile-bookmark-{profile.id}"
                                checked={profile.isStarred}
                                on:change={(e) =>
                                    setProfileStarred(
                                        profile.id,
                                        e.target instanceof HTMLInputElement && e.target.checked
                                    )}
                            />
                            <label class="form-check-label" for="profile-bookmark-{profile.id}"
                                >Bookmarks</label
                            >
                        </div>
                        <div>
                            <span class="badge text-bg-secondary">{profile.seedCount}</span>
                            Seeds
                        </div>
                    </div>
                    <div>
                        <span class="fs-4">{profile.name}</span>
                        <span class="text-secondary"
                            >by
                            <a
                                class="link-underline link-underline-opacity-0"
                                href={getByUserUrl(profile.userName)}>{profile.userName}</a
                            ></span
                        >
                    </div>
                    <div>
                        {profile.description}
                    </div>
                </li>
            {/each}
        </ul>
        <PageNav page={searchResult.page} count={searchResult.pageCount} href={getPageUrl} />
    {/if}
</div>
