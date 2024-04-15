<script lang="ts">
  interface Profile {
    name: string;
  }

  let profileNames = ['Dog', 'Texas', 'Chicken'];
  let profiles = profileNames
    .map((x) => {
      return {
        name: x
      };
    })
    .toSorted((a, b) => a.name.localeCompare(b.name));
  let selectedProfile: Profile | undefined = profiles[0];

  function selectProfile(profile: Profile) {
    selectedProfile = profile;
  }

  function duplicateProfile(profile: Profile) {
    const newProfile = { ...profile, name: profile.name + ' - Copy' };
    profiles = profiles.concat(newProfile).toSorted((a, b) => a.name.localeCompare(b.name));
    selectedProfile = newProfile;
  }

  function renameProfile(profile: Profile, value: string) {
    const newProfile = { ...profile, name: value };
    profiles = profiles
      .filter((x) => x != profile)
      .concat(newProfile)
      .toSorted((a, b) => a.name.localeCompare(b.name));
    selectedProfile = newProfile;
  }

  function deleteProfile(profile: Profile) {
    profiles = profiles
      .filter((x) => x != selectedProfile)
      .toSorted((a, b) => a.name.localeCompare(b.name));
    if (selectedProfile == profile) {
      if (profiles.length === 0) selectedProfile = undefined;
      else selectedProfile = profiles[0];
    }
  }
</script>

<div class="container-fluid">
  <div style="width: 500px; height: 300px;">
    <div class="d-flex flex-column h-100">
      <div class="p-2"><h1>Profile Manager</h1></div>
      <div class="p-2 flex-grow-1">
        <ul class="list-group list-group-flush">
          {#each profiles as profile}
            <button
              class="list-group-item list-group-item-action p-1"
              class:active={profile === selectedProfile}
              on:click={() => selectProfile(profile)}
            >
              <div class="d-flex">
                <div class="flex-grow-1 p-1">
                  {#if profile === selectedProfile}
                    <input
                      on:change={(e) => renameProfile(profile, e.target?.value)}
                      value={profile.name}
                    />
                  {:else}
                    <div>{profile.name}</div>
                  {/if}
                </div>
                <div>
                  <button
                    type="button"
                    class="btn btn-sm btn-light"
                    on:click={() => duplicateProfile(profile)}
                  >
                    <i class="bi bi-copy"></i></button
                  >
                  <button
                    type="button"
                    class="btn btn-sm btn-light"
                    on:click={() => deleteProfile(profile)}
                  >
                    <i class="bi bi-x"></i></button
                  >
                </div>
              </div>
            </button>
          {/each}
        </ul>
      </div>
      <div class="p-2">Flex item</div>
    </div>
  </div>
</div>

<style>
  input {
    border: 0;
    background: none;
    color: inherit;
  }

  input:focus-visible,
  input:focus,
  input:active {
    border: 0;
    outline: none;
  }
</style>
