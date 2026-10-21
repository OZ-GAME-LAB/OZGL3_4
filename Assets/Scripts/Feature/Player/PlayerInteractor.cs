using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerInteractor : MonoBehaviour
{
    private readonly List<IInteractable> _nearby = new List<IInteractable>();
    private Player _player;

    private IInteractable _current;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.TryGetComponent(out IInteractable target) && _nearby.Contains(target) == false)
        {
            _nearby.Add(target);
            _current = target;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.TryGetComponent(out IInteractable target) && _nearby.Contains(target))
        {
            _nearby.Remove(target);
            RefreshCurrent();
        }
    }

    private void FixedUpdate()
    {
        if(_nearby.Count > 1)
        {
            RefreshCurrent();
        }
    }

    public void TryInteract()
    {
        RefreshCurrent();
        if (_current == null)
            return;

        _current.Interact(_player);
    }

    private void RefreshCurrent()
    {
        IInteractable best = null;
        float bestDist = float.MaxValue;
        Vector2 myPos = transform.position;

        foreach(var target in _nearby)
        {
            float dist = (target.Position - myPos).sqrMagnitude;
            if(dist < bestDist)
            {
                bestDist = dist;
                best = target;
            }
        }

        if(best != _current)
        {
            _current = best;
        }
    }
}
