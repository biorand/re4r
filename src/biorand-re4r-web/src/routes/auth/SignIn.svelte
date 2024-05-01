<script lang="ts">
    import { goto } from '$app/navigation';
    import { validateClear, validateFormInputData, type FormInputData } from '$lib/Validation';
    import { getApi } from '$lib/api';
    import { getUserManager } from '$lib/userManager';
    import { Button, Helper, Spinner } from 'flowbite-svelte';
    import {
        CheckCircleOutline,
        EnvelopeSolid,
        ExclamationCircleOutline
    } from 'flowbite-svelte-icons';
    import FormInput from '../auth/FormInput.svelte';

    let emailData: FormInputData = {
        key: 'email',
        value: 'intelorca@gmail.com'
    };
    let codeData: FormInputData = {
        key: 'code',
        value: ''
    };
    let showCodeField = false;
    let serverWait = false;
    let serverMessage = '';

    async function onSubmit() {
        [emailData, codeData] = validateClear(emailData, codeData);
        serverWait = true;
        try {
            const api = getApi();
            if (!showCodeField) {
                const result = await api.signIn(emailData.value!);
                if (result.success) {
                    showCodeField = true;
                } else {
                    [emailData] = validateFormInputData(result.validation, emailData);
                }
            } else {
                const result = await api.signIn(emailData.value!, codeData.value!);
                if (result.success) {
                    const userManager = getUserManager();
                    userManager.setSignedIn(result);
                    await goto('/');
                } else {
                    [codeData] = validateFormInputData(result.validation, codeData);
                }
            }
            serverMessage = '';
        } catch {
            serverMessage = 'Failed to sign in due to server error.';
        } finally {
            serverWait = false;
        }
    }
</script>

<div class="w-full p-4">
    <div class="bg-gray-100 dark:bg-gray-700 p-4 max-w-96 rounded-lg mx-auto">
        <form on:submit={onSubmit}>
            <fieldset disabled={serverWait}>
                <FormInput
                    id="email"
                    type="email"
                    label="Email Address"
                    required={true}
                    disabled={showCodeField}
                    placeholder="albert.wesker.umbrella.com"
                    icon={EnvelopeSolid}
                    data={emailData}
                />
                {#if showCodeField}
                    <FormInput
                        id="code"
                        type="password"
                        label="Code"
                        required={true}
                        minlength={6}
                        maxlength={6}
                        icon={CheckCircleOutline}
                        help="An email has been sent containing your authorization code."
                        data={codeData}
                    />
                {/if}
                {#if serverMessage}
                    <Helper class="mb-3 inline-flex" color="red">
                        <ExclamationCircleOutline class="w-4 h-4 me-2" />{serverMessage}
                    </Helper>
                {/if}

                <Button type="submit" color="blue" class="w-full">
                    {#if serverWait}
                        <Spinner class="me-3" size="4" color="white" />
                    {/if}
                    Sign In
                </Button>
            </fieldset>
        </form>
    </div>
</div>
