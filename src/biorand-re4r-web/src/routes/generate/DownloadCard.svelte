<script lang="ts">
    import { Button, Card } from 'flowbite-svelte';
    import { DownloadOutline } from 'flowbite-svelte-icons';

    export let title: string;
    export let description: string;
    export let href: string;
</script>

<Card>
    <div class="flex flex-col h-full">
        <h5 class="mb-2 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
            {title}
        </h5>
        <p class="grow mb-3 font-normal text-gray-700 dark:text-gray-400 leading-tight">
            {description}
        </p>
        <Button class="w-fit" color="green" {href}>
            Download <DownloadOutline class="w-6 h-6 ms-2 text-white" />
        </Button>
    </div>
</Card>
