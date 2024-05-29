<script lang="ts" generics="T">
    import BioRandPagination from './BioRandPagination.svelte';
    import { type QueryResult } from './api';

    export let result: QueryResult<T>;
    export let getPageUrl: (page: number) => string;
</script>

<div class="flex justify-center mb-2">
    <div class="text-sm text-gray-700 dark:text-gray-400">
        Showing
        {#if result.pageResults.length > 0}
            <span class="font-semibold text-gray-900 dark:text-white">{result.pageStart}</span>
            to <span class="font-semibold text-gray-900 dark:text-white">{result.pageEnd}</span> of
            <span class="font-semibold text-gray-900 dark:text-white">{result.totalResults}</span>
        {:else}
            <span class="font-semibold text-gray-900 dark:text-white">{result.totalResults}</span>
        {/if}
        Results
    </div>
</div>
<BioRandPagination page={result.page} pageCount={result.pageCount} href={getPageUrl} />
