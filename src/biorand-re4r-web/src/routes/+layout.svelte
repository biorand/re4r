<script lang="ts">
    import type { UserAuthInfo } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import '../app.pcss';
    import Toolbar from './Toolbar.svelte';

    let currentUser: UserAuthInfo | undefined = undefined;
    let init = (async () => {
        const userManager = getUserManager();
        userManager.subscribe(() => {
            currentUser = userManager.info;
        });
        currentUser = userManager.info;
    })();
</script>

{#await init then}
    <Toolbar {currentUser} />
    <div style="margin-top: 74px;">
        <slot />
    </div>
{/await}
