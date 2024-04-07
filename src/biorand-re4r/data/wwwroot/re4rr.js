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
            }
        }
        return config;
    }

    function rng(low, high) {
        const range = high - low;
        return low + Math.round(Math.random() * range);
    }

    function refreshTheme() {
        var elSwitchTheme = document.getElementById('switch-theme');
        const theme = elSwitchTheme.checked ? 'dark' : 'light;'
        document.documentElement.setAttribute('data-bs-theme', theme);
    }

    function setupPage() {
        var elSwitchTheme = document.getElementById('switch-theme');
        elSwitchTheme.addEventListener('change', refreshTheme);
        refreshTheme();

        var elPassword = document.getElementById('input-password');
        elPassword.value = loadLocalData('password');

        var elSeed = document.getElementById('input-seed');
        var elShuffleSeed = document.getElementById('btn-shuffle-seed');
        elShuffleSeed.addEventListener('click', () => {
            const seed = rng(100000, 1000000);
            elSeed.value = seed;
            saveLocalData('seed', seed);
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
        for (const item of group.items) {
            html += getGroupItemHtml(item);
        }
        html += '</div>';
        return html;
    }

    function getGroupItemHtml(groupItem) {
        configDefMap[groupItem.id] = groupItem;
        const inputId = `cfg-${groupItem.id}`;
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
                widgetHtml = `<input id="${inputId}" type="range" class="form-range" min="${groupItem.min}" max="${groupItem.max}" step="${groupItem.step}">`;
                break;
        }
        const colClass = groupItem.size ? `col-${groupItem.size}` : 'col-6';
        let html =
            '<div class="row g-3 align-items-center">' +
            '    <div class="col-sm-2">' +
            '        <label for="' + inputId + '" class="form-label">' + groupItem.label + '</label>' +
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

        for (const key in configDefMap) {
            const inputEl = document.getElementById(`cfg-${key}`);
            inputEl.addEventListener('change', () => {
                const config = getConfig();
                saveLocalData('config', config);
            });
        }
    }

    function setConfig(config) {
        for (const key in config) {
            const value = config[key];
            const def = configDefMap[key];
            if (def) {
                const inputEl = document.getElementById(`cfg-${key}`);
                if (def.type == "switch") {
                    inputEl.checked = value;
                } else {
                    inputEl.value = value;
                }
            }
        }
    }

    function setSeed(seed) {
        const el = document.getElementById('input-seed');
        el.value = seed;
    }

    setupPage();
})();
