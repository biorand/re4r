<script lang="ts">
    import { writable } from 'svelte/store';
    import { LocalStorageKeys } from './localStorage';
    import { getUserManager } from './userManager';

    let isSignedIn = false;
    let userName = '';

    function refreshUser() {
        isSignedIn = userManager.isSignedIn();
        userName = userManager.info?.name || '';
    }

    function onSignOutClick() {
        userManager.signOut();
    }

    const theme = localStorage.getItem(LocalStorageKeys.Theme) || 'light';
    const isDarkTheme = writable(false);
    isDarkTheme.subscribe((value) => {
        const theme = value ? 'dark' : 'light';
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem(LocalStorageKeys.Theme, theme);
    });
    isDarkTheme.set(theme === 'dark');

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
    </nav>
</header>
