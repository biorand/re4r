<script lang="ts">
    import type { NewsItem } from '$lib/api';
    import { Button, Input, Label, Modal, Textarea } from 'flowbite-svelte';

    export let open = false;
    export let newsItem: NewsItem | undefined;

    $: dateTimeValue = unix2datetime(newsItem?.timestamp || 0);

    function unix2datetime(unixTimestamp: number) {
        const date = new Date(unixTimestamp * 1000);
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hour = String(date.getHours()).padStart(2, '0');
        const min = String(date.getMinutes()).padStart(2, '0');
        return `${year}-${month}-${day}T${hour}:${min}`;
    }
</script>

{#if newsItem}
    <Modal bind:open size="lg" autoclose={false} class="w-full">
        <form class="flex flex-col space-y-6" action="#">
            <h3 class="mb-4 text-xl font-medium text-gray-900 dark:text-white">Edit News Item</h3>
            <Label class="space-y-2">
                <span>Date</span>
                <Input type="datetime-local" name="date" required value={dateTimeValue} />
            </Label>
            <Label class="space-y-2">
                <span>Title</span>
                <Input type="text" name="text" required value={newsItem.title} />
            </Label>
            <Label class="space-y-2">
                <span>Body</span>
                <Textarea class="font-mono" rows="5" value={newsItem.body}></Textarea>
            </Label>
            <Button type="submit" class="w-full1">Save changes</Button>
        </form>
    </Modal>
{/if}
