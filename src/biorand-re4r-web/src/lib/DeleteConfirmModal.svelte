<script>
    import { Button, Modal } from 'flowbite-svelte';
    import { ExclamationCircleOutline } from 'flowbite-svelte-icons';
    import { createEventDispatcher } from 'svelte';

    const dispatch = createEventDispatcher();

    export let open = false;
</script>

<Modal bind:open size="xs" autoclose>
    <div class="text-center">
        <ExclamationCircleOutline class="mx-auto mb-4 text-gray-400 w-12 h-12 dark:text-gray-200" />
        <h3 class="mb-5 text-lg font-normal text-gray-500 dark:text-gray-400">
            Are you sure you want to delete this item?
        </h3>
        <Button on:click={() => dispatch('delete')} color="red" class="me-2">Delete</Button>
        <Button color="alternative">Cancel</Button>
    </div>
</Modal>
