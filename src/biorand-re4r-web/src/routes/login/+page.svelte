<script lang="ts">
    import { goto } from '$app/navigation';
    import { getUserManager } from '$lib/userManager';
    import SignIn from '../auth/SignIn.svelte';

    const isSignedIn = getUserManager().isSignedIn();
    if (isSignedIn) {
        goto('/');
    }
</script>

{#if !isSignedIn}
    <SignIn />
{/if}
