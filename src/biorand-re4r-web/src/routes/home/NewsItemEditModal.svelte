<script lang="ts">
    import DateTimeInput from '$lib/DateTimeInput.svelte';
    import type { NewsItem } from '$lib/api';
    import { Button, Input, Label, Modal, Textarea } from 'flowbite-svelte';
    import { createEventDispatcher } from 'svelte';

    const dispatch = createEventDispatcher();

    export let open = false;
    export let newsItem: NewsItem | undefined;
</script>

{#if newsItem}
    <Modal bind:open size="lg" autoclose={false} class="w-full">
        <form class="flex flex-col space-y-6" on:submit={() => dispatch('save')}>
            <h3 class="mb-4 text-xl font-medium text-gray-900 dark:text-white">
                {#if newsItem.id === 0}
                    Create News Item
                {:else}
                    Edit News Item
                {/if}
            </h3>
            <Label class="space-y-2">
                <span>Date</span>
                <DateTimeInput
                    type="datetime-local"
                    name="date"
                    required
                    bind:value={newsItem.timestamp}
                />
            </Label>
            <Label class="space-y-2">
                <span>Title</span>
                <Input type="text" name="text" required bind:value={newsItem.title} />
            </Label>
            <Label class="space-y-2">
                <span>Body</span>
                <Textarea class="font-mono" rows="5" bind:value={newsItem.body}></Textarea>
            </Label>
            <Button type="submit" class="w-full1">Save changes</Button>
        </form>
    </Modal>
{/if}
