using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 달 공개 연출 + 사냥/마을 선택 (#103). 라운드 시작마다 마을 씬에서 재생된다.
/// WaveManager(#43 임시 구조)에 있던 슬롯·전설 승급·확정 카드 연출을 그대로 옮겨왔다.
///
/// 사용법: AddComponent 하면 스스로 연출을 시작하고, Result가 None이 아니게 되면
/// 선택까지 끝난 것 — 붙인 쪽(VillageController)이 결과를 읽고 이 컴포넌트를 제거한다.
/// 연출은 시간 정지와 무관하게 실시간으로 진행된다 (#101과 같은 이유).
/// </summary>
public class MoonRevealUI : MonoBehaviour
{
  public enum Choice { None, Hunt, Stay }

  /// <summary>플레이어의 선택 — None이 아니면 연출·선택이 모두 끝난 것</summary>
  public Choice Result { get; private set; }

  /// <summary>연출·선택 진행 중인지 (PauseSystem이 일시정지 가드에 사용)</summary>
  public static bool Active { get; private set; }

  [Tooltip("슬롯 회전 시간(초) — 마을에서 여유 있게 보는 연출이라 사냥 시절(1.8초)보다 길게")]
  public float spinDuration = 3f;

  bool spinning;          // 슬롯 연출 중
  string spinName;        // 슬롯이 돌면서 보여주는 이름
  Sprite spinIcon;        // 슬롯이 돌면서 보여주는 아이콘
  MoonRarity spinRarity;  // 슬롯이 돌면서 보여주는 달의 희귀도
  Color spinColor;        // 슬롯이 돌면서 보여주는 달의 고유 색 (ambientColor)
  bool promoting;         // 등급 승급 연출 중 (전설 전용)
  int promoTier;          // 승급 연출에서 현재 보여주는 등급 (0=Common)
  float promoStepStart;   // 현재 승급 단계가 시작된 시각 (펀치·플래시용)
  string promoDecoyName;  // 가짜 공개용 미끼 달 이름
  Sprite promoDecoyIcon;  // 미끼 달 아이콘
  Color promoDecoyColor;  // 미끼 달의 고유 색 (ambientColor)
  float bannerTimer;      // 확정 카드 표시 시간 (선택 전까지 유지)
  bool choosing;          // 사냥/마을 버튼 표시 중

  MoonData Moon => GameManager.Instance != null ? GameManager.Instance.CurrentMoon : null;

  void Start()
  {
    Active = true;
    StartCoroutine(RevealRoutine());
  }

  void OnDestroy()
  {
    Active = false;
  }

  void Update()
  {
    if (bannerTimer > 0f)
    {
      bannerTimer -= Time.unscaledDeltaTime;
      if (choosing && bannerTimer < 0.01f) bannerTimer = 0.01f; // 선택할 때까지 카드 유지
    }
  }

  IEnumerator RevealRoutine()
  {
    MoonTable table = GameManager.Instance != null ? GameManager.Instance.moonTable : null;

    if (table != null && table.moons != null && table.moons.Length > 1 && Moon != null)
    {
      // 1) 슬롯: 달들이 빠르게 돌다가 감속하며 멈춘다
      spinning = true;
      float elapsed = 0f;
      float nextFlipAt = 0f;
      int idx = Random.Range(0, table.moons.Length);

      while (elapsed < spinDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= nextFlipAt)
        {
          idx = (idx + 1) % table.moons.Length;
          spinName = table.moons[idx].moonName;
          spinIcon = table.moons[idx].icon;
          spinRarity = table.moons[idx].rarity;
          spinColor = table.moons[idx].ambientColor;
          SoundManager.Instance?.PlaySlot();
          // 처음엔 빠르게(0.05초), 끝으로 갈수록 느리게(0.3초) — 슬롯 감속
          nextFlipAt = elapsed + Mathf.Lerp(0.05f, 0.3f, elapsed / spinDuration);
        }
        yield return null;
      }
      spinning = false;

      // 2) 등급 승급 연출 — 슈퍼 블루 블러드문(전설) 전용 의식:
      // 일반 달이 뜬 것처럼 가짜 공개 → 부들부들 → 땅! 땅! 땅! → 금색 대공개
      if (Moon != null && Moon.rarity == MoonRarity.Legendary)
      {
        int finalTier = (int)Moon.rarity;

        promoting = true;

        // 1단계: Common 달로 진짜 공개인 척 2초 — 완전히 방심시킨다
        promoTier = 0;
        SetPromoDisplay(table, MoonRarity.Common);
        promoStepStart = Time.unscaledTime;
        yield return new WaitForSecondsRealtime(2.0f);

        // 이후: 등급이 오를 때마다 그 등급의 달로 변모하며 땅땅땅
        for (int tier = 1; tier < finalTier; tier++)
        {
          promoTier = tier;
          SetPromoDisplay(table, (MoonRarity)tier);
          promoStepStart = Time.unscaledTime;
          SoundManager.Instance?.PlayPromoStep();
          yield return new WaitForSecondsRealtime(0.4f);
        }
        promoting = false;
      }

      bannerTimer = 1.6f; // 확정된 달 보여주기 (최종 공개가 마지막 땅!)
      SoundManager.Instance?.PlayMoonReveal(Moon?.rarity == MoonRarity.Legendary);
    }

    // 3) 사냥/마을 선택 — 카드가 떠 있는 채로 버튼 표시
    choosing = true;
  }

  /// <summary>승급 연출: 해당 등급의 달 중 하나를 골라 카드에 표시</summary>
  void SetPromoDisplay(MoonTable table, MoonRarity rarity)
  {
    var candidates = new List<MoonData>();
    foreach (MoonData m in table.moons)
      if (m != null && m.rarity == rarity) candidates.Add(m);

    if (candidates.Count > 0)
    {
      MoonData pick = candidates[Random.Range(0, candidates.Count)];
      promoDecoyName = pick.moonName;
      promoDecoyIcon = pick.icon;
      promoDecoyColor = pick.ambientColor;
    }
  }

  static Texture2D s_CircleGlow;

  static Texture2D CircleGlowTex()
  {
    if (s_CircleGlow != null) return s_CircleGlow;
    const int sz = 64;
    s_CircleGlow = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
    Color[] px = new Color[sz * sz];
    Vector2 c = new Vector2(sz * 0.5f, sz * 0.5f);
    for (int y = 0; y < sz; y++)
      for (int x = 0; x < sz; x++)
      {
        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (sz * 0.5f);
        px[y * sz + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d));
      }
    s_CircleGlow.SetPixels(px);
    s_CircleGlow.Apply();
    return s_CircleGlow;
  }

  public static Color RarityColor(MoonRarity r)
  {
    switch (r)
    {
      case MoonRarity.Rare: return new Color(0.4f, 0.7f, 1f);
      case MoonRarity.Epic: return new Color(0.8f, 0.4f, 1f);
      case MoonRarity.Legendary: return new Color(1f, 0.7f, 0.1f);
      default: return new Color(0.85f, 0.85f, 0.85f);
    }
  }

  void OnGUI()
  {
    // 달 슬롯머신 연출: 카드 안에서 달이 돌아가고, 카드가 반짝인다
    if (spinning && spinName != null)
    {
      float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 8f);
      Color spinBorder = RarityColor(spinRarity) * pulse;
      DrawMoonCard("오늘 밤의 달은...", spinName, spinIcon, spinBorder, 1f, Vector2.zero, spinRarity, spinColor * pulse);
    }

    // 등급 승급 연출: 미끼 달이 공개된 척하다가 떨리며 단계별로 땅! 땅! 승급
    if (promoting)
    {
      // 승급할수록 배경이 점점 어두워짐
      float darkAlpha = 0.25f + promoTier * 0.1f;
      GUI.color = new Color(0f, 0f, 0f, darkAlpha);
      GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
      GUI.color = Color.white;

      float ts = Time.unscaledTime - promoStepStart;
      Color pc = RarityColor((MoonRarity)promoTier);

      // 승급 순간 작은 플래시
      if (ts < 0.12f)
      {
        GUI.color = new Color(pc.r, pc.g, pc.b, 0.15f * (1f - ts / 0.12f));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
      }

      // 승급 순간 카드 펀치. 미끼 단계(0)는 떨지 않고, 승급이 시작되면 점점 격해짐
      float punch = 1f + 0.18f * Mathf.Pow(1f - Mathf.Clamp01(ts / 0.15f), 2f);
      float amp = promoTier == 0 ? 0f : 2f + promoTier * 3f;
      Vector2 shake = new Vector2(
          Mathf.Sin(Time.unscaledTime * 67f),
          Mathf.Cos(Time.unscaledTime * 53f) * 0.6f) * amp;

      DrawMoonCard($"{promoDecoyName}이 떠올랐다!", promoDecoyName, promoDecoyIcon, pc, punch, shake, (MoonRarity)promoTier, promoDecoyColor);
    }

    // 확정된 달 카드 — 플래시 → 카드 쿵 착지 → 광선 회전 + 카드 반짝임
    MoonData moon = Moon;
    if (moon != null && bannerTimer > 0f && !spinning && !promoting)
    {
      const float bannerDuration = 1.6f;
      float t = bannerDuration - bannerTimer; // 공개 후 경과 시간
      Color rc = RarityColor(moon.rarity);

      // 전설: 배경 어둡게
      if (moon.rarity == MoonRarity.Legendary)
      {
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
      }

      // 1) 공개 순간 등급색 화면 플래시 (0.35초간 사라짐)
      if (t < 0.35f)
      {
        GUI.color = new Color(rc.r, rc.g, rc.b, 0.4f * (1f - t / 0.35f));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
      }

      // 2) 카드 뒤에서 천천히 도는 등급색 광선 8줄기
      // Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.42f);
      // float rayLen = Screen.height * 0.34f;
      // Matrix4x4 saved = GUI.matrix;
      // for (int i = 0; i < 8; i++)
      // {
      //   GUI.matrix = saved;
      //   GUIUtility.RotateAroundPivot(i * 45f + Time.unscaledTime * 25f, center);
      //   GUI.color = new Color(rc.r, rc.g, rc.b, 0.1f);
      //   GUI.DrawTexture(
      //       new Rect(center.x - rayLen * 0.035f, center.y - rayLen, rayLen * 0.07f, rayLen),
      //       Texture2D.whiteTexture);
      // }
      // GUI.matrix = saved;
      // GUI.color = Color.white;

      // 3) 카드 쿵 착지 (1.35배 → 제자리) + 등급색 테두리
      float punch = 1f + 0.35f * Mathf.Pow(1f - Mathf.Clamp01(t / 0.25f), 2f);
      DrawMoonCard($"{moon.moonName}이 떠올랐다!", moon.moonName, moon.icon, rc, punch, Vector2.zero, moon.rarity, moon.ambientColor);
    }

    // 사냥/마을 선택 버튼 — 카드 아래
    if (choosing && Result == Choice.None)
    {
      float btnW = 200f, btnH = 55f;
      float btnY = Screen.height * 0.82f;

      if (GUI.Button(new Rect(Screen.width * 0.5f - btnW - 20, btnY, btnW, btnH), "사냥 나가기"))
        Choose(Choice.Hunt);

      if (GUI.Button(new Rect(Screen.width * 0.5f + 20, btnY, btnW, btnH), "마을 남기"))
        Choose(Choice.Stay);
    }
  }

  /// <summary>
  /// 선택 확정 — OnGUI 버튼과 PlayMode 테스트(배치모드라 버튼을 못 누름)가 함께 쓰는 진입점.
  /// </summary>
  public void Choose(Choice pick)
  {
    if (Result != Choice.None || pick == Choice.None) return;

    SoundManager.Instance?.PlayButton();
    Result = pick;
  }

  /// <summary>
  /// 달 카드 그리기: 테두리 + 어두운 카드 안에 아이콘·이름, 표면을 스치는 반짝임(샤인).
  /// scale은 등장 펀치용 (1 = 기본 크기).
  /// </summary>
  static void DrawMoonCard(string title, string moonName, Sprite icon, Color borderColor, float scale, Vector2 shakeOffset, MoonRarity rarity = MoonRarity.Common, Color moonColor = default)
  {
    float cardW = Mathf.Min(360f, Screen.width * 0.34f) * scale;
    float cardH = cardW * 1.3f;
    Rect card = new Rect(
        (Screen.width - cardW) * 0.5f + shakeOffset.x,
        Screen.height * 0.42f - cardH * 0.5f + shakeOffset.y,
        cardW, cardH);

    // Uncommon 이상: 테두리 바깥 글로우 — 희귀도별 강도
    if (rarity >= MoonRarity.Uncommon)
    {
      Color gc = moonColor.a > 0f ? moonColor : borderColor;
      float gPulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.5f);
      int layers = rarity == MoonRarity.Legendary ? 7 : rarity == MoonRarity.Epic ? 5 : rarity == MoonRarity.Rare ? 2 : 2;
      float maxPad = rarity == MoonRarity.Legendary ? 28f : rarity == MoonRarity.Epic ? 18f : rarity == MoonRarity.Rare ? 12f : 10f;
      float baseAlpha = rarity == MoonRarity.Legendary ? 0.38f : rarity == MoonRarity.Epic ? 0.30f : rarity == MoonRarity.Rare ? 0.26f : 0.22f;

      for (int i = layers; i >= 1; i--)
      {
        float pad = maxPad * i / layers;
        float alpha = baseAlpha * (1f - (float)(i - 1) / layers) * gPulse;
        GUI.color = new Color(gc.r, gc.g, gc.b, alpha);
        GUI.DrawTexture(
            new Rect(card.x - pad, card.y - pad, card.width + pad * 2, card.height + pad * 2),
            Texture2D.whiteTexture);
      }
      GUI.color = Color.white;
    }

    // 테두리
    GUI.color = borderColor;
    GUI.DrawTexture(new Rect(card.x - 4, card.y - 4, card.width + 8, card.height + 8), Texture2D.whiteTexture);

    // 카드 배경 — 밤하늘색 통일
    GUI.color = new Color(0.04f, 0.04f, 0.08f);
    GUI.DrawTexture(card, Texture2D.whiteTexture);
    GUI.color = Color.white;

    // Rare 이상: 모서리 L자 엑센트 — Rare는 작고 가늘게, Epic은 크고 굵게
    if (rarity >= MoonRarity.Rare && rarity != MoonRarity.Legendary)
    {
      bool isEpic = rarity == MoonRarity.Epic;
      float inset = isEpic ? 9f : 11f;
      float arm = isEpic ? Mathf.Min(22f, cardW * 0.09f) : Mathf.Min(13f, cardW * 0.055f);
      float thick = isEpic ? 2f : 1.2f;
      float aPulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.5f);
      GUI.color = new Color(borderColor.r, borderColor.g, borderColor.b, (isEpic ? 0.8f : 0.6f) * aPulse);
      float x0 = card.x + inset, x1 = card.x + card.width - inset;
      float y0 = card.y + inset, y1 = card.y + card.height - inset;
      GUI.DrawTexture(new Rect(x0, y0, arm, thick), Texture2D.whiteTexture);
      GUI.DrawTexture(new Rect(x0, y0, thick, arm), Texture2D.whiteTexture);
      GUI.DrawTexture(new Rect(x1 - arm, y0, arm, thick), Texture2D.whiteTexture);
      GUI.DrawTexture(new Rect(x1 - thick, y0, thick, arm), Texture2D.whiteTexture);
      GUI.DrawTexture(new Rect(x0, y1 - thick, arm, thick), Texture2D.whiteTexture);
      GUI.DrawTexture(new Rect(x0, y1 - arm, thick, arm), Texture2D.whiteTexture);
      GUI.DrawTexture(new Rect(x1 - arm, y1 - thick, arm, thick), Texture2D.whiteTexture);
      GUI.DrawTexture(new Rect(x1 - thick, y1 - arm, thick, arm), Texture2D.whiteTexture);
      GUI.color = Color.white;
    }

    // 제목 (카드 위 바깥)
    GUIStyle titleStyle = new GUIStyle
    {
      fontSize = 24,
      fontStyle = FontStyle.Bold,
      alignment = TextAnchor.MiddleCenter,
      normal = { textColor = borderColor }
    };
    GUI.Label(new Rect(0, card.y - 44, Screen.width, 34), title, titleStyle);

    // 아이콘 (카드 안 상단)
    if (icon != null)
    {
      float isz = cardW * (rarity == MoonRarity.Legendary ? 0.76f : 0.62f);
      float baseCenterY = card.y + cardH * 0.12f + cardW * 0.62f * 0.5f;
      float iconX = card.x + (cardW - isz) * 0.5f;
      float iconY = baseCenterY - isz * 0.5f;

      // Epic/Legendary 전용: 달 아이콘 원형 글로우 (카드 밖으로 안 나가도록 클립)
      if (rarity >= MoonRarity.Epic)
      {
        Color ic = moonColor.a > 0f ? moonColor : borderColor;
        Color iconGlow = new Color(
            Mathf.Max(ic.r * 0.5f, 0.08f),
            Mathf.Max(ic.g * 0.5f, 0.06f),
            Mathf.Max(ic.b * 0.5f, 0.10f));
        float iPulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 1.8f);
        float glowExt = isz * 0.09f;
        float relX = (cardW - isz) * 0.5f;
        float relY = iconY - card.y;
        GUI.BeginGroup(card);
        GUI.color = new Color(iconGlow.r, iconGlow.g, iconGlow.b, iPulse);
        GUI.DrawTexture(
            new Rect(relX - glowExt, relY - glowExt, isz + glowExt * 2, isz + glowExt * 2),
            CircleGlowTex(), ScaleMode.StretchToFill, true);
        GUI.color = Color.white;
        GUI.EndGroup();
      }

      GUI.DrawTexture(new Rect(iconX, iconY, isz, isz), icon.texture, ScaleMode.ScaleToFit, true);
    }

    // 달 이름 (카드 안 하단)
    GUIStyle nameStyle = new GUIStyle
    {
      fontSize = 26,
      fontStyle = FontStyle.Bold,
      alignment = TextAnchor.MiddleCenter,
      wordWrap = true,
      normal = { textColor = Color.white }
    };
    GUI.Label(new Rect(card.x + 8, card.y + cardH * 0.72f, cardW - 16, cardH * 0.24f), moonName, nameStyle);

  }
}
