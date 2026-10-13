public interface IInteractable
{
    void Interact(PlayerControl player);

    bool CanInteractWith(PlayerControl player);
}
