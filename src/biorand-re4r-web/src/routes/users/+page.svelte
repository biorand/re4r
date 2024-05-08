<script lang="ts">
    import BioRandPagination from '$lib/BioRandPagination.svelte';
    import RoleBadge from '$lib/RoleBadge.svelte';
    import SortedTable, { type SortedTableData } from '$lib/SortedTable.svelte';
    import SortedTableHeader from '$lib/SortedTableHeader.svelte';
    import TableTitle from '$lib/TableTitle.svelte';
    import Timestamp from '$lib/Timestamp.svelte';
    import { getApi, type User, type UserQueryOptions, type UserQueryResult } from '$lib/api';
    import { buildUrl, getLocation, tryParseInt } from '$lib/utility';
    import { Avatar, TableBodyCell, TableBodyRow, TableHead } from 'flowbite-svelte';
    import { readable } from 'svelte/store';

    const queryParams = readable<UserQueryOptions>(undefined, (set) => {
        getLocation().subscribe((location) => {
            const searchParams = new URLSearchParams(location.search);
            set({
                sort: searchParams.get('sort') || undefined,
                order: (searchParams.get('order') || undefined) as any,
                page: tryParseInt(searchParams.get('page'))
            });
        });
    });

    let searchInput: UserQueryOptions;
    let searchResult: UserQueryResult | undefined = undefined;
    let data: SortedTableData<User>;
    const api = getApi();
    queryParams.subscribe(async (params) => {
        searchInput = params;
        searchResult = await api.getUsers(params);
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
        return buildUrl('users', query);
    }

    function getPageUrl(page: number) {
        return getSearchUrl({ ...searchInput, page });
    }
</script>

<svelte:head>
    <title>Users - BioRand 4</title>
</svelte:head>

<div class="container mx-auto mb-3">
    <TableTitle title="Users" result={searchResult} />
    {#if searchResult}
        <SortedTable {data} on:sort={sortTable} let:item={user}>
            <TableHead slot="header">
                <SortedTableHeader key="name">Name</SortedTableHeader>
                <SortedTableHeader>Email</SortedTableHeader>
                <SortedTableHeader key="created">Joined</SortedTableHeader>
                <SortedTableHeader key="role">Role</SortedTableHeader>
            </TableHead>
            <TableBodyRow class="text-base font-semibold">
                <TableBodyCell tdClass="p-1">
                    <div class="flex">
                        <div class="content-center">
                            <Avatar class="mr-2 w-4 h-4" src={user.avatarUrl} />
                        </div>
                        <div>
                            <a class="text-blue-400 hover:text-blue-300" href="/user/{user.name}"
                                >{user.name}</a
                            >
                        </div>
                    </div>
                </TableBodyCell>
                <TableBodyCell tdClass="p-1">{user.email}</TableBodyCell>
                <TableBodyCell tdClass="p-1"><Timestamp value={user.created} /></TableBodyCell>
                <TableBodyCell tdClass="p-1"><RoleBadge role={user.role} /></TableBodyCell>
            </TableBodyRow>
        </SortedTable>
        <BioRandPagination
            page={searchResult.page}
            pageCount={searchResult.pageCount}
            href={getPageUrl}
        />
    {/if}
</div>
