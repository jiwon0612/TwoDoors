using TMPro;
using UnityEngine;

namespace TwoDoors.UI
{
    /// <summary>
    /// 기본 TMP 폰트(LiberationSans)에는 한글이 없으므로, 실행 시 윈도우 시스템 폰트를 TMP 대체 폰트로 등록한다.
    /// 폰트 파일을 프로젝트에 포함하지 않아 배포 라이선스 문제가 없다. 에디터 Scene 뷰에서는 한글이 □로 보일 수 있다.
    /// </summary>
    static class KoreanFontFallback
    {
        const string FamilyName = "Malgun Gothic";

        static TMP_FontAsset font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            if (font != null) return;

            font = TMP_FontAsset.CreateFontAsset(FamilyName, "Regular");
            if (font == null)
            {
                Debug.LogWarning($"KoreanFontFallback: 시스템 폰트 '{FamilyName}'을(를) 찾지 못해 한글이 표시되지 않을 수 있습니다.");
                return;
            }

            TMP_Settings.fallbackFontAssets.Add(font);
            // 에디터에서는 TMP Settings 에셋 변경이 Play 종료 후에도 남으므로 되돌린다.
            Application.quitting += () =>
            {
                TMP_Settings.fallbackFontAssets.Remove(font);
                font = null;
            };
        }
    }
}
