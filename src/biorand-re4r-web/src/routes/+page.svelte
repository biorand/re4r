<script lang="ts">
    import { UserRole } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import UserBanner from './UserBanner.svelte';
    import SignUp from './auth/SignUp.svelte';
    import Generate from './generate/Generate.svelte';

    const userManager = getUserManager();
    let isSignedIn = userManager.isSignedIn();
    let role = userManager.info?.role;
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

{#if isSignedIn}
    {#if showBanner}
        <UserBanner {role} />
    {:else}
        <Generate />
    {/if}
{:else}
    <SignUp />
{/if}
