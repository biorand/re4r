<script lang="ts">
    import type { ProfileGroup, ProfileViewModel } from '$lib/UserProfileManager';
    import ProfileManagerItem from './ProfileManagerItem.svelte';

    export let groups: ProfileGroup[];
    export let selectedProfile: ProfileViewModel | undefined;

    function onSelectProfile(profile: ProfileViewModel) {
        selectedProfile = profile;
    }
</script>

<div class="d-flex flex-column h-100">
    {#each groups as profileGroup}
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
