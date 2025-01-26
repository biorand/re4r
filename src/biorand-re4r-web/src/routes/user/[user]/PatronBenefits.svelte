<script lang="ts">
    import { getGameId } from '$lib/api';

    const gameId = getGameId();
</script>

<li>Highlighted user name on website.</li>
{#if gameId == 2}
    <li>Early access to RE2R randomizer.</li>
{:else}
    <li>Early access to Separate Ways randomizer.</li>
{/if}
