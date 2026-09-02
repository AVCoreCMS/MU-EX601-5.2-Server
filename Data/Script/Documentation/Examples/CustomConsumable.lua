-- EJEMPLO: item consumible personalizado 14,200.
-- Este archivo NO se carga automaticamente.

local CUSTOM_ITEM = AV.Item(14, 200)

AV.On("ItemUse", function(index, sourceSlot, targetSlot, itemIndex, level, durability)
    if itemIndex ~= CUSTOM_ITEM then return false end

    local item = AV.GetInventoryItem(index, sourceSlot)
    if not item or item.Index ~= CUSTOM_ITEM then return false end

    if not AV.DeleteItem(index, sourceSlot) then return false end

    AV.AddCoins(index, 10, 0, 0, "Consumible personalizado")
    AV.Notice(index, "Consumible utilizado", 1)
    return true
end)
