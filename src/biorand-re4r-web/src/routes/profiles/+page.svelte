<script lang="ts">
    import { page } from '$app/stores';
    import BioRandPagination from '$lib/BioRandPagination.svelte';
    import { getApi, type ProfileQueryOptions, type ProfileQueryResult } from '$lib/api';
    import { buildUrl, getLocation, idleTimeout, tryParseInt } from '$lib/utility';
    import { Input, Label, Listgroup, ListgroupItem, Tooltip } from 'flowbite-svelte';
    import { BookmarkSolid, ShuffleOutline } from 'flowbite-svelte-icons';
    import { readable, writable } from 'svelte/store';
    import ProfileBadge from './ProfileBadge.svelte';

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

<div class="container mx-auto mb-3">
    <h1 class="mb-3 text-4xl dark:text-white">Community Profiles</h1>
    <form class="bg-gray-100 dark:bg-gray-700 p-4 rounded-lg w-full mb-4">
        <Label for="input-filter" class="block mb-2">Filter</Label>
        <Input bind:value={$filter} id="input-filter" type="text" />
    </form>
    {#if searchResult}
        <Listgroup class="mb-3">
            {#each searchResult.pageResults as profile}
                <ListgroupItem class="text-base font-semibold gap-2 p-3">
                    <div class="flex">
                        <div class="grow">
                            <div>
                                <span class="text-2xl">{profile.name}</span>
                                <span class="text-gray-500"
                                    >by
                                    <a
                                        class="text-blue-400 hover:text-blue-300"
                                        href={getByUserUrl(profile.userName)}>{profile.userName}</a
                                    ></span
                                >
                            </div>
                            <div>
                                {profile.description}
                            </div>
                        </div>
                        <div class="">
                            <Tooltip>Generated Seeds</Tooltip>
                            <ProfileBadge tooltip="Generated Seeds">
                                <ShuffleOutline />
                                <span class="ml-1">{profile.seedCount}</span>
                            </ProfileBadge>
                            <ProfileBadge
                                active={profile.isStarred}
                                tooltip="Bookmarks"
                                on:click={() => setProfileStarred(profile.id, !profile.isStarred)}
                            >
                                <BookmarkSolid />
                                <span class="ml-1">{profile.starCount}</span>
                            </ProfileBadge>
                        </div>
                    </div>
                </ListgroupItem>
            {/each}
        </Listgroup>
        <BioRandPagination
            page={searchResult.page}
            pageCount={searchResult.pageCount}
            href={getPageUrl}
        />
    {/if}
</div>
