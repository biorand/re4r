import { derived, get, writable, type Writable } from "svelte/store";
import type { BioRandApi, Config, Profile } from "./api";
import { idleTimeout } from "./utility";

export class UserProfileManager {
    private readonly api: BioRandApi;
    private readonly userId: number;
    private currentProfile: Profile | undefined;
    private currentConfig: Config | undefined;

    profiles: Writable<Profile[]> = writable([]);
    tempProfile: Writable<Profile | undefined> = writable(undefined);
    selectedProfile: Writable<Profile | undefined> = writable(undefined);
    config: Writable<Config | undefined> = writable(undefined);

    constructor(api: BioRandApi, userId: number) {
        this.api = api;
        this.userId = userId;
        this.selectedProfile.subscribe(profile => {
            if (profile !== get(this.tempProfile)) {
                this.tempProfile.set(undefined);
            }
            this.currentProfile = profile;
            this.currentConfig = profile?.config;
            this.config.set(profile?.config);
        });

        idleTimeout(1000, (whenIdle) => {
            this.config.subscribe(async config => {
                if (this.currentConfig === config)
                    return;

                this.currentConfig = config;

                const currentProfile = this.currentProfile;
                if (currentProfile?.userId !== this.userId) {
                    const profile = <Profile>{
                        id: currentProfile?.id,
                        configId: 0,
                        name: `${currentProfile?.name} - Copy`,
                        description: currentProfile?.description,
                        userId: this.userId,
                        userName: 'Ahh',
                        isStarred: false,
                        starCount: 0,
                        seedCount: 0,
                        config: config
                    };
                    this.tempProfile.set(profile)
                    this.selectedProfile.set(profile);
                }

                whenIdle(async () => {
                    await this.api.updateTempConfig(currentProfile?.id || 0, config || {});
                })
            });
        });
    }

    async download() {
        this.profiles.set(await this.api.getProfiles());
    }

    profileGroups = derived([this.profiles, this.tempProfile], ([profiles, tempProfile]) => {
        const official = {
            category: 'Official',
            isReadOnly: true,
            profiles: profiles.filter((x) => x.userName == 'System')
        };
        const personal = {
            category: 'Personal',
            isReadOnly: false,
            profiles: (tempProfile ? [tempProfile] : []).concat(profiles.filter((x) => x.userId == this.userId))
        };
        const community = {
            category: 'Community',
            isReadOnly: true,
            profiles: profiles.filter(
                (x) => x.userName != 'System' && x.userId != this.userId
            )
        };
        return [personal, official, community];
    });
}
