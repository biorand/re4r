<script lang="ts">
    import { page } from '$app/stores';
    import { LocalStorageKeys } from '$lib/localStorage';
    import { getUserManager } from '$lib/userManager';
    import { onMount } from 'svelte';
    import { writable } from 'svelte/store';

    let isSignedIn = false;
    let userName = '';

    function refreshUser() {
        isSignedIn = userManager.isSignedIn();
        userName = userManager.info?.name || '';
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
</script>

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
