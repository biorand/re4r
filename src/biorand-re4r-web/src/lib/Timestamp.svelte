<script lang="ts">
    import { Tooltip } from 'flowbite-svelte';

    export let value: number;
    $: date = toLocaleDate(value);
    $: dateFriendly = date.toLocaleString();
    $: timeAgo = getTimeAgo(value);

    setInterval(() => {
        timeAgo = getTimeAgo(value);
    }, 30000);

    function toLocaleDate(t: number) {
        return new Date(t * 1000);
    }

    function getTimeAgo(timestamp: number) {
        const currentTime = new Date().getTime();
        const timeDifference = currentTime - timestamp * 1000;
        if (timeDifference < 60000) {
            return 'A few seconds ago';
        }

        const seconds = Math.floor(timeDifference / 1000);
        const minutes = Math.floor(seconds / 60);
        const hours = Math.floor(minutes / 60);
        const days = Math.floor(hours / 24);
        const months = Math.floor(days / 30);
        const years = Math.floor(months / 12);

        if (years > 0) {
            return `${years} year${years > 1 ? 's' : ''} ago`;
        } else if (months > 0) {
            return `${months} month${months > 1 ? 's' : ''} ago`;
        } else if (days > 0) {
            return `${days} day${days > 1 ? 's' : ''} ago`;
        } else if (hours > 0) {
            return `${hours} hour${hours > 1 ? 's' : ''} ago`;
        } else if (minutes > 0) {
            return `${minutes} minute${minutes > 1 ? 's' : ''} ago`;
        } else {
            return `${seconds} second${seconds > 1 ? 's' : ''} ago`;
        }
    }
</script>

<span data-unix={value}>{timeAgo}</span>
<Tooltip>{dateFriendly}</Tooltip>
