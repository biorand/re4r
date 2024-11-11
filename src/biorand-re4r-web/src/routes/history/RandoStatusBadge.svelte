<script lang="ts">
    import { RandoStatus } from '$lib/api';
    import { Badge } from 'flowbite-svelte';

    let className = '';
    export { className as class };
    export let status: RandoStatus;

    const roles = [
        ['Unknown', 'blue'],
        ['Unassigned', 'yellow'],
        ['Processing', 'yellow'],
        ['Completed', 'green'],
        ['Failed', 'red'],
        ['Expired', 'blue']
    ];
    $: roleName = roles[status][0];
    $: roleColor = roles[status][1] as any;
</script>

<Badge class={className} color={roleColor}>{roleName}</Badge>
