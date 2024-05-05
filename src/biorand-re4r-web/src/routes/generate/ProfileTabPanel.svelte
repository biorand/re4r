<script lang="ts">
    import type { ProfileViewModel } from '$lib/UserProfileManager';
    import { Input, Label, Textarea, Toggle } from 'flowbite-svelte';

    export let profile: ProfileViewModel;
</script>

<div class="mb-3">
    <Label class="mb-2" for="txt-profile-name">Name</Label>
    <Input
        type="text"
        id="txt-profile-name"
        readOnly={!profile.isOwner}
        bind:value={profile.name}
    />
</div>
<div class="mb-3">
    <Label class="mb-2" for="txt-profile-description">Description</Label>
    <Textarea
        id="txt-profile-description"
        rows="3"
        readOnly={!profile.isOwner}
        bind:value={profile.description}
    />
</div>
{#if profile.isOwner}
    <div class="mb-3">
        <Toggle bind:checked={profile.public}>Share profile with community</Toggle>
    </div>
{/if}
<div class="mb-3">Stars: {profile.starCount}</div>
<div class="mb-3">Seeds: {profile.seedCount}</div>
