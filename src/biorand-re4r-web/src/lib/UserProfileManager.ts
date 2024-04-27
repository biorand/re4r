import { derived, get, writable, type Writable } from "svelte/store";
import type { BioRandApi, Profile } from "./api";

export class UserProfileManager {
    private readonly api: BioRandApi;
    private readonly userId: number;

    profiles: Writable<Profile[]> = writable([]);
    selectedProfile: Writable<Profile | undefined> = writable(undefined);
    tempProfile: Writable<Profile | undefined> = writable(undefined);

    constructor(api: BioRandApi, userId: number) {
        this.api = api;
        this.userId = userId;
        this.readStorage();

        this.selectedProfile.subscribe(profile => {
            if (profile === undefined)
                return;

            const index = get(this.profiles).indexOf(profile);
            if (index == -1) {
                if (profile.userId !== this.userId) {
                    profile = <Profile>{
                        id: 0,
                        configId: 0,
                        name: `${profile?.name} - Copy`,
                        description: profile?.description,
                        userId: this.userId,
                        userName: 'Ahh',
                        isStarred: false,
                        starCount: 0,
                        seedCount: 0,
                        config: profile.config
                    };
                    this.selectedProfile.set(profile);
                }
            }

            if (profile.id === 0) {
                this.tempProfile.set(profile);
            } else {
                this.tempProfile.set(undefined);
            }

            this.saveStorage();
        });
    }

    async download() {
        this.profiles.set(await this.api.getProfiles());
    }

    private readStorage() {
        const json = localStorage.getItem('userProfileManager');
        if (json) {
            const storage = JSON.parse(json);
            if (storage.selectedProfile === 0) {
                this.selectedProfile.set(storage.profile);
            }
        }
    }

    private saveStorage() {
        localStorage.setItem('userProfileManager', JSON.stringify({
            selectedProfile: get(this.selectedProfile)?.id ?? 0,
            profile: get(this.selectedProfile)
        }));
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
