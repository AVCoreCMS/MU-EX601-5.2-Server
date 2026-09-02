-- EJEMPLO: NPC custom que abre una ventana Lua en el cliente.
--
-- La mitad cliente de este ejemplo esta en
-- Client\Data\Custom\Script\Scripts\CustomNpcWindow\CustomNpcWindow.lua -
-- los dos archivos van juntos. Activalos con AV.Load en sus respectivos
-- ScriptMain.lua.
--
-- IMPORTANTE: CUSTOM_NPC_CLASS de abajo es un placeholder. Cambialo por el
-- Class real del NPC que quieras usar (Data\Monster\Monster.xml).

local CUSTOM_NPC_CLASS = 9999

AV.On("NpcTalk", function(index, npcIndex)
    local npc = AV.GetObject(npcIndex)
    if not npc or npc.Class ~= CUSTOM_NPC_CLASS then
        return false
    end

    AV.SendClientEvent(index, "OpenCustomNpc", "NPC Custom",
        "Bienvenido. Este dialogo", "lo armo un script Lua.", "")

    return true
end)

AV.On("ClientEvent", function(index, eventName)
    if eventName ~= "CustomNpcAccept" then return end

    AV.AddCoins(index, 10, 0, 0, "NPC Custom")
    AV.Notice(index, "Recibiste 10 coins", 1)
end)
