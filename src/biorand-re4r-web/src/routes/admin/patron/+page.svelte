<script lang="ts">
    import {
        getApi,
        getWebsiteTitle,
        type PatronDonationsItem,
        type PatronDonationsResult,
        type PatronQueryOptions
    } from '$lib/api';
    import BioRandResultPagination from '$lib/BioRandResultPagination.svelte';
    import Chart, { createChart } from '$lib/Chart.svelte';
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
    const refresh = async () => {
        searchResult = await api.getPatronDonations(searchInput);
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
        return buildUrl('/admin/patron', query);
    }

    let showingError: ErrorModalContent | undefined = undefined;

    function formatCurrency(amount: number) {
        return '£' + amount.toFixed(2);
    }

    async function getChartOptions() {
        const daily = await api.getPatronDaily();
        const days = daily.map((x) => {
            const dt = new Date(x.day);
            return new Intl.DateTimeFormat('en-US', { month: 'short', day: '2-digit' }).format(dt);
        });
        return [
            createChart(
                'Donations',
                days,
                daily.map((x) => x.donations)
            ),
            createChart(
                'Amount',
                days,
                daily.map((x) => x.amount),
                (value: number) => `£${value}`
            )
        ];
    }

    let optionsA: any = undefined;
    let optionsB: any = undefined;
    getChartOptions().then((result) => {
        optionsA = result[0];
        optionsB = result[1];
    });

    let showUserMatchModal = false;
    let userMatchDonationId: number | undefined;
    let userMatchName = '';

    function matchUser(donation: PatronDonationsItem) {
        userMatchDonationId = donation.id;
        userMatchName = donation.user?.name ?? '';
        showUserMatchModal = true;
    }

    async function onSubmitUserMatch() {
        if (userMatchDonationId) {
            await api.updatePatronUser(userMatchDonationId, userMatchName);
        }
        showUserMatchModal = false;
        userMatchName = '';
        await refresh();
    }
</script>

<svelte:head>
    <title>{getWebsiteTitle('Patron Dashboard')}</title>
</svelte:head>

<PageBody>
    <PageTitle class="grow">Patron Dashboard</PageTitle>
    <Tabs>
        <TabItem title="Entries" open>
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
                        <TableBodyCell tdClass="p-1"
                            ><Timestamp value={donation.timestamp} /></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1">{donation.email}</TableBodyCell>
                        <TableBodyCell tdClass="p-1">
                            <div class="flex gap-2">
                                <Button
                                    on:click={() => matchUser(donation)}
                                    color="dark"
                                    class="!p-2"><EditOutline class="w-3 h-3" /></Button
                                >
                                {#if donation.user}
                                    <UserWidget
                                        user={donation.user}
                                        href="/admin/patron?user={donation.user.name}"
                                    />
                                {:else}
                                    (no match)
                                {/if}
                            </div>
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
        </TabItem>
        <TabItem title="Stats">
            <div class="flex">
                <div class="w-1/2">
                    <h2 class="text-2xl dark:text-gray-400">Donations</h2>
                    <Chart options={optionsA} />
                </div>
                <div class="w-1/2">
                    <h2 class="text-2xl dark:text-gray-400">Amount</h2>
                    <Chart options={optionsB} />
                </div>
            </div>
        </TabItem>
    </Tabs>
</PageBody>
<ErrorModal error={showingError} />
<InputModal
    bind:open={showUserMatchModal}
    title="Enter user to match"
    label="User Name"
    bind:value={userMatchName}
    on:submit={onSubmitUserMatch}
/>
