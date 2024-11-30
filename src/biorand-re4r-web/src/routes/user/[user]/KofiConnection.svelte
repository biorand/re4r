<script lang="ts">
    import { Alert } from 'flowbite-svelte';
    import { CheckCircleSolid, InfoCircleSolid } from 'flowbite-svelte-icons';

    export let isMember = false;
    export let showBenefits = true;
</script>

{#if isMember}
    <Alert color="green" border class="mt-3 p-2">
        <CheckCircleSolid slot="icon" class="w-5 h-5" />
        <span class="font-medium">BioRand Patron </span>
    </Alert>
{:else}
    <a target="_blank" href="http://ko-fi.com/intelorca/tiers"
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
            <p class="font-medium">Become a BioRand patron:</p>
            <ul class="mt-1.5 ms-4 list-disc list-inside">
                <li>
                    Support <a
                        class="font-medium text-blue-400 hover:text-blue-300"
                        href="http://ko-fi.com/intelorca/tiers"
                        target="_blank">IntelOrca</a
                    > on Ko-fi.
                </li>
                <li>Highlighted user name on website.</li>
                <li>Early access to Separate Ways randomizer.</li>
            </ul>
        {/if}
    </Alert>
{/if}
