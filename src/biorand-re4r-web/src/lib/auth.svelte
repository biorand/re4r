<script lang="ts">
    import { getApi } from './api';
    import { getUserManager } from './userManager';

    enum ModeKind {
        Menu,
        Register,
        SignIn,
        SignInCode
    }

    let mode = ModeKind.SignIn;
    let email = 'intelorca@gmail.com';
    let name = 'IntelOrca';
    let code = '';
    let errorMessage = '';
    let pending = false;

    async function onRegisterClick() {
        if (pending) return;
        pending = true;
        try {
            const api = getApi();
            const result = await api.register(email, name);
            if (result.success) {
                email = result.data.email;
                name = result.data.name;
                mode = ModeKind.SignInCode;
            } else {
                errorMessage = result.data.reason;
            }
        } catch {
            errorMessage = 'Unable to register';
        }
        pending = false;
    }

    async function onSignInClick(phase: number) {
        if (phase !== 0 && code == '') return;

        if (pending) return;
        pending = true;
        try {
            const api = getApi();
            const result = await api.signIn(email, phase == 0 ? undefined : code);
            if (result.success) {
                if (phase == 0) {
                    email = result.data.email;
                    mode = ModeKind.SignInCode;
                } else {
                    mode = ModeKind.Menu;
                    code = '';

                    const userManager = getUserManager();
                    userManager.setSignedIn(result.data.email, result.data.name, result.data.token);
                }
            } else {
                errorMessage = result.data.reason;
            }
        } catch {
            errorMessage = 'Unable to sign in';
        }
        pending = false;
    }
</script>

<div class="container d-flex justify-content-center align-items-center vh-100">
    <div class="col-md-6">
        <div class="card p-3">
            {#if mode == ModeKind.Menu}
                <div class="text-center">
                    <div class="m-2">
                        <button on:click={() => (mode = 1)} class="btn btn-lg btn-primary w-50"
                            >Register</button
                        >
                    </div>
                    <div class="m-2">
                        <button on:click={() => (mode = 2)} class="btn btn-lg btn-secondary w-50"
                            >Sign in</button
                        >
                    </div>
                </div>
            {:else if mode == ModeKind.Register}
                <form>
                    <div class="mb-3">
                        <label for="txt-email" class="form-label">Email address</label>
                        <input
                            type="email"
                            class="form-control"
                            id="txt-email"
                            required
                            bind:value={email}
                        />
                    </div>
                    <div class="mb-3">
                        <label for="txt-name" class="form-label">Name</label>
                        <input
                            type="text"
                            class="form-control"
                            id="txt-name"
                            required
                            bind:value={name}
                        />
                        <div class="form-text">User name you use for Twitch or Discord.</div>
                    </div>
                    <div class="mb-3">
                        <div class="invalid-feedback d-block">{errorMessage}</div>
                    </div>
                    <button on:click={onRegisterClick} type="submit" class="btn btn-primary">
                        <span
                            class="spinner-border spinner-border-sm"
                            class:d-none={!pending}
                            aria-hidden="true"
                        ></span>
                        <span role="status">Register</span>
                    </button>
                    <button on:click={() => (mode = ModeKind.Menu)} class="btn btn-secondary"
                        >Cancel</button
                    >
                </form>
            {:else if mode == ModeKind.SignIn}
                <form class="col-md-6">
                    <div class="mb-3">
                        <label for="txt-email" class="form-label">Email address</label>
                        <input type="email" class="form-control" id="txt-email" value={email} />
                    </div>
                    <div class="mb-3">
                        <div class="invalid-feedback d-block">{errorMessage}</div>
                    </div>
                    <button on:click={() => onSignInClick(0)} type="submit" class="btn btn-primary"
                        >Sign In</button
                    >
                    <button on:click={() => (mode = ModeKind.Menu)} class="btn btn-secondary"
                        >Cancel</button
                    >
                </form>
            {:else if mode == ModeKind.SignInCode}
                <form>
                    <div class="mb-3">
                        <label for="txt-email" class="form-label">Email address</label>
                        <input
                            type="email"
                            class="form-control"
                            id="txt-email"
                            disabled
                            bind:value={email}
                        />
                    </div>
                    <div class="mb-3">
                        <label for="txt-code" class="form-label">Code</label>
                        <input
                            type="text"
                            maxlength="6"
                            class="form-control form-control-lg"
                            id="txt-code"
                            required
                            bind:value={code}
                        />
                        <div class="form-text">
                            Type the code you received in the e-mail. If you are unable to find the
                            e-mail, try checking your spam folder.
                        </div>
                    </div>
                    <div class="mb-3">
                        <div class="invalid-feedback d-block">{errorMessage}</div>
                    </div>
                    <button on:click={() => onSignInClick(1)} type="submit" class="btn btn-primary"
                        >Sign In</button
                    >
                    <button on:click={() => (mode = ModeKind.Menu)} class="btn btn-secondary"
                        >Cancel</button
                    >
                </form>
            {/if}
        </div>
    </div>
</div>

<style>
    #txt-code {
        font-family: monospace;
    }
</style>
