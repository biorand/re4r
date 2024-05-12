<script lang="ts">
    import type { Config, ConfigGroup } from '$lib/api';
    import { Toolbar, ToolbarButton, Tooltip } from 'flowbite-svelte';
    import {
        ArrowsRepeatOutline,
        CircleMinusSolid,
        CirclePauseSolid,
        CirclePlusSolid,
        ShuffleOutline
    } from 'flowbite-svelte-icons';

    export let config: Config;
    export let group: ConfigGroup;

    $: showMinMaxButton = hasOtherThan('dropdown');
    $: showMidButton = hasOtherThan('switch', 'dropdown');
    $: showShuffleButton = hasOtherThan('scale');

    function hasOtherThan(...types: string[]) {
        return group.items.some((x) => types.indexOf(x.type) === -1);
    }

    function resetGroup(group: ConfigGroup, kind: 'min' | 'mid' | 'max' | 'rng' | 'default') {
        for (const item of group.items) {
            if (kind === 'default') {
                config[item.id] = item.default;
            } else {
                switch (item.type) {
                    case 'switch':
                        if (kind == 'min') {
                            config[item.id] = false;
                        } else if (kind == 'max') {
                            config[item.id] = true;
                        } else if (kind == 'rng') {
                            config[item.id] = !!(Math.random() >= 0.5);
                        }
                        break;
                    case 'dropdown':
                        const options = item.options!;
                        const index = ~~(Math.random() * options.length);
                        config[item.id] = options[index];
                        break;
                    default:
                        if (kind == 'min') {
                            config[item.id] = item.min;
                            console.log('a');
                        } else if (kind == 'mid') {
                            if (item.type == 'scale') {
                                config[item.id] = 10000;
                            } else {
                                config[item.id] = item.max! / 2;
                            }
                        } else if (kind == 'max') {
                            config[item.id] = item.max;
                        } else if (kind == 'rng') {
                            const range = item.max! - item.min!;
                            const value = item.min! + Math.random() * range;
                            const rounded = ~~(value / item.step!) * item.step!;
                            const fixedRounded = parseFloat(rounded.toFixed(2));
                            config[item.id] = fixedRounded;
                        }
                        break;
                }
            }
        }
    }
</script>

<div class="flex">
    <h4 class="grow">{group.label}</h4>
    <Toolbar class="p-0">
        {#if showMinMaxButton}
            <ToolbarButton on:click={() => resetGroup(group, 'min')} size="sm"
                ><CircleMinusSolid class="w-4 h-4" /></ToolbarButton
            >
            <Tooltip placement="top">Reset all to minimum value</Tooltip>
        {/if}
        {#if showMidButton}
            <ToolbarButton on:click={() => resetGroup(group, 'mid')} size="sm"
                ><CirclePauseSolid class="w-4 h-4" /></ToolbarButton
            >
        {/if}
        <Tooltip>Reset all to mid value</Tooltip>
        {#if showMinMaxButton}
            <ToolbarButton on:click={() => resetGroup(group, 'max')} size="sm"
                ><CirclePlusSolid class="w-4 h-4" /></ToolbarButton
            >
            <Tooltip>Reset all to maximum value</Tooltip>
        {/if}
        <div class="mx-1 h-5 border-r border-gray-500"></div>
        <ToolbarButton on:click={() => resetGroup(group, 'default')} size="sm"
            ><ArrowsRepeatOutline class="w-4 h-4" /></ToolbarButton
        >
        <Tooltip>Reset all to default value</Tooltip>
        {#if showShuffleButton}
            <ToolbarButton on:click={() => resetGroup(group, 'rng')} size="sm"
                ><ShuffleOutline class="w-4 h-4" /></ToolbarButton
            >
            <Tooltip>Reset all to random value</Tooltip>
        {/if}
    </Toolbar>
</div>
