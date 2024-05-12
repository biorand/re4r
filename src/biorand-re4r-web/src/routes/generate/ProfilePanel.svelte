<script lang="ts">
    import type { ProfileViewModel } from '$lib/UserProfileManager';
    import type { ConfigDefinition, ConfigGroup } from '$lib/api';
    import { Alert, TabItem, Tabs } from 'flowbite-svelte';
    import { InfoCircleSolid } from 'flowbite-svelte-icons';
    import ConfigControl from './ConfigControl.svelte';
    import ConfigGroupHeading from './ConfigGroupHeading.svelte';
    import GenerateTabPanel from './GenerateTabPanel.svelte';
    import ProfileTabPanel from './ProfileTabPanel.svelte';
    import RatioBar from './RatioBar.svelte';

    export let definition: ConfigDefinition;
    export let profile: ProfileViewModel;

    function pairs<T>(arr: T[]): T[][] {
        const result: T[][] = [];
        for (let i = 0; i < arr.length; i += 2) {
            result.push([arr[i + 0], arr[i + 1]]);
        }
        return result;
    }

    function hasRatioBar(g: ConfigGroup) {
        return g.items.some((x) => x.category);
    }
</script>

<Tabs tabStyle="pill">
    <TabItem title="Generate">
        <GenerateTabPanel {profile} />
    </TabItem>
    <TabItem title="Profile">
        <ProfileTabPanel bind:profile />
    </TabItem>
    {#each definition?.pages || [] as p, i}
        <TabItem title={p.label} open={i == 4}>
            {#each p.groups as g}
                <ConfigGroupHeading bind:config={profile.config} group={g} />

                {#if g.warning}
                    <Alert border color="yellow" class="my-4">
                        <InfoCircleSolid slot="icon" class="w-5 h-5" />{g.warning}
                    </Alert>
                {/if}
                {#if hasRatioBar(g)}
                    <RatioBar class="my-2" config={profile.config} group={g} />
                {/if}
                {#each pairs(g.items) as p}
                    <div class="sm:flex">
                        <div class="sm:w-1/2 mr-2">
                            <ConfigControl definition={p[0]} bind:config={profile.config} />
                        </div>
                        <div class="sm:w-1/2 ml-2">
                            {#if p[1]}
                                <ConfigControl definition={p[1]} bind:config={profile.config} />
                            {/if}
                        </div>
                    </div>
                {/each}
                <hr class="border-gray-500 mb-3" />
            {/each}
        </TabItem>
    {/each}
</Tabs>
