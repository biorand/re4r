<script lang="ts">
    import type { ProfileViewModel, UserProfileManager } from '$lib/UserProfileManager';
    import ProfileManagerItem from './ProfileManagerItem.svelte';

    export let userProfileManager: UserProfileManager;
    const profileGroups = userProfileManager.profileGroups;

    function onSelectProfile(profile: ProfileViewModel) {
        userProfileManager.selectedProfile.set(profile);
    }
</script>

<div class="d-flex flex-column h-100">
    {#each $profileGroups as profileGroup}
        <div class="mb-3">
            <h4 class="text-2xl">{profileGroup.category}</h4>
            <ul class="mt-1 ml-4">
                {#each profileGroup.profiles as profile}
                    <ProfileManagerItem {profile} on:click={() => onSelectProfile(profile)} />
                {/each}
            </ul>
        </div>
    {/each}
</div>
