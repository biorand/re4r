<script lang="ts" generics="T">
    import { type QueryResult } from './api';
    import PageTitle from './typography/PageTitle.svelte';

    export let title;
    export let result: QueryResult<T> | undefined;
</script>

<div class="flex mb-3">
    <PageTitle class="grow">{title}</PageTitle>
    {#if result}
        <span class="content-end">
            {#if result.pageResults.length > 0}
                {result.pageStart}-{result.pageEnd} of {result.totalResults}
            {:else}
                {result.totalResults} results
            {/if}
        </span>
    {/if}
</div>
