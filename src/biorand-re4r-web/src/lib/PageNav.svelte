<script lang="ts">
    export let page: number;
    export let count: number;
    export let href: (i: number) => string;

    const prevPage = page == 1 ? 0 : page - 1;
    const nextPage = page == count ? 0 : page + 1;
    const pageNumbers = Array.from({ length: count }, (_, index) => index + 1);
</script>

<nav aria-label="Page navigation example">
    <ul class="pagination">
        <li class="page-item" class:disabled={!prevPage}>
            <a href={href(prevPage)} class="page-link" aria-label="Previous">
                <span aria-hidden="true">&laquo;</span>
            </a>
        </li>
        {#each pageNumbers as pageNumber}
            <li class="page-item" class:active={page == pageNumber}>
                <a href={href(pageNumber)} class="page-link">{pageNumber}</a>
            </li>
        {/each}
        <li class="page-item" class:disabled={!nextPage}>
            <a href={href(nextPage)} class="page-link" aria-label="Next">
                <span aria-hidden="true">&raquo;</span>
            </a>
        </li>
    </ul>
</nav>
