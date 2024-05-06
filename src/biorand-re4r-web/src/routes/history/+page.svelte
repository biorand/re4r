<script lang="ts">
    import BioRandPagination from '$lib/BioRandPagination.svelte';
    import SortedTable, { type SortedTableData } from '$lib/SortedTable.svelte';
    import SortedTableHeader from '$lib/SortedTableHeader.svelte';
    import Timestamp from '$lib/Timestamp.svelte';
    import {
        getApi,
        type RandoHistoryItem,
        type RandoHistoryQueryOptions,
        type RandoHistoryResult
    } from '$lib/api';
    import { buildUrl, getLocation, tryParseInt } from '$lib/utility';
    import { Avatar, TableBodyCell, TableBodyRow, TableHead } from 'flowbite-svelte';
    import { readable } from 'svelte/store';

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
    const api = getApi();
    queryParams.subscribe(async (params) => {
        searchInput = params;
        searchResult = await api.getRandoHistory(params);
        data = {
            sort: searchInput.sort,
            order: searchInput.order,
            items: searchResult.pageResults
        };
    });

    function sortTable(e: any) {
        const url = getSearchUrl({ ...searchInput, sort: e.detail.sort, order: e.detail.order });
        window.history.replaceState({}, '', url);
    }

    function getSearchUrl(query: { [key: string]: any }) {
        return buildUrl('history', query);
    }

    function getPageUrl(page: number) {
        return getSearchUrl({ ...searchInput, page });
    }
</script>

<svelte:head>
    <title>History - BioRand 4</title>
</svelte:head>

<div class="container mx-auto mb-3">
    <h1 class="mb-3 text-4xl dark:text-white">History</h1>
    {#if searchResult}
        <SortedTable {data} on:sort={sortTable} let:item>
            <TableHead slot="header">
                <SortedTableHeader key="created">Time</SortedTableHeader>
                <SortedTableHeader>User</SortedTableHeader>
                <SortedTableHeader>Profile</SortedTableHeader>
                <SortedTableHeader>Version</SortedTableHeader>
                <SortedTableHeader>Seed</SortedTableHeader>
                <!--
                <SortedTableHeader></SortedTableHeader>
                -->
            </TableHead>
            <TableBodyRow class="text-base font-semibold">
                <TableBodyCell tdClass="p-1"><Timestamp value={item.created} /></TableBodyCell>
                <TableBodyCell tdClass="p-1">
                    <div class="flex">
                        <div class="content-center">
                            <Avatar class="mr-2 w-4 h-4" src={item.userAvatarUrl} />
                        </div>
                        <div>
                            <a
                                class="text-blue-400 hover:text-blue-300"
                                href="/history?user={item.userName}">{item.userName}</a
                            >
                        </div>
                    </div>
                </TableBodyCell>
                <TableBodyCell tdClass="p-1"
                    >{item.profileName}
                    <span class="text-gray-300 text-sm font-light">by {item.profileUserName}</span
                    ></TableBodyCell
                >
                <TableBodyCell tdClass="p-1 font-mono">{item.version}</TableBodyCell>
                <TableBodyCell tdClass="p-1 font-mono">{item.seed}</TableBodyCell>
                <!--
                <TableBodyCell tdClass="p-1"
                    ><a href="/" class="text-blue-400 hover:text-blue-300">Generate</a
                    ></TableBodyCell
                >
                -->
            </TableBodyRow>
        </SortedTable>
        <BioRandPagination
            page={searchResult.page}
            pageCount={searchResult.pageCount}
            href={getPageUrl}
        />
    {/if}
</div>
