<script lang="ts">
    import BioRandResultPagination from '$lib/BioRandResultPagination.svelte';
    import SortedTable, { type SortedTableData } from '$lib/SortedTable.svelte';
    import SortedTableHeader from '$lib/SortedTableHeader.svelte';
    import Timestamp from '$lib/Timestamp.svelte';
    import { UserProfileManager } from '$lib/UserProfileManager';
    import {
        getApi,
        getGameId,
        getWebsiteTitle,
        type RandoHistoryItem,
        type RandoHistoryQueryOptions,
        type RandoHistoryResult
    } from '$lib/api';
    import { getLocalStorageManager } from '$lib/localStorage';
    import PageBody from '$lib/typography/PageBody.svelte';
    import PageTitle from '$lib/typography/PageTitle.svelte';
    import { containsUserTag, getUserManager } from '$lib/userManager';
    import { buildUrl, getLocation, tryParseInt } from '$lib/utility';
    import { Avatar, TableBodyCell, TableBodyRow, TableHead } from 'flowbite-svelte';
    import { readable } from 'svelte/store';
    import RandoStatusBadge from './RandoStatusBadge.svelte';

    const queryParams = readable<RandoHistoryQueryOptions>(undefined, (set) => {
        getLocation().subscribe((location) => {
            const searchParams = new URLSearchParams(location.search);
            set({
                user: searchParams.get('user') || undefined,
                sort: searchParams.get('sort') || undefined,
                order: (searchParams.get('order') || undefined) as any,
                page: tryParseInt(searchParams.get('page'))
            });
        });
    });

    let searchInput: RandoHistoryQueryOptions;
    let searchResult: RandoHistoryResult | undefined = undefined;
    let data: SortedTableData<RandoHistoryItem>;
    let getPageUrl: (page: number) => string;
    const api = getApi();
    queryParams.subscribe(async (params) => {
        searchInput = params;
        searchResult = await api.getRandoHistory({ ...params, game: getGameId() });
        data = {
            sort: searchInput.sort,
            order: searchInput.order,
            items: searchResult.pageResults
        };
        getPageUrl = (page: number) => {
            return getSearchUrl({ ...searchInput, page });
        };
    });

    function loadConfig(item: RandoHistoryItem) {
        const userManager = getUserManager();
        let lsManager = getLocalStorageManager();
        const profileManager = new UserProfileManager(
            api,
            userManager.info?.user.id || 0,
            userManager.info?.user.name || ''
        );
        profileManager.loadProfile({
            id: 0,
            name: item.profileName,
            description: '',
            config: JSON.parse(item.config),
            userId: userManager.info?.user.id || 0,
            userName: userManager.info?.user.name || '',
            public: false,
            official: false,
            starCount: 0,
            seedCount: 0,

            category: 'Personal',
            originalId: item.profileId,
            isModified: true,
            isSelected: true,
            isOwner: true
        });
        lsManager.set('seed', item.seed);
    }

    function sortTable(e: any) {
        const url = getSearchUrl({ ...searchInput, sort: e.detail.sort, order: e.detail.order });
        window.history.replaceState({}, '', url);
    }

    function getSearchUrl(query: { [key: string]: any }) {
        return buildUrl('history', query);
    }

    function getNameColor(item: RandoHistoryItem) {
        const userTags = item.userTags || [];
        if (containsUserTag(userTags, '$GAME:patron')) {
            return 'text-green-700 hover:text-green-500 dark:text-yellow-400 dark:hover:text-yellow-300';
        }
        if (containsUserTag(userTags, '$GAME:tester') || containsUserTag(userTags, 'admin')) {
            return 'text-red-800 hover:text-red-700 dark:text-red-400 dark:hover:text-red-200';
        }
        return 'text-blue-800 hover:text-blue-500 dark:text-blue-400 dark:hover:text-blue-300';
    }
</script>

<svelte:head>
    <title>{getWebsiteTitle('History')}</title>
</svelte:head>

<PageBody>
    <PageTitle>History</PageTitle>
    {#if searchResult}
        <SortedTable {data} on:sort={sortTable} let:item>
            <TableHead slot="header">
                <SortedTableHeader key="created">Time</SortedTableHeader>
                <SortedTableHeader>User</SortedTableHeader>
                <SortedTableHeader>Profile</SortedTableHeader>
                <SortedTableHeader align="center">Version</SortedTableHeader>
                <SortedTableHeader>Seed</SortedTableHeader>
                <SortedTableHeader>Status</SortedTableHeader>
                <SortedTableHeader></SortedTableHeader>
            </TableHead>
            <TableBodyRow class="text-base font-semibold">
                <TableBodyCell tdClass="p-1"><Timestamp value={item.created} /></TableBodyCell>
                <TableBodyCell tdClass="p-1">
                    <div class="flex">
                        <div class="content-center">
                            <Avatar class="mr-2 w-4 h-4 overflow-hidden" src={item.userAvatarUrl} />
                        </div>
                        <div>
                            <a class={getNameColor(item)} href="/history?user={item.userName}"
                                >{item.userName}</a
                            >
                        </div>
                    </div>
                </TableBodyCell>
                <TableBodyCell tdClass="p-1"
                    >{item.profileName}
                    <span class="text-gray-700 dark:text-gray-300 text-sm font-light"
                        >by {item.profileUserName}</span
                    ></TableBodyCell
                >
                <TableBodyCell tdClass="p-1 font-mono text-center">
                    {#if item.version}
                        <span class="py-0.5 px-1 rounded text-xs bg-gray-200 dark:bg-gray-600">
                            {item.version}
                        </span>
                    {/if}
                </TableBodyCell>
                <TableBodyCell tdClass="p-1 font-mono">{item.seed}</TableBodyCell>
                <TableBodyCell tdClass="p-1 font-mono flex">
                    <RandoStatusBadge class="m-auto" status={item.status} />
                </TableBodyCell>
                <TableBodyCell tdClass="p-1"
                    ><a
                        on:click={() => loadConfig(item)}
                        href="/generate"
                        class="text-blue-400 hover:text-blue-300">Generate</a
                    ></TableBodyCell
                >
            </TableBodyRow>
        </SortedTable>
        <BioRandResultPagination result={searchResult} {getPageUrl} />
    {/if}
</PageBody>
