<script lang="ts">
    import { goto } from '$app/navigation';
    import BioRandTitle from '$lib/BioRandTitle.svelte';
    import RoleBadge from '$lib/RoleBadge.svelte';
    import { UserRole, getApi, isLocalhost, type StatsResult, type User } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import {
        Avatar,
        DarkMode,
        Dropdown,
        DropdownDivider,
        DropdownHeader,
        DropdownItem,
        NavBrand,
        NavHamburger,
        NavLi,
        NavUl,
        Navbar,
        Tooltip
    } from 'flowbite-svelte';
    import { ShuffleOutline } from 'flowbite-svelte-icons';
    import ApiSwitch from './ApiSwitch.svelte';

    export let currentUser: User | undefined;
    export let stats: StatsResult | undefined;

    $: role = currentUser?.role || UserRole.Pending;
    $: accountAccessible = role >= UserRole.EarlyAccess && role != UserRole.System;
    $: isAdmin = role == UserRole.Administrator;

    async function onSignOutClick() {
        const api = getApi();
        await api.signOut();
        const userManager = getUserManager();
        userManager.signOut();
        await goto('/');
    }
</script>

<Navbar class="px-2 sm:px-4 py-2.5 fixed w-full z-20 top-0 start-0 border-b" fluid={true}>
    <NavBrand href="/">
        <img src="/assets/umbrella.png" class="me-3 h-6 sm:h-9" alt="BioRand Logo" />
        <BioRandTitle />
        {#if stats}
            <div
                class="inline relative left-3 top-3 px-2 rounded h-6 text-black dark:text-white bg-gray-300 dark:bg-blue-800"
            >
                <ShuffleOutline class="w-3 h-3 inline align-top mt-1.5" />
                <span class="text-sm align-top">{stats.randoCount}</span>
            </div>
            <Tooltip placement="bottom">Randomizers generated</Tooltip>
        {/if}
    </NavBrand>
    {#if currentUser}
        <div class="flex items-center md:order-2">
            <div class="mr-4 hidden sm:block">
                <RoleBadge {role} />
            </div>
            <Avatar id="avatar-menu" class="overflow-hidden">
                <img alt="" src={currentUser.avatarUrl} />
            </Avatar>
            <NavHamburger class1="w-full md:flex md:w-auto md:order-1" />
            <div class="ml-3">
                <DarkMode />
            </div>
            {#if isLocalhost()}
                <ApiSwitch />
            {/if}
        </div>
        <Dropdown class="min-w-48" placement="bottom" triggeredBy="#avatar-menu">
            <DropdownHeader>
                <a class="block text-sm" href="/user/{currentUser.name}">{currentUser.name}</a>
            </DropdownHeader>
            {#if currentUser.role == UserRole.Administrator}
                <DropdownItem href="/admin/patron">Patron</DropdownItem>
                <DropdownDivider />
            {/if}
            <DropdownItem on:click={onSignOutClick}>Sign out</DropdownItem>
        </Dropdown>
        {#if accountAccessible}
            <NavUl>
                <NavLi href="/">Generate</NavLi>
                <NavLi href="/history">History</NavLi>
                <NavLi href="/profiles">Profiles</NavLi>
                {#if isAdmin}
                    <NavLi href="/users">Users</NavLi>
                {/if}
            </NavUl>
        {/if}
    {:else}
        <div class="ml-3 inline-flex">
            <div class="py-3 mr-2 text-sm">
                <a href="/login">Sign in</a>
            </div>
            <DarkMode />
            {#if isLocalhost()}
                <ApiSwitch />
            {/if}
        </div>
    {/if}
</Navbar>
