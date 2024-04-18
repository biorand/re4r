<script lang="ts">
    import { onMount } from 'svelte';
    import type { ConfigDefinition } from './config';
    import ConfigOptionView from './configOptionView.svelte';

    function downloadConfig(): Promise<ConfigDefinition> {
        return new Promise((resolve, reject) => {
            fetch('http://localhost:10285/api/config')
                .then((response) =>
                    response
                        .json()
                        .then((value) => resolve(value))
                        .catch((e) => reject(e))
                )
                .catch((e) => reject(e));
        });
    }

    function pairs<T>(arr: T[]): T[][] {
        const result: T[][] = [];
        for (let i = 0; i < arr.length; i += 2) {
            result.push([arr[i + 0], arr[i + 1]]);
        }
        return result;
    }

    let config: ConfigDefinition | undefined = undefined;
    onMount(() => {
        const cachedConfig = localStorage.getItem('cachedConfig');
        if (cachedConfig) {
            config = JSON.parse(cachedConfig);
        } else {
            downloadConfig().then((c) => {
                localStorage.setItem('cachedConfig', JSON.stringify(c));
                config = c;
            });
        }
    });
</script>

<h1>&nbsp;</h1>
<ul class="nav nav-tabs" role="tablist">
    {#each config?.groups || [] as g, i}
        <li class="nav-item" role="presentation">
            <button
                class="nav-link"
                class:active={i == 0}
                id="tab-{i}"
                data-bs-toggle="tab"
                data-bs-target="#pane-{i}"
                type="button"
                role="tab">{g.label}</button
            >
        </li>
    {/each}
</ul>
<div class="tab-content" id="myTabContent">
    {#each config?.groups || [] as g, i}
        <div
            class="tab-pane fade p-2"
            class:show={i == 0}
            class:active={i == 0}
            id="pane-{i}"
            role="tabpanel"
            aria-labelledby="tab-{i}"
            tabindex="0"
        >
            {#if g.warning}
                <div class="alert alert-warning">
                    {g.warning}
                </div>
            {/if}
            {#each pairs(g.items) as p}
                <div class="row">
                    <div class="col">
                        <ConfigOptionView definition={p[0]} />
                    </div>
                    <div class="col">
                        {#if p[1]}
                            <ConfigOptionView definition={p[1]} />
                        {/if}
                    </div>
                </div>
            {/each}
        </div>
    {/each}
</div>
