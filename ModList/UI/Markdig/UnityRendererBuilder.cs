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
        UnityRendererBuilder.IUIRendererBuidler
    {
        private Material _uiMat = null;
        private TMP_FontAsset _uiFont = null;
        private Color _uiColor = Color.white;
        private Color _linkColor = Color.cyan;
        private Color? _autolinkColor = null;

        private Color? _quoteColor = null;
        private Sprite _quoteBg = null;
        private Image.Type _quoteBgType;

        private Color? _codeColor = null;
        private Sprite _codeBg = null;
        private Image.Type? _codeBgType = null;
        private Color? _codeInlineColor = null;
        private Sprite _codeInlineBg = null;
        private Image.Type? _codeInlineBgType = null;
        private TMP_FontAsset _codeFont = null;
        private string _codeInlinePadding = "";

        private event Action<MarkdownObject, GameObject> ObjRenderCallback;
        private event UnityRenderer.LinkRendered LinkRenderCallback;

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

        public interface IUIRendererBuidler
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
            if (_uiMat == null) throw new ArgumentNullException(nameof(UnityRenderer.UIMaterial));
            if (_uiFont == null) throw new ArgumentNullException(nameof(UnityRenderer.UIFont));
            if (_quoteColor == null) throw new ArgumentNullException(nameof(UnityRenderer.QuoteColor));
            if (_quoteBg == null) throw new ArgumentNullException(nameof(UnityRenderer.QuoteBackground));

            var codeBg = _codeBg ?? _quoteBg;
            var codeBgType = _codeBgType ?? _quoteBgType;
            var codeColor = _codeColor ?? _quoteColor.Value;
            var inlineCodeBg = _codeInlineBg ?? codeBg;
            var inlineCodeBgType = _codeInlineBgType ?? codeBgType;
            var inlineCodeColor = _codeInlineColor ?? codeColor;

            var render = new UnityRenderer(_uiMat, _uiFont,
                _linkColor, _autolinkColor ?? _linkColor,
                _quoteBg, _quoteBgType, _quoteColor.Value,
                codeBg, codeBgType, codeColor,
                inlineCodeBg, inlineCodeBgType, inlineCodeColor) {UIColor = _uiColor, CodeFont = _codeFont, InlineCodePaddingText = _codeInlinePadding,};
            render.AfterObjectRendered += ObjRenderCallback;
            render.OnLinkRendered += LinkRenderCallback;
            return render;
        }

        UnityRendererBuilder ILinkRendererBuilder.UseColor(Color col) => Do(_linkColor = col);
        UnityRendererBuilder ILinkRendererBuilder.UseAutoColor(Color col) => Do(_autolinkColor = col);

        UnityRendererBuilder IUIRendererBuidler.Color(Color col) => Do(_uiColor = col);

        UnityRendererBuilder IUIRendererBuidler.Material(Material mat) => Do(_uiMat = mat);

        UnityRendererBuilder IUIRendererBuidler.Font(TMP_FontAsset font) => Do(_uiFont = font);

        UnityRendererBuilder IQuoteRendererBuilder.UseColor(Color col) => Do(_quoteColor = col);

        UnityRendererBuilder IQuoteRendererBuilder.UseBackground(Sprite bg, Image.Type type)
        {
            _quoteBg = bg;
            _quoteBgType = type;
            return this;
        }

        UnityRendererBuilder ICodeRendererBuilder.UseColor(Color col) => Do(_codeColor = col);

        UnityRendererBuilder ICodeRendererBuilder.UseBackground(Sprite bg, Image.Type type)
        {
            _codeBg = bg;
            _codeBgType = type;
            return this;
        }

        UnityRendererBuilder ICodeRendererBuilder.UseFont(TMP_FontAsset font) => Do(_codeFont = font);

        UnityRendererBuilder IInlineCodeRendererBuilder.UseBackground(Sprite bg, Image.Type type)
        {
            _codeInlineBg = bg;
            _codeInlineBgType = type;
            return this;
        }

        UnityRendererBuilder IInlineCodeRendererBuilder.UseColor(Color col) => Do(_codeInlineColor = col);
        UnityRendererBuilder IInlineCodeRendererBuilder.UsePadding(string padding) => Do(_codeInlinePadding = padding);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnityRendererBuilder Do<T>(T _) => this;

        public UnityRendererBuilder Builder => this;
    }
}