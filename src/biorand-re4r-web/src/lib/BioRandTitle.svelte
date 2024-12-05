<script lang="ts">
    import { getGameId } from './api';
</script>

<span class="text-5xl">
    {#if getGameId() == 1}
        <b>B</b>IORAND <b>4</b>
    {:else}
        <b>B</b>IORAND <b>2</b>
    {/if}
</span>

<style>
    @font-face {
        font-family: 'Resident Evil 7';
        src: url('/assets/re7.otf');
    }

    span {
        font-family: 'Resident Evil 7';
        letter-spacing: 2px;
        white-space: nowrap;
    }

    b {
        color: #c33;
    }
</style>
