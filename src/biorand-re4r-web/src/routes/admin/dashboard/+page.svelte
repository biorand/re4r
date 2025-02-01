<script lang="ts">
    import type { InfoResult } from '$lib/api';
    import { getApi, getWebsiteTitle } from '$lib/api';
    import { PageBody, PageTitle } from '$lib/typography';

    let info: InfoResult | undefined;
    const refresh = async () => {
        const api = getApi();
        info = await api.getInfo();
    };
    refresh();
</script>

<svelte:head>
    <title>{getWebsiteTitle('Admin Dashboard')}</title>
</svelte:head>

<PageBody>
    <PageTitle class="grow">Admin Dashboard</PageTitle>
    {#if info}
        {#each info.generators as generator}
            {generator.gameId}
        {/each}
    {/if}
</PageBody>
