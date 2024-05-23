<script lang="ts">
    import { goto } from '$app/navigation';
    import { getApi } from '$lib/api';
    import { LocalStorageKeys, getLocalStorageManager } from '$lib/localStorage';
    import { getUserManager } from '$lib/userManager';
    import { Spinner } from 'flowbite-svelte';

    let state: undefined | 'success' | 'failed';
    let error = '';

    let init = (async () => {
        const userManager = getUserManager();
        const userId = userManager.info?.user.id;
        if (!userId) {
            state = 'failed';
            error = 'Not logged in';
        } else {
            const searchParams = new URLSearchParams(location.search);
            const twitchArgs = {
                code: searchParams.get('code'),
                scope: searchParams.get('scope'),
                state: searchParams.get('state')
            };

            const lsManager = getLocalStorageManager();
            const expectedState = lsManager.getString(LocalStorageKeys.TwitchAuthState);
            if (expectedState !== twitchArgs.state || !twitchArgs.code) {
                state = 'failed';
                error = 'Invalid or expired Twitch authorization request.';
            } else {
                const api = getApi();
                const response = await api.updateUser(userId, {
                    twitchCode: twitchArgs.code
                });
                if (response.success) {
                    state = 'success';
                    goto(`/user/${userManager.info.user.name}`);
                } else {
                    state = 'failed';
                    error = response.validation?.['twitchCode'] || 'Unknown error';
                }
            }
        }
    })();
</script>

<div class="m-4">
    {#await init}
        <Spinner />
    {:then}
        <div>{error}</div>
    {/await}
</div>
