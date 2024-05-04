<script lang="ts">
    import { Button, Tooltip } from 'flowbite-svelte';

    export let tooltip = '';
    export let icon: any;
</script>

<Button class="px-2 py-1 rounded-sm" on:click {...$$restProps}
    ><svelte:component this={icon} class="w-3 h-3" /></Button
>
{#if tooltip}
    <Tooltip>{tooltip}</Tooltip>
{/if}
