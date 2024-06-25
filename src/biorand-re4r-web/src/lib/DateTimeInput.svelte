<script lang="ts">
    import { Input } from 'flowbite-svelte';

    export let value: number = 0;

    let lastValue = 0;
    let dateTimeLocalValue = '';

    $: {
        if (lastValue != value) {
            lastValue = value;
            dateTimeLocalValue = unix2datetime(value);
        }
    }

    function dateChanged(e: Event) {
        const el = e.target as HTMLInputElement;
        lastValue = el.valueAsNumber / 1000;
        value = lastValue;
    }

    function unix2datetime(unixTimestamp: number) {
        const date = new Date(unixTimestamp * 1000);
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hour = String(date.getHours()).padStart(2, '0');
        const min = String(date.getMinutes()).padStart(2, '0');
        return `${year}-${month}-${day}T${hour}:${min}`;
    }
</script>

<Input
    {...$$restProps}
    type="datetime-local"
    name="date"
    required
    bind:value={dateTimeLocalValue}
    on:change={dateChanged}
/>
