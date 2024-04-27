<script lang="ts">
    import { getUserManager } from '$lib/userManager';
    import Auth from './Auth.svelte';
    import Generate from './Generate.svelte';

    const userManager = getUserManager();
    let isSignedIn = userManager.isSignedIn();
    userManager.subscribe(() => {
        isSignedIn = userManager.isSignedIn();
    });
</script>

<div class="container-fluid">
    {#if isSignedIn}
        <Generate />
    {:else}
        <Auth />
    {/if}
</div>
