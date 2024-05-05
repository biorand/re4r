<script lang="ts">
    import { getApi } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import { Button, Input, Label } from 'flowbite-svelte';
    import { EnvelopeSolid, UserSolid } from 'flowbite-svelte-icons';

    enum ModeKind {
        Menu,
        Register,
        SignIn,
        SignInCode
    }

    let mode = ModeKind.Menu;
    let email = '';
    let name = '';
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
                email = result.email;
                name = result.name;
                mode = ModeKind.SignInCode;
            } else {
                errorMessage = result.message;
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
                    email = result.email;
                    mode = ModeKind.SignInCode;
                } else {
                    mode = ModeKind.Menu;
                    code = '';

                    const userManager = getUserManager();
                    userManager.setSignedIn(
                        result.id,
                        result.email,
                        result.name,
                        result.token || ''
                    );
                }
            } else {
                errorMessage = result.message;
            }
        } catch {
            errorMessage = 'Unable to sign in';
        }
        pending = false;
    }
</script>

<div class="fixed w-full h-full grid place-items-center">
    <div class="bg-gray-100 dark:bg-gray-700 p-4 rounded-lg w-full md:w-6/12 md:max-w-lg">
        {#if mode == ModeKind.Menu}
            <div class="grid gap-4 grid-cols-2">
                <div>
                    <Button on:click={() => (mode = ModeKind.Register)} class="w-full"
                        >Register</Button
                    >
                </div>
                <div>
                    <Button on:click={() => (mode = ModeKind.SignIn)} class="w-full">Sign In</Button
                    >
                </div>
            </div>
        {:else if mode == ModeKind.Register}
            <form>
                <div class="mb-2">
                    <Label for="email" class="block mb-2">Email Address</Label>
                    <Input
                        bind:value={email}
                        id="email"
                        type="email"
                        placeholder="albert.wesker@umbrella.com"
                    >
                        <EnvelopeSolid
                            slot="left"
                            class="w-5 h-5 text-gray-500 dark:text-gray-400"
                        />
                    </Input>
                </div>
                <div class="mb-6">
                    <Label for="name" class="block mb-2">Name</Label>
                    <Input bind:value={name} id="name" type="text" placeholder="awesker">
                        <UserSolid slot="left" class="w-5 h-5 text-gray-500 dark:text-gray-400" />
                    </Input>
                    <p
                        id="helper-text-explanation"
                        class="mt-2 text-sm text-gray-500 dark:text-gray-400"
                    >
                        This should be your Twitch / Discord user name.
                    </p>
                </div>
                <div class="mb-3">
                    <div class="invalid-feedback d-block">{errorMessage}</div>
                </div>
                <Button on:click={onRegisterClick} type="submit" color="blue">Register</Button>
                <Button on:click={() => (mode = ModeKind.Menu)} color="alternative">Cancel</Button>
            </form>
        {:else if mode == ModeKind.SignIn}
            <form>
                <div class="mb-6">
                    <Label for="email" class="block mb-2">Email Address</Label>
                    <Input
                        bind:value={email}
                        id="email"
                        type="email"
                        placeholder="albert.wesker@umbrella.com"
                    >
                        <EnvelopeSolid
                            slot="left"
                            class="w-5 h-5 text-gray-500 dark:text-gray-400"
                        />
                    </Input>
                </div>
                <div class="mb-3">
                    <div class="invalid-feedback d-block">{errorMessage}</div>
                </div>
                <Button on:click={() => onSignInClick(0)} type="submit" color="blue">Sign In</Button
                >
                <Button on:click={() => (mode = ModeKind.Menu)} color="alternative">Cancel</Button>
            </form>
        {:else if mode == ModeKind.SignInCode}
            <form>
                <div class="mb-2">
                    <Label for="email" class="block mb-2">Email Address</Label>
                    <Input
                        bind:value={email}
                        id="email"
                        type="email"
                        placeholder="albert.wesker@umbrella.com"
                        disabled
                    >
                        <EnvelopeSolid
                            slot="left"
                            class="w-5 h-5 text-gray-500 dark:text-gray-400"
                        />
                    </Input>
                </div>
                <div class="mb-6">
                    <Label for="code" class="block mb-2">Code</Label>
                    <Input bind:value={code} id="code" type="password" />
                    <p
                        id="helper-text-explanation"
                        class="mt-2 text-sm text-gray-500 dark:text-gray-400"
                    >
                        Type the code you received in the e-mail. If you are unable to find the
                        e-mail, try checking your spam folder.
                    </p>
                </div>
                <div class="mb-3">
                    <div class="invalid-feedback d-block">{errorMessage}</div>
                </div>
                <Button on:click={() => onSignInClick(1)} type="submit" color="blue">Sign In</Button
                >
                <Button on:click={() => (mode = ModeKind.Menu)} color="alternative">Cancel</Button>
            </form>
        {/if}
    </div>
</div>
