<script lang="ts">
    import type { Config, ConfigDefinition } from '$lib/api';
    import type { Writable } from 'svelte/store';
    import ConfigurationControl from './ConfigControl.svelte';

    export let definition: ConfigDefinition | undefined;
    export let configuration: Writable<Config | undefined>;

    function pairs<T>(arr: T[]): T[][] {
        const result: T[][] = [];
        for (let i = 0; i < arr.length; i += 2) {
            result.push([arr[i + 0], arr[i + 1]]);
        }
        return result;
    }
</script>

{#if $configuration}
    <ul class="nav nav-tabs" role="tablist">
        {#each definition?.pages || [] as p, i}
            <li class="nav-item" role="presentation">
                <button
                    class="nav-link"
                    class:active={i == 0}
                    id="tab-{i}"
                    data-bs-toggle="tab"
                    data-bs-target="#pane-{i}"
                    type="button"
                    role="tab">{p.label}</button
                >
            </li>
        {/each}
    </ul>
    <div class="tab-content" id="myTabContent">
        {#each definition?.pages || [] as p, i}
            <div
                class="tab-pane fade p-2"
                class:show={i == 0}
                class:active={i == 0}
                id="pane-{i}"
                role="tabpanel"
                aria-labelledby="tab-{i}"
                tabindex="0"
            >
                {#each p.groups as g}
                    {#if g.label}
                        <h4>{g.label}</h4>
                    {/if}
                    {#if g.warning}
                        <div class="alert alert-warning">
                            {g.warning}
                        </div>
                    {/if}
                    {#each pairs(g.items) as p}
                        <div class="row">
                            <div class="col-xxl-6">
                                <ConfigurationControl definition={p[0]} {configuration} />
                            </div>
                            <div class="col-xxl-6">
                                {#if p[1]}
                                    <ConfigurationControl definition={p[1]} {configuration} />
                                {/if}
                            </div>
                        </div>
                    {/each}
                    <hr />
                {/each}
            </div>
        {/each}
    </div>
{/if}
