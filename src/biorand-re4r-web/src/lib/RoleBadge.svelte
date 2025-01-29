<script lang="ts">
    import { Badge } from 'flowbite-svelte';
    import type { User } from './api';
    import { hasUserTag } from './userManager';

    let className = '';
    export { className as class };
    export let user: User;

    let roleName = 'Standard';
    let roleColor: any = 'yellow';

    $: {
        const roles = [
            ['banned', 'Banned', 'red'],
            ['system', 'System', 'blue'],
            ['admin', 'Administrator', 'blue'],
            ['$GAME:tester', 'Tester', 'yellow'],
            ['$GAME:patron/long', 'Long Term Supporter', 'green'],
            ['$GAME:patron', 'Patron', 'green'],
            ['pending', '(pending)', 'pink']
        ];
        roleName = 'Standard';
        roleColor = 'yellow';
        for (const role of roles) {
            if (hasUserTag(user, role[0])) {
                roleName = role[1];
                roleColor = role[2];
            }
        }
    }
</script>

<Badge class={className} color={roleColor}>{roleName}</Badge>
