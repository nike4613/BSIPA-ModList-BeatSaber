using Markdig.Syntax;
using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    public sealed class UnityRendererBuilder :
        UnityRendererBuilder.IQuoteRendererBuilder, 
        UnityRendererBuilder.ICodeRendererBuilder,
        UnityRendererBuilder.IUIRendererBuidler
    {
        private Material uiMat = null;
        private Color uiColor = Color.white;

        private Color? quoteColor = null;
        private Sprite quoteBg = null;
        private Image.Type quoteBgType;

        private Color? codeColor = null;
        private Sprite codeBg = null;
        private Image.Type? codeBgType = null;
        private TMP_FontAsset codeFont = null;
        private string codeInlinePadding = "";

        private event Action<MarkdownObject, GameObject> ObjRenderCallback;

        public interface IQuoteRendererBuilder
        {
            UnityRendererBuilder UseBackground(Sprite bg, Image.Type type);
            UnityRendererBuilder UseColor(Color col);
            UnityRendererBuilder Builder { get; }
        }

        public interface ICodeRendererBuilder
        {
            UnityRendererBuilder UseBackground(Sprite bg, Image.Type type);
            UnityRendererBuilder UseColor(Color col);
            UnityRendererBuilder UseFont(TMP_FontAsset font);
            UnityRendererBuilder UseInlineCodePadding(string padding);
            UnityRendererBuilder Builder { get; }
        }

        public interface IUIRendererBuidler
        {
            UnityRendererBuilder Material(Material mat);
            UnityRendererBuilder Color(Color col);
        }

        public IUIRendererBuidler UI 
        { 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this;
        }
        public IQuoteRendererBuilder Quote
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this;
        }
        public ICodeRendererBuilder Code
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this;
        }

        public UnityRendererBuilder UseObjectRendererCallback(Action<MarkdownObject, GameObject> callback)
        {
            ObjRenderCallback += callback;
            return this;
        }

        public UnityRenderer Build()
        {
            if (uiMat == null) throw new ArgumentNullException(nameof(UnityRenderer.UIMaterial));
            if (quoteColor == null) throw new ArgumentNullException(nameof(UnityRenderer.QuoteColor));
            if (quoteBg == null) throw new ArgumentNullException(nameof(UnityRenderer.QuoteBackground));

            var render = new UnityRenderer(uiMat,
                quoteBg, quoteBgType, quoteColor.Value,
                codeBg ?? quoteBg, codeBgType ?? quoteBgType, codeColor ?? quoteColor.Value)
            {
                UIColor = uiColor,
                CodeFont = codeFont,
                InlineCodePadding = codeInlinePadding,
            };
            render.AfterObjectRendered += ObjRenderCallback;
            return render;
        }

        UnityRendererBuilder IUIRendererBuidler.Color(Color col) => Do(uiColor = col);

        UnityRendererBuilder IUIRendererBuidler.Material(Material mat) => Do(uiMat = mat);

        UnityRendererBuilder IQuoteRendererBuilder.UseColor(Color col) => Do(quoteColor = col);

        UnityRendererBuilder IQuoteRendererBuilder.UseBackground(Sprite bg, Image.Type type)
        {
            quoteBg = bg;
            quoteBgType = type;
            return this;
        }

        UnityRendererBuilder ICodeRendererBuilder.UseColor(Color col) => Do(codeColor = col);

        UnityRendererBuilder ICodeRendererBuilder.UseBackground(Sprite bg, Image.Type type)
        {
            codeBg = bg;
            codeBgType = type;
            return this;
        }

        UnityRendererBuilder ICodeRendererBuilder.UseFont(TMP_FontAsset font)
            => Do(codeFont = font);

        UnityRendererBuilder ICodeRendererBuilder.UseInlineCodePadding(string padding)
            => Do(codeInlinePadding = padding);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnityRendererBuilder Do<T>(T _) => this;

        public UnityRendererBuilder Builder => this;
    }
}
