<script>
    import { Heading, List, Li, A } from 'flowbite-svelte';
</script>

<div class="px-4 mb-4">
    <Heading tag="h2" class="mb-4">About</Heading>
    <p class="mb-3">BioRand 4 is a cloud randomizer for CAPCOM's 2023 remake of Resident Evil 4.</p>
    <p class="mb-3">
        Randomizers can be configured and generated to improve replayability by enhancing the
        gameplay. Some of the changes BioRand can make are:
        <List class="m-4">
            <Li>Randomize the starting inventory</Li>
            <Li>Randomize the enemy types</Li>
            <Li>Randomize the enemy health</Li>
            <Li>Randomize the enemy item drops</Li>
            <Li>Randomize the items found throughout the campaign</Li>
            <Li>Randomize what the merchant sells</Li>
            <Li>Randomize the price of items and upgrades</Li>
            <Li>Add new enemy spawns</Li>
        </List>
    </p>
    <p class="mb-3">
        To generate a randomizer, sign up for an account and then login using your e-mail address
        and user name. Once you have signed in, you will be able to configure a profile which
        controls how BioRand randomizes the game. Once you are happy with your configuration, you
        can then generate and download a new unique seed in the form of a .pak file or <A
            href="https://github.com/fluffy-mods/ModManager">fluffy</A
        > mod.
    </p>

    <Heading tag="h4" class="mb-4">Contributors</Heading>
    <p class="mb-3">BioRand is created and developed by IntelOrca and the BioRand team.</p>
    <List class="mb-3">
        <Li>IntelOrca</Li>
        <Li>AfkKun - Modding information and discovery</Li>
        <Li>MightyKusKus - Game asset modification</Li>
    </List>

    <p class="mb-3">Special thanks to early access testers including but not limited to:</p>
    <List>
        <Li>404scamnotfound</Li>
        <Li>DoubleEdger</Li>
        <Li>Matthewsaurusrex</Li>
    </List>
</div>
