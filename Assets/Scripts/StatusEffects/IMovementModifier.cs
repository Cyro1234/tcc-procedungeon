using UnityEngine;

// Interface para modificadores de movimento do jogador!!!
public interface IMovementModifier
{
    Vector2 ModifyDirection(Vector2 direction, float dt);
}
