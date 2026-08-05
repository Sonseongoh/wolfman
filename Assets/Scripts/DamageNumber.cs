using UnityEngine;

/// <summary>
/// 떠오르는 데미지 숫자 (#31). 코드로 생성 — 프리팹 불필요.
/// 위로 떠오르며 흐려지다 사라진다.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    public float lifetime = 0.7f;
    public float riseSpeed = 1.6f;

    TextMesh text;
    float t;
    Color baseColor;
    float driftX;

    public static void Spawn(Vector3 position, string message, Color color, float size = 1f)
    {
        // 설정에서 끌 수 있음 (GameManager Inspector → Show Damage Numbers)
        if (GameManager.Instance != null && !GameManager.Instance.showDamageNumbers) return;

        GameObject go = new GameObject("DamageNumber");
        go.transform.position = position + new Vector3(Random.Range(-0.15f, 0.15f), 0.35f, 0f);

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = message;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 48;
        tm.characterSize = 0.045f * size;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        // 텍스트가 스프라이트 뒤에 묻히지 않게
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sortingOrder = 50;

        DamageNumber dn = go.AddComponent<DamageNumber>();
        dn.text = tm;
        dn.baseColor = color;
        dn.driftX = Random.Range(-0.4f, 0.4f);
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = t / lifetime;

        if (p >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += new Vector3(driftX * Time.deltaTime, riseSpeed * Time.deltaTime * (1f - p * 0.5f), 0f);

        Color c = baseColor;
        c.a = 1f - p * p; // 끝으로 갈수록 빠르게 사라짐
        text.color = c;
    }
}
