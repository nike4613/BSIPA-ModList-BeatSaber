using Markdig.Syntax;
using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    public sealed class UnityRendererBuilder :
        UnityRendererBuilder.ILinkRendererBuilder,
        UnityRendererBuilder.IQuoteRendererBuilder,
        UnityRendererBuilder.ICodeRendererBuilder,
        UnityRendererBuilder.IInlineCodeRendererBuilder,
        UnityRendererBuilder.IUIRendererBuilder
    {
        private Material? uiMat = null;
        private TMP_FontAsset? uiFont = null;
        private Color uiColor = Color.white;
        private Color linkColor = Color.cyan;
        private Color? autolinkColor = null;

        private Color? quoteColor = null;
        private Sprite? quoteBg = null;
        private Image.Type quoteBgType;

        private Color? codeColor = null;
        private Sprite? codeBg = null;
        private Image.Type? codeBgType = null;
        private Color? codeInlineColor = null;
        private Sprite? codeInlineBg = null;
        private Image.Type? codeInlineBgType = null;
        private TMP_FontAsset? codeFont = null;
        private string codeInlinePadding = "";

        private event Action<MarkdownObject, GameObject>? ObjRenderCallback;
        private event UnityRenderer.LinkRendered? LinkRenderCallback;

        public interface ILinkRendererBuilder
        {
            UnityRendererBuilder UseColor(Color col);
            UnityRendererBuilder UseAutoColor(Color col);
        }

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
            IInlineCodeRendererBuilder Inline { get; }
            UnityRendererBuilder Builder { get; }
        }

        public interface IInlineCodeRendererBuilder
        {
            UnityRendererBuilder UseBackground(Sprite bg, Image.Type type);
            UnityRendererBuilder UseColor(Color col);
            UnityRendererBuilder UsePadding(string padding);
            UnityRendererBuilder Builder { get; }
        }

        public interface IUIRendererBuilder
        {
            UnityRendererBuilder Material(Material mat);
            UnityRendererBuilder Color(Color col);
            UnityRendererBuilder Font(TMP_FontAsset font);
        }

        public ILinkRendererBuilder Link
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this;
        }

        public IUIRendererBuilder UI
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

        IInlineCodeRendererBuilder ICodeRendererBuilder.Inline => this;

        public UnityRendererBuilder UseObjectRenderedCallback(Action<MarkdownObject, GameObject> callback)
        {
            ObjRenderCallback += callback;
            return this;
        }

        public UnityRendererBuilder UseLinkRenderedCallback(UnityRenderer.LinkRendered callback)
        {
            LinkRenderCallback += callback;
            return this;
        }

        public UnityRenderer Build()
        {
            if (uiMat == null) throw new ArgumentNullException(nameof(UnityRenderer.UIMaterial));
            if (uiFont == null) throw new ArgumentNullException(nameof(UnityRenderer.UIFont));
            if (quoteColor == null) throw new ArgumentNullException(nameof(UnityRenderer.QuoteColor));
            if (quoteBg == null) throw new ArgumentNullException(nameof(UnityRenderer.QuoteBackground));

            var codeBg = this.codeBg ?? quoteBg;
            var codeBgType = this.codeBgType ?? quoteBgType;
            var codeColor = this.codeColor ?? quoteColor.Value;
            var inlineCodeBg = codeInlineBg ?? codeBg;
            var inlineCodeBgType = codeInlineBgType ?? codeBgType;
            var inlineCodeColor = codeInlineColor ?? codeColor;

            var render = new UnityRenderer(uiMat, uiFont,
                linkColor, autolinkColor ?? linkColor,
                quoteBg, quoteBgType, quoteColor.Value,
                codeBg, codeBgType, codeColor,
                inlineCodeBg, inlineCodeBgType, inlineCodeColor) { UIColor = uiColor, CodeFont = codeFont, InlineCodePaddingText = codeInlinePadding };
            render.AfterObjectRendered += ObjRenderCallback;
            render.OnLinkRendered += LinkRenderCallback;
            return render;
        }

        UnityRendererBuilder ILinkRendererBuilder.UseColor(Color col) => Do(linkColor = col);
        UnityRendererBuilder ILinkRendererBuilder.UseAutoColor(Color col) => Do(autolinkColor = col);

        UnityRendererBuilder IUIRendererBuilder.Color(Color col) => Do(uiColor = col);

        UnityRendererBuilder IUIRendererBuilder.Material(Material mat) => Do(uiMat = mat);

        UnityRendererBuilder IUIRendererBuilder.Font(TMP_FontAsset font) => Do(uiFont = font);

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

        UnityRendererBuilder ICodeRendererBuilder.UseFont(TMP_FontAsset font) => Do(codeFont = font);

        UnityRendererBuilder IInlineCodeRendererBuilder.UseBackground(Sprite bg, Image.Type type)
        {
            codeInlineBg = bg;
            codeInlineBgType = type;
            return this;
        }

        UnityRendererBuilder IInlineCodeRendererBuilder.UseColor(Color col) => Do(codeInlineColor = col);
        UnityRendererBuilder IInlineCodeRendererBuilder.UsePadding(string padding) => Do(codeInlinePadding = padding);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnityRendererBuilder Do<T>(T _) => this;

        public UnityRendererBuilder Builder => this;
    }
}