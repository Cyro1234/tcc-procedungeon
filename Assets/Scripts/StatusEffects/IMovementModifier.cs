using UnityEngine;

// Interface para modificadores de movimento do jogador!!!
public interface IMovementModifier
{
    Vector2 ModifyDirection(Vector2 direction, float dt);
}

// Durante o dash o PlayerMovement chama ModifyDash no lugar de ModifyDirection.
// progress vai de 0 ate 1
// A direcao recebida ja vem com a velocidade do dash!!
public interface IDashModifier
{
    Vector2 ModifyDash(Vector2 direction, float progress, float dt);
}

// modificadores que atrasam o input (tipo o debuff de lag) informam o atraso,
// assim o dash sabe quando ele realmente vai acontecer
public interface IInputDelay
{
    float Delay { get; }
}
