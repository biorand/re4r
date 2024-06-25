<script lang="ts">
    import Chart from '$lib/Chart.svelte';
    import { Card } from 'flowbite-svelte';

    export let chart: any;
</script>

{#if chart}
    <Card padding="sm" size="none">
        <h5 class="mb-2 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
            {chart.series[0].name}
        </h5>
        <Chart options={chart}></Chart>
    </Card>
{/if}
