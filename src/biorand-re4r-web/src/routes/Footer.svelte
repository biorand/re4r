<script>
    import {
        Footer,
        FooterLinkGroup,
        FooterLink,
        FooterBrand,
        FooterIcon,
        FooterCopyright
    } from 'flowbite-svelte';
    import {
        FacebookSolid,
        GithubSolid,
        DiscordSolid,
        TwitterSolid,
        XSolid
    } from 'flowbite-svelte-icons';
</script>

<Footer footerType="sitemap">
    <div class="py-6 px-4 bg-gray-700 md:flex md:items-center md:justify-between">
        <FooterCopyright
            spanClass="text-sm text-gray-300 sm:text-center"
            by="BioRand Team"
            copyrightMessage=""
        />
        <FooterLinkGroup
            ulClass="flex flex-wrap items-center mt-3 text-sm text-gray-500 dark:text-gray-400 sm:mt-0"
        >
            <FooterLink href="/about">About</FooterLink>
            <FooterLink href="/privacy">Privacy</FooterLink>
            <FooterLink href="/terms">Terms</FooterLink>
            <FooterLink href="https://biorand.net" target="_blank">Classic RE</FooterLink>
        </FooterLinkGroup>
        <div class="flex mt-4 space-x-6 rtl:space-x-reverse sm:justify-center md:mt-0">
            <FooterIcon href="https://github.com/intelorca/biorand" target="_blank">
                <GithubSolid
                    class="w-5 h-5 text-gray-500 dark:text-gray-500 hover:text-gray-900 dark:hover:text-white"
                />
            </FooterIcon>
            <FooterIcon href="https://discord.gg/8WAr38Svxd" target="_blank">
                <DiscordSolid
                    class="w-5 h-5 text-gray-500 dark:text-gray-500 hover:text-gray-900 dark:hover:text-white"
                />
            </FooterIcon>
            <FooterIcon href="https://x.com/@re_biorand" target="_blank">
                <XSolid
                    class="w-5 h-5 text-gray-500 dark:text-gray-500 hover:text-gray-900 dark:hover:text-white"
                />
            </FooterIcon>
            <FooterIcon href="/"></FooterIcon>
        </div>
    </div>
</Footer>
