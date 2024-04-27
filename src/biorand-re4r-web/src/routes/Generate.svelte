<script lang="ts">
    import ConfigView from '$lib/configView.svelte';
    import ProfileManagerView from '$lib/profileManagerView.svelte';
</script>

<div class="container-fluid">
    <div class="row">
        <div class="col-md-5" style="max-width: 512px;">
            <ProfileManagerView />
        </div>
        <div class="col">
            <ConfigView />
        </div>
    </div>
</div>
