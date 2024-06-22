<script lang="ts">
    import type { NewsItem } from '$lib/api';
    import { TimelineItem } from 'flowbite-svelte';

    export let newsItem: NewsItem;
</script>

<TimelineItem title={newsItem.title} date={newsItem.date}>
    <p class="mb-4 text-base font-normal text-gray-500 dark:text-gray-400">
        {@html newsItem.body}
    </p>
</TimelineItem>

<style>
    p :global(ul) {
        margin: 12px 0 12px 24px;
        list-style: disc;
    }
</style>
