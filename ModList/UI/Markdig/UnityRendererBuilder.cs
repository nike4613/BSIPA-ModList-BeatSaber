using Markdig.Syntax;
using System;
using System.Runtime.CompilerServices;
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

        private event Action<MarkdownObject, GameObject> ObjRenderCallback;

        public interface IQuoteRendererBuilder
        {
            IQuoteRendererBuilder WithBackground(Sprite bg, Image.Type type);
            UnityRendererBuilder OfColor(Color col);
            UnityRendererBuilder Builder { get; }
        }

        public interface ICodeRendererBuilder
        {
            ICodeRendererBuilder WithBackground(Sprite bg, Image.Type type);
            UnityRendererBuilder OfColor(Color col);
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

        public UnityRendererBuilder WithObjectRenderCallback(Action<MarkdownObject, GameObject> callback)
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
            };
            render.AfterObjectRendered += ObjRenderCallback;
            return render;
        }

        UnityRendererBuilder IUIRendererBuidler.Color(Color col) => Do(uiColor = col);

        UnityRendererBuilder IUIRendererBuidler.Material(Material mat) => Do(uiMat = mat);

        UnityRendererBuilder IQuoteRendererBuilder.OfColor(Color col) => Do(quoteColor = col);

        IQuoteRendererBuilder IQuoteRendererBuilder.WithBackground(Sprite bg, Image.Type type)
        {
            quoteBg = bg;
            quoteBgType = type;
            return this;
        }

        UnityRendererBuilder ICodeRendererBuilder.OfColor(Color col) => Do(codeColor = col);

        ICodeRendererBuilder ICodeRendererBuilder.WithBackground(Sprite bg, Image.Type type)
        {
            codeBg = bg;
            codeBgType = type;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnityRendererBuilder Do<T>(T _) => this;

        public UnityRendererBuilder Builder => this;
    }
}
