<script lang="ts">
    import { goto } from '$app/navigation';
    import { Pagination } from 'flowbite-svelte';
    import { ChevronLeftOutline, ChevronRightOutline } from 'flowbite-svelte-icons';

    export let page = 1;
    export let pageCount = 1;
    export let href = (pageNumber: number) => '/';

    $: pages = getPageNumbers(page, pageCount).map((i) => {
        if (i === null) {
            return {
                name: '...'
            };
        } else {
            return {
                name: i.toString(),
                href: href(i),
                active: page == i
            };
        }
    });

    function getPageNumbers(page: number, count: number) {
        const numbers = [
            ...new Set([...enumerate(Math.max(1, page - 2), 5), 1, 2, count - 1, count])
        ]
            .filter((x) => x >= 1 && x <= count)
            .sort((a, b) => a - b);
        const result: (number | null)[] = [];
        let lastNumber = 0;
        for (let i = 0; i < numbers.length; i++) {
            const curr = numbers[i];
            if (curr > lastNumber + 1) {
                result.push(null);
            }
            result.push(curr);
            lastNumber = curr;
        }
        return result;
    }

    function enumerate(start: number, count: number) {
        let p = [];
        for (let i = 0; i < count; i++) {
            p.push(start + i);
        }
        return p;
    }

    function onPrevious() {
        if (page > 1) {
            goto(href(page - 1));
        }
    }

    function onNext() {
        if (page < pageCount) {
            goto(href(page + 1));
        }
    }
</script>

<div class="flex justify-center">
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
</div>
