using UnityEditor;

public enum StatType // Fiz um enum pra delimitar os tipos de stats que da pra ser modificados com buffs e debuffs!!!
{
    Health,         // Vida do jogador
    Speed,          // Velocidade de movimento do jogador
    Damage,         // Dano do jogador
    AttackCooldown  // Tempo de recarga do ataque do jogador fswin!!

    // Conforme a gente vai adicionando mais stats é só colocar eles como tipos aqui!!!

    // Pode ter mais stats como recarga de item, chance de acerto critico, chance de bloqueio etc, mas como eu não me odeio, não vou fazer isso agora kk
}

public enum ModifierType
{
    Flat,       // Flat é literalmente só uma adição ou subtração do valor base!!
    Percent     // Er, meio obvio...
}

public enum ModifierSource
{
    Item,       // Modificador vindo de um item
    Buff,       // Modificador vindo de um buff
    Debuff      // Modificador vindo de um debuff
}
