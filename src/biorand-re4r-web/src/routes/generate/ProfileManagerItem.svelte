<script lang="ts">
    import type { ProfileViewModel } from '$lib/UserProfileManager';
    import {
        BookmarkOutline,
        FileCopySolid,
        FloppyDiskSolid,
        TrashBinSolid
    } from 'flowbite-svelte-icons';
    import ProfileManagerItemButton from './ProfileManagerItemButton.svelte';

    export let profile: ProfileViewModel;
</script>

<li>
    <button
        class="w-full p-1 text-left {profile.isSelected ? 'bg-blue-900' : 'hover:bg-gray-600'}"
        on:click
    >
        <div class="flex">
            <div class="grow flex">
                <div>{profile.name}</div>
                {#if profile.isModified}
                    <div class="ml-2 text-red-300">[modified]</div>
                {/if}
            </div>
            {#if profile.isSelected}
                <div class="flex gap-1">
                    {#if profile.onSave}
                        <ProfileManagerItemButton
                            on:click={profile.onSave}
                            color="green"
                            icon={FloppyDiskSolid}
                            tooltip="Save"
                        />
                    {/if}
                    {#if profile.onDuplicate}
                        <ProfileManagerItemButton
                            on:click={profile.onDuplicate}
                            color="blue"
                            icon={FileCopySolid}
                            tooltip="Duplicate"
                        />
                    {/if}
                    {#if profile.onDelete}
                        <ProfileManagerItemButton
                            on:click={profile.onDelete}
                            color="red"
                            icon={TrashBinSolid}
                            tooltip="Delete"
                        />
                    {/if}
                    {#if profile.onRemove}
                        <ProfileManagerItemButton
                            on:click={profile.onRemove}
                            color="red"
                            icon={BookmarkOutline}
                            tooltip="Remove"
                        />
                    {/if}
                </div>
            {/if}
        </div>
    </button>
</li>
