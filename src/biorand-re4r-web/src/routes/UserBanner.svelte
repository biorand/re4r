<script lang="ts">
    import { UserRole } from '$lib/api';
    import {
        CloseCircleSolid,
        HourglassSolid,
        LockSolid,
        LockTimeSolid
    } from 'flowbite-svelte-icons';

    export let role: UserRole | undefined;

    let icon: any;
    let title: string;
    let body: string;

    switch (role) {
        default:
        case UserRole.Pending:
            icon = HourglassSolid;
            title = 'Account Pending';
            body =
                'Your account status is currently pending e-mail verification. ' +
                'Please verify the e-mail address registered with the account.';
            break;
        case UserRole.PendingEarlyAccess:
            icon = LockTimeSolid;
            title = 'Early Access Pending';
            body =
                'Your account status is currently pending early access. ' +
                'You will receive an email notification once access is granted. ' +
                'Alternatively you can <a class="text-blue-400 hover:text-blue-300" href="/user">subscribe</a> via Ko-fi or Twitch to gain access immediately.';
            break;
        case UserRole.Banned:
            icon = LockSolid;
            title = 'Account Locked';
            body =
                'Your account is currently locked. ' +
                'Contact an administrator for more information.';
            break;
        case UserRole.System:
            icon = CloseCircleSolid;
            title = 'Account Invalid';
            body = 'This is a system account, and should not be signed in to.';
            break;
    }
</script>

<section class="py-8 px-4 mx-auto max-w-screen-md text-center lg:py-16 lg:px-12">
    <svelte:component this={icon} class="mx-auto mb-4 w-14 h-14 text-gray-400" />
    <h1
        class="mb-4 text-4xl font-bold tracking-tight leading-none text-gray-900 lg:mb-6 md:text-5xl xl:text-6xl dark:text-white"
    >
        {title}
    </h1>
    <p class="font-light text-gray-500 md:text-lg xl:text-xl dark:text-gray-400">
        {@html body}
    </p>
</section>
