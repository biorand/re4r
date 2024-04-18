<script lang="ts">
    import { afterUpdate, beforeUpdate } from 'svelte';

    interface Profile {
        name: string;
        category: string;
        isReadOnly: boolean;
    }

    function groupProfiles(profiles: Profile[]) {
        let profileGroups: { [key: string]: Profile[] } = {};
        for (const p of profiles) {
            let list = profileGroups[p.category];
            if (!list) {
                list = [];
                profileGroups[p.category] = list;
            }
            list.push(p);
        }
        const categoryOrder = ['Official', 'Custom', 'Community'];
        const getCategoryOrderId = (category: string) => {
            const result = categoryOrder.indexOf(category);
            return result == -1 ? categoryOrder.length : result;
        };
        return Object.entries(profileGroups)
            .map((x) => ({ category: x[0], profiles: x[1] }))
            .toSorted((a, b) => getCategoryOrderId(a.category) - getCategoryOrderId(b.category));
    }

    let profileNames = ['Dog', 'Texas', 'Chicken'];
    let profiles = profileNames
        .map((x) => {
            return {
                name: x,
                category: x == 'Chicken' ? 'Custom' : 'Official',
                isReadOnly: x == 'Dog'
            };
        })
        .toSorted((a, b) => a.name.localeCompare(b.name));
    let selectedProfile: Profile | undefined = profiles[0];
    let profileGroups = groupProfiles(profiles);

    function updateProfiles(newProfiles: Profile[], newSelectedProfile?: Profile) {
        profiles = newProfiles.toSorted((a, b) => a.name.localeCompare(b.name));
        if (newSelectedProfile) selectProfile(newSelectedProfile);
        profileGroups = groupProfiles(profiles);
    }

    function selectProfile(profile: Profile) {
        selectedProfile = profile;
    }

    function duplicateProfile(profile: Profile) {
        const newProfile = {
            ...profile,
            name: profile.name + ' - Copy',
            category: 'Custom',
            isReadOnly: false
        };
        updateProfiles(profiles.concat(newProfile), newProfile);
    }

    function renameProfile(profile: Profile, value: string) {
        const newProfile = { ...profile, name: value };
        updateProfiles(profiles.filter((x) => x != profile).concat(newProfile), newProfile);
    }

    function deleteProfile(profile: Profile) {
        updateProfiles(profiles.filter((x) => x != profile));
        if (selectedProfile == profile) {
            if (profiles.length === 0) selectedProfile = undefined;
            else selectedProfile = profiles[0];
        }
    }

    function onRenameProfileEvent(profile: Profile, target: EventTarget | null) {
        const value = (<HTMLInputElement>target)?.value;
        renameProfile(profile, value);
    }

    beforeUpdate(() => {
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        const tooltipList = [...tooltipTriggerList]
            // @ts-ignore
            .map((el) => bootstrap.Tooltip.getOrCreateInstance(el))
            .forEach((x) => x.hide());
    });

    afterUpdate(() => {
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltipTriggerList.forEach((el) =>
            // @ts-ignore
            bootstrap.Tooltip.getOrCreateInstance(el)
        );
    });
</script>

<div class="d-flex flex-column h-100">
    <div class="p-2"><h1>Profile Manager</h1></div>
    <div class="p-2 flex-grow-1">
        {#each profileGroups as profileGroup}
            <h4>{profileGroup.category}</h4>
            <ul class="list-group list-group-flush">
                {#each profileGroup.profiles as profile}
                    <button
                        class="list-group-item list-group-item-action p-1"
                        class:active={profile === selectedProfile}
                        on:click={() => selectProfile(profile)}
                    >
                        <div class="d-flex">
                            <div class="flex-grow-1 p-1">
                                {#if profile === selectedProfile && !profile.isReadOnly}
                                    <input
                                        on:change={(e) => onRenameProfileEvent(profile, e.target)}
                                        value={profile.name}
                                    />
                                {:else}
                                    <div>{profile.name}</div>
                                {/if}
                            </div>
                            <div>
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
                                {#if !profile.isReadOnly}
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
                                        <i class="bi bi-x"></i></button
                                    >
                                {/if}
                            </div>
                        </div>
                    </button>
                {/each}
            </ul>
        {/each}
    </div>
</div>

<style>
    input {
        border: 0;
        background: none;
        color: inherit;
    }

    input:focus-visible,
    input:focus,
    input:active {
        border: 0;
        outline: none;
    }

    ul:not(:last-child) {
        margin-bottom: 16px;
    }
</style>
