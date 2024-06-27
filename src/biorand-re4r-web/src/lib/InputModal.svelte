<script>
    import { Button, Input, Label, Modal } from 'flowbite-svelte';
    import { createEventDispatcher } from 'svelte';

    const dispatch = createEventDispatcher();

    export let open = false;
    export let title = 'Input';
    export let label = 'Label';
    export let value = '';
</script>

<Modal bind:open size="xs">
    <form class="flex flex-col space-y-6" on:submit|preventDefault={() => dispatch('submit')}>
        <h3 class="mb-4 text-xl font-medium text-gray-900 dark:text-white">{title}</h3>
        <Label class="space-y-2">
            <span>{label}</span>
            <Input type="text" name="text" required bind:value />
        </Label>
        <Button type="submit" class="w-full1">Submit</Button>
    </form>
</Modal>
