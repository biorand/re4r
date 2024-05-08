<script lang="ts" generics="T">
    import { type QueryResult } from './api';

    export let title;
    export let result: QueryResult<T> | undefined;
</script>

<div class="flex mb-3">
    <h1 class="grow text-4xl dark:text-white">{title}</h1>
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
