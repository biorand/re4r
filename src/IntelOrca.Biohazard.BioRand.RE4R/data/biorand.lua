function logToFile(text)
    local logPath = "biorand/log.json"
    local logFile = json.load_file(logPath) or {}
    if logFile.log == nil then
        logFile.log = {}
    end
    table.insert(logFile.log, text)
    json.dump_file(logPath, logFile)
end

function getExtraEnemyPositions()
    local path = "biorand/enemy.json"
    local file = json.load_file(path) or {}
    return file.enemies or {}
end
function dumpEnemyPosition(enemy)
    local path = "biorand/enemy.json"
    local file = json.load_file(path) or {}
    if file.enemies == nil then
        file.enemies = {}
    end

    table.insert(file.enemies, enemy)
    json.dump_file(path, file)
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
        local transform = player:get_BodyGameObject():get_Transform()
        local pos = transform:get_Position()
        local rot = transform:get_Rotation()
        return {
            stage = player:get_CurrentStageID(),
            position = pos,
            rotation = rot,
            direction = quaternionToEulerDegrees(rot).yaw
        }
    else
        return nil
    end
end

function getComponents(typeName)
    local sceneManager = sdk.get_native_singleton("via.SceneManager")
    local sceneManagerType = sdk.find_type_definition("via.SceneManager")
    local scene = sdk.call_native_func(sceneManager, sceneManagerType, "get_CurrentScene")
    if scene == nil then
        return {}
    end

    local componentType = sdk.typeof(typeName)
    if componentType == nil then
        return {}
    end

    local components = scene:call("findComponents", componentType)
    return components
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
        local playerInfo = getPlayerInfo()
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
                    draw.world_text(spawnName .. "\n" .. "Controller: " .. spawnControllerGuid .. "\nCondition:\n  " .. spawnCondition, text_pos, 0xFFFFFFFF)
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
    local size = getResolution()
    local playerInfo = getPlayerInfo()

    local displayer = InfoDisplayer:new()
    displayer:begin()
    displayer:write_indent("Biorand:")
    if playerInfo then
        displayer:write_indent("Player:")
        displayer:write(string.format("stage: %d", playerInfo.stage))
        displayer:write(string.format("pos: %.1f, %.1f, %.1f", playerInfo.position.x, playerInfo.position.y, playerInfo.position.z))
        displayer:write(string.format("rot: %.1f", playerInfo.direction))
        displayer:unindent()
    end
    displayer:write_indent("Flags:")
    local flagHistory = flagHooker:getHistory(4)
    for i, flag in ipairs(flagHistory) do
        displayer:write(flag)
    end
    displayer:unindent()
    displayer:unindent()
end)

re.on_draw_ui(function()

end)

re.on_frame(function()
    local playerInfo = getPlayerInfo()
    local enemies = getExtraEnemyPositions()
    for i, enemy in ipairs(enemies) do
        local pos = Vector3f.new(enemy.x, enemy.y, enemy.z)
        local distance = (pos - playerInfo.position):length()
        if distance < 50 then
            local top = pos + Vector3f.new(0, 2, 0)
            local color = 0xFF00FFFF
            local radius = 0.05
            if enemy.small then
                top = pos + Vector3f.new(0, 1, 0)
                color = 0xFF00CCCC
                radius = 0.025
            end
            draw.capsule(pos, top, radius, color, 0xFFFFFFFF)
        end
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
    local bDown = kb:isRelease(bKeyCode)
    local nDown = kb:isRelease(nKeyCode)
    if bDown or nDown then
        local playerInfo = getPlayerInfo()
        local enemy = {
            stage = playerInfo.stage,
            x = math.floor(playerInfo.position.x + 0.5),
            y = math.floor(playerInfo.position.y + 0.5),
            z = math.floor(playerInfo.position.z + 0.5),
            direction = math.floor(playerInfo.direction + 0.5)
        }
        if nDown then
            enemy.small = true
        end
        dumpEnemyPosition(enemy)
    end
end)

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

function quaternionToEulerDegrees(rotation)
    local x = rotation.x
    local y = rotation.y
    local z = rotation.z
    local w = rotation.w

    -- Calculate yaw, pitch, and roll in radians
    local yaw = math.atan(2 * (y * w + x * z), 1 - 2 * (y^2 + z^2))
    local pitch = math.asin(2 * (y * z - x * w))
    local roll = math.atan(2 * (x * y + z * w), 1 - 2 * (x^2 + y^2))

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
