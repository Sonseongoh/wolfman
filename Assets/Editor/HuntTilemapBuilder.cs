using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 사냥 씬(HuntScene) 바닥에 공동묘지 타일맵을 깐다 (#91).
/// Tools/wolfman/사냥 배경 타일 깔기 메뉴 실행.
/// </summary>
public static class HuntTilemapBuilder
{
    const int   FieldMinX = -20, FieldMaxX = 20;
    const int   FieldMinY = -14, FieldMaxY = 14;
    const float SpawnClearRadius = 5f;

    // 구역 내 장애물 밀도 (%)
    const int ZoneObstaclePercent = 45;
    // 열린 공간 해골 잔해 밀도 (%)
    const int DebrisPercent = 3;

    const int GroundSortingOrder  = -100;
    const int DebrisSortingOrder  =  -95;
    const int ObstacleSortingOrder = -90;

    const string SpriteDir    = "Assets/Art/Tiles/Hunt";
    const string TileAssetDir = "Assets/Art/Tiles/Hunt/TileAssets";
    const string SceneName    = "HuntScene";
    const string GridName     = "HuntGrid";
    const string GroundName   = "Ground";
    const string DebrisName   = "Debris";
    const string ObstacleName = "Obstacles";

    static readonly string[] GroundTiles = { "HuntGround_0", "HuntGround_1", "HuntGround_2" };

    // 나무 구역 타일 풀
    static readonly string[] TreeTiles  = { "DarkPine", "DarkPine", "DarkPine", "DeadTree", "MossRock" };
    // 묘지 구역 타일 풀
    static readonly string[] GraveTiles = { "GraveMound", "GraveMound", "Cross", "Gravestone_S", "Gravestone_L", "Cross" };
    // 땅 위 잔해 (콜라이더 없음)
    static readonly string[] DebrisTiles = { "Skull" };

    // 구역 정의: (x, y, 반경, 타입) 타입 0=나무숲, 1=묘지
    // 맵(-20~+20, -14~+14) 안에서 플레이어 스폰(0,0)과 충분히 떨어진 위치
    static readonly (int x, int y, int r, int type)[] Zones =
    {
        // 나무 숲 (4곳 — 외곽 코너 근처)
        (-15,  10,  5, 0),
        ( 15,   9,  5, 0),
        (-16,  -9,  5, 0),
        ( 14, -11,  5, 0),

        // 묘지 클러스터 (4곳 — 숲 사이 공간)
        ( -8,   5,  4, 1),
        (  8,  -5,  4, 1),
        ( -5, -11,  3, 1),
        ( 13,   2,  3, 1),
    };

    [MenuItem("Tools/wolfman/사냥 배경 타일 깔기")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != SceneName)
        {
            EditorUtility.DisplayDialog(
                "사냥 씬이 아닙니다",
                $"먼저 {SceneName} 을 열고 실행하세요.\n지금 열린 씬: {scene.name}",
                "확인");
            return;
        }

        var tiles = LoadOrCreateTiles();
        if (tiles.Count == 0)
        {
            EditorUtility.DisplayDialog("타일 없음", $"{SpriteDir} 에 PNG가 없습니다.", "확인");
            return;
        }

        var (ground, debris, obstacles) = PrepareTilemaps();
        ground.ClearAllTiles();
        debris.ClearAllTiles();
        obstacles.ClearAllTiles();

        for (int cy = FieldMinY; cy <= FieldMaxY; cy++)
        {
            for (int cx = FieldMinX; cx <= FieldMaxX; cx++)
            {
                // ── 바닥 ──────────────────────────────────────────────
                string gName = GroundTiles[Hash(cx, cy, 1) % GroundTiles.Length];
                if (tiles.TryGetValue(gName, out var gt))
                    ground.SetTile(new Vector3Int(cx, cy, 0), gt);

                // 스폰 주변 클리어
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                if (dist < SpawnClearRadius) continue;

                // ── 가장 가까운 구역 찾기 ─────────────────────────────
                int bestZone = -1;
                float bestDist = float.MaxValue;
                for (int z = 0; z < Zones.Length; z++)
                {
                    float dz = Mathf.Sqrt(
                        (cx - Zones[z].x) * (cx - Zones[z].x) +
                        (cy - Zones[z].y) * (cy - Zones[z].y));
                    if (dz < Zones[z].r && dz < bestDist)
                    {
                        bestDist = dz;
                        bestZone = z;
                    }
                }

                if (bestZone >= 0)
                {
                    // 구역 내 장애물 배치
                    if (Hash(cx, cy, 3) % 100 < ZoneObstaclePercent)
                    {
                        string[] pool = Zones[bestZone].type == 0 ? TreeTiles : GraveTiles;
                        string oName = pool[Hash(cx, cy, 7) % pool.Length];
                        if (tiles.TryGetValue(oName, out var ot))
                            obstacles.SetTile(new Vector3Int(cx, cy, 0), ot);
                    }
                }
                else
                {
                    // 열린 공간 — 해골 잔해 드물게
                    if (Hash(cx, cy, 11) % 100 < DebrisPercent)
                    {
                        string dName = DebrisTiles[Hash(cx, cy, 13) % DebrisTiles.Length];
                        if (tiles.TryGetValue(dName, out var dt))
                            debris.SetTile(new Vector3Int(cx, cy, 0), dt);
                    }
                }
            }
        }

        // Obstacles 레이어에만 콜라이더 (Debris는 걸어다닐 수 있음)
        var col = obstacles.gameObject.GetComponent<TilemapCollider2D>();
        if (col == null) col = obstacles.gameObject.AddComponent<TilemapCollider2D>();

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[HuntTilemapBuilder] 사냥 배경 타일 배치 완료. Ctrl+S 로 씬 저장하세요.");
    }

    static int Hash(int cx, int cy, int salt)
    {
        long n = ((long)cx * 73856093) ^ ((long)cy * 19349663) ^ ((long)salt * 83492791);
        n &= 0x7FFFFFFF;
        n = ((n ^ (n >> 13)) * 1274126177) & 0x7FFFFFFF;
        return (int)((n ^ (n >> 16)) & 0x7FFFFFFF);
    }

    static Dictionary<string, TileBase> LoadOrCreateTiles()
    {
        if (!AssetDatabase.IsValidFolder(TileAssetDir))
            AssetDatabase.CreateFolder(SpriteDir, "TileAssets");

        // 장애물(충돌) 타일 목록 — Skull과 Debris는 제외
        var collidableTiles = new HashSet<string>
            { "GraveMound", "Cross", "Gravestone_S", "Gravestone_L", "MossRock", "DeadTree", "DarkPine" };

        var result = new Dictionary<string, TileBase>();
        var guids  = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir });

        foreach (var guid in guids)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guid);
            string name    = Path.GetFileNameWithoutExtension(texPath);
            var sprite     = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            if (sprite == null) continue;

            string tilePath = $"{TileAssetDir}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = collidableTiles.Contains(name)
                    ? Tile.ColliderType.Sprite
                    : Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            result[name] = tile;
        }
        return result;
    }

    static (Tilemap ground, Tilemap debris, Tilemap obstacles) PrepareTilemaps()
    {
        var gridGo = GameObject.Find(GridName);
        if (gridGo == null)
        {
            gridGo = new GameObject(GridName);
            Undo.RegisterCreatedObjectUndo(gridGo, "HuntGrid 생성");
        }

        var grid = gridGo.GetComponent<Grid>();
        if (grid == null)
            grid = Undo.AddComponent<Grid>(gridGo);
        grid.cellSize = new Vector3(1f, 1f, 0f);

        Tilemap MakeLayer(string layerName, int sortOrder)
        {
            var tf = gridGo.transform.Find(layerName);
            GameObject go;
            if (tf != null)
            {
                go = tf.gameObject;
            }
            else
            {
                go = new GameObject(layerName);
                Undo.RegisterCreatedObjectUndo(go, layerName + " 생성");
                go.transform.SetParent(gridGo.transform, false);
            }

            var tm = go.GetComponent<Tilemap>();
            if (tm == null) tm = Undo.AddComponent<Tilemap>(go);

            var tr = go.GetComponent<TilemapRenderer>();
            if (tr == null) tr = Undo.AddComponent<TilemapRenderer>(go);
            tr.sortingOrder = sortOrder;
            return tm;
        }

        return (
            MakeLayer(GroundName,    GroundSortingOrder),
            MakeLayer(DebrisName,    DebrisSortingOrder),
            MakeLayer(ObstacleName,  ObstacleSortingOrder)
        );
    }
}
