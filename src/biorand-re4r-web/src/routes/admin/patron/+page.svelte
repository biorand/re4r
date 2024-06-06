<script lang="ts">
    import BioRandResultPagination from '$lib/BioRandResultPagination.svelte';
    import ErrorModal, { type ErrorModalContent } from '$lib/ErrorModal.svelte';
    import SortedTable, { type SortedTableData } from '$lib/SortedTable.svelte';
    import SortedTableHeader from '$lib/SortedTableHeader.svelte';
    import Timestamp from '$lib/Timestamp.svelte';
    import UserWidget from '$lib/UserWidget.svelte';
    import {
        getApi,
        type PatronDonationsItem,
        type PatronDonationsResult,
        type PatronQueryOptions
    } from '$lib/api';
    import { PageTitle } from '$lib/typography';
    import PageBody from '$lib/typography/PageBody.svelte';
    import { buildUrl, getLocation, tryParseInt } from '$lib/utility';
    import { Badge, Button, TableBodyCell, TableBodyRow, TableHead } from 'flowbite-svelte';
    import { readable } from 'svelte/store';

    const queryParams = readable<PatronQueryOptions>(undefined, (set) => {
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

    let searchInput: PatronQueryOptions;
    let searchResult: PatronDonationsResult | undefined = undefined;
    let data: SortedTableData<PatronDonationsItem>;
    let getPageUrl: (page: number) => string;
    const api = getApi();
    queryParams.subscribe(async (params) => {
        searchInput = params;
        searchResult = await api.getPatronDonations(params);
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
        return buildUrl('/admin/patron', query);
    }

    let showingError: ErrorModalContent | undefined = undefined;

    function formatCurrency(amount: number) {
        return '£' + amount.toFixed(2);
    }
</script>

<svelte:head>
    <title>Patron Dashboard - BioRand 4</title>
</svelte:head>

<PageBody>
    <PageTitle class="grow">Patron Dashboard</PageTitle>
    {#if searchResult}
        <SortedTable {data} on:sort={sortTable} let:item={donation}>
            <TableHead slot="header">
                <SortedTableHeader>Message Id</SortedTableHeader>
                <SortedTableHeader key="timestamp">Timestamp</SortedTableHeader>
                <SortedTableHeader key="email">Ko-fi Email</SortedTableHeader>
                <SortedTableHeader key="username">Matched User</SortedTableHeader>
                <SortedTableHeader key="price" align="right">Amount</SortedTableHeader>
                <SortedTableHeader key="tiername" align="center">Tier</SortedTableHeader>
            </TableHead>
            <TableBodyRow class="text-base font-semibold">
                <TableBodyCell tdClass="p-1 font-mono"
                    ><span class="py-0.5 px-1 rounded text-xs bg-gray-200 dark:bg-gray-600"
                        >{donation.messageId}</span
                    ></TableBodyCell
                >
                <TableBodyCell tdClass="p-1"><Timestamp value={donation.timestamp} /></TableBodyCell
                >
                <TableBodyCell tdClass="p-1">{donation.email}</TableBodyCell>
                <TableBodyCell tdClass="p-1">
                    {#if donation.user}
                        <UserWidget
                            user={donation.user}
                            href="/admin/patron?user={donation.user.name}"
                        />
                    {:else}
                        <Button class="p-1 px-2" size="xs">Match User</Button>
                    {/if}
                </TableBodyCell>
                <TableBodyCell tdClass="p-1 text-right"
                    >{formatCurrency(donation.amount)}</TableBodyCell
                >
                <TableBodyCell tdClass="p-1 text-center">
                    {#if donation.tierName}
                        <Badge color="green">{donation.tierName}</Badge>
                    {:else}
                        <Badge color="dark">Single</Badge>
                    {/if}
                </TableBodyCell>
            </TableBodyRow>
        </SortedTable>
        <BioRandResultPagination result={searchResult} {getPageUrl} />
    {/if}
</PageBody>
<ErrorModal error={showingError} />
