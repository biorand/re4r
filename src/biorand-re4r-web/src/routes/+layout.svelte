<script>
    import Toolbar from './Toolbar.svelte';
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
