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
    int shotsFired;
    Boss2Controller.AttackPattern pattern;

    protected override Status OnStart()
    {
        controller = GameObject.GetComponent<Boss2Controller>();
        timer = 0f;
        shotsFired = 0;
        if (controller != null) pattern = controller.BeginAttackCycle();
        if (pattern is Boss2Controller.AttackPattern.Basic or Boss2Controller.AttackPattern.Frenzy)
            controller?.PlaySpreadAnimation();
        return controller == null ? Status.Failure : Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (pattern == Boss2Controller.AttackPattern.Sniper || pattern == Boss2Controller.AttackPattern.Drone) return Status.Success;

        timer += Time.deltaTime;
        if (shotsFired == 0)
        {
            if (timer < controller.SpreadWindup) return Status.Running;
            if (!controller.TryFireSpread()) return Status.Running;
            shotsFired = 1;
            timer = 0f;
        }

        if (pattern == Boss2Controller.AttackPattern.Frenzy)
        {
            if (shotsFired < 3 && timer >= controller.FrenzyShotInterval)
            {
                if (!controller.TryFireSpread()) return Status.Running;
                shotsFired++;
                timer = 0f;
            }
            return shotsFired == 3 ? Status.Success : Status.Running;
        }

        return timer < controller.SpreadRecovery ? Status.Running : Status.Success;
    }

    protected override void OnEnd()
    {
        if (pattern != Boss2Controller.AttackPattern.Drone) controller?.PlayIdleAnimation();
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
    int shotsFired;
    int shotsToFire;
    bool warningStarted;

    protected override Status OnStart()
    {
        controller = GameObject.GetComponent<Boss2Controller>();
        timer = 0f;
        shotsFired = 0;
        warningStarted = false;
        shotsToFire = controller != null && controller.CurrentAttackPattern == Boss2Controller.AttackPattern.Sniper ? 2 : 1;
        return controller == null ? Status.Failure : Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (controller.CurrentAttackPattern is Boss2Controller.AttackPattern.Frenzy or Boss2Controller.AttackPattern.Drone) return Status.Success;

        if (!warningStarted)
        {
            if (!controller.TryBeginAimed(out direction, out warning)) return Status.Running;
            controller.PlayAimedAnimation();
            warningStarted = true;
            timer = 0f;
        }
        timer += Time.deltaTime;
        if (warning != null && timer >= controller.AimedWarning)
        {
            controller.FireAimed(direction, warning);
            warning = null;
            shotsFired++;
            timer = 0f;
        }

        if (shotsFired < shotsToFire)
        {
            if (warning == null && timer >= controller.SniperShotInterval) warningStarted = false;
            return Status.Running;
        }
        return timer >= controller.AimedRecovery ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        controller?.CancelWarning(warning);
        controller?.PlayIdleAnimation();
        warning = null;
    }
}
