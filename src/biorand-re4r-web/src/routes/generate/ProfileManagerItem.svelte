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

<li
    class="w-full text-left {profile.isSelected
        ? 'bg-blue-200 dark:bg-blue-900'
        : 'hover:bg-gray-300 dark:hover:bg-gray-600'}"
>
    <div class="flex">
        <button class="grow flex p-1" on:click>
            <div>
                <span class={profile.public ? 'font-semibold text-blue-800 dark:text-blue-200' : ''}
                    >{profile.name}</span
                >
                <span class="text-gray-800 dark:text-gray-400 text-sm font-light"
                    >by {profile.userName}</span
                >
            </div>
            {#if profile.isModified}
                <div class="ml-2 font-light text-red-900 dark:text-red-300">[modified]</div>
            {/if}
        </button>
        {#if profile.isSelected}
            <div class="flex gap-1 p-1">
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
</li>
