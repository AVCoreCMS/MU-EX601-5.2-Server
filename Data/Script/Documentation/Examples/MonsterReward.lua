-- EJEMPLO: recompensa cuando muere el monster class 2.
-- Este archivo NO se carga automaticamente.

AV.On("MonsterDie", function(killerIndex, monsterIndex)
    local monster = AV.GetObject(monsterIndex)
    if not monster or monster.Class ~= 2 then return end
    if not AV.IsConnected(killerIndex) then return end

    AV.RewardBag(killerIndex, 200)
end)
