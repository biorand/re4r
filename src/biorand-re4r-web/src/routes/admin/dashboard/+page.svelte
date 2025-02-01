<script lang="ts">
    import type { InfoResult, RandoHistoryItem, RandoHistoryResult } from '$lib/api';
    import { getApi, getWebsiteTitle, RandoStatus } from '$lib/api';
    import Timestamp from '$lib/Timestamp.svelte';
    import { PageBody, PageTitle } from '$lib/typography';
    import H2 from '$lib/typography/H2.svelte';
    import { containsUserTag } from '$lib/userManager';
    import {
        Avatar,
        Table,
        TableBody,
        TableBodyCell,
        TableBodyRow,
        TableHead,
        TableHeadCell,
        Tooltip
    } from 'flowbite-svelte';
    import RandoStatusBadge from '../../history/RandoStatusBadge.svelte';
    import GameBadge from './GameBadge.svelte';

    let info: InfoResult | undefined;
    let history: RandoHistoryResult | undefined;
    const refresh = async () => {
        const api = getApi();
        history = await api.getRandoHistory({});
        info = await api.getInfo();
    };
    refresh();

    setInterval(() => {
        refresh();
    }, 1000);

    function formatMemory(memory: number) {
        const gib = memory / (1024 * 1024 * 1024);
        const rounded = Math.round(gib * 100) / 100;
        return `${rounded} GiB`;
    }

    function getNameColor(item: RandoHistoryItem) {
        const userTags = item.userTags || [];
        if (containsUserTag(userTags, '$GAME:patron')) {
            return 'text-green-700 hover:text-green-500 dark:text-yellow-400 dark:hover:text-yellow-300';
        }
        if (containsUserTag(userTags, '$GAME:tester') || containsUserTag(userTags, 'admin')) {
            return 'text-red-800 hover:text-red-700 dark:text-red-400 dark:hover:text-red-200';
        }
        return 'text-blue-800 hover:text-blue-500 dark:text-blue-400 dark:hover:text-blue-300';
    }

    function getFailReason(id: number) {
        const r = info?.generatedRandos?.find((x) => x.id == id);
        return r?.failReason;
    }
</script>

<svelte:head>
    <title>{getWebsiteTitle('Admin Dashboard')}</title>
</svelte:head>

<PageBody>
    <PageTitle class="grow">Admin Dashboard</PageTitle>
    {#if info}
        <H2>Stats</H2>
        <div>
            Memory: {formatMemory(info.totalRandoMemory)}
        </div>
        <div>
            Stored randos: {info.generatedRandos.filter((x) => x.status == RandoStatus.Completed)
                .length}
        </div>

        <H2>Generators</H2>
        <Table>
            <TableHead>
                <TableHeadCell>Id</TableHeadCell>
                <TableHeadCell>Registered</TableHeadCell>
                <TableHeadCell>Heartbeat</TableHeadCell>
                <TableHeadCell class="text-center">Game</TableHeadCell>
                <TableHeadCell>Status</TableHeadCell>
            </TableHead>
            <TableBody tableBodyClass="divide-y">
                {#each info.generators as generator}
                    <TableBodyRow>
                        <TableBodyCell tdClass="p-1 font-mono">{generator.id}</TableBodyCell>
                        <TableBodyCell tdClass="p-1"
                            ><Timestamp value={generator.registerTime} /></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1"
                            ><Timestamp value={generator.lastHeartbeatTime} /></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1 font-mono flex"
                            ><GameBadge
                                class="m-auto"
                                moniker={generator.gameMoniker}
                            /></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1">{generator.status}</TableBodyCell>
                    </TableBodyRow>
                {/each}
            </TableBody>
        </Table>
    {/if}

    {#if history}
        <H2>Live History</H2>
        <Table>
            <TableHead>
                <TableHeadCell>Time</TableHeadCell>
                <TableHeadCell>User</TableHeadCell>
                <TableHeadCell>Profile</TableHeadCell>
                <TableHeadCell class="text-center">Game</TableHeadCell>
                <TableHeadCell class="text-center">Version</TableHeadCell>
                <TableHeadCell>Seed</TableHeadCell>
                <TableHeadCell>Status</TableHeadCell>
            </TableHead>
            <TableBody>
                {#each history.pageResults as item}
                    <TableBodyRow class="text-base font-semibold">
                        <TableBodyCell tdClass="p-1"
                            ><Timestamp value={item.created} /></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1">
                            <div class="flex">
                                <div class="content-center">
                                    <Avatar
                                        class="mr-2 w-4 h-4 overflow-hidden"
                                        src={item.userAvatarUrl}
                                    />
                                </div>
                                <div>
                                    <a
                                        class={getNameColor(item)}
                                        href="/history?user={item.userName}">{item.userName}</a
                                    >
                                </div>
                            </div>
                        </TableBodyCell>
                        <TableBodyCell tdClass="p-1"
                            >{item.profileName}
                            <span class="text-gray-700 dark:text-gray-300 text-sm font-light"
                                >by {item.profileUserName}</span
                            ></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1 font-mono text-center">
                            {#if item.gameMoniker}
                                <GameBadge class="m-auto" moniker={item.gameMoniker} />
                            {/if}
                        </TableBodyCell>
                        <TableBodyCell tdClass="p-1 font-mono text-center">
                            {#if item.version}
                                <span
                                    class="py-0.5 px-1 rounded text-xs bg-gray-200 dark:bg-gray-600"
                                >
                                    {item.version}
                                </span>
                            {/if}
                        </TableBodyCell>
                        <TableBodyCell tdClass="p-1 font-mono">{item.seed}</TableBodyCell>
                        <TableBodyCell tdClass="p-1 font-mono flex">
                            <RandoStatusBadge class="m-auto" status={item.status} />
                            {#if getFailReason(item.id)}
                                <Tooltip placement="bottom">{getFailReason(item.id)}</Tooltip>
                            {/if}
                        </TableBodyCell>
                    </TableBodyRow>
                {/each}
            </TableBody>
        </Table>
    {/if}

    <!--
    {#if info}
        <H2>Randos</H2>
        <Table>
            <TableHead>
                <TableHeadCell>Id</TableHeadCell>
                <TableHeadCell>Seed</TableHeadCell>
                <TableHeadCell>Status</TableHeadCell>
                <TableHeadCell>Start</TableHeadCell>
                <TableHeadCell>Finish</TableHeadCell>
                <TableHeadCell>Fail Reason</TableHeadCell>
            </TableHead>
            <TableBody tableBodyClass="divide-y">
                {#each info.generatedRandos as rando}
                    <TableBodyRow>
                        <TableBodyCell tdClass="p-1 font-mono">{rando.randoId}</TableBodyCell>
                        <TableBodyCell tdClass="p-1 font-mono"
                            ><Badge color="dark">{rando.seed}</Badge></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1"
                            ><RandoStatusBadge status={rando.status} /></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1"
                            ><Timestamp value={rando.startTime} /></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1"
                            ><Timestamp value={rando.finishTime} /></TableBodyCell
                        >
                        <TableBodyCell tdClass="p-1">{rando.failReason}</TableBodyCell>
                    </TableBodyRow>
                {/each}
            </TableBody>
        </Table>
    {/if}
    -->
</PageBody>
