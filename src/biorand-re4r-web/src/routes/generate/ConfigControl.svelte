<script lang="ts">
    import { Transformer } from '$lib/Transformer';
    import type { Config, ConfigOption } from '$lib/api';
    import { Label, Range, Select, Toggle, Tooltip } from 'flowbite-svelte';
    import { derived, get, writable, type Writable } from 'svelte/store';

    export let definition: ConfigOption;
    export let config: Config;

    const initialValue = definition.id in config ? config[definition.id] : definition.default;
    let value = writable<any>(initialValue);
    let transformed = getTransformed(value, definition);
    let formatted = transformed.formatted;
    value.set(transformed.untransform(initialValue));

    value.subscribe((v) => {
        const v2 = transformed.transform(v);
        if (config[definition.id] !== v2) {
            config[definition.id] = v2;
        }
    });

    $: {
        const v = config[definition.id];
        const v2 = transformed.untransform(v);
        if (get(value) !== v2) {
            value.set(v2);
        }
    }

    function getTransformed(value: Writable<any>, definition: ConfigOption) {
        if (definition.type === 'percent') return mapPercent(value, definition);
        if (definition.type === 'scale') return mapScaled(value, definition);
        return mapDirect(value, definition);
    }

    function mapDirect(value: Writable<any>, definition: ConfigOption) {
        return {
            min: definition.min,
            max: definition.max,
            step: definition.step,
            untransform: (value: number) => value,
            transform: (value: number) => value,
            formatted: derived(value, (value) => value)
        };
    }

    function mapScaled(value: Writable<any>, definition: ConfigOption) {
        const scaledRanges = [
            [1, 10],
            [10, 100],
            [100, 5_000],
            [500, 10_000],
            [1_000, 50_000],
            [5_000, 100_000],
            [100_000, 1_000_000]
        ];

        const transformer = new Transformer();
        for (const r of scaledRanges) {
            if (definition.max! >= r[1]) {
                transformer.addStepRange(r[0], r[1]);
            }
        }

        return {
            min: transformer.getMin(),
            max: transformer.getMax(),
            step: 1,
            untransform: (value: number) => transformer.untransform(value),
            transform: (value: number) => transformer.transform(value),
            formatted: derived(value, (value) => transformer.transform(value).toLocaleString())
        };
    }

    function mapPercent(value: Writable<any>, definition: ConfigOption) {
        return {
            ...mapDirect(value, definition),
            formatted: derived(value, (value) => `${~~(value * 100)}%`)
        };
    }
</script>

<div class="sm:flex m-1">
    <div class="sm:w-1/2">
        <Label class="inline-block h-full content-center" for="cfg-{definition.id}"
            >{definition.label}</Label
        >
        {#if definition.description}
            <Tooltip>{definition.description}</Tooltip>
        {/if}
    </div>
    <div class="sm:w-1/2">
        {#if definition.type === 'switch'}
            <Toggle size="small" id="cfg-{definition.id}" bind:checked={$value} />
        {:else if definition.type === 'range' || definition.type === 'percent' || definition.type === 'scale'}
            <div class="flex">
                <div class="grow">
                    <Range
                        id="cfg-{definition.id}"
                        min={transformed.min}
                        max={transformed.max}
                        step={transformed.step}
                        bind:value={$value}
                    />
                </div>
                <div class="w-14 ml-2">{$formatted}</div>
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
