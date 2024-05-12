<script lang="ts">
    export let user: string;
</script>

<a
    class="font-bold hover:text-blue-600 dark:hover:text-blue-300"
    target="_blank"
    href="https://twitch.tv/{user.toLowerCase()}">{user}</a
>
