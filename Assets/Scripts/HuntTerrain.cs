using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 사냥터 지형 관리 (#98 타일맵 후처리). WaveManager가 자동 장착 — 씬 배치 불필요.
///
/// 1) 무한 맵 대응: 칠해진 범위 밖으로 나가면 바닥이 없다 → 칠해진 패턴에서 자주 쓰인
///    바닥 타일(가장자리 전용 타일 제외)을 뽑아 플레이어 주변에 계속 이어 깐다.
/// 2) 구조물 솎아내기: 길을 막는 장애물 타일을 시작 시 확률적으로 지워 간격을 넓힌다.
///    원본 씬 데이터는 건드리지 않는다 — 런타임에만 지워진다.
/// </summary>
public class HuntTerrain : MonoBehaviour
{
    [Tooltip("플레이어 주변 몇 유닛까지 바닥을 보장할지 (화면 모서리 약 16.3 + 여유)")]
    public float fillRadius = 24f;

    [Tooltip("장애물 타일을 지울 확률 (0.45 = 45% 제거)")]
    [Range(0f, 1f)] public float obstacleRemoveChance = 0.45f;

    [Tooltip("바닥 채움에 쓸 타일의 최소 등장 비율 — 이보다 드문 타일(가장자리 등)은 반복하면 어색해서 제외")]
    [Range(0f, 0.5f)] public float fillTileMinShare = 0.05f;

    Tilemap ground;
    Transform player;
    readonly List<TileBase> fillTiles = new List<TileBase>();   // 가중치 = 등장 횟수만큼 중복 수록
    Vector3Int lastFillCenter = new Vector3Int(int.MinValue, 0, 0);

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        Tilemap obstacles = null;
        foreach (Tilemap tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm.gameObject.name == "Ground") ground = tm;
            else if (tm.gameObject.name == "Obstacles") obstacles = tm;
        }

        BuildFillPalette();
        ThinObstacles(obstacles);
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

    /// <summary>장애물 타일을 확률적으로 지운다 — 수가 줄면 간격은 자연히 넓어진다</summary>
    void ThinObstacles(Tilemap obstacles)
    {
        if (obstacles == null || obstacleRemoveChance <= 0f) return;

        foreach (Vector3Int pos in obstacles.cellBounds.allPositionsWithin)
        {
            if (obstacles.GetTile(pos) == null) continue;
            if (Random.value < obstacleRemoveChance) obstacles.SetTile(pos, null);
        }
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
            }
        }
    }
}
