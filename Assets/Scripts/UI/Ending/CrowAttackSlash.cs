using UnityEngine;
using UnityEngine.UI;

// 까마귀가 화면을 한 번 가로지르는 줄기 하나.
// 이 오브젝트(줄기 영역)를 원하는 각도·위치로 돌려 두면, 까마귀가 왼쪽 끝에서 오른쪽 끝으로 날아가고
// 지나간 자리에 검은 자국(Image_Trail)이 채워진다. 영역은 화면보다 넓게 잡아야 까마귀가 화면 밖에서 들어와 밖으로 나간다.
public class CrowAttackSlash : MonoBehaviour
{
    [SerializeField] private Image Image_Trail;   // 지나간 자리의 검은 자국. 이 오브젝트의 자식
    [SerializeField] private Image Image_Crow;    // 날아가는 까마귀. 이 오브젝트의 자식

    private RectTransform _trailTransform;
    private RectTransform _crowTransform;

    public void Init()
    {
        if (Image_Trail == null || Image_Crow == null)
        {
            Debug.LogError($"[CrowAttackSlash] {name}: Image_Trail과 Image_Crow를 연결하세요.", this);
            return;
        }

        _trailTransform = Image_Trail.rectTransform;
        _crowTransform = Image_Crow.rectTransform;

        // 자국은 줄기 영역의 왼쪽부터 오른쪽으로 늘어나도록, 까마귀는 진행 위치에 놓이도록 기준점을 맞춘다.
        _trailTransform.anchorMin = Vector2.zero;
        _trailTransform.anchorMax = new Vector2(0f, 1f);
        _trailTransform.offsetMin = Vector2.zero;
        _trailTransform.offsetMax = Vector2.zero;

        _crowTransform.anchorMin = new Vector2(0f, 0.5f);
        _crowTransform.anchorMax = new Vector2(0f, 0.5f);
        _crowTransform.anchoredPosition = Vector2.zero;

        Image_Trail.gameObject.SetActive(true);
        Image_Crow.gameObject.SetActive(false);
    }

    // progress 0 = 시작 전, 1 = 다 지나감. 까마귀는 이동 중에만 보인다.
    public void SetProgress(float progress)
    {
        if (_trailTransform == null || _crowTransform == null)
        {
            return;
        }

        progress = Mathf.Clamp01(progress);

        _trailTransform.anchorMax = new Vector2(progress, 1f);

        Vector2 crowAnchor = new Vector2(progress, 0.5f);
        _crowTransform.anchorMin = crowAnchor;
        _crowTransform.anchorMax = crowAnchor;

        bool isFlying = progress > 0f && progress < 1f;
        Image_Crow.gameObject.SetActive(isFlying);
    }

    public void SetCrowSprite(Sprite sprite)
    {
        if (Image_Crow == null || sprite == null)
        {
            return;
        }

        Image_Crow.sprite = sprite;
    }
}
