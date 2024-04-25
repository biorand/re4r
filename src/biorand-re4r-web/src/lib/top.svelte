<script lang="ts">
    import Toolbar from './+toolbar.svelte';
    import Auth from './auth.svelte';
    import ConfigView from './configView.svelte';
    import ProfileManagerView from './profileManagerView.svelte';
    import { getUserManager } from './userManager';

    const userManager = getUserManager();
    let isSignedIn = userManager.isSignedIn();
    userManager.subscribe(() => {
        isSignedIn = userManager.isSignedIn();
    });
</script>

<Toolbar />
<div class="container-fluid">
    {#if isSignedIn}
        <div class="pt-5">
            <div class="row">
                <div class="col-md-5" style="max-width: 512px;">
                    <ProfileManagerView />
                </div>
                <div class="col">
                    <ConfigView />
                </div>
            </div>
        </div>
    {:else}
        <Auth />
    {/if}
</div>
