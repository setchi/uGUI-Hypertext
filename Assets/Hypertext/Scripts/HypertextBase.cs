/*
 * uGUI-Hypertext (https://github.com/setchi/uGUI-Hypertext)
 * Copyright (c) 2019 setchi
 * Licensed under MIT (https://github.com/setchi/uGUI-Hypertext/blob/master/LICENSE)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hypertext
{
    public abstract class HypertextBase : Text, IPointerClickHandler
    {
        class Span
        {
            public readonly int StartIndex;
            public readonly int Length;
            public readonly Color Color;
            public readonly Action<string> Callback;
            public List<Rect> BoundingBoxes;

            public Span(int startIndex, int length, Color color, Action<string> callback)
            {
                StartIndex = startIndex;
                Length = length;
                Color = color;
                Callback = callback;
                BoundingBoxes = new List<Rect>();
            }
        };

        readonly List<Span> spans = new List<Span>();

        // TODO: 頂点が生成されない空白文字をすべて洗い出す
        readonly char[] invisibleChars =
        {
            Space,
            Tab,
            LineFeed
        };
        static readonly ObjectPool<List<UIVertex>> verticesPool = new ObjectPool<List<UIVertex>>(null, l => l.Clear());

        const int CharVerts = 6;
        const char 
            Tab = '\t',
            LineFeed = '\n',
            Space = ' ',
            LesserThan = '<',
            GreaterThan = '>';

        int[] visibleCharIndexMap;

        Canvas rootCanvas;
        Canvas RootCanvas => rootCanvas ?? (rootCanvas = GetComponentInParent<Canvas>());

        /// <summary>
        /// 指定した部分文字列にクリックイベントリスナを登録します
        /// </summary>
        /// <param name="startIndex">部分文字列の開始文字位置</param>
        /// <param name="length">部分文字列の長さ</param>
        /// <param name="color">部分文字列につける色</param>
        /// <param name="onClick">部分文字列がクリックされたときのコールバック</param>
        protected void OnClick(int startIndex, int length, Color color, Action<string> onClick)
        {
            if (onClick == null)
            {
                throw new ArgumentNullException(nameof(onClick));
            }

            if (startIndex < 0 || startIndex > text.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (length < 1 || startIndex + length > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            spans.Add(new Span(startIndex, length, color, onClick));
        }

        /// <summary>
        /// イベントリスナを削除します
        /// </summary>
        public virtual void RemoveListeners()
        {
            spans.Clear();
        }

        /// <summary>
        /// イベントリスナを追加します
        /// テキストの変更などでイベントリスナの再登録が必要なときにも呼び出されます
        /// <see cref="HypertextBase.OnClick"/> を使ってクリックイベントリスナを登録してください
        /// </summary>
        protected abstract void AddListeners();

        readonly UIVertex[] tempVerts = new UIVertex[4];
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (font == null)
            {
                return;
            }

            m_DisableFontTextureRebuiltCallback = true;

            var extents = rectTransform.rect.size;

            var settings = GetGenerationSettings(extents);
            settings.generateOutOfBounds = true;
            cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);

            var verts = cachedTextGenerator.verts;
            var unitsPerPixel = 1 / pixelsPerUnit;
            int vertCount = verts.Count;

            if (vertCount <= 0)
            {
                toFill.Clear();
                return;
            }

            var roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
            roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
            toFill.Clear();

            if (roundingOffset != Vector2.zero)
            {
                for (int i = 0; i < vertCount; ++i)
                {
                    int tempVertsIndex = i & 3;
                    tempVerts[tempVertsIndex] = verts[i];
                    tempVerts[tempVertsIndex].position *= unitsPerPixel;
                    tempVerts[tempVertsIndex].position.x += roundingOffset.x;
                    tempVerts[tempVertsIndex].position.y += roundingOffset.y;

                    if (tempVertsIndex == 3)
                    {
                        toFill.AddUIVertexQuad(tempVerts);
                    }
                }
            }
            else
            {
                for (int i = 0; i < vertCount; ++i)
                {
                    int tempVertsIndex = i & 3;
                    tempVerts[tempVertsIndex] = verts[i];
                    tempVerts[tempVertsIndex].position *= unitsPerPixel;

                    if (tempVertsIndex == 3)
                    {
                        toFill.AddUIVertexQuad(tempVerts);
                    }
                }
            }

            var vertices = verticesPool.Get();
            toFill.GetUIVertexStream(vertices);

            GenerateVisibleCharIndexMap(vertices.Count < text.Length * CharVerts);

            spans.Clear();
            AddListeners();
            GenerateHrefBoundingBoxes(ref vertices);

            toFill.Clear();
            toFill.AddUIVertexTriangleStream(vertices);
            verticesPool.Release(vertices);

            m_DisableFontTextureRebuiltCallback = false;
        }

        void GenerateHrefBoundingBoxes(ref List<UIVertex> vertices)
        {
            var verticesCount = vertices.Count;

            for (var i = 0; i < spans.Count; i++)
            {
                var span = spans[i];

                var startIndex = visibleCharIndexMap[span.StartIndex];
                var endIndex = visibleCharIndexMap[span.StartIndex + span.Length - 1];

                for (var textIndex = startIndex; textIndex <= endIndex; textIndex++)
                {
                    var vertexStartIndex = textIndex * CharVerts;
                    if (vertexStartIndex + CharVerts > verticesCount)
                    {
                        break;
                    }

                    var min = Vector2.one * float.MaxValue;
                    var max = Vector2.one * float.MinValue;

                    for (var vertexIndex = 0; vertexIndex < CharVerts; vertexIndex++)
                    {
                        var vertex = vertices[vertexStartIndex + vertexIndex];
                        vertex.color = span.Color;
                        vertices[vertexStartIndex + vertexIndex] = vertex;

                        var pos = vertices[vertexStartIndex + vertexIndex].position;

                        if (pos.y < min.y)
                        {
                            min.y = pos.y;
                        }

                        if (pos.x < min.x)
                        {
                            min.x = pos.x;
                        }

                        if (pos.y > max.y)
                        {
                            max.y = pos.y;
                        }

                        if (pos.x > max.x)
                        {
                            max.x = pos.x;
                        }
                    }

                    span.BoundingBoxes.Add(new Rect {min = min, max = max});
                }

                // 文字ごとのバウンディングボックスを行ごとのバウンディングボックスにまとめる
                span.BoundingBoxes = CalculateLineBoundingBoxes(span.BoundingBoxes);
            }
        }

        static List<Rect> CalculateLineBoundingBoxes(List<Rect> charBoundingBoxes)
        {
            var lineBoundingBoxes = new List<Rect>();
            var lineStartIndex = 0;

            for (var i = 1; i < charBoundingBoxes.Count; i++)
            {
                if (charBoundingBoxes[i].xMin >= charBoundingBoxes[i - 1].xMin)
                {
                    continue;
                }

                lineBoundingBoxes.Add(CalculateAABB(charBoundingBoxes.GetRange(lineStartIndex, i - lineStartIndex)));
                lineStartIndex = i;
            }

            if (lineStartIndex < charBoundingBoxes.Count)
            {
                lineBoundingBoxes.Add(CalculateAABB(charBoundingBoxes.GetRange(lineStartIndex, charBoundingBoxes.Count - lineStartIndex)));
            }

            return lineBoundingBoxes;
        }

        static Rect CalculateAABB(IReadOnlyList<Rect> rects)
        {
            var min = Vector2.one * float.MaxValue;
            var max = Vector2.one * float.MinValue;

            for (var i = 0; i < rects.Count; i++)
            {
                if (rects[i].xMin < min.x)
                {
                    min.x = rects[i].xMin;
                }

                if (rects[i].yMin < min.y)
                {
                    min.y = rects[i].yMin;
                }

                if (rects[i].xMax > max.x)
                {
                    max.x = rects[i].xMax;
                }

                if (rects[i].yMax > max.y)
                {
                    max.y = rects[i].yMax;
                }
            }

            return new Rect {min = min, max = max};
        }

        void GenerateVisibleCharIndexMap(bool verticesReduced)
        {
            if (visibleCharIndexMap == null || visibleCharIndexMap.Length < text.Length)
            {
                Array.Resize(ref visibleCharIndexMap, text.Length);
            }

            if (!verticesReduced)
            {
                for (var i = 0; i < visibleCharIndexMap.Length; i++)
                {
                    visibleCharIndexMap[i] = i;
                }
                return;
            }

            var offset = 0;
            var inTag = false;

            for (var i = 0; i < text.Length; i++)
            {
                var character = text[i];

                if (inTag)
                {
                    offset--;

                    if (character == GreaterThan)
                    {
                        inTag = false;
                    }
                }
                else if (supportRichText && character == LesserThan)
                {
                    offset--;
                    inTag = true;
                }
                else if (invisibleChars.Contains(character))
                {
                    offset--;
                }

                visibleCharIndexMap[i] = Mathf.Max(0, i + offset);
            }
        }

        Vector3 CalculateLocalPosition(Vector3 position, Camera pressEventCamera)
        {
            if (!RootCanvas)
            {
                return Vector3.zero;
            }

            if (RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return transform.InverseTransformPoint(position);
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                position,
                pressEventCamera,
                out var localPosition
            );

            return localPosition;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            var localPosition = CalculateLocalPosition(eventData.position, eventData.pressEventCamera);

            for (var s = 0; s < spans.Count; s++)
            {
                for (var b = 0; b < spans[s].BoundingBoxes.Count; b++)
                {
                    if (spans[s].BoundingBoxes[b].Contains(localPosition))
                    {
                        spans[s].Callback(text.Substring(spans[s].StartIndex, spans[s].Length));
                        break;
                    }
                }
            }
        }
    }
}
