<script lang="ts">
    import { page } from '$app/stores';
    import RoleBadge from '$lib/RoleBadge.svelte';
    import { validateClear, validateFormInputData, type FormInputData } from '$lib/Validation';
    import { UserRole, getApi, type User } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import { Button, Checkbox, Helper, Label, Select, Spinner } from 'flowbite-svelte';
    import { EnvelopeSolid, ExclamationCircleOutline, UserSolid } from 'flowbite-svelte-icons';
    import FormInput from '../../auth/FormInput.svelte';

    const userName = $page.params.user;
    let user: User;

    const userManager = getUserManager();
    let isAdmin = userManager.info?.user.role == UserRole.Administrator;

    let emailData: FormInputData = {
        key: 'email',
        value: ''
    };
    let nameData: FormInputData = {
        key: 'name',
        value: ''
    };
    let role = UserRole.Pending;
    let shareHistory = false;
    let serverWait = false;
    let serverMessage = '';

    let roles = [
        { name: '(pending)', value: UserRole.Pending },
        { name: 'Early Access (pending)', value: UserRole.PendingEarlyAccess },
        { name: 'Banned', value: UserRole.Banned },
        { name: 'Early Access', value: UserRole.EarlyAccess },
        { name: 'Tester', value: UserRole.Tester },
        { name: 'Standard', value: UserRole.Standard },
        { name: 'Administrator', value: UserRole.Administrator },
        { name: 'System', value: UserRole.System }
    ];

    let init = (async () => {
        const api = getApi();
        user = await api.getUser(userName);
        emailData = { ...emailData, value: user.email };
        nameData = { ...nameData, value: user.name };
        role = user.role;
        shareHistory = user.shareHistory;
        return user;
    })();

    async function onSubmit() {
        [emailData, nameData] = validateClear(emailData, nameData);
        serverWait = true;
        try {
            const api = getApi();
            const result = await api.updateUser(user.id, {
                email: emailData.value,
                name: nameData.value,
                role,
                shareHistory
            });
            if (result.success) {
            } else {
                [emailData, nameData] = validateFormInputData(
                    result.validation,
                    emailData,
                    nameData
                );
            }
        } catch {
            serverMessage = 'Failed to update user due to server error.';
        } finally {
            serverWait = false;
        }
    }
</script>

{#await init}
    <Spinner />
{:then}
    <div class="container mx-auto mb-3 p-3">
        <h1 class="mb-3 text-4xl dark:text-white">{user.name}</h1>
        <form on:submit={onSubmit}>
            <div class="max-w-3xl">
                <FormInput
                    id="email"
                    type="email"
                    label="Email Address"
                    required={true}
                    disabled={!isAdmin}
                    icon={EnvelopeSolid}
                    data={emailData}
                />
            </div>
            <div class="max-w-lg">
                <FormInput
                    id="name"
                    type="text"
                    label="Name"
                    required={true}
                    disabled={!isAdmin}
                    minlength={4}
                    maxlength={32}
                    icon={UserSolid}
                    data={nameData}
                />
            </div>
            <div class="max-w-60">
                <Label for="role" class="block mb-2">Role</Label>
                {#if isAdmin}
                    <Select
                        id="role"
                        class="mb-3"
                        disabled={!isAdmin}
                        items={roles}
                        bind:value={role}
                    />
                {:else}
                    <RoleBadge class="mb-3" role={user.role} />
                {/if}
            </div>
            <div>
                <Checkbox class="mb-3 inline-block" id="public-history" bind:checked={shareHistory}
                    >Allow other users to view your randomizer history</Checkbox
                >
            </div>
            {#if serverMessage}
                <div>
                    <Helper class="mb-3 inline-flex" color="red">
                        <ExclamationCircleOutline class="w-4 h-4 me-2" />{serverMessage}
                    </Helper>
                </div>
            {/if}
            <Button type="submit" color="blue">
                {#if serverWait}
                    <Spinner class="me-3" size="4" color="white" />
                {/if}
                Save Changes
            </Button>
        </form>
    </div>
{/await}
