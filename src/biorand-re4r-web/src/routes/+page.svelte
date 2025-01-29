<script lang="ts">
    import { getWebsiteTitle } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import UserBanner from './UserBanner.svelte';
    import SignUp from './auth/SignUp.svelte';
    import Home from './home/Home.svelte';

    const userManager = getUserManager();
    let isSignedIn = userManager.isSignedIn();
    let userTags = userManager.info?.user?.tags || [];
    let showBanner = shouldShowBanner(userTags);
    userManager.subscribe(() => {
        isSignedIn = userManager.isSignedIn();
    });

    function shouldShowBanner(userTags: string[]) {
        const list = ['pending', 'banned', 'system'];
        return userTags.findIndex((x) => list.indexOf(x) != -1) != -1;
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
        <UserBanner {userTags} />
    {:else}
        <Home />
    {/if}
{:else}
    <SignUp />
{/if}
