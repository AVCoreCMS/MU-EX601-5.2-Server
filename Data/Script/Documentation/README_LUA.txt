SISTEMA LUA - AV EMULATOR
========================

DOCUMENTACION COMPLETA
  Documentation\00_INDEX.txt

ESTRUCTURA
  ScriptMain.lua            Lista ordenada de scripts activos.
  LuaConfig.xml             Configuracion del motor.
  Documentation\           Manual separado por temas.
  Examples\                Ejemplos que NO se cargan solos.
  Scripts\Nombre\Nombre.lua  Una carpeta por script (config aparte, si hace
                              falta, tambien va en esa misma carpeta).

RECARGA
  GameServer -> Reload -> Reload Lua

No pongas todo dentro de ScriptMain.lua. Cada script debe tener su propia
carpeta; ScriptMain.lua solamente debe cargarlo con AV.Load.
