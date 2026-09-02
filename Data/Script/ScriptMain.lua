--=============================================================================
-- AV EMULATOR - PUNTO DE ENTRADA LUA
--=============================================================================
-- Carga cada sistema en una linea y agrupalo por carpeta.
-- Quita los dos guiones solamente cuando quieras activarlo.
--
-- Una carpeta por script, con el nombre del script (Scripts\Nombre\Nombre.lua):
-- AV.Load("Scripts\\ScrambleWords\\ScrambleWords.lua")
-- AV.Load("Scripts\\CustomNpcWindow\\CustomNpcWindow.lua") -- ejemplo: ventana Lua en el cliente
--
-- La documentacion comienza en Documentation\\00_INDEX.txt
--=============================================================================

AV.Log("ScriptMain.lua cargado")

AV.On("ServerStart", function()
    AV.Log("Sistema Lua iniciado")
end)

AV.On("ServerShutdown", function()
    AV.Log("Sistema Lua detenido o recargado")
end)
