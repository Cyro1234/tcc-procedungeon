// Discriminador Inspector-friendly pra DowngradeOption poder apontar pra um
// StatusEffect. StatusEffect nao e Serializable/polimorfico no Inspector, entao
// a opcao guarda esse enum + duracao, e StatusEffectFactory monta o objeto certo.
public enum StatusEffectKind
{
    None,
    Drunkenness,
    HeavyLimp,
    Labyrinthitis
}
