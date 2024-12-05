<script lang="ts">
    import { getWebsiteTitle, UserRole } from '$lib/api';
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
            case UserRole.Standard:
            case UserRole.Patron:
            case UserRole.LongTermSupporter:
            case UserRole.Tester:
            case UserRole.Administrator:
                return false;
            default:
                return true;
        }
    }
</script>

<svelte:head>
    {#if isSignedIn}
        <title>{getWebsiteTitle('Home')}</title>
    {:else}
        <title>{getWebsiteTitle('Sign Up')}</title>
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
