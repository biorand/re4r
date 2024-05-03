<script lang="ts">
    import { Badge } from 'flowbite-svelte';
    import type { UserRole } from './api';

    export let role: UserRole;

    const roles = [
        ['(pending)', 'pink'],
        ['Early Access (pending)', 'pink'],
        ['Banned', 'red'],
        ['Early Access', 'yellow'],
        ['Tester', 'yellow'],
        ['Standard', 'dark'],
        ['Administrator', 'blue'],
        ['System', 'blue']
    ];
    $: roleName = roles[role][0];
    $: roleColor = roles[role][1] as any;
</script>

<Badge color={roleColor}>{roleName}</Badge>
