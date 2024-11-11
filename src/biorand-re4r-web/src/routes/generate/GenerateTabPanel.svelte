<script lang="ts">
    import LoadingButton from '$lib/LoadingButton.svelte';
    import type { ProfileViewModel } from '$lib/UserProfileManager';
    import { getApi, RandoStatus, type Rando } from '$lib/api';
    import { getLocalStorageManager } from '$lib/localStorage';
    import { rng } from '$lib/utility';
    import { Alert, Button, ButtonGroup, Hr, Input, Label } from 'flowbite-svelte';
    import { CloseCircleSolid, InfoCircleSolid, ShuffleOutline } from 'flowbite-svelte-icons';
    import DownloadCard from './DownloadCard.svelte';

    export let profile: ProfileViewModel;
    export let generateResult: Rando | undefined;

    let lsManager = getLocalStorageManager();
    let seed = generateResult?.seed.toString() || '0';
    let generating = false;
    let generateError = '';
    let generateProcessMessage = '';
    function onShuffleSeed() {
        seed = rng(100000, 1000000).toString();
    }
    if (!generateResult) {
        const savedSeed = lsManager.getNumber('seed');
        if (savedSeed) {
            seed = savedSeed.toString();
        } else {
            onShuffleSeed();
        }
    }

    $: lsManager.set('seed', seed);

    async function onGenerate() {
        generating = true;
        generateResult = undefined;
        generateError = '';
        try {
            const api = getApi();
            generateResult = await api.generate({
                seed: parseInt(seed) || 0,
                profileId: profile.originalId,
                config: profile.config || {}
            });
            generateProcessMessage = 'Seed is queued for generation';
            const timer = setInterval(async () => {
                try {
                    if (generateResult) {
                        generateResult = await api.getRando(generateResult.id);
                        switch (generateResult.status) {
                            case RandoStatus.Unassigned:
                                generateProcessMessage = 'Seed is queued for generation';
                                break;
                            case RandoStatus.Processing:
                                generateProcessMessage = 'Seed is being generated';
                                break;
                            case RandoStatus.Discarded:
                                generating = false;
                                generateProcessMessage = 'Seed was discarded for another';
                                clearInterval(timer);
                                break;
                            case RandoStatus.Completed:
                                generating = false;
                                generateProcessMessage = '';
                                clearInterval(timer);
                                break;
                            default:
                                generating = false;
                                generateError =
                                    'An error occured on the server while generating this seed.';
                                clearInterval(timer);
                                break;
                        }
                    } else {
                        generating = false;
                        generateError = '';
                        generateProcessMessage = '';
                        clearInterval(timer);
                    }
                } catch {
                    generateError = 'An error occured on the server while generating this seed.';
                    generating = false;
                }
            }, 1000);
        } catch (e: any) {
            generateError = 'An error occured on the server while generating this seed.';
            generating = false;
        }
    }
</script>

<div class="mb-3">
    <h1 class="text-3xl">{profile.name}</h1>
    <h2 class="ml-4 font-light text-gray-700 dark:text-gray-300">by {profile.userName}</h2>
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
{#if generateError}
    <Alert border color="red" class="my-4">
        <CloseCircleSolid slot="icon" class="w-5 h-5" />{generateError}
    </Alert>
{:else if generateResult}
    {#if generateResult.status == RandoStatus.Completed}
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
                    description="Drop this zip file into Fluffy Mod Manager's mod folder and enable it."
                    href={generateResult.downloadUrlMod}
                />
            </div>

            <p class="mt-3">What should I do if my game crashes?</p>
            <ol class="ml-8 list-decimal text-gray-300">
                <li>Reload from last checkpoint and try again.</li>
                <li>
                    Alter the enemy sliders slightly or reduce the number temporarily. This will
                    reshuffle the enemies. Reload from last checkpoint and try again.
                </li>
                <li>As a last resort, change your seed, and reload from last checkpoint.</li>
            </ol>
        </div>
    {:else}
        <Alert border color="yellow" class="my-4">
            <InfoCircleSolid slot="icon" class="w-5 h-5" />{generateProcessMessage}
        </Alert>
    {/if}
{/if}
