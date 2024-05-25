<script lang="ts">
    import { getApi, type UserTwitchInfo } from '$lib/api';
    import { LocalStorageKeys, getLocalStorageManager } from '$lib/localStorage';
    import { buildUrl } from '$lib/utility';
    import { Alert, Button, ButtonGroup } from 'flowbite-svelte';
    import { CheckCircleSolid, InfoCircleSolid } from 'flowbite-svelte-icons';

    export let userId: number;
    export let twitch: UserTwitchInfo | undefined = undefined;
    export let showUnsubscribed = true;

    function connectTwitch() {
        const state = getSecureHexString(32);

        const lsManager = getLocalStorageManager();
        lsManager.set(LocalStorageKeys.TwitchAuthState, state);

        const redirectUri = `${window.location.origin}/auth/twitch`;
        const twitchAuthUri = buildUrl('https://id.twitch.tv/oauth2/authorize', {
            client_id: 'mi6184w9paqm7r4p803eioulkct635',
            redirect_uri: redirectUri,
            response_type: 'code',
            scope: 'user:read:subscriptions',
            state: state
        });
        window.location.replace(twitchAuthUri);
    }

    async function disconnectTwitch() {
        if (twitch) {
            const api = getApi();
            await api.updateUser(userId, {
                twitchCode: ''
            });
            twitch = undefined;
        }
    }

    function getSecureHexString(length: number) {
        var array = new Uint8Array(length / 2);
        window.crypto.getRandomValues(array);
        var hexString = Array.from(array)
            .map((byte) => ('0' + byte.toString(length)).slice(-2))
            .join('');
        return hexString;
    }
</script>

{#if twitch}
    <ButtonGroup>
        <Button href="https://twitch.tv/{twitch.displayName}" target="_blank" color="primary"
            >{twitch.displayName}</Button
        >
        <Button on:click={disconnectTwitch} color="alternative">Disconnect</Button>
    </ButtonGroup>
    {#if !twitch.isSubscribed}
        {#if showUnsubscribed}
            <Alert color="yellow" border class="mt-3 p-2 !items-start">
                <span slot="icon">
                    <InfoCircleSolid slot="icon" class="w-5 h-5" />
                    <span class="sr-only">Info</span>
                </span>
                <p class="font-medium">Unsubscribed</p>
                <ul class="mt-1.5 ms-4 list-disc list-inside">
                    <li>
                        Support <a
                            class="font-medium text-blue-400 hover:text-blue-300"
                            href="https://twitch.tv/intelorca"
                            target="_blank">IntelOrca</a
                        > by subscribing on Twitch.
                    </li>
                    <li>Instant early access to the randomizer</li>
                    <li>Exclusive access to preview functionality</li>
                </ul>
            </Alert>
        {/if}
    {:else}
        <Alert color="green" border class="mt-3 p-2">
            <CheckCircleSolid slot="icon" class="w-5 h-5" />
            <span class="font-medium">Subscribed </span><span class="font-light">to </span>
            <a
                class="font-medium text-blue-400 hover:text-blue-300"
                href="https://twitch.tv/intelorca"
                target="_blank">IntelOrca</a
            >
        </Alert>
    {/if}
{:else}
    <Button on:click={connectTwitch}>Connect Twitch Account</Button>
    <Alert color="dark" border class="mt-3 p-2 !items-start">
        <span slot="icon">
            <InfoCircleSolid slot="icon" class="w-5 h-5" />
            <span class="sr-only">Info</span>
        </span>
        <p class="font-medium">Connect your Twitch account to:</p>
        <ul class="mt-1.5 ms-4 list-disc list-inside">
            <li>Advertise when you are live and playing BioRand.</li>
            <li>Use your Twitch profile picture as your avatar.</li>
            <li>
                Instant access to the randomizer if subscribed to
                <a
                    class="font-medium text-blue-400 hover:text-blue-300"
                    href="https://twitch.tv/intelorca"
                    target="_blank">IntelOrca</a
                >.
            </li>
            <li>
                Exclusive access to preview functionality if subscribed to
                <a
                    class="font-medium text-blue-400 hover:text-blue-300"
                    href="https://twitch.tv/intelorca"
                    target="_blank">IntelOrca</a
                >.
            </li>
        </ul>
    </Alert>
{/if}
