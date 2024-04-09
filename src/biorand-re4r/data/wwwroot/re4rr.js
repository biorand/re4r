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

    function randomizeSeed() {
        var elSeed = document.getElementById('input-seed');
        const seed = rng(100000, 1000000);
        elSeed.value = seed;
        saveLocalData('seed', seed);
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

        for (const key in configDefMap) {
            const outputEl = document.getElementById(`cfg-${key}-display`);
            const inputEl = document.getElementById(`cfg-${key}`);
            if (outputEl) {
                outputEl.innerText = inputEl.value;
                inputEl.addEventListener('input', () => {
                    outputEl.innerText = parseFloat(inputEl.value).toFixed(2);
                });
            }
            inputEl.addEventListener('change', () => {
                outputEl.innerText = parseFloat(inputEl.value).toFixed(2);
                const config = getConfig();
                saveLocalData('config', config);
            });
        }

        // Enable tooltips
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
        const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))
    }

    function setConfig(config) {
        for (const key in config) {
            const value = config[key];
            const def = configDefMap[key];
            if (def) {
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
