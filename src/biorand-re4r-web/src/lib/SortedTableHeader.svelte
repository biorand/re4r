<script lang="ts" generics="T">
    import { TableHeadCell } from 'flowbite-svelte';
    import { CaretDownSolid, CaretUpSolid } from 'flowbite-svelte-icons';
    import { getContext } from 'svelte';
    import type { SortedTableContext, SortedTableOrder } from './SortedTable.svelte';

    export let key: string | undefined = undefined;

    const ctx = getContext<SortedTableContext<T>>('sorted-table');
    let order: SortedTableOrder;
    ctx.dataStore.subscribe(() => {
        order = key ? ctx.getOrder(key) : undefined;
    });

    $: nextOrder = order === 'asc' ? 'desc' : <SortedTableOrder>undefined;

    function onSort() {
        if (key) {
            ctx.sort(key, nextOrder);
        }
    }
</script>

<TableHeadCell on:click={onSort} class="select-none">
    {#if order === 'asc'}
        <CaretDownSolid class="inline w-4 h-4" />
    {:else if order === 'desc'}
        <CaretUpSolid class="inline w-4 h-4" />
    {/if}
    <slot />
</TableHeadCell>
