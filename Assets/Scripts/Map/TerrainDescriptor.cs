using UnityEngine;

public enum TerrainKind
{
    Background,
    Floor,
    SolidWall,
    DashPassableWall,
    BoundaryWall,
    Platform,
    Spike,
    ElectricFloor,
    EnhancedElectricFloor
}

[System.Flags]
public enum ActorTarget
{
    None = 0,
    Player = 1 << 0,
    Boss = 1 << 1
}

/// TBD(TERR-006): 플레이어-발판 상호작용은 기획 미확정. 코드에 고정하지 말고 Inspector 값으로만 바꾼다.
public enum PlayerPlatformPolicy
{
    Solid,
    OneWayUp,
    PassThrough,
    Disabled
}

/// 지형 오브젝트의 게임플레이 의미를 담는 진실의 원천 (terrain_system_handover.md 9.2~9.3).
/// 외형·Tilemap 레이어·에셋 이름으로 지형 타입을 추론하지 않는다 (TERR-009).
/// 통과 가능 여부는 terrainKind에서 파생되므로 잘못된 조합 자체가 불가능하다.

public sealed class TerrainDescriptor : MonoBehaviour
{
    public TerrainKind terrainKind;
    public ActorTarget targetMask = ActorTarget.Player | ActorTarget.Boss;

    [Tooltip("TBD(문서 8.3): 공격/투사체 차단 여부는 기획 미확정. 이동 차단과 별개 값으로 유지한다.")]
    public bool blocksProjectile = true;

    [Header("Platform 전용 - TBD(TERR-006)")]
    public PlayerPlatformPolicy playerPlatformPolicy = PlayerPlatformPolicy.Solid;

    public bool AllowsWallDashPass => terrainKind == TerrainKind.DashPassableWall;
    public bool IsAbsoluteBoundary => terrainKind == TerrainKind.BoundaryWall;

    /// 충돌한 Collider에서 지형 의미를 찾는다. 벽뚫대시 판정 등에서 사용.
    public static TerrainDescriptor From(Collider2D collider)
    {
        return collider != null ? collider.GetComponentInParent<TerrainDescriptor>() : null;
    }
}
