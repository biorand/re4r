<script lang="ts">
    import { goto } from '$app/navigation';
    import { getWebsiteTitle } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import SignIn from '../auth/SignIn.svelte';

    const isSignedIn = getUserManager().isSignedIn();
    if (isSignedIn) {
        goto('/');
    }
</script>

<svelte:head>
    <title>{getWebsiteTitle('Sign In')}</title>
</svelte:head>

{#if !isSignedIn}
    <SignIn />
{/if}
