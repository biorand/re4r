<script lang="ts">
    import type { ConfigOption } from './config';

    export let definition: ConfigOption;
    export let value: any = definition.default;

    function onInput(e: InputEvent) {
        const target = e.target as HTMLInputElement;
        value = target?.value || value;
    }
</script>

<div class="row g-3 align-items-center">
    <div class="col-4">
        <label
            for="cfg-{definition.id}"
            class="form-label"
            data-bs-toggle="tooltip"
            data-bs-placement="top"
            data-bs-custom-class="custom-tooltip"
            data-bs-title={definition.description}>{definition.label}</label
        >
    </div>
    <div class="col-8">
        {#if definition.type === 'switch'}
            <div class="form-check form-switch">
                <input
                    id="cfg-{definition.id}"
                    class="form-check-input"
                    type="checkbox"
                    role="switch"
                    checked={value}
                />
            </div>
        {:else if definition.type === 'range'}
            <div class="row">
                <div class="col-auto">
                    <input
                        on:input={onInput}
                        id="cfg-{definition.id}"
                        type="range"
                        class="form-range"
                        min={definition.min}
                        max={definition.max}
                        step={definition.step}
                        {value}
                    />
                </div>
                <div id="cfg-{definition.id}" class="col-auto">{value}</div>
            </div>
        {:else if definition.type === 'dropdown'}
            <select id="cfg-{definition.id}" class="form-select" {value}>
                {#each definition.options || [] as option}
                    <option>{option}</option>
                {/each}
            </select>
        {/if}
    </div>
</div>
