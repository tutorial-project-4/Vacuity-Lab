using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Boss 2 Spread Shot", story: "Boss 2 fires spread shot", category: "Action/Boss 2", id: "37bf6fcb55b84cbba97297713f7a55b1")]
public partial class Boss2SpreadShotAction : Action
{
    Boss2Controller controller;
    float timer;
    bool fired;

    protected override Status OnStart()
    {
        controller = GameObject.GetComponent<Boss2Controller>();
        timer = 0f;
        fired = false;
        return controller == null ? Status.Failure : Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!fired)
        {
            if (!controller.TryFireSpread()) return Status.Running;
            fired = true;
        }
        timer += Time.deltaTime;
        return timer < controller.SpreadRecovery ? Status.Running : Status.Success;
    }
}

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Boss 2 Aimed Shot", story: "Boss 2 warns and fires aimed shot", category: "Action/Boss 2", id: "a0ff7dfec2aa4bb4bdb3cc588299db69")]
public partial class Boss2AimedShotAction : Action
{
    Boss2Controller controller;
    GameObject warning;
    Vector2 direction;
    float timer;
    bool fired;

    protected override Status OnStart()
    {
        controller = GameObject.GetComponent<Boss2Controller>();
        timer = 0f;
        fired = false;
        return controller == null ? Status.Failure : Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (warning == null && !fired)
        {
            if (!controller.TryBeginAimed(out direction, out warning)) return Status.Running;
            timer = 0f;
        }
        timer += Time.deltaTime;
        if (!fired && timer >= controller.AimedWarning)
        {
            controller.FireAimed(direction, warning);
            warning = null;
            fired = true;
            timer = 0f;
        }
        return fired && timer >= controller.AimedRecovery ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        if (!fired) controller?.CancelWarning(warning);
        warning = null;
    }
}
