<script lang="ts">
    import { page } from '$app/stores';
    import RoleBadge from '$lib/RoleBadge.svelte';
    import { validateClear, validateFormInputData, type FormInputData } from '$lib/Validation';
    import { UserRole, getApi, getWebsiteTitle, type User } from '$lib/api';
    import PageBody from '$lib/typography/PageBody.svelte';
    import PageTitle from '$lib/typography/PageTitle.svelte';
    import { getUserManager } from '$lib/userManager';
    import {
        Alert,
        Button,
        Checkbox,
        Helper,
        Input,
        Label,
        Select,
        Spinner
    } from 'flowbite-svelte';
    import {
        CloseCircleSolid,
        EnvelopeSolid,
        ExclamationCircleOutline,
        UserSolid
    } from 'flowbite-svelte-icons';
    import FormInput from '../../auth/FormInput.svelte';
    import KofiConnection from './KofiConnection.svelte';
    import TwitchConnection from './TwitchConnection.svelte';

    const userName = $page.params.user;
    let user: User | undefined;

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
    let kofiEmailData: FormInputData = {
        key: 'kofiEmail',
        value: ''
    };
    let role = UserRole.Pending;
    let shareHistory = false;
    let isKofiMember = false;
    let isTwitchMember = false;
    let isPatron = false;
    let serverWait = false;
    let serverMessage = '';

    let roles = [
        { name: '(pending)', value: UserRole.Pending },
        { name: 'Standard (pending)', value: UserRole.PendingStandard },
        { name: 'Banned', value: UserRole.Banned },
        { name: 'Standard', value: UserRole.Standard },
        { name: 'Tester', value: UserRole.Tester },
        { name: 'Patron', value: UserRole.Patron },
        { name: 'Administrator', value: UserRole.Administrator },
        { name: 'System', value: UserRole.System },
        { name: 'Long Term Supporter', value: UserRole.LongTermSupporter }
    ];

    let init = (async () => {
        const api = getApi();
        user = await api.getUser(userName);
        user.tags = [];
        emailData = { ...emailData, value: user.email };
        nameData = { ...nameData, value: user.name };
        kofiEmailData = { ...kofiEmailData, value: user.kofiEmail };
        role = user.role;
        shareHistory = user.shareHistory;
        isKofiMember = !!user.tags?.find((x) => x == 're4r:patron/kofi');
        isTwitchMember = !!user.tags?.find((x) => x == 're4r:patron/twitch');
        isPatron = !!user.tags?.find((x) => x.startsWith('re4r:patron/'));
        return user;
    })();

    async function onSubmit() {
        if (!user) return;

        [emailData, nameData, kofiEmailData] = validateClear(emailData, nameData, kofiEmailData);
        serverWait = true;
        try {
            const api = getApi();
            const result = await api.updateUser(user.id, {
                email: emailData.value,
                name: nameData.value,
                kofiEmail: kofiEmailData.value,
                role,
                shareHistory
            });
            if (result.success) {
            } else {
                [emailData, nameData, kofiEmailData] = validateFormInputData(
                    result.validation,
                    emailData,
                    nameData,
                    kofiEmailData
                );
            }
        } catch {
            serverMessage = 'Failed to update user due to server error.';
        } finally {
            serverWait = false;
        }
    }

    async function reverifyKofiEmail() {
        if (!user) return;

        const api = getApi();
        await api.reverifyKofiEmail(user.id);
    }
</script>

<svelte:head>
    <title>{getWebsiteTitle(user?.name || userName)}</title>
</svelte:head>

{#await init}
    <Spinner />
{:then}
    {#if user}
        <PageBody>
            <PageTitle>{user.name}</PageTitle>
            <form on:submit={onSubmit}>
                <div class="mb-3">
                    <Label for="role" class="block mb-2">Profile Picture</Label>
                    <Alert border color="default">
                        In order to change your profile picture, please upload an image using
                        <a
                            href="https://gravatar.com/profile"
                            target="_blank"
                            class="font-medium text-primary-600 hover:underline dark:text-primary-500"
                        >
                            Gravatar</a
                        >
                        for your registered email address. Alternatively you can connect your Twitch
                        account and use your Twitch profile picture.
                    </Alert>
                </div>
                {#if isAdmin}
                    <div class="max-w-60">
                        <Label for="id" class="block mb-2">User Id</Label>
                        <Input class="mb-3" name="id" readonly value={user.id} />
                    </div>
                {/if}
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
                {#if isAdmin}
                    <div class="max-w-5xl">
                        <Label for="tags" class="block mb-2">Tags</Label>
                        <Input class="mb-3 font-mono" name="tags" value={user.tags?.join(',')} />
                    </div>
                {/if}
                <div>
                    <Checkbox
                        class="mb-3 inline-block"
                        id="public-history"
                        bind:checked={shareHistory}
                        >Allow other users to view your randomizer history</Checkbox
                    >
                </div>
                <div class="mb-3">
                    <Label for="role" class="block mb-2">Ko-fi</Label>
                    <KofiConnection isMember={isKofiMember} showBenefits={!isPatron} />
                </div>
                <div class="max-w-5xl">
                    <FormInput
                        id="kofi-email"
                        type="email"
                        label="Ko-fi Email Address"
                        placeholder={user.email}
                        help="Set this if your ko-fi registered email address is different to your biorand email address."
                        icon={EnvelopeSolid}
                        data={kofiEmailData}
                    >
                        <div slot="right">
                            {#if user.kofiEmail}
                                {#if user.kofiEmailVerified}
                                    <div class="text-sm text-green-400">Verified</div>
                                {:else}
                                    <div class="text-sm text-red-400">Unverified</div>
                                {/if}
                            {/if}
                        </div>
                        <div slot="button">
                            {#if user.kofiEmail && !user.kofiEmailVerified}
                                <Button on:click={reverifyKofiEmail}
                                    >Re-send verification email</Button
                                >
                            {/if}
                        </div>
                    </FormInput>
                </div>
                <div class="mb-3">
                    <Label for="role" class="block mb-2">Twitch</Label>
                    <TwitchConnection
                        userId={user.id}
                        bind:twitch={user.twitch}
                        showUnsubscribed={!isPatron}
                    />
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
        </PageBody>
    {/if}
{:catch err}
    <Alert border color="red" class="my-4">
        <CloseCircleSolid slot="icon" class="w-5 h-5" />{err}
    </Alert>
{/await}
