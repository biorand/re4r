<script lang="ts">
    import { UserRole } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import UserBanner from './UserBanner.svelte';
    import SignUp from './auth/SignUp.svelte';
    import Home from './home/Home.svelte';

    const userManager = getUserManager();
    let isSignedIn = userManager.isSignedIn();
    let role = userManager.info?.user?.role;
    let showBanner = shouldShowBanner(role);
    userManager.subscribe(() => {
        isSignedIn = userManager.isSignedIn();
    });

    function shouldShowBanner(role: UserRole | undefined) {
        switch (role) {
            case UserRole.EarlyAccess:
            case UserRole.Tester:
            case UserRole.Standard:
            case UserRole.Administrator:
                return false;
            default:
                return true;
        }
    }
</script>

<svelte:head>
    {#if isSignedIn}
        <title>Generate - BioRand 4</title>
    {:else}
        <title>Sign Up - BioRand 4</title>
    {/if}
</svelte:head>

{#if isSignedIn}
    {#if showBanner}
        <UserBanner {role} />
    {:else}
        <Home />
    {/if}
{:else}
    <SignUp />
{/if}
