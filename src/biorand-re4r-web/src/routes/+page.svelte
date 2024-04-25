<script lang="ts">
    import Auth from '$lib/auth.svelte';
    import ConfigView from '$lib/configView.svelte';
    import ProfileManagerView from '$lib/profileManagerView.svelte';
    import { getUserManager } from '$lib/userManager';

    const userManager = getUserManager();
</script>

<header class="fixed-top">
    <nav class="navbar bg-body-tertiary">
        <div class="container-fluid">
            <span class="navbar-brand mb-0 h1">BioRand for Resident Evil 4 (2023)</span>
        </div>
    </nav>
</header>
<div class="container-fluid">
    {#if userManager.isSignedIn()}
        <div class="row">
            <div class="col-md-5" style="max-width: 512px;">
                <ProfileManagerView />
            </div>
            <div class="col">
                <ConfigView />
            </div>
        </div>
    {:else}
        <Auth />
    {/if}
</div>
