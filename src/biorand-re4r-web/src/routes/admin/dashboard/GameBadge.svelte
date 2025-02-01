<script lang="ts">
    import { Badge } from 'flowbite-svelte';

    let className = '';
    export { className as class };
    export let moniker: string;

    const dict: { [key: string]: string } = {
        re2r: 'blue',
        re4r: 'green'
    };
    $: color = dict[moniker] as any;
</script>

<Badge class={className} {color}>{moniker}</Badge>
