<script lang="ts">
    import { UserProfileManager } from '$lib/UserProfileManager';
    import { getApi, type ConfigDefinition } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import { onMount } from 'svelte';
    import ConfigPanel from './ConfigPanel.svelte';
    import ProfileManagerPanel from './ProfileManagerPanel.svelte';

    const api = getApi();
    const userManager = getUserManager();
    const profileManager = new UserProfileManager(api, userManager.info!.id);

    let configDefinition: ConfigDefinition | undefined = undefined;
    let profile = profileManager.selectedProfile;

    onMount(async () => {
        await profileManager.download();
        configDefinition = await api.getConfigDefinition();
    });
</script>

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
