<script lang="ts">
    import { createChart } from '$lib/Chart.svelte';
    import { getApi, type DailyResult, type HomeNewsResult } from '$lib/api';
    import PageTitle from '$lib/typography/PageTitle.svelte';
    import { Timeline } from 'flowbite-svelte';
    import NewsItem from './NewsItem.svelte';
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
        seedChart.xaxis.axisTicks.show = false;
        seedChart.xaxis.labels.show = false;
    };
    init();

    function createDailyChart(title: string, daily: DailyResult[]) {
        const result = createChart(
            title,
            daily.map((x) => x.day),
            daily.map((x) => x.value)
        );
        result.xaxis.axisTicks.show = false;
        result.xaxis.labels.show = false;
        return result;
    }
</script>

<div class="container mx-auto p-3">
    <PageTitle>Home</PageTitle>
    <div class="lg:flex">
        <div class="grow">
            {#if newsResult}
                <h2 class="text-2xl">Recent Updates</h2>
                <Timeline>
                    {#each newsResult.items as newsItem}
                        <NewsItem {newsItem} />
                    {/each}
                </Timeline>
            {/if}
        </div>
        <div class="lg:w-96 flex flex-col gap-3">
            <SideChart chart={seedChart} />
            <SideChart chart={totalUsersChart} />
        </div>
    </div>
</div>
