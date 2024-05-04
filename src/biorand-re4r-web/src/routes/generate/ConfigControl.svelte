<script lang="ts">
    import type { Config, ConfigOption } from '$lib/api';
    import { Label, Range, Select, Toggle, Tooltip } from 'flowbite-svelte';
    import { get, writable } from 'svelte/store';

    export let definition: ConfigOption;
    export let config: Config;

    const initialValue = definition.id in config ? config[definition.id] : definition.default;
    let value = writable<any>(initialValue);
    value.subscribe((v) => {
        if (config[definition.id] !== v) {
            config[definition.id] = v;
        }
    });

    $: {
        if (get(value) !== config[definition.id]) {
            value.set(config[definition.id]);
        }
    }
</script>

<div class="sm:flex m-1">
    <div class="sm:w-1/2">
        <Label class="h-full content-center" for="cfg-{definition.id}">{definition.label}</Label>
        {#if definition.description}
            <Tooltip>{definition.description}</Tooltip>
        {/if}
    </div>
    <div class="sm:w-1/2">
        {#if definition.type === 'switch'}
            <Toggle size="small" id="cfg-{definition.id}" bind:checked={$value} />
        {:else if definition.type === 'range'}
            <div class="flex">
                <div class="grow">
                    <Range
                        id="cfg-{definition.id}"
                        min={definition.min}
                        max={definition.max}
                        step={definition.step}
                        bind:value={$value}
                    />
                </div>
                <div class="w-12 ml-2">{$value}</div>
            </div>
        {:else if definition.type === 'dropdown'}
            <Select
                id="cfg-{definition.id}"
                underline
                size="sm"
                items={(definition.options || []).map((x) => ({ name: x, value: x }))}
                bind:value={$value}
            />
        {/if}
    </div>
</div>
