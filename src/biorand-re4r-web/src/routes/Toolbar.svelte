<script lang="ts">
    import { LocalStorageKeys } from '$lib/localStorage';
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
    import { onMount } from 'svelte';
    import { writable } from 'svelte/store';

    let isSignedIn = false;
    let userName = '';
    let email = '';
    let avatarUrl = '';

    async function refreshUser() {
        isSignedIn = userManager.isSignedIn();
        userName = userManager.info?.name || '';
        email = userManager.info?.email || '';
        avatarUrl = await getAvatarUrl(email);
    }

    function onSignOutClick() {
        userManager.signOut();
    }

    const isDarkTheme = writable(false);
    onMount(() => {
        const theme = localStorage.getItem(LocalStorageKeys.Theme) || 'light';
        isDarkTheme.subscribe((value) => {
            const theme = value ? 'dark' : 'light';
            document.documentElement.setAttribute('data-bs-theme', theme);
            localStorage.setItem(LocalStorageKeys.Theme, theme);
        });
        isDarkTheme.set(theme === 'dark');
    });

    const userManager = getUserManager();
    userManager.subscribe(() => {
        refreshUser();
    });
    refreshUser();

    async function digestMessage(message: string) {
        const msgUint8 = new TextEncoder().encode(message); // encode as (utf-8) Uint8Array
        const hashBuffer = await crypto.subtle.digest('SHA-256', msgUint8); // hash the message
        const hashArray = Array.from(new Uint8Array(hashBuffer)); // convert buffer to byte array
        const hashHex = hashArray.map((b) => b.toString(16).padStart(2, '0')).join(''); // convert bytes to hex string
        return hashHex;
    }

    async function getAvatarUrl(email: string) {
        const hash = await digestMessage(email);
        return `https://gravatar.com/avatar/${hash}`;
    }
</script>

<Navbar class="px-2 sm:px-4 py-2.5 fixed w-full z-20 top-0 start-0 border-b" fluid={true}>
    <NavBrand href="/">
        <img src="/umbrella.png" class="me-3 h-6 sm:h-9" alt="BioRand Logo" />
        <span class="self-center whitespace-nowrap text-5xl font-semibold title dark:text-white"
            >BIORAND 4</span
        >
    </NavBrand>
    {#if isSignedIn}
        <div class="flex items-center md:order-2">
            <Avatar id="avatar-menu">
                <img alt="" src={avatarUrl} />
            </Avatar>
            <NavHamburger class1="w-full md:flex md:w-auto md:order-1" />
            <div class="ml-3">
                <DarkMode />
            </div>
        </div>
        <Dropdown class="min-w-48" placement="bottom" triggeredBy="#avatar-menu">
            <DropdownHeader>
                <span class="block text-sm">{userName}</span>
            </DropdownHeader>
            <DropdownItem on:click={onSignOutClick}>Sign out</DropdownItem>
        </Dropdown>
        <NavUl>
            <NavLi href="/">Generate</NavLi>
            <NavLi href="/profiles">Profiles</NavLi>
        </NavUl>
    {:else}
        <div class="ml-3">
            <DarkMode />
        </div>
    {/if}
</Navbar>

<!--
<header class="fixed-top">
    <nav class="navbar navbar-expand-lg bg-body-tertiary">
        <div class="container-fluid">
            <a class="navbar-brand" href="/">BioRand for Resident Evil 4 (2023)</a>
            <button
                class="navbar-toggler"
                type="button"
                data-bs-toggle="collapse"
                data-bs-target="#navbarSupportedContent"
                aria-controls="navbarSupportedContent"
                aria-expanded="false"
                aria-label="Toggle navigation"
            >
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarSupportedContent">
                <ul class="navbar-nav me-auto mb-2 mb-lg-0">
                    <li class="nav-item">
                        <a
                            class="nav-link"
                            class:active={$page.url.pathname == '/'}
                            aria-current="page"
                            href="/">Generate</a
                        >
                    </li>
                    <li class="nav-item">
                        <a
                            class="nav-link"
                            class:active={$page.url.pathname == '/profiles'}
                            href="/profiles">Profiles</a
                        >
                    </li>
                </ul>
                <div class="d-flex">
                    {#if isSignedIn}
                        <a class="nav-link" href="/">{userName}</a>
                        <span class="mx-2">|</span>
                        <a on:click={onSignOutClick} class="nav-link" href="/">Sign out</a>
                    {/if}
                    <div class="ms-3 form-check form-switch">
                        <input
                            bind:checked={$isDarkTheme}
                            class="form-check-input"
                            type="checkbox"
                            role="switch"
                            id="switch-theme"
                        />
                        <label class="form-check-label" for="switch-theme">Dark</label>
                    </div>
                </div>
            </div>
        </div>
    </nav>
</header>
-->

<style>
    .title {
        font-family: 'Resident Evil 7';
        letter-spacing: 2px;
    }
</style>
