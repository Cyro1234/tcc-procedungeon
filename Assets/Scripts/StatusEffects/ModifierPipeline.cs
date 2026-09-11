using System.Collections.Generic;

// Pipeline generico de modificadores que pode ser usado pra qualquer tipo de modificador, tipo o IMovementModifier e o IAttackModifier
public class ModifierPipeline<T>
{
    private readonly List<T> modifiers = new List<T>();

    public IReadOnlyList<T> Modifiers => modifiers;

    public void Register(T modifier)
    {
        modifiers.Add(modifier);
    }

    public void Unregister(T modifier)
    {
        modifiers.Remove(modifier);
    }
}
