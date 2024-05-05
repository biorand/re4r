<script lang="ts">
    import { Pagination } from 'flowbite-svelte';
    import { ChevronLeftOutline, ChevronRightOutline } from 'flowbite-svelte-icons';

    export let page = 1;
    export let pageCount = 1;
    export let href = (pageNumber: number) => '/';

    $: pages = enumerate(1, pageCount).map((i) => ({
        name: i.toString(),
        href: href(i),
        active: page == i
    }));

    function enumerate(start: number, count: number) {
        let p = [];
        for (let i = 0; i < count; i++) {
            p.push(start + i);
        }
        return p;
    }

    function onPrevious() {
        if (page > 1) {
            window.location.href = href(page - 1);
        }
    }

    function onNext() {
        if (page < pageCount) {
            window.location.href = href(page + 1);
        }
    }
</script>

<Pagination {pages} on:previous={onPrevious} on:next={onNext} icon>
    <svelte:fragment slot="prev">
        <span class="sr-only">Previous</span>
        <ChevronLeftOutline class="w-6 h-6" />
    </svelte:fragment>
    <svelte:fragment slot="next">
        <span class="sr-only">Next</span>
        <ChevronRightOutline class="w-6 h-6" />
    </svelte:fragment>
</Pagination>
