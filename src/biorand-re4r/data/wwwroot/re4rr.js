(function () {
    const configDefMap = {};

    function loadLocalData(key) {
        return JSON.parse(localStorage.getItem(key));
    }

    function saveLocalData(key, value) {
        return localStorage.setItem(key, JSON.stringify(value));
    }

    function getApiUrl(url) {
        var baseUrl = new URL(document.URL).origin;
        var url = baseUrl + `/api/${url}`;
        return url;
    }

    function getConfigDefinition() {
        return new Promise((resolve, reject) => {
            fetch(getApiUrl('config'))
                .then(response => {
                    if (response.ok) {
                        return response.json();
                    } else {
                        throw new Error("Request failed");
                    }
                })
                .then(data => {
                    resolve(data);
                })
                .catch(error => {
                    reject(error);
                })
        });
    }

    function getConfig() {
        const config = {};
        for (key in configDefMap) {
            const def = configDefMap[key];
            const el = document.getElementById(`cfg-${key}`);
            switch (def.type) {
                case 'switch':
                    config[key] = el.checked;
                    break;
                case 'number':
                case 'range':
                    config[key] = parseFloat(el.value);
                    break;
                case 'dropdown':
                    config[key] = el.value;
                    break;
            }
        }
        return config;
    }

    function rng(low, high) {
        const range = high - low;
        return low + Math.round(Math.random() * range);
    }

    function randomizeSeed() {
        var elSeed = document.getElementById('input-seed');
        const seed = rng(100000, 1000000);
        elSeed.value = seed;
        saveLocalData('seed', seed);
    }

    function refreshTheme(theme) {
        if (!theme) {
            var elSwitchTheme = document.getElementById('switch-theme');
            theme = elSwitchTheme.checked ? 'dark' : 'light';
        }
        document.documentElement.setAttribute('data-bs-theme', theme);
        saveLocalData('theme', theme);
    }


    class ProfileManager {
        constructor() {
            this.profiles = {};
            this.id = 0;
        }

        create(name, config) {
            this.profiles[name] = { name: name, config: config };
            this.saveToStorage();
            return this.profiles[name];
        }

        delete(name) {
            delete this.profiles[name];
            this.saveToStorage();
        }

        getAll() {
            return Object.values(this.profiles);
        }

        get(name) {
            return this.profiles[name];
        }

        update(name, config) {
            this.profiles[name] = {
                name: name,
                config: config
            }
            this.saveToStorage();
        }

        loadFromStorage() {
            const profiles = loadLocalData('profiles');
            if (profiles) {
                for (const p of profiles) {
                    this.profiles[p.name] = p;
                }
            }
        }

        saveToStorage() {
            saveLocalData('profiles', this.getAll());
        }
    }
    const profileManager = new ProfileManager();

    function initProfileWidgets() {
        const elProfileSelect = document.getElementById('select-profile');
        const elProfileNewName = document.getElementById('btn-profile-new-name');
        const elProfileCreate = document.getElementById('btn-profile-create');
        const elProfileDelete = document.getElementById('btn-profile-delete');
        const elProfileExport = document.getElementById('btn-profile-export');
        const elProfileImport = document.getElementById('btn-profile-import');

        initProfilesDropdown = (name) => {
            const profiles = profileManager.getAll();
            let options = '';
            let index = 0;
            let i = 0;
            for (const profile of profiles) {
                options += `<option>${profile.name}</option>`
                if (profile.name === name) {
                    index = i;
                }
                i++;
            }
            elProfileSelect.innerHTML = options;
            elProfileSelect.selectedIndex = profiles.length === 0 ? -1 : index;
        }

        getSelectedProfile = () => {
            const profiles = profileManager.getAll();
            return profiles[elProfileSelect.selectedIndex];
        }

        profileManager.loadFromStorage();
        initProfilesDropdown();

        const selectedProfile = loadLocalData('selectedProfile');
        const profiles = profileManager.getAll();
        if (typeof selectedProfile === "number" && selectedProfile >= 0 && selectedProfile < profiles.length) {
            elProfileSelect.selectedIndex = selectedProfile;
        }

        elProfileSelect.addEventListener('change', () => {
            const profile = getSelectedProfile();
            if (profile) {
                setConfig(profile.config);
                saveLocalData('selectedProfile', elProfileSelect.selectedIndex);
            }
        });
        elProfileCreate.addEventListener('click', () => {
            const name = elProfileNewName.value.trim();
            if (!name) {
                elProfileNewName.focus();
            } else {
                if (profileManager.get(name)) {
                    elProfileNewName.focus();
                } else {
                    const profile = profileManager.create(name, getConfig());
                    initProfilesDropdown(profile.name);
                    elProfileNewName.value = '';
                }
            }
        });
        elProfileDelete.addEventListener('click', () => {
            const profile = getSelectedProfile();
            if (profile) {
                profileManager.delete(profile.name);
                initProfilesDropdown();
            }
        });
    }

    function setupPage() {
        var elSwitchTheme = document.getElementById('switch-theme');
        elSwitchTheme.addEventListener('change', () => refreshTheme());
        refreshTheme(loadLocalData('theme'));

        initProfileWidgets();

        var elPassword = document.getElementById('input-password');
        elPassword.value = loadLocalData('password');

        var elSeed = document.getElementById('input-seed');
        var elShuffleSeed = document.getElementById('btn-shuffle-seed');
        elShuffleSeed.addEventListener('click', () => {
            randomizeSeed();
        });

        var elGenerate = document.getElementById('btn-generate');
        var elSpinner = document.getElementById('spinner-generating');
        var elDownloadPak = document.getElementById('btn-download-pak');
        var elDownloadMod = document.getElementById('btn-download-mod');
        var elSuccessContainer = document.getElementById('container-generate-success');
        var elFailureContainer = document.getElementById('container-generate-failure');
        var elFailureMessage = document.getElementById('generate-failure-message');
        if (elGenerate) {
            elGenerate.addEventListener('click', function () {
                const password = elPassword.value;
                saveLocalData('password', password);

                const seed = parseInt(elSeed.value);

                elSpinner.classList.remove('invisible');
                elSuccessContainer.classList.add('invisible');
                elSuccessContainer.classList.remove('d-none');
                elFailureContainer.classList.add('d-none');
                elGenerate.disabled = true;
                fetch(getApiUrl('generate'), {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        seed: seed,
                        password: password,
                        config: getConfig()
                    })
                })
                    .then(response => {
                        if (response.ok) {
                            return response.json();
                        } else {
                            throw new Error("Request failed");
                        }
                    })
                    .then(data => {
                        elSpinner.classList.add('invisible');
                        elGenerate.disabled = false;
                        if (data.result === 'success') {
                            elSuccessContainer.classList.remove('invisible');
                            elDownloadPak.href = data.downloadUrl;
                            elDownloadMod.href = data.downloadUrlMod;
                        } else {
                            elSuccessContainer.classList.add('d-none');
                            elFailureContainer.classList.remove('d-none');
                            elFailureMessage.innerText = data.message;
                        }
                    })
                    .catch(error => {
                        elSpinner.classList.add('invisible');
                        elGenerate.disabled = false;
                        elSuccessContainer.classList.add('d-none');
                        elFailureContainer.classList.remove('d-none');
                        elFailureMessage.innerText = error.message;
                    })
            });
        }

        getConfigDefinition()
            .then(configDefinition => {
                setConfigDefinition(configDefinition);
                setConfig(loadLocalData('config'));
                setSeed(loadLocalData('seed'));
            })
    }

    function createWidgets(def) {
        let html = '';
        for (const group of def.groups) {
            html += createGroup(group);
        }
        return html;
    }

    function createGroup(group) {
        let html = '<div>';
        html += '<hr>';
        html += '<h4>' + group.label + '</h4>';
        const itemHtml = group.items.map(getGroupItemHtml);
        for (let i = 0; i < itemHtml.length; i += 2) {
            const left = itemHtml[i + 0];
            const right = itemHtml[i + 1];
            html += '<div class="row">';
            html += '<div class="col-md-6">';
            html += left;
            html += '</div>';
            if (right) {
                html += '<div class="col-md-6">';
                html += right;
                html += '</div>';
            }
            html += '</div>';
        }
        html += '</div>';
        return html;
    }

    function getGroupItemHtml(groupItem) {
        configDefMap[groupItem.id] = groupItem;
        const inputId = `cfg-${groupItem.id}`;

        let tooltipHtml = '';
        if (groupItem.description) {
            tooltipHtml = `data-bs-toggle="tooltip" data-bs-placement="top" data-bs-custom-class="custom-tooltip" data-bs-title="${groupItem.description}"`;
        }

        let widgetHtml = '';
        switch (groupItem.type) {
            case 'checkbox':
                widgetHtml = '<input id="' + inputId + '" class="form-check-input" type="checkbox">';
                break;
            case 'switch':
                widgetHtml = '<div class="form-check form-switch"><input id="' + inputId + '" class="form-check-input" type="checkbox" role="switch"></div>';
                break;
            case 'number':
                widgetHtml = '<input id="' + inputId + '" type="text" class="form-control form-control-sm">';
                break;
            case 'range':
                widgetHtml += `<div class="row">`;
                widgetHtml += `<div class="col-auto">`;
                widgetHtml += `<input id="${inputId}" type="range" class="form-range" min="${groupItem.min}" max="${groupItem.max}" step="${groupItem.step}">`;
                widgetHtml += `</div>`;
                widgetHtml += `<div id="${inputId}-display" class="col-auto"></div>`;
                widgetHtml += `</div>`;
                break;
            case 'dropdown':
                // <select id="select-profile" class="form-select"></select>
                widgetHtml += `<select id="${inputId}" class="form-select">`;
                if (groupItem.options) {
                    for (const option of groupItem.options) {
                        widgetHtml += `<option>${option}</option>`;
                    }
                }
                widgetHtml += `</select>`;
                break;
        }
        const colClass = groupItem.size ? `col-${groupItem.size}` : 'col-8';
        let html =
            '<div class="row g-3 align-items-center">' +
            '    <div class="col-4">' +
            '        <label for="' + inputId + '" class="form-label"' + tooltipHtml + '>' + groupItem.label + '</label>' +
            '    </div>' +
            '    <div class="' + colClass + '">' +
            widgetHtml +
            '    </div>' +
            '</div>';
        return html;
    }

    function setConfigDefinition(configDefinition) {
        const configContainerEl = document.getElementById('config-container');
        configContainerEl.innerHTML = createWidgets(configDefinition);

        const defaultConfig = {};
        for (const key in configDefMap) {
            const def = configDefMap[key];
            defaultConfig[key] = def.default;

            const outputEl = document.getElementById(`cfg-${key}-display`);
            const inputEl = document.getElementById(`cfg-${key}`);
            if (outputEl) {
                outputEl.innerText = inputEl.value;
                inputEl.addEventListener('input', () => {
                    outputEl.innerText = parseFloat(inputEl.value).toFixed(2);
                });
            }
            inputEl.addEventListener('change', () => {
                if (outputEl)
                    outputEl.innerText = parseFloat(inputEl.value).toFixed(2);
                const config = getConfig();
                saveLocalData('config', config);
            });
        }

        profileManager.update('Default', defaultConfig);

        const selectedProfile = getSelectedProfile();
        if (selectedProfile) {
            initProfilesDropdown(selectedProfile.name);
        } else {
            initProfilesDropdown();
        }

        // Enable tooltips
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
        const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))
    }

    function setConfig(config) {
        for (const key in configDefMap) {
            const def = configDefMap[key];
            let value = def.default;
            if (config && key in config) {
                value = config[key];
            }

            const inputEl = document.getElementById(`cfg-${key}`);
            const outputEl = document.getElementById(`cfg-${key}-display`);
            if (def.type == "switch") {
                inputEl.checked = value;
            } else {
                inputEl.value = value;
                if (outputEl) {
                    outputEl.innerText = value.toFixed(2);
                }
            }
        }
    }

    function setSeed(seed) {
        if (seed) {
            const el = document.getElementById('input-seed');
            el.value = seed;
        } else {
            randomizeSeed();
        }
    }

    setupPage();
})();
