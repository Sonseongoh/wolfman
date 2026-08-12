using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 사냥터 지형 관리 (#98 후처리, #115). WaveManager가 자동 장착 — 씬 배치 불필요.
///
/// 1) 무한 맵 대응: 칠해진 범위 밖으로 나가면 바닥이 없다 → 칠해진 패턴에서 자주 쓰인
///    바닥 타일(가장자리 전용 타일 제외)을 뽑아 플레이어 주변에 계속 이어 깐다.
/// 2) 장애물 전체 랜덤: 칠해진 1x1 장애물 타일은 전부 걷어내고(모서리 투성이라 적이 낌),
///    매 사냥마다 큰 장애물 프리팹(바위·고목·묘비, 둥근 콜라이더)을 무작위로 흩뿌린다.
///    원본 씬 데이터는 건드리지 않는다 — 모두 런타임에서만 일어난다.
/// </summary>
public class HuntTerrain : MonoBehaviour
{
    [Tooltip("플레이어 주변 몇 유닛까지 바닥을 보장할지 (화면 모서리 약 16.3 + 여유)")]
    public float fillRadius = 24f;

    [Tooltip("셀당 큰 장애물이 설 확률 — 매 사냥 배치가 달라진다")]
    [Range(0f, 0.2f)] public float obstacleChance = 0.012f;

    [Tooltip("플레이어에서 이 거리 안에는 장애물을 만들지 않음 (머리 위에 벽이 생기지 않게)")]
    public float obstacleSafeRadius = 5f;

    [Tooltip("바닥 채움에 쓸 타일의 최소 등장 비율 — 이보다 드문 타일(가장자리 등)은 반복하면 어색해서 제외")]
    [Range(0f, 0.5f)] public float fillTileMinShare = 0.05f;

    Tilemap ground;
    Tilemap obstacles;
    Transform player;
    readonly List<TileBase> fillTiles = new List<TileBase>(); // 가중치 = 등장 횟수만큼 중복 수록
    GameObject[] props;   // 흩뿌릴 큰 장애물 프리팹 (Resources/Obstacles)
    Transform propParent; // 흩뿌린 프리팹을 담아 하이어라키를 깔끔하게
    Vector3Int lastFillCenter = new Vector3Int(int.MinValue, 0, 0);

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        foreach (Tilemap tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm.gameObject.name == "Ground") ground = tm;
            else if (tm.gameObject.name == "Obstacles") obstacles = tm;
        }

        BuildFillPalette();
        ClearPaintedObstacles();

        props = Resources.LoadAll<GameObject>("Obstacles");
        propParent = new GameObject("HuntProps").transform;

        ScatterOverPaintedArea();
    }

    /// <summary>칠해진 바닥에서 "자주 쓰인" 타일만 모아 채움 팔레트를 만든다 (등장 횟수 = 가중치)</summary>
    void BuildFillPalette()
    {
        if (ground == null) return;

        var counts = new Dictionary<TileBase, int>();
        int total = 0;

        foreach (Vector3Int pos in ground.cellBounds.allPositionsWithin)
        {
            TileBase t = ground.GetTile(pos);
            if (t == null) continue;
            counts[t] = counts.TryGetValue(t, out int c) ? c + 1 : 1;
            total++;
        }

        int minCount = Mathf.Max(1, Mathf.RoundToInt(total * fillTileMinShare));
        foreach (var kv in counts)
        {
            if (kv.Value < minCount) continue; // 가장자리·희귀 장식 타일 제외
            for (int i = 0; i < kv.Value; i++) fillTiles.Add(kv.Key);
        }
    }

    /// <summary>칠해진 1x1 장애물 타일을 전부 걷어낸다 — 배치는 매 사냥 랜덤 프리팹이 대신한다</summary>
    void ClearPaintedObstacles()
    {
        if (obstacles == null) return;

        foreach (Vector3Int pos in obstacles.cellBounds.allPositionsWithin)
            if (obstacles.GetTile(pos) != null) obstacles.SetTile(pos, null);
    }

    /// <summary>칠해진 바닥 전역에 초기 장애물을 흩뿌린다 (바깥은 탐험하며 이어서 뿌려진다)</summary>
    void ScatterOverPaintedArea()
    {
        if (ground == null) return;

        foreach (Vector3Int pos in ground.cellBounds.allPositionsWithin)
            if (ground.HasTile(pos)) TryScatterProp(pos);
    }

    /// <summary>이 셀에 확률적으로 큰 장애물을 세운다 — 플레이어 주변은 안전지대</summary>
    void TryScatterProp(Vector3Int pos)
    {
        if (props == null || props.Length == 0) return;
        if (Random.value >= obstacleChance) return;

        Vector3 wp = ground.GetCellCenterWorld(pos);
        if (player != null && Vector2.Distance(wp, player.position) <= obstacleSafeRadius) return;

        wp.y -= 0.5f; // 프리팹 피벗이 바닥이라 셀 아래 변에 세운다
        Instantiate(props[Random.Range(0, props.Length)], wp, Quaternion.identity, propParent);
    }

    void Update()
    {
        if (ground == null || player == null || fillTiles.Count == 0) return;

        // 4셀 이상 움직였을 때만 갱신 (매 프레임 전체 검사 방지)
        Vector3Int center = ground.WorldToCell(player.position);
        if ((center - lastFillCenter).sqrMagnitude < 16) return;
        lastFillCenter = center;

        int r = Mathf.CeilToInt(fillRadius);
        for (int y = center.y - r; y <= center.y + r; y++)
        {
            for (int x = center.x - r; x <= center.x + r; x++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (ground.HasTile(pos)) continue;

                ground.SetTile(pos, fillTiles[Random.Range(0, fillTiles.Count)]);
                TryScatterProp(pos); // 새로 깔린 땅에도 같은 밀도로
            }
        }
    }
}
