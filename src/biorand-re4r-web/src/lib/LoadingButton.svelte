<script>
    import { Button, Spinner } from 'flowbite-svelte';

    export let loading = false;
</script>

<Button class="w-full" disabled={loading} on:click {...$$restProps}>
    {#if loading}
        <Spinner class="me-3" size="4" color="white" />
    {/if}
    <slot />
</Button>
