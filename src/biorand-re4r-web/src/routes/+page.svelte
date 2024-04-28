<script lang="ts">
    import { getUserManager } from '$lib/userManager';
    import Auth from './auth/Auth.svelte';
    import Generate from './generate/Generate.svelte';

    const userManager = getUserManager();
    let isSignedIn = userManager.isSignedIn();
    userManager.subscribe(() => {
        isSignedIn = userManager.isSignedIn();
    });
</script>

{#if isSignedIn}
    <Generate />
{:else}
    <Auth />
{/if}
