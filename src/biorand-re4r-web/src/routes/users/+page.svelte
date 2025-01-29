<script lang="ts">
    import BioRandResultPagination from '$lib/BioRandResultPagination.svelte';
    import ErrorModal, { type ErrorModalContent } from '$lib/ErrorModal.svelte';
    import RoleBadge from '$lib/RoleBadge.svelte';
    import SortedTable, { type SortedTableData } from '$lib/SortedTable.svelte';
    import SortedTableHeader from '$lib/SortedTableHeader.svelte';
    import Timestamp from '$lib/Timestamp.svelte';
    import {
        getApi,
        getWebsiteTitle,
        type User,
        type UserQueryOptions,
        type UserQueryResult
    } from '$lib/api';
    import { PageTitle } from '$lib/typography';
    import PageBody from '$lib/typography/PageBody.svelte';
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
    let getPageUrl: (page: number) => string;
    const api = getApi();
    queryParams.subscribe(async (params) => {
        searchInput = params;
        searchResult = await api.getUsers(params);
        data = {
            sort: searchInput.sort,
            order: searchInput.order,
            items: searchResult.pageResults
        };
        getPageUrl = (page: number) => {
            return getSearchUrl({ ...searchInput, page });
        };
    });

    function sortTable(e: any) {
        const url = getSearchUrl({ ...searchInput, sort: e.detail.sort, order: e.detail.order });
        window.history.replaceState({}, '', url);
    }

    function getSearchUrl(query: { [key: string]: any }) {
        return buildUrl('users', query);
    }

    let showingError: ErrorModalContent | undefined = undefined;
</script>

<svelte:head>
    <title>{getWebsiteTitle('Users')}</title>
</svelte:head>

<PageBody>
    <PageTitle class="grow">Users</PageTitle>
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
                            <Avatar class="mr-2 w-4 h-4 overflow-hidden" src={user.avatarUrl} />
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
                <TableBodyCell tdClass="p-1"><RoleBadge {user} /></TableBodyCell>
            </TableBodyRow>
        </SortedTable>
        <BioRandResultPagination result={searchResult} {getPageUrl} />
    {/if}
</PageBody>
<ErrorModal error={showingError} />
