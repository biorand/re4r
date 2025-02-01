<script lang="ts">
    import { getGameMoniker } from '$lib/api';
    import ErrorModal, { type ErrorModalContent } from '$lib/ErrorModal.svelte';
    import LoadFileLink from '$lib/LoadFileLink.svelte';
    import type {
        ProfileGroup,
        ProfileViewModel,
        UserProfileManager
    } from '$lib/UserProfileManager';
    import ProfileManagerItem from './ProfileManagerItem.svelte';

    export let profileManager: UserProfileManager;
    export let groups: ProfileGroup[];
    export let selectedProfile: ProfileViewModel | undefined;

    let importError: ErrorModalContent | undefined;

    function onSelectProfile(profile: ProfileViewModel) {
        selectedProfile = profile;
    }

    function importProfile(e: CustomEvent) {
        try {
            const importedProfile = JSON.parse(e.detail.content);
            const currentMoniker = getGameMoniker();
            if (importedProfile.game != currentMoniker) {
                importError = {
                    title: 'Import Profile',
                    body: 'Profile invalid or for a different game.'
                };
                return;
            }

            profileManager.importProfile(importedProfile);
        } catch (e) {
            importError = {
                title: 'Import Profile',
                body: 'Failed to import profile'
            };
        }
    }
</script>

<div class="d-flex flex-column h-100">
    {#each groups as profileGroup}
        <div class="mb-3">
            <h4 class="text-lg">{profileGroup.category}</h4>
            <ul class="text-sm mt-1 ml-4">
                {#each profileGroup.profiles as profile}
                    <ProfileManagerItem {profile} on:click={() => onSelectProfile(profile)} />
                {/each}
            </ul>
        </div>
    {/each}
    <div class="ml-3 text-center">
        <LoadFileLink class="mx-auto text-green-300 hover:text-green-400" on:change={importProfile}
            >Import profile</LoadFileLink
        >
        &bullet;
        <a class="mx-auto text-green-300 hover:text-green-400" href="/profiles"
            >Discover more profiles</a
        >
    </div>
</div>
<ErrorModal error={importError} />
