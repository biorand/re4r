<script lang="ts" context="module">
    export type SortedTableOrder = 'asc' | 'desc' | undefined;
    export interface SortedTableData<T> {
        sort: string | undefined;
        order: SortedTableOrder;
        items: T[];
    }
    export interface SortedTableContext<T> {
        dataStore: Writable<SortedTableData<T>>;
        getOrder: (key: string) => SortedTableOrder;
        sort: (sort: string, order: SortedTableOrder) => void;
    }
</script>

<script lang="ts" generics="T">
    import { Table, TableBody } from 'flowbite-svelte';
    import { createEventDispatcher, setContext } from 'svelte';
    import { writable, type Writable } from 'svelte/store';

    export let data: SortedTableData<T>;

    let dataStore = writable(data);
    $: dataStore.set(data);

    const dispatch = createEventDispatcher();

    setContext('sorted-table', <SortedTableContext<T>>{
        dataStore,
        getOrder,
        sort
    });

    function getOrder(key: string): SortedTableOrder {
        if (key && data.sort === key) {
            if (data.order === 'desc') return 'desc';
            else return 'asc';
        } else {
            return undefined;
        }
    }
    function sort(sort: string, order: SortedTableOrder) {
        dispatch('sort', {
            sort,
            order
        });
    }
</script>

<Table class="mb-3">
    <slot name="header" />
    <TableBody tableBodyClass="divide-y">
        {#each data.items as item}
            <slot {item} />
        {/each}
    </TableBody>
</Table>
