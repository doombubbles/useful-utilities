#if DEBUG
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.UI;
using BTD_Mod_Helper.Extensions;
using Il2CppFacepunch.Steamworks;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamNative;
using Il2CppTMPro;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppFacepunch.Steamworks.HTMLKeyModifiers;
using Color = UnityEngine.Color;
using HTMLMouseButton = Il2CppFacepunch.Steamworks.HTMLMouseButton;
using Image = UnityEngine.UI.Image;
using Main = Il2CppAssets.Scripts.Main;

namespace UsefulUtilities.Utilities.InGameCharts;

public class Browser : ModWindow
{
    public static string UserAgent => $"btd6-{Application.platform}-{Application.version}";

    public override bool BlockHotkeysWhileFocused => true;

    private const float ResolutionScaleFactor = .5f;

    public override string Icon => VanillaSprites.GwendolinFirefoxPetIcon;

    public override bool AllowMultiple => false;

    public override bool ShowTitleOnWindow => false;

    public override bool HideOverlappingTopBarItems => false;

    public override bool RightClickOnContent => false;

    [JsonObject(MemberSerialization.OptIn)]
    [RegisterTypeInIl2Cpp(false)]
    public class BrowserData(IntPtr ptr) : MonoBehaviour(ptr)
    {
        [JsonProperty]
        public string url = "https://bloonswiki.com";

        public ModHelperPanel browser = null!;
        public HtmlSurface html = null!;
        public RawImage image = null!;
        public ModHelperInputField searchBox = null!;
        public ModHelperText titleText = null!;
    }

    public override void ModifyWindow(ModHelperWindow window)
    {
        window.main.AddComponent<Mask>();
        var browser = window.content.AddPanel(new Info("Browser", InfoPreset.FillParent)
        {
            Scale = new Vector3(1, -1, 1)
        });
        var rawImage = browser.AddComponent<RawImage>();
        rawImage.enabled = false;


        var data = window.AddComponent<BrowserData>();
        data.browser = browser;
        data.image = rawImage;

        var html = data.html = new HtmlSurface(Main.SteamworksClient);


        html.OnNeedsPaint += new Action<uint, uint, Il2CppStructArray<byte>>((w, h, textureBytes) =>
        {
            rawImage.enabled = true;
            if (rawImage.texture == null)
            {
                rawImage.texture = new Texture2D((int) w, (int) h, TextureFormat.BGRA32, false, true);
            }
            else
            {
                rawImage.texture.Cast<Texture2D>().Resize((int) w, (int) h);
            }

            rawImage.texture.Cast<Texture2D>().LoadRawTextureData(textureBytes);
            rawImage.texture.Cast<Texture2D>().Apply();
        });
        html.OnStartRequest += new Action<string>(url =>
        {
            if (!url.StartsWith("result://"))
            {
                data.url = url;
                html.AllowStartRequest(true);
                return;
            }
            html.AllowStartRequest(false);

            try
            {
                url = WebUtility.UrlDecode(url.Replace("result://", ""));
                var split = url.IndexOf("=", StringComparison.Ordinal);
                OnResultJS(window, url[..split], url[(split + 1)..]);
            }
            catch (Exception e)
            {
                ModHelper.Warning<UsefulUtilitiesMod>(e);
            }
        });

        html.Init();
        html.CreateBrowser(UserAgent, null, new Action(() =>
        {
            var size = browser.RectTransform.rect.size * ResolutionScaleFactor;
            html.LoadURL(data.url, (uint) size.x, (uint) size.y);
        }), new Action(() =>
        {
            ModHelper.Msg<UsefulUtilitiesMod>("Browser fail");
        }));


        window.topLeftGroup.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.MinSize;

        var back = window.topLeftGroup.AddButton(new Info("Back", window.topBarHeight),
            VanillaSprites.SmallSquareWhiteGradient, new Action(() => html.ExecuteJavascript("history.back()")));
        back.Button.UseBackgroundTint();
        back.AddImage(new Info("Icon", InfoPreset.FillParent)
        {
            Scale = new Vector3(-1, 1, 1)
        }, VanillaSprites.UpgradeArrow);

        var forward = window.topLeftGroup.AddButton(new Info("Forward", window.topBarHeight),
            VanillaSprites.SmallSquareWhiteGradient, new Action(() => html.ExecuteJavascript("history.forward()")));
        forward.Button.UseBackgroundTint();
        forward.AddImage(new Info("Icon", InfoPreset.FillParent), VanillaSprites.UpgradeArrow);

        var reload = window.topLeftGroup.AddButton(new Info("Reload", window.topBarHeight),
            VanillaSprites.SmallSquareWhiteGradient, new Action(html.Reload));
        reload.Button.UseBackgroundTint();
        reload.AddImage(new Info("Icon", InfoPreset.FillParent), VanillaSprites.RestartIcon);

        var box = data.searchBox = window.topLeftGroup.AddInputField(new Info("Box", 500, window.topBarHeight), "",
                      VanillaSprites.SmallSquareWhiteGradient);
        box.Text.Text.alignment = TextAlignmentOptions.CaplineLeft;
        box.Text.Text.margin = box.Text.Text.margin with
        {
            x = 10
        };
        box.Text.Text.AutoLocalize = false;
        box.GetComponent<Image>().pixelsPerUnitMultiplier = 2;
        box.GetComponent<Selectable>().colors = box.GetComponent<Selectable>().colors with
        {
            normalColor = new Color(0, 0, 0, 0.25f),
            selectedColor = new Color(0, 0, 0, 0.25f),
            pressedColor = new Color(0, 0, 0, 0.25f),
            highlightedColor = new Color(0, 0, 0, 0.2f)
        };
        ResizeSearchBox(window);

        box.InputField.onSubmit.AddListener(new Action<string>(url =>
        {
            url = url.Trim();

            if (url == data.url) return;

            var looksLikeUrl = false;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                looksLikeUrl = true;
            }
            else if (Regex.IsMatch(url, @"^[^\s]+\.[^\s]{2,}$") || Path.HasExtension(url) && !url.Contains(' '))
            {
                url = "https://" + url;
                looksLikeUrl = true;
            }

            if (!looksLikeUrl)
            {
                url = $"https://www.google.com/search?q={WebUtility.UrlEncode(url)}";
            }

            var size = browser.RectTransform.rect.size * ResolutionScaleFactor;
            data.titleText.SetText(url);
            html.LoadURL(url, (uint) size.x, (uint) size.y);
        }));

        data.titleText = box.Text.Duplicate(box.Viewport);
        data.titleText.Text.AutoLocalize = false;
        data.titleText.Text.color = Color.clear;

        html.OnFinishedRequest += new Action<string>(url =>
        {
            data.url = url;
            box.InputField.SetText(url);
            html.ExecuteJavascript( // language=js
                """
                window.location.href = `result://title=${document.title}`
                """);
        });
    }

    public void OnResultJS(ModHelperWindow window, string id, string value)
    {
        var data = window.GetComponent<BrowserData>();
        switch (id)
        {
            case "title":
                data.titleText.SetText(value);
                break;
        }
    }

    public override void OnUpdate(ModHelperWindow window)
    {
        var data = window.GetComponent<BrowserData>();

        var html = data.html;

        var showTitle = !data.searchBox.InputField.isFocused && !data.searchBox.InputField.m_isSelected &&
                        !string.IsNullOrEmpty(data.titleText.Text.text);
        data.titleText.Text.color = showTitle ? Color.white : Color.clear;
        data.searchBox.Text.Text.color = showTitle ? Color.clear : Color.white;

        if (window.locked || window.transform.GetSiblingIndex() < window.transform.parent.childCount - 1 ||
            Input.mousePosition.Raycast().FirstOrDefault()?.gameObject != data.browser.gameObject ||
            !Event.current.Is(out var e)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(data.browser, Input.mousePosition, null,
            out var mousePos);


        mousePos *= ResolutionScaleFactor;
        var size = data.browser.RectTransform.rect.size * ResolutionScaleFactor;
        mousePos += size / 2;

        if (mousePos.x >= 0 && mousePos.x <= size.x && mousePos.y >= 0 && mousePos.y <= size.y)
        {
            html.MouseMove((int) mousePos.x, (int) mousePos.y);
        }


        var modifiers = None;
        // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) modifiers |= AltDown;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) modifiers |= ShiftDown;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) modifiers |= CtrlDown;
        // ReSharper restore BitwiseOperatorOnEnumWithoutFlags

        switch (e.type)
        {
            case EventType.MouseDown:
                html.MouseDown((HTMLMouseButton) e.button);
                return;
            case EventType.MouseUp:
                html.MouseUp((HTMLMouseButton) e.button);
                return;
            case EventType.KeyDown:
                if (ConvertSpecialKeyToVirtualKey(e.keyCode, out var keyDown))
                {
                    html.KeyDown(keyDown, modifiers);
                }
                else if (!char.IsControl(e.character))
                {
                    html.KeyChar(e.character, modifiers);
                }
                else if (e.character == '\n')
                {
                    html.KeyDown((uint) KeyCode.Return, modifiers);
                }
                return;
            case EventType.KeyUp:
                if (ConvertSpecialKeyToVirtualKey(e.keyCode, out var keyUp))
                {
                    html.KeyUp(keyUp, modifiers);
                }
                return;
            case EventType.ScrollWheel:
                html.MouseWheel((int) (e.delta.y * -100));
                return;
        }
    }

    public override void OnResize(ModHelperWindow window, Vector2 oldSize, Vector2 newSize)
    {
        var data = window.GetComponent<BrowserData>();
        var size = data.browser.RectTransform.rect.size * ResolutionScaleFactor;
        data.html.client.native.htmlSurface.Cast<SteamHTMLSurface>()
            .SetSize(data.html.browserHandle, (uint) size.x, (uint) size.y);

        ResizeSearchBox(window);
    }

    public override void OnUnMinimized(ModHelperWindow window)
    {
        ResizeSearchBox(window);
    }

    public void ResizeSearchBox(ModHelperWindow window)
    {
        var data = window.GetComponent<BrowserData>();
        var newSize = window.RectTransform.sizeDelta;
        var width = newSize.x - window.topBarHeight * 7 - ModHelperWindow.Margin * 8;
        data.searchBox.LayoutElement.minWidth = data.searchBox.LayoutElement.preferredWidth = width;
    }

    public override void OnClose(ModHelperWindow window)
    {
        var data = window.GetComponent<BrowserData>();
        data.html.OnNeedsPaint = null;
        data.html.OnFinishedRequest = null;
        data.html.OnStartRequest = null;
        data.html.Shutdown();
    }

    public override bool SaveWindow(ModHelperWindow window, ref JObject saveData)
    {
        saveData = JObject.FromObject(window.GetComponent<BrowserData>());
        return true;
    }

    public override void LoadWindow(ModHelperWindow window, JObject saveData)
    {
        var data = window.GetComponent<BrowserData>();
        JsonConvert.PopulateObject(saveData.ToString(), data);

    }


    public static bool ConvertSpecialKeyToVirtualKey(KeyCode keyCode, out uint vKey)
    {
        switch (keyCode)
        {
            case KeyCode.Backspace:
                vKey = 8;
                return true; // VK_BACK
            case KeyCode.Tab:
                vKey = 9;
                return true; // VK_TAB
            case KeyCode.Return:
                vKey = 13;
                return true; // VK_RETURN
            case KeyCode.Escape:
                vKey = 27;
                return true; // VK_ESCAPE
        }

        if (keyCode <= KeyCode.Delete)
        {
            switch (keyCode)
            {
                case KeyCode.Space:
                    vKey = 32; // VK_SPACE
                    return false; // NOTE: matches the decompiled behavior
                case KeyCode.Delete:
                    vKey = 46; // VK_DELETE
                    return true;
                default:
                    vKey = 0;
                    return false;
            }

        }

        switch (keyCode)
        {
            case KeyCode.RightArrow:
                vKey = 39;
                return true; // VK_RIGHT
            case KeyCode.LeftArrow:
                vKey = 37;
                return true; // VK_LEFT
            case KeyCode.Insert:
                vKey = 0;
                return false; // (no mapping)
            case KeyCode.Home:
                vKey = 36;
                return true; // VK_HOME
            case KeyCode.End:
                vKey = 35;
                return true; // VK_END
            case KeyCode.PageUp:
                vKey = 33;
                return true; // VK_PRIOR (PageUp)
            case KeyCode.PageDown:
                vKey = 34;
                return true; // VK_NEXT  (PageDown)
            case KeyCode.CapsLock:
                vKey = 20;
                return true; // VK_CAPITAL (CapsLock)
            case KeyCode.RightShift:
                vKey = 161;
                return true; // VK_RSHIFT
            case KeyCode.LeftShift:
                vKey = 160;
                return true; // VK_LSHIFT
            case KeyCode.RightControl:
                vKey = 163;
                return true; // VK_RCONTROL
            case KeyCode.LeftControl:
                vKey = 162;
                return true; // VK_LCONTROL
            case KeyCode.RightAlt:
                vKey = 165;
                return true; // VK_RMENU (Right Alt)
            case KeyCode.LeftAlt:
                vKey = 164;
                return true; // VK_LMENU (Left Alt)

            default:
                vKey = 0;
                return false;
        }
    }

}
#endif