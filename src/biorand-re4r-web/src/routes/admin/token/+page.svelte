<script lang="ts">
    import {
        getApi,
        type PatronDonationsItem,
        type PatronDonationsResult,
        type PatronQueryOptions,
        type QueryOptions,
        type TokenModel,
        type TokenModelResult
    } from '$lib/api';
    import BioRandResultPagination from '$lib/BioRandResultPagination.svelte';
    import ErrorModal, { type ErrorModalContent } from '$lib/ErrorModal.svelte';
    import InputModal from '$lib/InputModal.svelte';
    import SortedTable, { type SortedTableData } from '$lib/SortedTable.svelte';
    import SortedTableHeader from '$lib/SortedTableHeader.svelte';
    import Timestamp from '$lib/Timestamp.svelte';
    import { PageTitle } from '$lib/typography';
    import PageBody from '$lib/typography/PageBody.svelte';
    import UserWidget from '$lib/UserWidget.svelte';
    import { buildUrl, getLocation, tryParseInt } from '$lib/utility';
    import {
        Badge,
        Button,
        TabItem,
        TableBodyCell,
        TableBodyRow,
        TableHead,
        Tabs
    } from 'flowbite-svelte';
    import { EditOutline } from 'flowbite-svelte-icons';
    import { readable } from 'svelte/store';

    const queryParams = readable<QueryOptions>(undefined, (set) => {
        getLocation().subscribe((location) => {
            const searchParams = new URLSearchParams(location.search);
            set({
                sort: searchParams.get('sort') || undefined,
                order: (searchParams.get('order') || undefined) as any,
                page: tryParseInt(searchParams.get('page'))
            });
        });
    });

    let searchInput: QueryOptions;
    let searchResult: TokenModelResult | undefined = undefined;
    let data: SortedTableData<TokenModel>;
    let getPageUrl: (page: number) => string;
    const api = getApi();
    const refresh = async () => {
        searchResult = await api.getTokens(searchInput);
        data = {
            sort: searchInput.sort,
            order: searchInput.order,
            items: searchResult.pageResults
        };
        getPageUrl = (page: number) => {
            return getSearchUrl({ ...searchInput, page });
        };
    };
    queryParams.subscribe(async (params) => {
        searchInput = params;
        await refresh();
    });

    function sortTable(e: any) {
        const url = getSearchUrl({ ...searchInput, sort: e.detail.sort, order: e.detail.order });
        window.history.replaceState({}, '', url);
    }

    function getSearchUrl(query: { [key: string]: any }) {
        return buildUrl('/admin/token', query);
    }
</script>

<svelte:head>
    <title>Auth Tokens - BioRand 4</title>
</svelte:head>

<PageBody>
    <PageTitle class="grow">User Authentication Tokens</PageTitle>
    {#if searchResult}
        <SortedTable {data} on:sort={sortTable} let:item={token}>
            <TableHead slot="header">
                <SortedTableHeader>Token Id</SortedTableHeader>
                <SortedTableHeader key="created">Created</SortedTableHeader>
                <SortedTableHeader key="lastUsed">Last Used</SortedTableHeader>
                <SortedTableHeader key="username">Matched User</SortedTableHeader>
                <SortedTableHeader key="useremail">Email</SortedTableHeader>
                <SortedTableHeader>Token</SortedTableHeader>
                <SortedTableHeader>Code</SortedTableHeader>
            </TableHead>
            <TableBodyRow class="text-base font-semibold">
                <TableBodyCell tdClass="p-1 font-mono">{token.id}</TableBodyCell>
                <TableBodyCell tdClass="p-1"><Timestamp value={token.created} /></TableBodyCell>
                <TableBodyCell tdClass="p-1">
                    {#if token.lastUsed}
                        <Timestamp value={token.lastUsed} />
                    {/if}
                </TableBodyCell>
                <TableBodyCell tdClass="p-1">
                    <UserWidget user={token.user} href="/user/{token.user.name}" />
                </TableBodyCell>
                <TableBodyCell tdClass="p-1">{token.user.email}</TableBodyCell>
                <TableBodyCell tdClass="p-1 font-mono">{token.token}</TableBodyCell>
                <TableBodyCell tdClass="p-1 text-center"
                    ><Badge color="dark">{token.code}</Badge></TableBodyCell
                >
            </TableBodyRow>
        </SortedTable>
        <BioRandResultPagination result={searchResult} {getPageUrl} />
    {/if}
</PageBody>
