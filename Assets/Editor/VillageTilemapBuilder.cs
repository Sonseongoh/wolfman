using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 마을 씬 바닥에 지형 타일을 깐다 (#54).
///
/// 메뉴 한 번으로 세 가지를 한다:
///   1. Assets/Art/Tiles/ 의 스프라이트마다 Tile 에셋을 만든다 (이미 있으면 재사용)
///   2. VillageScene 에 Grid + Tilemap 을 구성한다 (Cell Size 1 = 타일 64px ÷ PPU 64)
///   3. 광장·길 규칙에 따라 어떤 칸에 어떤 타일이 올지 계산해 채운다
///
/// 지형 모양을 바꾸려면 아래 상수만 고치고 메뉴를 다시 실행하면 된다.
/// 아트 규격은 Docs/Assets.md, 근거는 Docs/adr/0001 참고.
/// </summary>
public static class VillageTilemapBuilder
{
    // --- 지형 정의 (cell 좌표. cell (cx,cy) 의 좌하단이 월드 (cx,cy)) ---

    /// 흙 광장 범위. 시설이 (-4,2)·(4,2), 플레이어가 (0,0) 에 있다.
    const int PlazaMinX = -7, PlazaMaxX = 6;
    const int PlazaMinY = -4, PlazaMaxY = 4;

    /// 타일을 채우는 전체 범위. 카메라(ortho 5 → 세로 10유닛)가 보는 것보다 넓게 둔다.
    const int FieldMinX = -14, FieldMaxX = 13;
    const int FieldMinY = -10, FieldMaxY = 10;

    /// 자갈길. 가로길은 시설 두 채를 잇고, 세로길이 광장을 남북으로 가른다.
    const int RoadY = 2, RoadX = 0;

    /// 광장 내부에서 자갈이 촘촘한 칸이 나올 확률(%)과 풀밭 돌멩이 확률(%).
    const int GravelPercent = 22, RockPercent = 7;

    /// 바닥이므로 다른 스프라이트(전부 order 0)보다 확실히 뒤에 둔다.
    const int GroundSortingOrder = -100;

    const string SpriteDir = "Assets/Art/Tiles";
    const string TileAssetDir = "Assets/Art/Tiles/TileAssets";
    const string SceneName = "VillageScene";
    const string GridName = "VillageGrid";
    const string GroundName = "Ground";

    [MenuItem("Tools/wolfman/마을 배경 타일 깔기")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != SceneName)
        {
            EditorUtility.DisplayDialog(
                "마을 씬이 아닙니다",
                $"먼저 {SceneName} 을 열고 실행하세요.\n지금 열린 씬: {scene.name}",
                "확인");
            return;
        }

        var tiles = LoadOrCreateTiles();
        if (tiles.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "타일 스프라이트가 없습니다",
                $"{SpriteDir} 에 타일 PNG 가 보이지 않습니다.",
                "확인");
            return;
        }

        var tilemap = PrepareTilemap();
        tilemap.ClearAllTiles();

        int painted = 0, missing = 0;
        var notFound = new HashSet<string>();

        for (int cy = FieldMinY; cy <= FieldMaxY; cy++)
        {
            for (int cx = FieldMinX; cx <= FieldMaxX; cx++)
            {
                string name = TileFor(cx, cy);
                if (tiles.TryGetValue(name, out var tile))
                {
                    tilemap.SetTile(new Vector3Int(cx, cy, 0), tile);
                    painted++;
                }
                else
                {
                    notFound.Add(name);
                    missing++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        string warn = missing > 0
            ? $"\n\n※ 없는 타일 {missing}칸: {string.Join(", ", notFound)}"
            : "";
        Debug.Log($"[VillageTilemapBuilder] {painted}칸을 채웠습니다. " +
                  $"타일 종류 {tiles.Count}개.{warn}\n씬 저장은 Ctrl+S 로 직접 하세요.");
    }

    // ------------------------------------------------------------------ 타일 선택

    /// <summary>
    /// 이 칸에 어떤 타일이 올지 정한다. 이웃이 흙인지 보고 가장자리·모서리를 고른다.
    /// </summary>
    static string TileFor(int cx, int cy)
    {
        if (!IsDirt(cx, cy))
        {
            if (Hash(cx, cy, 5) % 100 < RockPercent) return "GrassRock_0";
            return "Grass_" + (Hash(cx, cy, 1) % 3);
        }

        bool gn = !IsDirt(cx, cy + 1);
        bool gs = !IsDirt(cx, cy - 1);
        bool ge = !IsDirt(cx + 1, cy);
        bool gw = !IsDirt(cx - 1, cy);

        // 두 변이 풀 → 볼록 모서리
        if (gn && gw) return "DirtCorner_NW";
        if (gn && ge) return "DirtCorner_NE";
        if (gs && gw) return "DirtCorner_SW";
        if (gs && ge) return "DirtCorner_SE";

        // 한 변만 풀 → 가장자리
        if (gn) return "DirtEdge_N";
        if (gs) return "DirtEdge_S";
        if (gw) return "DirtEdge_W";
        if (ge) return "DirtEdge_E";

        // 사방이 흙인데 대각선이 풀 → 오목 모서리
        if (!IsDirt(cx - 1, cy + 1)) return "DirtInner_NW";
        if (!IsDirt(cx + 1, cy + 1)) return "DirtInner_NE";
        if (!IsDirt(cx - 1, cy - 1)) return "DirtInner_SW";
        if (!IsDirt(cx + 1, cy - 1)) return "DirtInner_SE";

        // 광장 안쪽 — 길이 지나가면 길, 아니면 흙 변형
        int ix0 = PlazaMinX + 1, ix1 = PlazaMaxX - 1;
        int iy0 = PlazaMinY + 1, iy1 = PlazaMaxY - 1;
        bool onH = cy == RoadY, onV = cx == RoadX;

        if (onH && onV) return "Path_Cross";
        if (onH)
        {
            if (cx == ix0) return "Path_End_W";
            if (cx == ix1) return "Path_End_E";
            return "Path_H";
        }
        if (onV)
        {
            if (cy == iy1) return "Path_End_N";
            if (cy == iy0) return "Path_End_S";
            return "Path_V";
        }

        if (Hash(cx, cy, 3) % 100 < GravelPercent)
            return "DirtGravel_" + (Hash(cx, cy, 4) % 2);
        return "Dirt_" + (Hash(cx, cy, 2) % 3);
    }

    static bool IsDirt(int cx, int cy)
    {
        return cx >= PlazaMinX && cx <= PlazaMaxX
            && cy >= PlazaMinY && cy <= PlazaMaxY;
    }

    /// 좌표 → 안정적인 난수. 같은 칸은 언제 실행해도 같은 변형이 나온다.
    static int Hash(int cx, int cy, int salt)
    {
        long n = ((long)cx * 73856093) ^ ((long)cy * 19349663) ^ ((long)salt * 83492791);
        n &= 0x7FFFFFFF;
        n = ((n ^ (n >> 13)) * 1274126177) & 0x7FFFFFFF;
        return (int)((n ^ (n >> 16)) & 0x7FFFFFFF);
    }

    // ------------------------------------------------------------------ 에셋 · 씬

    static Dictionary<string, TileBase> LoadOrCreateTiles()
    {
        if (!AssetDatabase.IsValidFolder(TileAssetDir))
            AssetDatabase.CreateFolder(SpriteDir, "TileAssets");

        var result = new Dictionary<string, TileBase>();
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir });

        foreach (var guid in guids)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(texPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            if (sprite == null) continue;

            string tilePath = $"{TileAssetDir}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else if (tile.sprite != sprite)
            {
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
            }
            result[name] = tile;
        }
        return result;
    }

    static Tilemap PrepareTilemap()
    {
        var gridGo = GameObject.Find(GridName);
        if (gridGo == null)
        {
            gridGo = new GameObject(GridName);
            Undo.RegisterCreatedObjectUndo(gridGo, "마을 Grid 생성");
        }

        var grid = gridGo.GetComponent<Grid>();
        if (grid == null) grid = gridGo.AddComponent<Grid>();
        // 타일 64px ÷ PPU 64 = 1유닛. 규격이 바뀌면 Docs/Assets.md 를 따라 고칠 것.
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var groundTf = gridGo.transform.Find(GroundName);
        GameObject groundGo = groundTf != null ? groundTf.gameObject : null;
        if (groundGo == null)
        {
            groundGo = new GameObject(GroundName);
            groundGo.transform.SetParent(gridGo.transform, false);
            Undo.RegisterCreatedObjectUndo(groundGo, "마을 Ground 생성");
        }

        var tilemap = groundGo.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = groundGo.AddComponent<Tilemap>();

        var renderer = groundGo.GetComponent<TilemapRenderer>();
        if (renderer == null) renderer = groundGo.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = GroundSortingOrder;

        return tilemap;
    }
}
