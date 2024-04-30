<script lang="ts">
    import { getUserManager } from '$lib/userManager';
    import SignUp from './auth/SignUp.svelte';
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
    <SignUp />
{/if}
