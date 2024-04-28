<script lang="ts">
    import type { UserProfileManager } from '$lib/UserProfileManager';
    import { type Profile } from '$lib/api';

    export let userProfileManager: UserProfileManager;
    const profileGroups = userProfileManager.profileGroups;
    const selectedProfile = userProfileManager.selectedProfile;

    function updateProfiles(newProfiles: Profile[], newSelectedProfile?: Profile) {
        // profiles = newProfiles.toSorted((a, b) => a.name.localeCompare(b.name));
        // if (newSelectedProfile) selectProfile(newSelectedProfile);
        // profileGroups = groupProfiles(profiles);
    }

    function selectProfile(profile: Profile) {
        selectedProfile.set(profile);
    }

    function duplicateProfile(profile: Profile) {
        // const newProfile = {
        //     ...profile,
        //     name: profile.name + ' - Copy',
        //     category: 'Custom',
        //     isReadOnly: false
        // };
        // updateProfiles(profiles.concat(newProfile), newProfile);
    }

    function renameProfile(profile: Profile, value: string) {
        // const newProfile = { ...profile, name: value };
        // updateProfiles(profiles.filter((x) => x != profile).concat(newProfile), newProfile);
    }

    function deleteProfile(profile: Profile) {
        // updateProfiles(profiles.filter((x) => x != profile));
        // if (selectedProfile == profile) {
        //     if (profiles.length === 0) selectedProfile = undefined;
        //     else selectedProfile = profiles[0];
        // }
    }

    function onRenameProfileEvent(profile: Profile, target: EventTarget | null) {
        // const value = (<HTMLInputElement>target)?.value;
        // renameProfile(profile, value);
    }
</script>

<div class="d-flex flex-column h-100">
    {#each $profileGroups as profileGroup}
        <div class="mb-3">
            <h4 class="text-2xl">{profileGroup.category}</h4>
            <ul class="mt-1 ml-4">
                {#each profileGroup.profiles as profile}
                    <li>
                        <button
                            class="w-full p-1 text-left {profile === $selectedProfile
                                ? 'bg-blue-500'
                                : 'hover:bg-gray-600'}"
                            on:click={() => selectProfile(profile)}
                        >
                            <div class="flex">
                                <div class="grow">
                                    <div>{profile.name}</div>
                                </div>
                                <div>
                                    {#if !profileGroup.isReadOnly}
                                        {#if profile.id !== 0}
                                            <button
                                                type="button"
                                                class="btn btn-sm btn-light"
                                                data-bs-toggle="tooltip"
                                                data-bs-title="Duplicate"
                                                on:click={(e) => {
                                                    duplicateProfile(profile);
                                                    e.stopPropagation();
                                                }}
                                            >
                                                <i class="bi bi-copy"></i></button
                                            >
                                        {/if}
                                        <button
                                            type="button"
                                            class="btn btn-sm btn-light"
                                            data-bs-toggle="tooltip"
                                            data-bs-title="Delete"
                                            on:click={(e) => {
                                                deleteProfile(profile);
                                                e.stopPropagation();
                                            }}
                                        >
                                            <i class="bi bi-save"></i></button
                                        >
                                    {/if}
                                </div>
                            </div>
                        </button>
                    </li>
                {/each}
            </ul>
        </div>
    {/each}
</div>
