<script lang="ts">
    import type { Config, ConfigDefinition, Profile } from '$lib/api';
    import { writable, type Writable } from 'svelte/store';
    import ConfigurationControl from './ConfigControl.svelte';

    export let definition: ConfigDefinition | undefined;
    export let profile: Writable<Profile | undefined>;

    let lastProfile: Profile | undefined = undefined;
    let lastConfiguration: Config | undefined = undefined;
    let configuration = writable({});
    profile.subscribe((p) => {
        if (p !== lastProfile) {
            lastProfile = p;
            if (p?.config) {
                lastConfiguration = p.config;
                configuration.set(p.config);
            }
        }
    });
    configuration.subscribe((c) => {
        if (lastConfiguration !== c) {
            lastConfiguration = c;
            profile.update((p) => {
                lastProfile = <Profile>{ ...p, config: c };
                return lastProfile;
            });
        }
    });

    function pairs<T>(arr: T[]): T[][] {
        const result: T[][] = [];
        for (let i = 0; i < arr.length; i += 2) {
            result.push([arr[i + 0], arr[i + 1]]);
        }
        return result;
    }
</script>

{#if $profile}
    <ul class="nav nav-tabs" role="tablist">
        <li class="nav-item" role="presentation">
            <button
                class="nav-link active"
                id="tab-profile"
                data-bs-toggle="tab"
                data-bs-target="#pane-profile"
                type="button"
                role="tab">Profile</button
            >
        </li>
        {#each definition?.pages || [] as p, i}
            <li class="nav-item" role="presentation">
                <button
                    class="nav-link"
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
        <div
            class="tab-pane fade p-2 show active"
            id="pane-profile"
            role="tabpanel"
            aria-labelledby="tab-profile"
            tabindex="0"
        >
            <div class="mb-3">
                <label for="txt-profile-name" class="form-label">Name</label>
                <input
                    type="text"
                    class="form-control"
                    id="txt-profile-name"
                    bind:value={$profile.name}
                />
            </div>
            <div class="mb-3">
                <label for="txt-profile-description" class="form-label">Description</label>
                <textarea
                    class="form-control"
                    id="txt-profile-description"
                    rows="3"
                    bind:value={$profile.description}
                ></textarea>
            </div>
            <div class="row">
                <span class="col-sm-2 col-form-label">Stars</span>
                <div class="col-sm-10">
                    <span class="form-control-plaintext">4</span>
                </div>
            </div>
            <div class="row">
                <span class="col-sm-2 col-form-label">Seeds generated</span>
                <div class="col-sm-10">
                    <span class="form-control-plaintext">4</span>
                </div>
            </div>
        </div>
        {#each definition?.pages || [] as p, i}
            <div
                class="tab-pane fade p-2"
                id="pane-{i}"
                role="tabpanel"
                aria-labelledby="tab-{i}"
                tabindex={i + 1}
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
