using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITimeRewindable
{
    string GetEntityId();
    EntitySnapshot TakeSnapshot();
    BulletSnapshot TakeBulletSnapshot(); // For bullets
    void RestoreFromSnapshot(EntitySnapshot snapshot);
    bool IsActive();
}