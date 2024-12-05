<script lang="ts">
    import { getWebsiteTitle } from '$lib/api';
    import Generate from './Generate.svelte';
</script>

<svelte:head>
    <title>{getWebsiteTitle('Generate')}</title>
</svelte:head>

<div class="w-full my-2 font-light">
    <Generate />
</div>
