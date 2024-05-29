<script lang="ts" context="module">
    import { Button, Modal } from 'flowbite-svelte';

    export interface ErrorModalContent {
        title: string;
        body: string;
    }
</script>

<script lang="ts">
    export let error: ErrorModalContent | undefined = undefined;
</script>

<Modal title={error?.title} open={error !== undefined} color="red" autoclose>
    <div class="text-base leading-relaxed">{error?.body}</div>
    <svelte:fragment slot="footer">
        <Button color="red">Close</Button>
    </svelte:fragment>
</Modal>
