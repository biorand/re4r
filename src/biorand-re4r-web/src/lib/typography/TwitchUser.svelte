<script lang="ts">
    export let user: string;
    export let href: string | undefined = undefined;

    if (!href) href = `https://twitch.tv/${user.toLowerCase()}`;
</script>

<a class="font-bold hover:text-blue-600 dark:hover:text-blue-300" target="_blank" {href}>{user}</a>
