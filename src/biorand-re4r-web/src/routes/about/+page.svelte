<script>
    import { H2, Li, P, PageBody, PageTitle, TwitchUser, Ul } from '$lib/typography';
    import { A } from 'flowbite-svelte';
</script>

<PageBody>
    <PageTitle>About</PageTitle>
    <P>BioRand 4 is a cloud randomizer for CAPCOM's 2023 remake of Resident Evil 4.</P>
    <P>
        Randomizers can be configured and generated to improve replayability by enhancing the
        gameplay. Some of the changes BioRand can make are:
    </P>
    <Ul>
        <Li>Randomize the starting inventory</Li>
        <Li>Randomize the enemy types</Li>
        <Li>Randomize the enemy health</Li>
        <Li>Randomize the enemy item drops</Li>
        <Li>Randomize the items found throughout the campaign</Li>
        <Li>Randomize what the merchant sells</Li>
        <Li>Randomize the price of items and upgrades</Li>
        <Li>Add new enemy spawns</Li>
    </Ul>
    <P>
        To generate a randomizer, <A href="/">sign up</A> for an account and then <A href="/signin"
            >sign in</A
        > using your e-mail address and user name. Once you have signed in, you will be able to configure
        a profile which controls how BioRand randomizes the game. Once you are happy with your configuration,
        you can then generate and download a new unique seed in the form of a .pak file or <A
            href="https://github.com/fluffy-mods/ModManager">fluffy</A
        > mod.
    </P>

    <H2>Contributors</H2>
    <P>
        BioRand is created and developed by <TwitchUser user="IntelOrca" /> and the BioRand team.
    </P>
    <Ul>
        <Li><TwitchUser user="IntelOrca" /></Li>
        <Li
            ><TwitchUser user="AfkKun" />
            <span class="dark:text-gray-300">- Modding information and discovery</span></Li
        >
        <Li
            ><TwitchUser user="MightyKusKus" />
            <span class="dark:text-gray-300">- Game asset modification (inc. Del Orca)</span></Li
        >
        <Li
            ><TwitchUser user="DoubleEdger" />
            <span class="dark:text-gray-300">- Stress testing (10x enemy stability)</span></Li
        >
    </Ul>

    <P>Special thanks to early access testers including but not limited to:</P>
    <Ul>
        <Li><TwitchUser user="404scamnotfound" /></Li>
        <Li><TwitchUser user="7rayD" /></Li>
        <Li><TwitchUser user="Death2024" /></Li>
        <Li><TwitchUser user="ElonNuttMusk" /></Li>
        <Li
            ><TwitchUser
                user="Glitch5970"
                href="https://next.nexusmods.com/profile/Glitch5970"
            /></Li
        >
        <Li><TwitchUser user="Matthewsaurusrex" /></Li>
        <Li><TwitchUser user="ShinyPockets" /></Li>
    </Ul>
</PageBody>
