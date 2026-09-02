-- EJEMPLO: comando /premiolua
-- Este archivo NO se carga automaticamente.

AV.On("Command", function(index, command, arguments, rawMessage)
    if command ~= "premiolua" then return false end

    AV.AddCoins(index, 10, 0, 0, "Premio Lua")
    AV.Notice(index, "Recibiste 10 coins", 1)
    return true
end)
