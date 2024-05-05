<script lang="ts">
    import { type FormInputData } from '$lib/Validation';
    import { Helper, Input, Label, type InputType } from 'flowbite-svelte';

    type InputColor = 'red' | 'green' | undefined;

    export let label = '';
    export let id = '';
    export let type: InputType = 'text';
    export let required = false;
    export let minlength: number | undefined = undefined;
    export let maxlength: number | undefined = undefined;
    export let disabled = false;
    export let placeholder = '';
    export let icon: ConstructorOfATypedSvelteComponent | undefined;
    export let help = '';
    export let data: FormInputData = {
        key: '',
        value: ''
    };

    $: color = getColor(data.valid);

    function getColor(valid?: boolean): InputColor {
        return valid ? 'green' : valid === false ? 'red' : undefined;
    }
</script>

<div class="mb-2">
    <Label for={id} class="block mb-2" {color}>{label}</Label>
    <Input
        color={color || 'base'}
        {id}
        {type}
        {placeholder}
        {required}
        {minlength}
        {maxlength}
        {disabled}
        bind:value={data.value}
    >
        <svelte:component
            this={icon}
            slot="left"
            class="w-5 h-5 text-gray-500 dark:text-gray-400"
        />
    </Input>
    {#if data.valid === false}
        <Helper class="mt-2" {color}>{data.message}</Helper>
    {/if}
    {#if help}
        <p id="helper-text-explanation" class="mt-2 text-sm text-gray-500 dark:text-gray-400">
            {help}
        </p>
    {/if}
</div>
