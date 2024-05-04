<script lang="ts">
    import { goto } from '$app/navigation';
    import BioRandTitle from '$lib/BioRandTitle.svelte';
    import RoleBadge from '$lib/RoleBadge.svelte';
    import { UserRole, type User } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import {
        Avatar,
        DarkMode,
        Dropdown,
        DropdownHeader,
        DropdownItem,
        NavBrand,
        NavHamburger,
        NavLi,
        NavUl,
        Navbar
    } from 'flowbite-svelte';

    export let currentUser: User | undefined = undefined;

    $: role = currentUser?.role || UserRole.Pending;
    $: accountAccessible = role >= UserRole.EarlyAccess && role != UserRole.System;
    $: isAdmin = role == UserRole.Administrator;

    async function onSignOutClick() {
        const userManager = getUserManager();
        userManager.signOut();
        await goto('/');
    }
</script>

<Navbar class="px-2 sm:px-4 py-2.5 fixed w-full z-20 top-0 start-0 border-b" fluid={true}>
    <NavBrand href="/">
        <img src="/assets/umbrella.png" class="me-3 h-6 sm:h-9" alt="BioRand Logo" />
        <BioRandTitle />
    </NavBrand>
    {#if currentUser}
        <div class="flex items-center md:order-2">
            <div class="mr-4">
                <RoleBadge {role} />
            </div>
            <Avatar id="avatar-menu">
                <img alt="" src={currentUser.avatarUrl} />
            </Avatar>
            <NavHamburger class1="w-full md:flex md:w-auto md:order-1" />
            <div class="ml-3">
                <DarkMode />
            </div>
        </div>
        <Dropdown class="min-w-48" placement="bottom" triggeredBy="#avatar-menu">
            <DropdownHeader>
                <a class="block text-sm" href="/user/{currentUser.name}">{currentUser.name}</a>
            </DropdownHeader>
            <DropdownItem on:click={onSignOutClick}>Sign out</DropdownItem>
        </Dropdown>
        {#if accountAccessible}
            <NavUl>
                <NavLi href="/">Generate</NavLi>
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
        </div>
    {/if}
</Navbar>
