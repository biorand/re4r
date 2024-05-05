<script lang="ts">
    import { UserProfileManager } from '$lib/UserProfileManager';
    import { getApi, type ConfigDefinition } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import { Alert, Spinner } from 'flowbite-svelte';
    import { CloseCircleSolid } from 'flowbite-svelte-icons';
    import ProfileManagerPanel from './ProfileManagerPanel.svelte';
    import ProfilePanel from './ProfilePanel.svelte';

    const api = getApi();
    const userManager = getUserManager();
    const profileManager = new UserProfileManager(api, userManager.info?.user.id || 0);

    let configDefinition: ConfigDefinition | undefined = undefined;
    let profileGroups = profileManager.profileGroups;
    let selectedProfile = profileManager.selectedProfile;

    let init = (async () => {
        await profileManager.download();
        configDefinition = await api.getConfigDefinition();
    })();
</script>

{#await init}
    <div class="container mx-auto p-3">
        <Spinner class="mx-auto block" />
    </div>
{:then}
    <div class="lg:flex grow">
        <div class="lg:w-1/3 lg:max-w-lg p-2 border-r border-gray-100 dark:border-gray-700">
            <ProfileManagerPanel groups={$profileGroups} bind:selectedProfile={$selectedProfile} />
        </div>
        <div class="lg:grow m-2">
            {#if configDefinition && $selectedProfile}
                <ProfilePanel definition={configDefinition} bind:profile={$selectedProfile} />
            {/if}
        </div>
    </div>
{:catch}
    <div class="container mx-auto p-3">
        <Alert border color="red" class="my-4">
            <CloseCircleSolid slot="icon" class="w-5 h-5" />
            There was an error downloading your profiles from the server.
        </Alert>
    </div>
{/await}
