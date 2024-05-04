import { derived, get, writable, type Writable } from "svelte/store";
import type { BioRandApi, Config, Profile } from "./api";
import { groupBy, replaceBy } from "./utility";

export type ProfileCategory = 'Personal' | 'Official' | 'Community';

export interface ProfileViewModel {
    id: number;
    name: string;
    description: string;
    config: Config;
    userId: number;
    userName: string;
    starCount: number;
    seedCount: number;

    category: string;
    originalId: number;
    isNew: boolean;
    isModified: boolean;
    isSelected: boolean;

    onSave?: VoidFunction;
    onDuplicate?: VoidFunction;
    onDelete?: VoidFunction;
    onRemove?: VoidFunction;
}

export class UserProfileManager {
    private readonly api: BioRandApi;
    private _downloadedProfiles: ProfileViewModel[] = [];
    private _selectedProfile: ProfileViewModel | undefined;

    readonly userId: number;

    profiles: Writable<ProfileViewModel[]> = writable([]);
    selectedProfile: Writable<ProfileViewModel | undefined> = writable(undefined);

    constructor(api: BioRandApi, userId: number) {
        this.api = api;
        this.userId = userId;

        this.selectedProfile.subscribe(profile => {
            if (profile === undefined)
                return;
            if (profile === this._selectedProfile)
                return;

            if (profile.isModified && profile.userId !== this.userId) {
                // Profile is not ours, create new one
                const originalProfile = this._downloadedProfiles.find(p => p.id === profile?.id);
                const originalProfileName = originalProfile?.name;
                let newProfileName = profile.name;
                if (newProfileName === originalProfileName) {
                    newProfileName = `${originalProfileName} - Copy`;
                }
                profile = <ProfileViewModel>{
                    ...profile,
                    id: 0,
                    name: newProfileName,
                    userId: this.userId,
                    userName: '',

                    category: 'Personal',
                    originalId: profile.id,
                    isNew: true,
                    isSelected: true
                };
            } else {
                profile = { ...profile, isSelected: true };
            }

            this._selectedProfile = this.setProfileActions(profile);
            this.updateProfileList();
            this.selectedProfile.set(this._selectedProfile);
            this.saveStorage();
        });
    }

    private updateProfileList() {
        let profiles = this._downloadedProfiles.map(p => {
            if (p.id === this._selectedProfile?.id) {
                return { ...this._selectedProfile, isSelected: true };
            }
            return p;
        });
        if (this._selectedProfile?.id === 0) {
            profiles = [this._selectedProfile, ...profiles];
        }
        this.profiles.set(profiles);
    }

    async download() {
        const profiles = await this.api.getProfiles();
        this._downloadedProfiles = profiles.map(x => this.toProfileViewModel(x));
        this.updateProfileList();
        this.readStorage();
    }

    private toProfileViewModel(profile: Profile) {
        const category = this.getCategory(profile);
        const result = <ProfileViewModel>{
            id: profile.id,
            name: profile.name,
            description: profile.description,
            userId: profile.userId,
            userName: profile.userName,
            config: profile.config,
            seedCount: profile.seedCount,
            starCount: profile.starCount,

            originalId: profile.id,
            category,
            isNew: false,
            isModified: false,
            isSelected: false
        };
        return this.setProfileActions(result);
    }

    private getCategory(profile: Profile): ProfileCategory {
        if (profile.userId === this.userId)
            return 'Personal';
        else if (profile.userName === 'System')
            return 'Official';
        return 'Community';
    }

    private setProfileActions(profile: ProfileViewModel) {
        const createProfileAction = function (condition: boolean, cb?: () => void) {
            return condition ? () => { if (cb) { cb(); } } : undefined;
        };

        const isSavable = profile.category === 'Personal' && profile.isModified;
        const isUpdatable = profile.category === 'Personal' && profile.id !== 0;
        const isCommunity = profile.category === 'Community';
        const result = <ProfileViewModel>{
            ...profile,
            onSave: createProfileAction(isSavable, () => this.save(result)),
            onDelete: createProfileAction(isUpdatable, () => this.delete(result)),
            onDuplicate: createProfileAction(isUpdatable),
            onRemove: createProfileAction(isCommunity)
        };
        return result;
    }

    private readStorage() {
        const json = localStorage.getItem('userProfileManager');
        if (json) {
            const storage = JSON.parse(json);
            if (storage.modifiedProfile) {
                this.selectedProfile.set(storage.modifiedProfile);
            } else {
                const profiles = get(this.profiles);
                const selectedProfile = profiles.find(x => x.id === storage.selectedProfileId);
                this.selectedProfile.set(selectedProfile);
            }
        }
    }

    private saveStorage() {
        localStorage.setItem('userProfileManager', JSON.stringify({
            selectedProfileId: get(this.selectedProfile)?.id,
            modifiedProfile: get(this.selectedProfile)
        }));
    }

    async save(profile: ProfileViewModel) {
        const p = <Profile>{
            id: profile.id,
            name: profile.name,
            description: profile.description,
            config: profile.config
        };

        if (profile.id === 0) {
            const newProfile = await this.api.createProfile(p);
            const newProfileView = this.toProfileViewModel(newProfile);
            this._downloadedProfiles = [newProfileView, ...this._downloadedProfiles];
            this.selectedProfile.set(newProfileView);
        } else {
            const newProfile = await this.api.updateProfile(p);
            const newProfileView = this.toProfileViewModel(newProfile);
            this._downloadedProfiles = replaceBy(this._downloadedProfiles,
                x => x.id === newProfileView.id, newProfileView);
            this.selectedProfile.set(newProfileView);
        }
    }

    async delete(profile: ProfileViewModel) {
        await this.api.deleteProfile(profile.id);
        this._downloadedProfiles = this._downloadedProfiles.filter(p => p.id !== profile.id);
        this.selectDefaultProfile();
    }

    private selectDefaultProfile() {
        const p = this._downloadedProfiles[0];
        this.selectedProfile.set(p);
    }

    profileGroups = derived(this.profiles, (profiles) => {
        const categoryOrder = ['Personal', 'Official', 'Community'];
        const groups = groupBy(profiles, p => p.category);
        return categoryOrder.map(category => ({
            category,
            profiles: groups[category] || []
        }));
    });
}
