<script lang="ts">
    import type { ProfileViewModel } from '$lib/UserProfileManager';
    import type { Config, ConfigDefinition } from '$lib/api';
    import { Alert, TabItem, Tabs } from 'flowbite-svelte';
    import { InfoCircleSolid } from 'flowbite-svelte-icons';
    import { writable, type Writable } from 'svelte/store';
    import ConfigurationControl from './ConfigControl.svelte';
    import GeneratePanel from './GeneratePanel.svelte';
    import ProfilePanel from './ProfilePanel.svelte';

    export let definition: ConfigDefinition | undefined;
    export let profile: Writable<ProfileViewModel | undefined>;

    let lastProfile: ProfileViewModel | undefined = undefined;
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
                lastProfile = <ProfileViewModel>{ ...p, config: c, isModified: true };
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
    <Tabs tabStyle="pill">
        <TabItem open title="Generate">
            <GeneratePanel profile={$profile} />
        </TabItem>
        <TabItem title="Profile">
            <ProfilePanel bind:profile={$profile} />
        </TabItem>
        {#each definition?.pages || [] as p, i}
            <TabItem title={p.label}>
                {#each p.groups as g}
                    {#if g.label}
                        <h4>{g.label}</h4>
                    {/if}
                    {#if g.warning}
                        <Alert border color="yellow" class="my-4">
                            <InfoCircleSolid slot="icon" class="w-5 h-5" />{g.warning}
                        </Alert>
                    {/if}
                    {#each pairs(g.items) as p}
                        <div class="sm:flex">
                            <div class="sm:w-1/2 mr-2">
                                <ConfigurationControl definition={p[0]} {configuration} />
                            </div>
                            <div class="sm:w-1/2 ml-2">
                                {#if p[1]}
                                    <ConfigurationControl definition={p[1]} {configuration} />
                                {/if}
                            </div>
                        </div>
                    {/each}
                    <hr class="border-gray-500 mb-3" />
                {/each}
            </TabItem>
        {/each}
    </Tabs>
{/if}
