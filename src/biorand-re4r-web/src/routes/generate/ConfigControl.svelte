<script lang="ts">
    import type { Config, ConfigOption } from '$lib/api';
    import { writable, type Writable } from 'svelte/store';

    export let definition: ConfigOption;
    export let configuration: Writable<Config>;

    let currentConfig: Config | undefined = undefined;
    let value = writable(definition.default);

    configuration.subscribe((newValue) => {
        if (newValue !== currentConfig) {
            currentConfig = newValue;
            value.set(newValue[definition.id]);
        }
    });

    value.subscribe((newValue) => {
        if (currentConfig) {
            if (newValue !== currentConfig[definition.id]) {
                const newConfig = { ...currentConfig };
                newConfig[definition.id] = newValue;
                currentConfig = newConfig;
                configuration.set(newConfig);
            }
        }
    });
</script>

<div class="row align-items-center">
    <div class="col-6">
        <label
            for="cfg-{definition.id}"
            class="form-label"
            data-bs-toggle="tooltip"
            data-bs-placement="top"
            data-bs-custom-class="custom-tooltip"
            data-bs-title={definition.description}>{definition.label}</label
        >
    </div>
    <div class="col">
        {#if definition.type === 'switch'}
            <div class="form-check form-switch">
                <input
                    id="cfg-{definition.id}"
                    class="form-check-input"
                    type="checkbox"
                    role="switch"
                    bind:checked={$value}
                />
            </div>
        {:else if definition.type === 'range'}
            <div class="row">
                <div class="col">
                    <input
                        id="cfg-{definition.id}"
                        type="range"
                        class="form-range"
                        min={definition.min}
                        max={definition.max}
                        step={definition.step}
                        bind:value={$value}
                    />
                </div>
                <div id="cfg-{definition.id}" class="col-2">{$value}</div>
            </div>
        {:else if definition.type === 'dropdown'}
            <select id="cfg-{definition.id}" class="form-select" bind:value={$value}>
                {#each definition.options || [] as option}
                    <option>{option}</option>
                {/each}
            </select>
        {/if}
    </div>
</div>
