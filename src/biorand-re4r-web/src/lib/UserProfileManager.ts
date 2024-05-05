import { derived, get, writable, type Writable } from "svelte/store";
import type { BioRandApi, Config, Profile } from "./api";
import { groupBy, objectEquals, replaceBy } from "./utility";

export type ProfileCategory = 'Personal' | 'Official' | 'Community';

export interface ProfileGroup {
    category: ProfileCategory;
    profiles: ProfileViewModel[];
}

export interface ProfileViewModel {
    id: number;
    name: string;
    description: string;
    config: Config;
    userId: number;
    userName: string;
    public: boolean;
    starCount: number;
    seedCount: number;

    category: string;
    originalId: number;
    isModified: boolean;
    isSelected: boolean;
    isOwner: boolean;

    onSave?: VoidFunction;
    onDuplicate?: VoidFunction;
    onDelete?: VoidFunction;
    onRemove?: VoidFunction;
}

export class UserProfileManager {
    private readonly api: BioRandApi;
    private _ready = false;
    private _downloadedProfiles: ProfileViewModel[] = [];
    private _selectedProfile: ProfileViewModel | undefined;
    private _stashedProfile: ProfileViewModel | undefined;

    readonly userId: number;

    profiles: Writable<ProfileViewModel[]> = writable([]);
    selectedProfile: Writable<ProfileViewModel | undefined> = writable(undefined);

    constructor(api: BioRandApi, userId: number) {
        this.api = api;
        this.userId = userId;

        this.selectedProfile.subscribe(profile => {
            if (!this._ready)
                return;

            if (profile !== this._selectedProfile) {
                this._stashedProfile = undefined;
                if (profile) {
                    profile.isSelected = true;
                    this._stashedProfile = { ...profile, config: { ...profile.config } };
                }
                this._selectedProfile = profile;
            } else if (profile === undefined) {
                this._stashedProfile = undefined;
            } else {
                if (this._stashedProfile && this.profileModified(this._stashedProfile, profile)) {
                    profile.isModified = true;
                }

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
                        public: false,
                        seedCount: 0,
                        starCount: 0,

                        category: 'Personal',
                        originalId: profile.id,
                        isOwner: true,
                        isSelected: true
                    };
                    this.selectedProfile.set(profile);
                }
            }
            if (profile)
                this.setProfileActions(profile);
            this.updateProfileList();
            this.saveStorage();
        });
    }

    private updateProfileList() {
        let profiles = this._downloadedProfiles.map(p => {
            if (p.id === this._selectedProfile?.id) {
                return this._selectedProfile;
            } else {
                return <ProfileViewModel>{ ...p, config: { ...p.config } };
            }
        });
        if (this._selectedProfile?.id === 0) {
            profiles = [this._selectedProfile, ...profiles];
        }
        this.profiles.set(profiles);
    }

    async download() {
        const profiles = await this.api.getProfiles();
        this._downloadedProfiles = profiles.map(x => this.toProfileViewModel(x));
        this._ready = true;
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
            public: profile.public,

            originalId: profile.id,
            category,
            isModified: false,
            isSelected: false,
            isOwner: profile.userId === this.userId
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
        profile.onSave = createProfileAction(isSavable, () => this.save(profile));
        profile.onDelete = createProfileAction(isUpdatable, () => this.delete(profile));
        profile.onDuplicate = createProfileAction(isUpdatable, () => this.duplicate(profile));
        profile.onRemove = createProfileAction(isCommunity, () => this.remove(profile));
        return profile;
    }

    private readStorage() {
        const json = localStorage.getItem('userProfileManager');
        if (json) {
            const storage = JSON.parse(json);
            if (storage.modifiedProfile) {
                this.selectedProfile.set(storage.modifiedProfile);
            } else {
                this.selectProfileId(storage.selectedProfileId);
            }
        }
    }

    private saveStorage() {
        const selectedProfile = get(this.selectedProfile);
        localStorage.setItem('userProfileManager', JSON.stringify({
            selectedProfileId: selectedProfile?.id,
            modifiedProfile: selectedProfile?.isModified ? selectedProfile : undefined
        }));
    }

    async save(profile: ProfileViewModel) {
        const p = <Profile>{
            id: profile.id,
            name: profile.name,
            description: profile.description,
            public: profile.public,
            config: profile.config
        };

        let newProfileView: ProfileViewModel;
        if (profile.id === 0) {
            const newProfile = await this.api.createProfile(p);
            newProfileView = this.toProfileViewModel(newProfile);
            this._downloadedProfiles = [newProfileView, ...this._downloadedProfiles];
        } else {
            const newProfile = await this.api.updateProfile(p);
            newProfileView = this.toProfileViewModel(newProfile);
            this._downloadedProfiles = replaceBy(this._downloadedProfiles,
                x => x.id === newProfileView.id, newProfileView);
            this._selectedProfile = undefined;
        }
        this.updateProfileList();
        this.selectProfileId(newProfileView.id);
    }

    duplicate(profile: ProfileViewModel) {
        const newProfile = <ProfileViewModel>{
            ...profile,
            id: 0,
            name: `${profile.name} - Copy`,
            starCount: 0,
            seedCount: 0,
            config: profile.config,
            isModified: true,
            isOwner: true
        };
        this.selectedProfile.set(newProfile);
    }

    async remove(profile: ProfileViewModel) {
        await this.api.setProfileStar(profile.id, false);
        this._downloadedProfiles = this._downloadedProfiles.filter(p => p.id !== profile.id);
        this.updateProfileList();
        this.selectDefaultProfile();
    }

    async delete(profile: ProfileViewModel) {
        await this.api.deleteProfile(profile.id);
        this._downloadedProfiles = this._downloadedProfiles.filter(p => p.id !== profile.id);
        this.updateProfileList();
        this.selectDefaultProfile();
    }

    private selectDefaultProfile() {
        const profiles = get(this.profiles);
        this.selectedProfile.set(profiles[0]);
    }

    private selectProfileId(id: number) {
        const profiles = get(this.profiles);
        const profile = profiles.find(p => p.id === id);
        this.selectedProfile.set(profile);
    }

    private profileModified(orig: ProfileViewModel, curr: ProfileViewModel) {
        if (orig.name !== curr.name) return true;
        if (orig.description !== curr.description) return true;
        if (orig.public !== curr.public) return true;
        if (!objectEquals(orig.config, curr.config)) return true;
        return false;
    }

    profileGroups = derived(this.profiles, (profiles) => {
        const categoryOrder = ['Personal', 'Official', 'Community'];
        const groups = groupBy(profiles, p => p.category);
        return categoryOrder.map(category => (<ProfileGroup>{
            category,
            profiles: groups[category] || []
        }));
    });
}
