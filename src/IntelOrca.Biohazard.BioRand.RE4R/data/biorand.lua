local paths = {
    enemies_json = "biorand/enemies.json",
    enemies_csv = "biorand/enemies.csv",
    gimmicks_json = "biorand/gimmicks.json",
    gimmicks_csv = "biorand/gimmicks.csv"
}

function loadConfig()
    local configPath = "biorand/config.json"
    return json.load_file(configPath) or {
        showPlayer = true,
        showFlags = true,
        showSpawnControllers = true,
        showMarkers = true,
        showObjects = true
    }
end

function saveConfig(cfg)
    local configPath = "biorand/config.json"
    json.dump_file(configPath, cfg)
end

local cfg = loadConfig()

function logToFile(text)
    local logPath = "biorand/log.json"
    local logFile = json.load_file(logPath) or {}
    if logFile.log == nil then
        logFile.log = {}
    end
    table.insert(logFile.log, text)
    json.dump_file(logPath, logFile)
end

---@param entries table[]
---@param definition [string, string][]
local function getCsv(entries, definition)
    local csv = ""
    for _, entry in ipairs(entries) do
        for _, col in ipairs(definition) do
            local entryKey = string.lower(string.sub(col[1], 1, 1)) .. string.sub(col[1], 2)
            local entryValue = entry[entryKey] or ""
            local fmt = col[2]
            csv = csv .. string.format(fmt, entryValue)
            csv = csv .. ","
        end
        csv = csv:sub(1, -2) .. "\n"
    end
    return csv
end

---@param path string
---@param entries table[]
---@param definition [string, string][]
local function dumpCsv(path, entries, definition)
    fs.write(path, getCsv(entries, definition))
end

local function getExtraEnemyPositions()
    local file = json.load_file(paths.enemies_json) or {}
    return file.enemies or {}
end

local function dumpEnemyPosition(enemy)
    local file = json.load_file(paths.enemies_json) or {}
    if file.enemies == nil then
        file.enemies = {}
    end

    table.insert(file.enemies, enemy)
    json.dump_file(paths.enemies_json, file)

    dumpCsv(paths.enemies_csv,
        file.enemies,
        {
            { "Campaign",      "%s" },
            { "Guid",          "%s" },
            { "Chapter",       "%d" },
            { "Description",   "%s" },
            { "Stage",         "%d" },
            { "X",             "%.2f" },
            { "Y",             "%.2f" },
            { "Z",             "%.2f" },
            { "Yaw",           "%.2f" },
            { "Pitch",         "%.2f" },
            { "Roll",          "%.2f" },
            { "Condition",     "%s" },
            { "SkipCondition", "%s" },
            { "MiniBoss",      "%s" },
            { "Battle",        "%s" },
            { "Tags",          "%s" },
            { "Include",       "%s" },
            { "Exclude",       "%s" }
        }
    )
end

local function getExtraGimmickPositions()
    local file = json.load_file(paths.gimmicks_json) or {}
    return file.gimmicks or {}
end

local gimmickCsvDefinition = {
    { "Campaign",  "%s" },
    { "Chapter",   "%d" },
    { "Kind",      "%s" },
    { "Stage",     "%d" },
    { "X",         "%.2f" },
    { "Y",         "%.2f" },
    { "Z",         "%.2f" },
    { "Yaw",       "%.2f" },
    { "Pitch",     "%.2f" },
    { "Roll",      "%.2f" },
    { "Condition", "%s" },
    { "Events",    "%s" },
    { "Param1",    "%s" }
}

local function dumpGimmickPosition(gimmick)
    local file = json.load_file(paths.gimmicks_json) or {}
    if file.gimmicks == nil then
        file.gimmicks = {}
    end

    table.insert(file.gimmicks, gimmick)
    json.dump_file(paths.gimmicks_json, file)

    dumpCsv(paths.gimmicks_csv, file.gimmicks, gimmickCsvDefinition)
end

local function getSingleGimmickCsv(gimmick)
    local csv = getCsv({ gimmick }, gimmickCsvDefinition)
    return csv:match("([^\n]*)")
end

function getResolution()
    local sceneManager = sdk.get_native_singleton("via.SceneManager")
    local sceneManagerType = sdk.find_type_definition("via.SceneManager")
    local mainView = sdk.call_native_func(sceneManager, sceneManagerType, "get_MainView")
    local size = mainView:call("get_Size")
    return { width = size.w, height = size.h }
end

function getPlayerInfo()
    local characterManager = sdk.get_managed_singleton("chainsaw.CharacterManager")
    local player = characterManager:getPlayerContextRef()
    if player then
        local body = player:get_BodyGameObject()
        if body ~= nil then
            local transform = player:get_BodyGameObject():get_Transform()
            local pos = transform:get_Position()
            local rot = transform:get_Rotation()
            return {
                stage = player:get_CurrentStageID(),
                position = pos,
                rotation = rot,
                direction = quaternionToEulerDegrees(rot).yaw
            }
        end
    end
    return nil
end

-- function getComponents(typeName)
--     local sceneManager = sdk.get_native_singleton("via.SceneManager")
--     local sceneManagerType = sdk.find_type_definition("via.SceneManager")
--     local scene = sdk.call_native_func(sceneManager, sceneManagerType, "get_CurrentScene")
--     if scene == nil then
--         return {}
--     end
--
--     local componentType = sdk.typeof(typeName)
--     if componentType == nil then
--         return {}
--     end
--
--     local components = scene:call("findComponents", componentType)
--     return components
-- end

function getComponents(types)
    local sceneManager = sdk.get_native_singleton("via.SceneManager")
    local sceneManagerType = sdk.find_type_definition("via.SceneManager")
    local scene = sdk.call_native_func(sceneManager, sceneManagerType, "get_CurrentScene")
    if scene == nil then
        return {}
    end

    -- accept a single type name string
    if type(types) == "string" then
        local componentType = sdk.typeof(types)
        if componentType == nil then
            return {}
        end
        return scene:call("findComponents", componentType) or {}
    end

    -- accept an array of type name strings
    if type(types) ~= "table" then
        return {}
    end

    local results = {}
    local seen = {}

    for _, typeName in ipairs(types) do
        local componentType = sdk.typeof(typeName)
        if componentType ~= nil then
            local comps = scene:call("findComponents", componentType) or {}
            for _, c in ipairs(comps) do
                local addr = nil
                local ok, res = pcall(function() return c:get_address() end)
                if ok and res ~= nil then
                    addr = tostring(res)
                else
                    addr = tostring(c)
                end
                if not seen[addr] then
                    table.insert(results, c)
                    seen[addr] = true
                end
            end
        end
    end

    return results
end

function getComponent(gameObject, componentName)
    local componentType = sdk.typeof(componentName)
    return gameObject:call("getComponent", componentType)
end

function getParentGameObject(gameObject)
    local transform = gameObject:get_Transform()
    local parent = transform:get_Parent()
    if parent == nil then
        return nil
    end
    return parent:get_GameObject()
end

function getSpawnController(spawn)
    local parent = getParentGameObject(spawn)
    if parent ~= nil then
        return getComponent(parent, "chainsaw.CharacterSpawnController")
    end
    return nil
end

-- Class: Spawn Controller Display
SpawnControllerDisplayer = {}
SpawnControllerDisplayer.__index = SpawnControllerDisplayer

function SpawnControllerDisplayer:new()
    local instance = setmetatable({}, SpawnControllerDisplayer)
    return instance
end

function SpawnControllerDisplayer:begin()
    re.on_frame(function()
        if not cfg.showSpawnControllers then
            return
        end

        local playerInfo = getPlayerInfo()
        if playerInfo == nil then
            return
        end

        local playerPosition = playerInfo.position
        local components = getComponents("chainsaw.Ch1c0SpawnParamCommon")
        for index, component in ipairs(components) do
            local gameObject = component:get_GameObject()
            local transform = gameObject:get_Transform()
            local pos = transform:get_Position()
            local distance = (playerPosition - pos):length()
            if distance < 25 then
                local spawnName = gameObject:get_Name()
                local spawnController = getSpawnController(gameObject)
                if spawnController ~= nil then
                    local spawnControllerGuid = spawnController:get_GUID():call("ToString()")
                    local spawnCondition = spawnController:get_SpawnCondition():get_CheckFlags()
                    if #spawnCondition == 0 then
                        spawnCondition = ''
                    else
                        spawnCondition = spawnCondition[0]:get_CheckFlag():call("ToString()")
                    end
                    local top = pos + Vector3f.new(0, 2, 0)
                    local text_pos = top + Vector3f.new(0, 0.5, 0)
                    draw.capsule(pos, top, 0.05, 0xFFFFFFFF, 0xFF0000FF)
                    if distance < 10 then
                        draw.world_text(
                            spawnName ..
                            "\n" .. "Controller: " .. spawnControllerGuid .. "\nCondition:\n  " .. spawnCondition,
                            text_pos,
                            0xFFFFFFFF)
                    end
                end
            end
        end
    end)
end

local spawnControllerDisplayer = SpawnControllerDisplayer:new()
SpawnControllerDisplayer:begin()

-- Class: Flag Hooker
FlagHooker = {}
FlagHooker.__index = FlagHooker

function FlagHooker:new()
    local instance = setmetatable({}, FlagHooker)
    instance.history = {}
    return instance
end

function FlagHooker:begin()
    local scenarioFlagManagerDefinition = sdk.find_type_definition("chainsaw.ScenarioFlagManager")
    sdk.hook(scenarioFlagManagerDefinition:get_method("requestSetFlag(System.Guid, System.Boolean)"),
        function(args)
            local type = sdk.find_type_definition("System.Guid")
            local guid = sdk.call_native_func(args[3], type, "ToString()")
            self:onSetFlag(guid)
            if guid == "c2e15c16-a5c1-4518-89d6-be06ad113b16" then
                return sdk.PreHookResult.SKIP_ORIGINAL
            else
                return sdk.PreHookResult.CALL_ORIGINAL
            end
        end,
        function(retval)
            return retval
        end,
        true)
end

function FlagHooker:onSetFlag(guid)
    table.insert(self.history, guid)
    -- log.debug(guid)
    logToFile("onSetFlag(" .. guid .. ")")
end

function FlagHooker:getHistory(count)
    local history = self.history
    local historyCount = #history
    if count > historyCount then
        count = historyCount
    end

    local result = {}
    for i = historyCount, historyCount - count + 1, -1 do
        table.insert(result, history[i])
    end
    return result
end

local flagHooker = FlagHooker:new()
flagHooker:begin()

-- Class: InfoDisplayer
InfoDisplayer = {}
InfoDisplayer.__index = InfoDisplayer
function InfoDisplayer:new()
    local instance = setmetatable({}, InfoDisplayer)
    instance.drawX = 0
    instance.drawY = 0
    return instance
end

function InfoDisplayer:begin()
    self.drawX = 16
    self.drawY = 16
end

function InfoDisplayer:write_indent(text)
    self:write(text)
    self.drawX = self.drawX + 30
end

function InfoDisplayer:write(text)
    draw.text(text, self.drawX, self.drawY, 0xFFFFFFFF)
    self.drawY = self.drawY + 30
end

function InfoDisplayer:unindent()
    self.drawX = self.drawX - 30
end

re.on_frame(function()
    if not cfg.showPlayer and not cfg.showFlags then
        return
    end

    local size = getResolution()
    local playerInfo = getPlayerInfo()

    local displayer = InfoDisplayer:new()
    displayer:begin()
    displayer:write_indent("Biorand:")
    if playerInfo and cfg.showPlayer then
        displayer:write_indent("Player:")
        displayer:write(string.format("stage: %d", playerInfo.stage))
        displayer:write(string.format("pos: %.1f, %.1f, %.1f", playerInfo.position.x, playerInfo.position.y,
            playerInfo.position.z))
        displayer:write(string.format("rot: %.1f", playerInfo.direction))
        displayer:unindent()
    end
    if cfg.showFlags then
        displayer:write_indent("Flags:")
        local flagHistory = flagHooker:getHistory(4)
        for i, flag in ipairs(flagHistory) do
            displayer:write(flag)
        end
        displayer:unindent()
    end
    displayer:unindent()
end)

re.on_draw_ui(function()
    if not imgui.collapsing_header("Biorand") then return end

    local anyChanged = false
    local checkbox = function(label, value)
        local changed = false
        changed, result = imgui.checkbox(label, value)
        if changed then
            anyChanged = true
        end
        return result
    end

    cfg.showPlayer = checkbox("Show player coords", cfg.showPlayer)
    cfg.showFlags = checkbox("Show flags", cfg.showFlags)
    cfg.showSpawnControllers = checkbox("Show spawn controllers", cfg.showSpawnControllers)
    cfg.showMarkers = checkbox("Show markers", cfg.showMarkers)
    cfg.showObjects = checkbox("Show objects", cfg.showObjects)

    if anyChanged then
        saveConfig(cfg)
    end
end)

re.on_frame(function()
    if not cfg.showMarkers then
        return
    end

    local playerInfo = getPlayerInfo()
    if playerInfo == nil then
        return
    end

    local drawMarker = function(entity, color, radius, height)
        local pos = Vector3f.new(entity.x, entity.y, entity.z)
        local distance = (pos - playerInfo.position):length()
        if distance < 50 then
            local top = pos + Vector3f.new(0, height, 0)
            draw.capsule(pos, top, radius, color, 0xFFFFFFFF)
        end
    end

    local enemies = getExtraEnemyPositions()
    for _, enemy in ipairs(enemies) do
        if enemy.small then
            drawMarker(enemy, 0xFF00CCCC, 0.025, 1)
        else
            drawMarker(enemy, 0xFF00FFFF, 0.050, 2)
        end
    end

    local gimmicks = getExtraGimmickPositions()
    for _, gimmick in ipairs(gimmicks) do
        drawMarker(gimmick, 0xFFFF00FF, 0.1, 0.25)
    end
end)

re.on_application_entry("UpdateHID", function()
    local keyboard = sdk.get_native_singleton("via.hid.Keyboard")
    local keyboardDefinition = sdk.find_type_definition("via.hid.Keyboard")
    local kb = sdk.call_native_func(keyboard, keyboardDefinition, "get_Device")
    if kb == nil then
        return
    end

    local keyboardKeyDefinition = sdk.find_type_definition("via.hid.KeyboardKey")
    local altCode = keyboardKeyDefinition:get_field("Menu"):get_data(nil)
    local bKeyCode = keyboardKeyDefinition:get_field("B"):get_data(nil)
    local nKeyCode = keyboardKeyDefinition:get_field("N"):get_data(nil)
    local gKeyCode = keyboardKeyDefinition:get_field("G"):get_data(nil)
    local bDown = kb:isRelease(bKeyCode)
    local nDown = kb:isRelease(nKeyCode)
    local gDown = kb:isRelease(gKeyCode)
    if bDown or nDown then
        local playerInfo = getPlayerInfo()
        local enemy = {
            campaign = "Leon",
            chapter = -1,
            description = "[EXTRA]",
            stage = playerInfo.stage,
            x = math.floor(playerInfo.position.x + 0.5),
            y = math.floor(playerInfo.position.y + 0.5),
            z = math.floor(playerInfo.position.z + 0.5),
            yaw = math.floor(playerInfo.direction + 0.5),
            pitch = 0,
            roll = 0
        }
        if nDown then
            enemy.include = "small"
        end
        dumpEnemyPosition(enemy)
    elseif gDown then
        local playerInfo = getPlayerInfo()
        local gimmick = {
            campaign = "Leon",
            chapter = -1,
            stage = playerInfo.stage,
            x = math.floor(playerInfo.position.x * 100 + 0.5) / 100,
            y = math.floor(playerInfo.position.y * 100 + 0.5) / 100,
            z = math.floor(playerInfo.position.z * 100 + 0.5) / 100,
            yaw = math.floor(playerInfo.direction + 0.5),
            pitch = 0,
            roll = 0
        }
        dumpGimmickPosition(gimmick)
    end
end)

-- Class: ObjectTable
ObjectTable = {}
ObjectTable.__index = ObjectTable
function ObjectTable:new()
    local instance = setmetatable({}, ObjectTable)
    instance.defered = {}
    instance.items = {}
    instance.filter = ""
    instance.selectedObject = nil
    instance.cycles = 0
    instance.updates = 0
    instance.enableRefresh = true
    instance.forceRefresh = false
    return instance
end

function ObjectTable:begin()
    re.on_application_entry("UpdateScene", function()
        if cfg.showObjects then
            self.cycles = self.cycles + 1
            if self.forceRefresh or (self.enableRefresh and self.cycles % 25 == 0) then
                self.forceRefresh = false
                self.updates = self.updates + 1
                self:updateObjectSearch()
            end
        end
    end)
    re.on_application_entry("UpdateMotion", function()
        for _, func in ipairs(self.defered) do
            func()
        end
        self.defered = {}
    end)
    re.on_frame(function()
        if cfg.showObjects then
            self:renderTable()
        end
    end)
end

---@param callback function
function ObjectTable:defer(callback)
    table.insert(self.defered, callback)
end

function ObjectTable:updateObjectSearch()
    local playerInfo = getPlayerInfo()
    if playerInfo == nil then
        return
    end

    local maximum = 10
    local playerPosition = playerInfo.position
    local components = getComponents({
        "chainsaw.DropItem",
        "chainsaw.GimmickCore"
    })
    local items = {}
    for _, component in ipairs(components) do
        local gameObject = component:get_GameObject()
        local transform = gameObject:get_Transform()
        local pos = transform:get_Position()
        local distance = (pos - playerPosition):length()
        local guid = getGameObjectGuid(gameObject)
        local name = gameObject:get_Name()
        local kind = "Unknown"

        local allComponents = gameObject:call("get_Components"):get_elements()
        for _, component in ipairs(allComponents or {}) do
            local type_definition = component:get_type_definition()
            local component_name = type_definition:get_full_name()
            if component_name == "chainsaw.DropItem" then
                kind = "DropItem"
            elseif startsWith(component_name, "chainsaw.Gm") then
                kind = component_name:sub(10)
            end
        end

        if self.filter == "" or string.find(string.lower(guid), string.lower(self.filter)) or string.find(string.lower(name), string.lower(self.filter))
            or string.find(string.lower(kind), string.lower(self.filter)) then
            table.insert(items,
                { gameObject = gameObject, guid = guid, name = name, distance = distance, kind = kind })
        end
    end
    table.sort(items, function(a, b) return a.distance < b.distance end)
    if #items > maximum then
        items = { table.unpack(items, 1, maximum) }
    end
    self.items = items
end

function ObjectTable:renderTable()
    local playerInfo = getPlayerInfo()
    if playerInfo == nil then
        return
    end

    local monospaceFont = imgui.load_font('DroidSansMono.ttf', 30)

    local CHINESE_GLYPH_RANGES = {
        0x0020, 0x00FF, -- Basic Latin + Latin Supplement
        0x2000, 0x206F, -- General Punctuation
        0x3000, 0x30FF, -- CJK Symbols and Punctuations, Hiragana, Katakana
        0x31F0, 0x31FF, -- Katakana Phonetic Extensions
        0xFF00, 0xFFEF, -- Half-width characters
        0x4e00, 0x9FAF, -- CJK Ideograms
        0,
    }
    local jpFont = imgui.load_font('NotoSansJP-Regular.otf', 30, CHINESE_GLYPH_RANGES)

    imgui.begin_window("Objects", true, 0)

    -- imgui.text(string.format("Cycles: %d", self.cycles))
    -- imgui.text(string.format("Updates: %d", self.updates))
    local changed, result = imgui.checkbox("Refresh", self.enableRefresh)
    if changed then
        self.enableRefresh = result
    end
    local changed, newFilter = imgui.input_text("Filter", self.filter, 0)
    if changed then
        self.filter = newFilter
        self.forceRefresh = true
    end

    imgui.begin_table("strid", 7, 0, Vector2f.new(0, 0), 0)
    imgui.table_setup_column("X", 16, 64, 0)
    imgui.table_setup_column("Kind", 16, 256, 0)
    imgui.table_setup_column("Guid", 16, 512, 0)
    imgui.table_setup_column("Name", 0, 1, 0)
    imgui.table_setup_column("X", 16, 100, 0)
    imgui.table_setup_column("Y", 16, 100, 0)
    imgui.table_setup_column("Z", 16, 100, 0)
    -- imgui.table_setup_column("Yaw", 16, 100, 0)
    -- imgui.table_setup_column("Pitch", 16, 100, 0)
    -- imgui.table_setup_column("Roll", 16, 100, 0)
    imgui.table_headers_row()


    for _, item in ipairs(self.items) do
        local transform = item.gameObject:get_Transform()
        local pos = transform:get_Position()
        local rot = quaternionToEulerDegrees(transform:get_Rotation())

        imgui.table_next_column()
        imgui.push_id(string.format("object_table_checkbox_%s", item.guid))
        local changed, result = imgui.checkbox("", item.gameObject == self.selectedObject)
        imgui.pop_id()
        if changed then
            if result then
                self.selectedObject = item.gameObject
            else
                self.selectedObject = nil
            end
        end
        if result then
            local address = item.gameObject:get_address()
            local mat = transform:call("get_WorldMatrix()")
            local changed, newMat = draw.gizmo(address, mat)
            if changed then
                self:defer(function()
                    transform:set_Position(newMat[3])
                    transform:set_Rotation(newMat:to_quat())
                end)
            end
        end

        imgui.table_next_column()
        imgui.text(item.kind)

        imgui.table_next_column()
        imgui.push_font(monospaceFont)
        imgui.push_item_width(500)
        imgui.input_text("", item.guid, 16384)
        -- if imgui.button(item.guid) then
        --     -- no released API yet for copy
        -- end
        imgui.pop_font()
        imgui.table_next_column()
        imgui.push_font(jpFont)
        imgui.text(item.name)
        imgui.pop_font()

        imgui.table_next_column()
        imgui.push_id(string.format("object_table_x_%s", item.guid))
        imgui.text(string.format("%.1f", pos.x))
        imgui.table_next_column()
        imgui.text(string.format("%.1f", pos.y))
        imgui.table_next_column()
        imgui.text(string.format("%.1f", pos.z))
        -- imgui.table_next_column()
        -- imgui.text(string.format("%.1f", rot.yaw))
        -- imgui.table_next_column()
        -- imgui.text(string.format("%.1f", rot.pitch))
        -- imgui.table_next_column()
        -- imgui.text(string.format("%.1f", rot.roll))
        imgui.table_next_row(0, 0)
    end
    imgui.end_table()

    if self.selectedObject ~= nil then
        local gameObject = self.selectedObject
        local transform = gameObject:get_Transform()
        local pos = transform:get_Position()
        local euler = transform:get_EulerAngle()

        local slider = function(id, precision, value, callback)
            imgui.set_next_item_width(300)
            imgui.push_id(id)
            local changed, value = imgui.drag_float("", value, precision, -9999, 9999, "%.3f")
            if changed then
                self:defer(function()
                    callback(value)
                end)
            end
        end

        imgui.begin_rect()
        -- Row 1
        slider("X", 0.001, pos.x, function(v)
            pos.x = v
            transform:set_Position(pos)
        end)
        imgui.same_line();
        slider("Y", 0.001, pos.y, function(v)
            pos.y = v
            transform:set_Position(pos)
        end)
        imgui.same_line();
        slider("Z", 0.001, pos.z, function(v)
            pos.z = v
            transform:set_Position(pos)
        end)
        imgui.same_line();
        imgui.text("Position")

        -- Row 2
        slider("Yaw", 1, math.deg(euler.x), function(v)
            transform:set_EulerAngle(Vector3f.new(math.rad(v), euler.y, euler.z))
        end)
        imgui.same_line();
        slider("Pitch", 1, math.deg(euler.y), function(v)
            transform:set_EulerAngle(Vector3f.new(euler.x, math.rad(v), euler.z))
        end)
        imgui.same_line();
        slider("Roll", 1, math.deg(euler.z), function(v)
            transform:set_EulerAngle(Vector3f.new(euler.x, euler.y, math.rad(v)))
        end)
        imgui.same_line();
        imgui.text("Rotation")
        imgui.same_line();
        if imgui.button("Reset") then
            self:defer(function()
                transform:set_Rotation(Quaternion.new(1, 0, 0, 0))
            end)
        end

        local gimmick = {
            campaign = "Leon",
            chapter = -1,
            stage = playerInfo.stage,
            x = pos.x,
            y = pos.y,
            z = pos.z,
            yaw = euler.y,
            pitch = euler.x,
            roll = euler.z
        }
        imgui.input_text("csv", getSingleGimmickCsv(gimmick), 0)
        imgui.end_rect(4, 0)
    end
    imgui.end_window()
end

local objectTable = ObjectTable:new()
objectTable:begin()

function writeClassDef(def)
    log.debug(def:get_name())
    local fields = def:get_fields()
    local methods = def:get_methods()
    for index, value in ipairs(fields) do
        local name = value:get_name()
        log.debug("field: " .. name)
    end
    for index, value in ipairs(methods) do
        local returnType = value:get_return_type():get_name()
        local name = value:get_name()
        log.debug("method: " .. returnType .. " " .. name)
    end
    local parent = def:get_parent_type()
    if parent ~= nil then
        writeClassDef(parent)
    end
end

function writeObjDef(obj)
    local def = obj:get_type_definition()
    if def ~= nil then
        writeClassDef(def)
    end
end

function dumpObjInfo(obj)
    local def = obj:get_type_definition()
    if def ~= nil then
        local toStringValue = obj:call("ToString()")
        log.debug(toStringValue)

        while def ~= nil do
            local methods = def:get_methods()
            for index, method in ipairs(methods) do
                local name = method:get_name()
                if startsWith(name, "get_") and method:get_num_params() == 0 then
                    local result = obj:call(name .. "()")
                    -- local value = result:call("ToString()")
                    local value = tostring(result)
                    if type(result) == "userdata" then
                        value = result:call("ToString()")
                    end
                    log.debug("    " .. name:sub(5) .. " = " .. value)
                end
            end
            def = def:get_parent_type()
        end
    end
end

function startsWith(str, start)
    return str:sub(1, #start) == start
end

function endsWith(str, ending)
    return str:sub(- #ending) == ending
end

function getGameObjectGuid(gameObject)
    local toString = gameObject:call("ToString()")
    return toString:match("@(.-)%]")
end

function quaternionToEulerDegrees(rotation)
    local x = rotation.x
    local y = rotation.y
    local z = rotation.z
    local w = rotation.w

    -- Calculate yaw, pitch, and roll in radians
    local yaw = math.atan(2 * (y * w + x * z), 1 - 2 * (y ^ 2 + z ^ 2))
    local pitch = math.asin(2 * (y * z - x * w))
    local roll = math.atan(2 * (x * y + z * w), 1 - 2 * (x ^ 2 + y ^ 2))

    -- Convert radians to degrees
    local function radToDeg(radians)
        return radians * (180 / math.pi)
    end

    local yawDegrees = radToDeg(yaw)
    local pitchDegrees = radToDeg(pitch)
    local rollDegrees = radToDeg(roll)

    return {
        yaw = yawDegrees,
        pitch = pitchDegrees,
        roll = rollDegrees
    }
end

function eulerDegreesToQuaternion(yaw, pitch, roll)
    -- Assuming YXZ order: yaw around Y, pitch around X, roll around Z
    local pitchRad = pitch * math.pi / 180 * 0.5
    local yawRad = yaw * math.pi / 180 * 0.5
    local rollRad = roll * math.pi / 180 * 0.5

    local sp, cp = math.sin(pitchRad), math.cos(pitchRad)
    local sy, cy = math.sin(yawRad), math.cos(yawRad)
    local sr, cr = math.sin(rollRad), math.cos(rollRad)

    local w = cp * cy * cr - sp * sy * sr
    local x = sp * cy * cr + cp * sy * sr
    local y = cp * sy * cr - sp * cy * sr
    local z = cp * cy * sr + sp * sy * cr

    return Quaternion.new(w, x, y, z)
end
