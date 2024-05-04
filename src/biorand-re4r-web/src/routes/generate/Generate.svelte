<script lang="ts">
    import { UserProfileManager } from '$lib/UserProfileManager';
    import { getApi, type ConfigDefinition } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import { Alert, Spinner } from 'flowbite-svelte';
    import { CloseCircleSolid } from 'flowbite-svelte-icons';
    import ConfigPanel from './ConfigPanel.svelte';
    import ProfileManagerPanel from './ProfileManagerPanel.svelte';

    const api = getApi();
    const userManager = getUserManager();
    const profileManager = new UserProfileManager(api, userManager.info?.user.id || 0);

    let configDefinition: ConfigDefinition | undefined = undefined;
    let profile = profileManager.selectedProfile;

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
    <div class="mb-3">
        <div class="md:flex">
            <div class="md:w-1/3 md:max-w-lg m-2">
                <ProfileManagerPanel userProfileManager={profileManager} />
            </div>
            <div class="md:w-2/3 m-2">
                <ConfigPanel definition={configDefinition} {profile} />
            </div>
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
