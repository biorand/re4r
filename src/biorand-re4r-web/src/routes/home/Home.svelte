<script lang="ts">
    import { createChart } from '$lib/Chart.svelte';
    import DeleteConfirmModal from '$lib/DeleteConfirmModal.svelte';
    import {
        UserRole,
        getApi,
        type DailyResult,
        type HomeNewsResult,
        type NewsItem
    } from '$lib/api';
    import { PageBody, PageTitle } from '$lib/typography';
    import { getUserManager } from '$lib/userManager';
    import { Timeline } from 'flowbite-svelte';
    import NewsItemEditModal from './NewsItemEditModal.svelte';
    import NewsItemView from './NewsItemView.svelte';
    import SideChart from './SideChart.svelte';

    let newsResult: HomeNewsResult;
    let seedChart: any;
    let totalUsersChart: any;
    const init = async () => {
        const api = getApi();
        newsResult = await api.getHomeNews();

        const statsResult = await api.getHomeStats();
        seedChart = createDailyChart('Seeds', statsResult.seeds);
        totalUsersChart = createDailyChart('Registered Users', statsResult.totalUsers);
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

    function editNewsItem(newsItem: NewsItem) {
        editingNewsItem = newsItem;
        showEditModal = true;
    }

    function deleteNewsItem(newsItem: NewsItem) {
        editingNewsItem = newsItem;
        showDeleteModal = true;
    }

    function deleteConfirmNewsItem() {
        if (!editingNewsItem) return;

        alert(editingNewsItem.title);
    }
</script>

<PageBody>
    <PageTitle>Home</PageTitle>
    <div class="lg:flex lg:gap-3">
        <div class="grow">
            {#if newsResult}
                <h2 class="text-2xl mb-4">Recent Updates</h2>
                <Timeline>
                    {#each newsResult.items as newsItem}
                        <NewsItemView
                            on:edit={() => editNewsItem(newsItem)}
                            on:delete={() => deleteNewsItem(newsItem)}
                            canEdit
                            {newsItem}
                        />
                    {/each}
                </Timeline>
            {/if}
        </div>
        <div class="grow w-144 max-w-[40%]">
            <div class="flex flex-col gap-3">
                <SideChart chart={seedChart} />
                <SideChart chart={totalUsersChart} />
            </div>
        </div>
    </div>
</PageBody>
<NewsItemEditModal bind:open={showEditModal} newsItem={editingNewsItem} />
<DeleteConfirmModal bind:open={showDeleteModal} on:delete={() => deleteConfirmNewsItem()} />
