<script lang="ts">
    import { Badge } from 'flowbite-svelte';
    import type { UserRole } from './api';

    let className = '';
    export { className as class };
    export let role: UserRole;

    const roles = [
        ['(pending)', 'pink'],
        ['Standard (pending)', 'pink'],
        ['Banned', 'red'],
        ['Standard', 'yellow'],
        ['Patron', 'green'],
        ['Tester', 'yellow'],
        ['Administrator', 'blue'],
        ['System', 'blue'],
        ['Long Term Supporter', 'green']
    ];
    $: roleName = roles[role][0];
    $: roleColor = roles[role][1] as any;
</script>

<Badge class={className} color={roleColor}>{roleName}</Badge>
