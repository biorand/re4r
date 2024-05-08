<script lang="ts">
    import { isCustomApi, isLocalhost, switchApi } from '$lib/api';
    import { GlobeSolid, HomeSolid } from 'flowbite-svelte-icons';

    let showGlobe = isCustomApi() || !isLocalhost();

    function onClick() {
        if (isCustomApi()) {
            switchApi(true);
        } else {
            switchApi(false);
        }
        showGlobe = isCustomApi();
    }
</script>

<button
    on:click={onClick}
    class="text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none rounded-lg text-sm p-2.5"
    type="button"
>
    {#if showGlobe}
        <GlobeSolid />
    {:else}
        <HomeSolid />
    {/if}
</button>
