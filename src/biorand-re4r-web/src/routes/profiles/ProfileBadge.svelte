<script lang="ts">
    import { Tooltip } from 'flowbite-svelte';

    export let active = false;
    export let tooltip = '';
</script>

<button
    class="{active
        ? 'bg-gray-100 text-gray-800 dark:text-blue-400 border-blue-400'
        : 'bg-blue-100 text-blue-800 dark:text-gray-400 border-gray-500'} border inline-flex items-center px-2.5 py-0.5 rounded text-lg min-w-16 text-gray-600 dark:text-white dark:bg-gray-700"
    on:click
>
    <slot />
</button>
{#if tooltip}
    <Tooltip>{tooltip}</Tooltip>
{/if}
