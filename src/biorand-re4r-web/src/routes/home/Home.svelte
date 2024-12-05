<script lang="ts">
    import { createChart } from '$lib/Chart.svelte';
    import DeleteConfirmModal from '$lib/DeleteConfirmModal.svelte';
    import {
        UserRole,
        getApi,
        getGameId,
        type DailyResult,
        type MonthlyResult,
        type NewsItem
    } from '$lib/api';
    import { PageBody, PageTitle } from '$lib/typography';
    import { getUserManager } from '$lib/userManager';
    import { Button, Timeline } from 'flowbite-svelte';
    import NewsItemEditModal from './NewsItemEditModal.svelte';
    import NewsItemView from './NewsItemView.svelte';
    import SideChart from './SideChart.svelte';

    let newsItems: NewsItem[] = [];
    let seedChart: any;
    let totalUsersChart: any;
    const init = async () => {
        const api = getApi();
        newsItems = await api.getNewsItems(getGameId());

        const statsResult = await api.getHomeStats();
        seedChart = createDailyChart('Seeds', statsResult.seeds);
        totalUsersChart = createMonthlyChart('Registered Users', statsResult.totalUsers);
    };
    init();

    const canEdit = (() => {
        const userManager = getUserManager();
        const userRole = userManager.info?.user.role ?? UserRole.Pending;
        return userRole >= UserRole.Administrator;
    })();

    let showEditModal = false;
    let showDeleteModal = false;
    let editingNewsItem: NewsItem | undefined = undefined;

    function createDailyChart(title: string, daily: DailyResult[]) {
        const days = daily.map((x) => {
            const dt = new Date(x.day);
            return new Intl.DateTimeFormat('en-US', { month: 'short', day: '2-digit' }).format(dt);
        });
        const result = createChart(
            title,
            days,
            daily.map((x) => x.value)
        );
        result.xaxis.axisTicks.show = true;
        result.xaxis.labels.show = false;
        return result;
    }

    function createMonthlyChart(title: string, monthly: MonthlyResult[]) {
        const months = monthly.map((x) => {
            const dt = new Date(x.month);
            return new Intl.DateTimeFormat('en-US', { month: 'short' }).format(dt);
        });
        const result = createChart(
            title,
            months,
            monthly.map((x) => x.value)
        );
        return result;
    }

    function createNewsItem() {
        editingNewsItem = <NewsItem>{
            id: 0,
            gameId: getGameId(),
            title: '',
            timestamp: Math.floor(Date.now() / 1000),
            date: '',
            body: ''
        };
        showEditModal = true;
    }

    function editNewsItem(newsItem: NewsItem) {
        editingNewsItem = newsItem;
        showEditModal = true;
    }

    function deleteNewsItem(newsItem: NewsItem) {
        editingNewsItem = newsItem;
        showDeleteModal = true;
    }

    async function saveNewsItem() {
        if (!editingNewsItem) return;

        const api = getApi();
        const req = {
            timestamp: editingNewsItem.timestamp,
            title: editingNewsItem.title,
            body: editingNewsItem.body
        };

        if (editingNewsItem.id === 0) {
            await api.createNewsItem(req);
        } else {
            await api.updateNewsItem(editingNewsItem.id, req);
        }
        showEditModal = false;
        await init();
    }

    async function deleteConfirmNewsItem() {
        if (!editingNewsItem) return;

        const api = getApi();
        await api.deleteNewsItem(editingNewsItem.id);
        await init();
    }
</script>

<PageBody>
    <PageTitle>Home</PageTitle>
    <div class="xl:flex xl:gap-3">
        <div class="xl:w-2/3 mb-4">
            <h2 class="text-2xl mb-4">Recent Updates</h2>
            {#if canEdit}
                <Button class="mb-4" on:click={() => createNewsItem()} size="lg" color="light"
                    >Create news item</Button
                >
            {/if}
            <Timeline>
                {#each newsItems as newsItem}
                    <NewsItemView
                        on:edit={() => editNewsItem(newsItem)}
                        on:delete={() => deleteNewsItem(newsItem)}
                        {canEdit}
                        {newsItem}
                    />
                {/each}
            </Timeline>
        </div>
        <div class="xl:w-1/3">
            <div class="flex flex-col gap-3">
                <SideChart chart={seedChart} />
                <SideChart chart={totalUsersChart} />
            </div>
        </div>
    </div>
</PageBody>
<NewsItemEditModal
    bind:open={showEditModal}
    on:save={() => saveNewsItem()}
    newsItem={editingNewsItem}
/>
<DeleteConfirmModal bind:open={showDeleteModal} on:delete={() => deleteConfirmNewsItem()} />
