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

<div class="container-fluid">
    <div class="row">
        <div class="col-lg-4" style="max-width: 512px;">
            <ProfileManagerPanel userProfileManager={profileManager} />
        </div>
        <div class="col-lg-8">
            <ConfigPanel definition={configDefinition} {profile} />
        </div>
    </div>
</div>
