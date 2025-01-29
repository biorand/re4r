<script lang="ts">
    import { CloseCircleSolid, HourglassSolid, LockSolid } from 'flowbite-svelte-icons';

    export let userTags: string[];

    let icon: any;
    let title: string;
    let body: string;

    let hasTag = (label: string) => userTags.indexOf(label) != -1;
    if (hasTag('pending')) {
        icon = HourglassSolid;
        title = 'Account Pending';
        body =
            'Your account status is currently pending e-mail verification. ' +
            'Please verify the e-mail address registered with the account.';
    } else if (hasTag('banned')) {
        icon = LockSolid;
        title = 'Account Locked';
        body =
            'Your account is currently locked. ' + 'Contact an administrator for more information.';
    } else if (hasTag('system')) {
        icon = CloseCircleSolid;
        title = 'Account Invalid';
        body = 'This is a system account, and should not be signed in to.';
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
