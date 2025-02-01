<script lang="ts">
    import { createEventDispatcher } from 'svelte';

    const dispatch = createEventDispatcher();

    let inputElement: HTMLInputElement;

    function onButtonClick() {
        inputElement?.click();
    }

    function onInputChange() {
        const file = inputElement.files ? inputElement.files[0] : undefined;
        if (file) {
            const reader = new FileReader();
            reader.onload = (e) => {
                const content = e.target?.result;
                dispatch('change', {
                    content
                });
            };
            reader.readAsText(file);
        }
    }
</script>

<input bind:this={inputElement} on:change={onInputChange} type="file" style="display: none;" />
<button {...$$restProps} on:click|preventDefault={onButtonClick}><slot></slot></button>
