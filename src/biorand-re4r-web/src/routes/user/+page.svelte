<script lang="ts">
    import { goto } from '$app/navigation';
    import { getApi } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import { Spinner } from 'flowbite-svelte';

    let state: undefined | 'success' | 'failed';
    let error = '';

    let init = (async () => {
        const searchParams = new URLSearchParams(location.search);
        const args = {
            action: searchParams.get('action'),
            token: searchParams.get('token')
        };
        if (args.action === 'verifykofi') {
            const api = getApi();
            try {
                await api.verifyEmail(args.token || '');
                goto('/');
            } catch (err) {
                state = 'failed';
                error = 'Failed to verify email address';
            }
        } else {
            const userManager = getUserManager();
            const userName = userManager.info?.user.name;
            if (!userName) {
                goto('/');
            } else {
                goto(`/user/${userName}`);
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
