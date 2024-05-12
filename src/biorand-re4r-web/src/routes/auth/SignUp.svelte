<script lang="ts">
    import BioRandTitle from '$lib/BioRandTitle.svelte';
    import { validateClear, validateFormInputData, type FormInputData } from '$lib/Validation';
    import { getApi } from '$lib/api';
    import { A, Button, Helper, Spinner } from 'flowbite-svelte';
    import { EnvelopeSolid, ExclamationCircleOutline, UserSolid } from 'flowbite-svelte-icons';
    import FormInput from './FormInput.svelte';

    let emailData: FormInputData = {
        key: 'email',
        value: ''
    };
    let nameData: FormInputData = {
        key: 'name',
        value: ''
    };
    let serverWait = false;
    let serverMessage = '';
    let success = false;
    let registeredName = '';

    async function onSubmit() {
        [emailData, nameData] = validateClear(emailData, nameData);
        serverWait = true;
        try {
            const api = getApi();
            const result = await api.register(emailData.value!, nameData.value!);
            if (result.success) {
                registeredName = result.name;
                success = true;
            } else {
                [emailData, nameData] = validateFormInputData(
                    result.validation,
                    emailData,
                    nameData
                );
            }
            serverMessage = '';
        } catch {
            serverMessage = 'Failed to register due to server error.';
        } finally {
            serverWait = false;
        }
    }
</script>

{#if !success}
    <div class="sm:flex mx-auto">
        <div class="sm:w-1/2 p-4 flex">
            <div class="sm:grow"></div>
            <div class="grow text-center sm:grow-0 sm:text-left">
                <h1 class="text-5xl"><BioRandTitle /></h1>
                <h2 class="text-2xl">Sign up for early access</h2>
                <div class="mt-4 w-64 mx-auto sm:mx-0">
                    <img
                        style="filter: blur(1px) sepia(50%); "
                        alt=""
                        src="/assets/madchainsaw.jpg"
                    />
                </div>
            </div>
        </div>
        <div class="sm:w-1/2 p-4">
            <div class="bg-gray-100 dark:bg-gray-700 p-4 max-w-96 rounded-lg mx-auto sm:m-0">
                <form on:submit={onSubmit}>
                    <fieldset disabled={serverWait}>
                        <FormInput
                            id="email"
                            type="email"
                            label="Email Address"
                            required={true}
                            placeholder="albert.wesker.umbrella.com"
                            icon={EnvelopeSolid}
                            data={emailData}
                        />
                        <FormInput
                            id="name"
                            type="text"
                            label="Name"
                            required={true}
                            placeholder="awesker2"
                            minlength={4}
                            maxlength={32}
                            icon={UserSolid}
                            help="This should be your Twitch / Discord user name."
                            data={nameData}
                        />
                        {#if serverMessage}
                            <Helper class="mb-3 inline-flex" color="red">
                                <ExclamationCircleOutline class="w-4 h-4 me-2" />{serverMessage}
                            </Helper>
                        {/if}

                        <Button type="submit" color="blue" class="w-full">
                            {#if serverWait}
                                <Spinner class="me-3" size="4" color="white" />
                            {/if}
                            Sign Up
                        </Button>

                        <Helper class="mt-3 text-xs">
                            By signing up, you agree to the <A target="_blank" href="/terms"
                                >terms and conditions</A
                            >.
                        </Helper>
                    </fieldset>
                </form>
            </div>
        </div>
    </div>
{:else}
    <div class="p-4">
        <div class="block w-1/2 mx-auto p-4 rounded-lg bg-gray-100 dark:bg-gray-700">
            <h1 class="text-3xl mb-4">Registration Successful</h1>
            <p>
                Welcome <span class="text-gray-700 dark:text-blue-200">{registeredName}</span>,
            </p>
            <p class="indent-4">
                You have successfully registered for early access. You can <a
                    class="font-medium text-primary-600 hover:underline dark:text-primary-500"
                    href="/login">login</a
                > to check your status.
            </p>
        </div>
    </div>
{/if}
