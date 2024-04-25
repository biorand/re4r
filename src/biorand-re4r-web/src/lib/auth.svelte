<script lang="ts">
    import { getApi } from './api';

    enum ModeKind {
        Menu,
        Register,
        SignIn,
        SignInCode
    }

    let mode = ModeKind.Register;
    let email = '';
    let name = '';
    let errorMessage = '';
    let pending = false;

    async function onRegisterClick() {
        if (pending) return;
        pending = true;
        try {
            const api = getApi();
            const result = await api.register(email, name);
            if (result.success) {
            } else {
                errorMessage = result.data.reason;
            }
        } catch {
            errorMessage = 'Unable to register';
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
                            value={email}
                        />
                    </div>
                    <div class="mb-3">
                        <label for="txt-name" class="form-label">Name</label>
                        <input
                            type="text"
                            class="form-control"
                            id="txt-name"
                            required
                            value={name}
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
            {:else}
                <form class="col-md-6">
                    <div class="mb-3">
                        <label for="txt-email" class="form-label">Email address</label>
                        <input type="email" class="form-control" id="txt-email" value={email} />
                    </div>
                    <button type="submit" class="btn btn-primary">Sign In</button>
                    <button on:click={() => (mode = ModeKind.Menu)} class="btn btn-secondary"
                        >Cancel</button
                    >
                </form>
            {/if}
        </div>
    </div>
</div>
