<script>
</script>

<div>
    <ul class="nav nav-tabs" role="tablist">
        {#each Object.keys($$slots) as slotName}
            <li class="nav-item" role="presentation">
                <button
                    class="nav-link"
                    class:active={slotName == 'a'}
                    id="tab-profile"
                    data-bs-toggle="tab"
                    data-bs-target="#pane-profile"
                    type="button"
                    role="tab">{slotName}</button
                >
            </li>
        {/each}
    </ul>
</div>
