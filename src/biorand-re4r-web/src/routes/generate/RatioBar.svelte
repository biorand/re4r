<script lang="ts">
    import type { Config, ConfigGroup, ConfigOptionCategory } from '$lib/api';
    import { Tooltip } from 'flowbite-svelte';

    export let group: ConfigGroup;
    export let config: Config;

    let className = '';
    export { className as class };

    $: bars = getBars(group, config);

    function getBars(group: ConfigGroup, config: Config) {
        const bars: { [key: string]: { category: ConfigOptionCategory; value: number } } = {};
        let total = 0;
        for (const item of group.items) {
            const category = item.category;
            if (category) {
                if (!(category.label in bars)) {
                    bars[category.label] = {
                        category: category,
                        value: 0
                    };
                }

                const value = config[item.id] || 0;
                bars[category.label].value += value;
                total += value;
            }
        }
        return Object.values(bars)
            .map((x) => ({
                category: x.category,
                value: Math.ceil((x.value / total) * 100)
            }))
            .filter((x) => !isNaN(x.value));
    }
</script>

<div
    class="{className} flex text-xs font-semibold overflow-hidden rounded-lg border-2 border-gray-600"
>
    {#if bars.length === 0}
        <div class="px-2 py-0.5 overflow-hidden" style="background-color: black; width: 100%;">
            &nbsp;
        </div>
    {:else}
        {#each bars as bar}
            <div
                style="background-color: {bar.category.backgroundColor}; color: {bar.category
                    .textColor}; width: {bar.value}%;"
            >
                <div class="mx-2 m-0.5 text-center overflow-hidden">
                    {bar.category.label}
                </div>
            </div>
            <Tooltip placement="bottom">{bar.category.label} ({bar.value.toFixed()}%)</Tooltip>
        {/each}
    {/if}
</div>
