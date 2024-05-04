<script lang="ts">
    import LoadingButton from '$lib/LoadingButton.svelte';
    import type { ProfileViewModel } from '$lib/UserProfileManager';
    import { getApi, type GenerateResult } from '$lib/api';
    import { rng } from '$lib/utility';
    import { Alert, Button, ButtonGroup, Hr, Input, Label } from 'flowbite-svelte';
    import { CloseCircleSolid, ShuffleOutline } from 'flowbite-svelte-icons';
    import DownloadCard from './DownloadCard.svelte';

    export let profile: ProfileViewModel;

    let seed = 0;
    let generating = false;
    let generateResult: GenerateResult | undefined;
    let generateError = '';
    function onShuffleSeed() {
        seed = rng(100000, 1000000);
    }
    onShuffleSeed();

    async function onGenerate() {
        generating = true;
        generateResult = undefined;
        try {
            const api = getApi();
            generateResult = await api.generate({
                seed,
                profileId: profile.originalId,
                config: profile.config || {}
            });
            generating = false;
        } catch {
            generateError = 'An error occured on the server while generating this seed.';
            generating = false;
        }
    }
</script>

<div class="mb-3">
    <h1 class="text-3xl">{profile.name}</h1>
    <h2 class="ml-4 font-light text-gray-300">by {profile.userName}</h2>
</div>
<div class="mb-6">
    <Label class="mb-2" for="txt-profile-description">Seed</Label>
    <div class="flex flex-col sm:flex-row gap-3">
        <div class="max-w-56">
            <ButtonGroup class="w-full" size="sm">
                <Input id="seed" type="text" bind:value={seed} />
                <Button on:click={onShuffleSeed} color="primary"><ShuffleOutline /></Button>
            </ButtonGroup>
        </div>
        <div class="w-full sm:w-56">
            <LoadingButton
                on:click={onGenerate}
                loading={generating}
                class="w-full"
                color="primary"
                size="sm">Generate</LoadingButton
            >
        </div>
    </div>
</div>
<Hr hrClass="h-px my-4 bg-gray-200 border-0 dark:bg-gray-700" />
{#if generateResult}
    <div>
        <h2 class="text-2xl">Your randomizer is ready!</h2>
        <h3 class="mb-3">Download the appropriate file and enjoy!</h3>
        <div class="flex flex-wrap gap-3">
            <DownloadCard
                title="Patch"
                description="Simply drop this file into your RE 4 install folder."
                href={generateResult.downloadUrl}
            />
            <DownloadCard
                title="Fluffy Mod"
                description="Add this to Fluffy Mod manager and enable it."
                href={generateResult.downloadUrlMod}
            />
        </div>
    </div>
{:else if generateError}
    <Alert border color="red" class="my-4">
        <CloseCircleSolid slot="icon" class="w-5 h-5" />{generateError}
    </Alert>
{/if}
