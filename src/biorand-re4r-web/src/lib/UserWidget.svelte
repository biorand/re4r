<script lang="ts">
    import { Avatar } from 'flowbite-svelte';
    import type { LightUserInfo } from './api';

    export let user: LightUserInfo;
    export let href: string | undefined;
</script>

<div class="flex">
    <div class="content-center">
        <Avatar class="mr-2 w-4 h-4 overflow-hidden" src={user.avatarUrl} />
    </div>
    <div>
        <a
            class="text-blue-800 hover:text-blue-500 dark:text-blue-400 dark:hover:text-blue-300"
            {href}>{user.name}</a
        >
    </div>
</div>
