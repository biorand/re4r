<script lang="ts">
    import type { User } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import '../app.pcss';
    import Toolbar from './Toolbar.svelte';

    let currentUser: User | undefined = undefined;
    let init = (async () => {
        const userManager = getUserManager();
        userManager.subscribe(() => {
            currentUser = userManager.info?.user;
        });
        currentUser = userManager.info?.user;
        await userManager.refresh();
    })();
</script>

{#await init then}
    <Toolbar {currentUser} />
    <div style="margin-top: 74px;">
        <slot />
    </div>
{/await}
