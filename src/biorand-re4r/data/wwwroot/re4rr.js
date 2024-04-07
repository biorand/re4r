(function () {
    const configDefMap = {};

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

        var elSeed = document.getElementById('input-seed');
        var elShuffleSeed = document.getElementById('btn-shuffle-seed');
        elShuffleSeed.addEventListener('click', () => {
            elSeed.value = rng(100000, 1000000);
        });

        var elGenerate = document.getElementById('btn-generate');
        var elSpinner = document.getElementById('spinner-generating');
        var elDownloadPak = document.getElementById('btn-download-pak');
        var elDownloadMod = document.getElementById('btn-download-mod');
        var elSuccessContainer = document.getElementById('container-generate-success');
        if (elGenerate) {
            elGenerate.addEventListener('click', function () {
                elSpinner.classList.remove('invisible');
                elSuccessContainer.classList.add('invisible');
                elGenerate.disabled = true;
                var baseUrl = new URL(document.URL).origin;
                var url = baseUrl + "/api/generate";
                fetch(url)
                    .then(response => {
                        if (response.ok) {
                            return response.json();
                        } else {
                            throw new Error("Request failed");
                        }
                    })
                    .then(data => {
                        elSpinner.classList.add('invisible');
                        elSuccessContainer.classList.remove('invisible');
                        elGenerate.disabled = false;
                        elDownloadPak.href = data.downloadUrl;
                        elDownloadMod.href = data.downloadUrlMod;
                    })
                    .catch(error => {
                        elSpinner.classList.add('invisible');
                        elGenerate.disabled = false;
                        alert(error.message);
                    })
            });
        }
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
                widgetHtml = '<input id="' + inputId + '" type="range" class="form-range" min="0" max="1" step="0.01">';
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

    function setConfigDefinition(configDef) {
        const configContainerEl = document.getElementById('config-container');
        configContainerEl.innerHTML = createWidgets(configDefinition);
    }

    function setConfig(config) {
        for (const key in config) {
            const value = config[key];
            const def = configDefMap[key];

            const inputEl = document.getElementById(`cfg-${key}`);
            if (def.type == "switch") {
                inputEl.checked = value;
            } else {
                inputEl.value = value;
            }
        }
    }

    function setSeed(seed) {
        const el = document.getElementById('input-seed');
        el.value = seed;
    }

    const configDefinition = {
        groups: [
            {
                "label": "General",
                "items": [
                    {
                        "label": "Enemy Multiplier",
                        "id": "enemy-multiplier",
                        "type": "range"
                    },
                    {
                        "label": "Progressive Difficulty",
                        "id": "progressive-difficulty",
                        "type": "switch"
                    }
                ]
            },
            {
                "label": "Enemies",
                "items": [
                    {
                        "label": "Brute",
                        "id": "enemy-ratio-brute",
                        "type": "range"
                    },
                    {
                        "label": "Regenerador",
                        "id": "enemy-ratio-regenerador",
                        "type": "range"
                    }
                ]
            }
        ]
    };

    const config = {
        "enemy-multiplier": 1,
        "progressive-difficulty": true,
        "enemy-ratio-brute": 0.3,
        "enemy-ratio-regenerador": 0.7
    };

    setupPage();
    setConfigDefinition(configDefinition);
    setConfig(config);
    setSeed(23857523);
})();
