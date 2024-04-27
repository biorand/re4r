<script>
    import { afterUpdate, beforeUpdate } from 'svelte';
    import Toolbar from './Toolbar.svelte';

    beforeUpdate(() => {
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        const tooltipList = [...tooltipTriggerList]
            // @ts-ignore
            .map((el) => bootstrap.Tooltip.getOrCreateInstance(el))
            .forEach((x) => x.hide());
    });

    afterUpdate(() => {
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltipTriggerList.forEach((el) =>
            // @ts-ignore
            bootstrap.Tooltip.getOrCreateInstance(el)
        );
    });
</script>

<Toolbar />
<div id="main-container" class="container-fluid">
    <slot />
</div>

<style>
    #main-container {
        margin-top: 68px;
    }
</style>
