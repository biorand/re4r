<script lang="ts">
    import { getGameId } from '$lib/api';
    import { Alert } from 'flowbite-svelte';
    import { CheckCircleSolid, InfoCircleSolid } from 'flowbite-svelte-icons';
    import PatronBenefits from './PatronBenefits.svelte';

    export let isMember = false;
    export let showBenefits = true;

    const gameId = getGameId();
    const recipient = gameId == 2 ? 'namsku' : 'intelorca';
    const recipientDisplay = gameId == 2 ? 'Namsku' : 'IntelOrca';
    const gameNumber = gameId == 2 ? 'II' : 'IV';
</script>

{#if isMember}
    <Alert color="green" border class="mt-3 p-2">
        <CheckCircleSolid slot="icon" class="w-5 h-5" />
        <span class="font-medium">BioRand Patron </span>
    </Alert>
{:else}
    <a target="_blank" href="http://ko-fi.com/{recipient}/tiers"
        ><img
            class="h-10"
            alt="Support me on Ko-Fi"
            src="https://storage.ko-fi.com/cdn/brandasset/kofi_button_blue.png"
        /></a
    >
    <Alert color="dark" border class="mt-3 p-2 !items-start">
        <span slot="icon">
            <InfoCircleSolid slot="icon" class="w-5 h-5" />
            <span class="sr-only">Info</span>
        </span>
        {#if !showBenefits}
            <p class="font-medium">Ko-fi can be used as an alternative to a Twitch subscription.</p>
        {:else}
            <p class="font-medium">Become a BioRand {gameNumber} patron:</p>
            <ul class="mt-1.5 ms-4 list-disc list-inside">
                <li>
                    Support <a
                        class="font-medium text-blue-400 hover:text-blue-300"
                        href="http://ko-fi.com/{recipient}/tiers"
                        target="_blank">{recipientDisplay}</a
                    > on Ko-fi.
                </li>
                <PatronBenefits />
            </ul>
        {/if}
    </Alert>
{/if}
