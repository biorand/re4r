<script lang="ts">
    import { type NewsItem } from '$lib/api';
    import { Button, TimelineItem } from 'flowbite-svelte';
    import { EditOutline, TrashBinOutline } from 'flowbite-svelte-icons';
    import { createEventDispatcher } from 'svelte';

    const dispatch = createEventDispatcher();

    export let newsItem: NewsItem;
    export let canEdit = false;
</script>

<div class="flex">
    <div class="grow">
        <TimelineItem title={newsItem.title} date={newsItem.date}>
            <p class="mb-4 text-base font-normal text-gray-500 dark:text-gray-400">
                {@html newsItem.body}
            </p>
        </TimelineItem>
    </div>
    {#if canEdit}
        <div>
            <div class="flex gap-2">
                <Button on:click={() => dispatch('edit')} color="light" class="!p-2"
                    ><EditOutline class="w-6 h-6" /></Button
                >
                <Button on:click={() => dispatch('delete')} color="red" class="!p-2"
                    ><TrashBinOutline class="w-6 h-6" /></Button
                >
            </div>
        </div>
    {/if}
</div>

<style>
    p :global(ul) {
        margin: 12px 0 12px 24px;
        list-style: disc;
    }
</style>
