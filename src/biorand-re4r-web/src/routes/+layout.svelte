<script lang="ts">
    import { type User } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import { Spinner } from 'flowbite-svelte';
    import '../app.pcss';
    import Footer from './Footer.svelte';
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

    // Disable showing seed stats on toolbar since polling is too stressful on server
    let stats = undefined;
    // const lsManager = getLocalStorageManager();
    // let stats = lsManager.get<StatsResult>(LocalStorageKeys.Stats);
    // let refreshStats = async () => {
    //     try {
    //         const api = getApi();
    //         stats = await api.getStats(getGameId());
    //         lsManager.set(LocalStorageKeys.Stats, stats);
    //     } catch {}
    // };
    // refreshStats();
    // setInterval(() => refreshStats(), 60000);
</script>

{#await init}
    <div class="container mx-auto p-3">
        <Spinner class="mx-auto block" />
    </div>
{:then}
    <Toolbar {currentUser} {stats} />
    <div class="min-h-screen flex flex-col">
        <div style="height: 73px;"></div>
        <div class="flex grow">
            <slot />
        </div>
        <Footer />
    </div>
{/await}
