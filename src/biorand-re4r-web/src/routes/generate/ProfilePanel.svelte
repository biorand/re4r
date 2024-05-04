<script lang="ts">
    import type { ProfileViewModel } from '$lib/UserProfileManager';
    import { Input, Label, Textarea } from 'flowbite-svelte';

    export let profile: ProfileViewModel;

    let name = profile.name;
    let description = profile.description;
    $: {
        if (profile.name !== name || profile.description !== description) {
            profile = {
                ...profile,
                isModified: true,
                name,
                description
            };
        }
    }
</script>

<div class="mb-3">
    <Label class="mb-2" for="txt-profile-name">Name</Label>
    <Input type="text" id="txt-profile-name" bind:value={name} />
</div>
<div class="mb-3">
    <Label class="mb-2" for="txt-profile-description">Description</Label>
    <Textarea id="txt-profile-description" rows="3" bind:value={description} />
</div>
<div class="mb-3">Stars: {profile.starCount}</div>
<div class="mb-3">Seeds: {profile.seedCount}</div>
