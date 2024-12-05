<script lang="ts">
    import { page } from '$app/stores';
    import BioRandResultPagination from '$lib/BioRandResultPagination.svelte';
    import {
        getApi,
        getGameId,
        getWebsiteTitle,
        type ProfileQueryOptions,
        type ProfileQueryResult
    } from '$lib/api';
    import { PageBody, PageTitle } from '$lib/typography';
    import { buildUrl, getLocation, idleTimeout, tryParseInt } from '$lib/utility';
    import { Input, Label, Listgroup, ListgroupItem } from 'flowbite-svelte';
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
    let getPageUrl: (page: number) => string;
    const api = getApi();
    queryParams.subscribe(async (params) => {
        searchInput = params;
        searchResult = await api.searchProfiles({ ...searchInput, game: getGameId() });
        getPageUrl = (page: number) => {
            return getSearchUrl({ ...searchInput, page });
        };
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
</script>

<svelte:head>
    <title>{getWebsiteTitle('Community Profiles')}</title>
</svelte:head>

<PageBody>
    <PageTitle>Community Profiles</PageTitle>
    <form class="bg-gray-100 dark:bg-gray-700 p-4 rounded-lg w-full mb-4">
        <Label for="input-filter" class="block mb-2">Filter</Label>
        <Input bind:value={$filter} id="input-filter" type="text" />
    </form>
    <p class="mb-3 dark:text-gray-300">
        Community profiles are preset randomizer configurations designed and shared by other users.
        You can bookmark a profile by clicking the bookmark button, then select it in the profile
        manager when generating a randomizer. You can share your own profile by toggling the share
        button which can be found in the profile tab of the profile editor.
    </p>
    {#if searchResult}
        <Listgroup class="mb-3">
            {#each searchResult.pageResults as profile}
                <ListgroupItem class="text-base gap-2 p-3">
                    <div class="flex">
                        <div class="grow">
                            <div>
                                <span class="text-2xl">{profile.name}</span>
                                <span class="dark:text-gray-500"
                                    >by
                                    <a
                                        class="text-blue-400 hover:text-blue-300"
                                        href={getByUserUrl(profile.userName)}>{profile.userName}</a
                                    ></span
                                >
                            </div>
                            <div class="font-light">
                                {profile.description}
                            </div>
                        </div>
                        <div>
                            <div class="flex gap-2">
                                <ProfileBadge tooltip="Generated Seeds">
                                    <ShuffleOutline />
                                    <span class="ml-1">{profile.seedCount}</span>
                                </ProfileBadge>
                                <ProfileBadge
                                    active={profile.isStarred}
                                    tooltip="Bookmarks"
                                    on:click={() =>
                                        setProfileStarred(profile.id, !profile.isStarred)}
                                >
                                    <BookmarkSolid />
                                    <span class="ml-1">{profile.starCount}</span>
                                </ProfileBadge>
                            </div>
                        </div>
                    </div>
                </ListgroupItem>
            {/each}
        </Listgroup>
        <BioRandResultPagination result={searchResult} {getPageUrl} />
    {/if}
</PageBody>
