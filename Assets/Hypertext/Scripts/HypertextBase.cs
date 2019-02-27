using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hypertext
{
    public abstract class HypertextBase : Text, IPointerClickHandler
    {
        Canvas rootCanvas;
        Canvas RootCanvas { get { return rootCanvas ?? (rootCanvas = GetComponentInParent<Canvas>()); } }

        const int CharVerts = 6;
        readonly List<Span> spans = new List<Span>();
        static readonly ObjectPool<List<UIVertex>> verticesPool = new ObjectPool<List<UIVertex>>(null, l => l.Clear());

        struct Span
        {
            public int StartIndex;
            public int Length;
            public Color Color;
            public Action<string> Callback;
            public List<Rect> BoundingBoxes;

            public Span(int startIndex, int endIndex, Color color, Action<string> callback)
            {
                StartIndex = startIndex;
                Length = endIndex;
                Color = color;
                Callback = callback;
                BoundingBoxes = new List<Rect>();
            }
        };

        /// <summary>
        /// 指定した部分文字列にクリックイベントを登録します
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
        /// テキストの変更などでイベントの再登録が必要なときにも呼び出されます
        /// <see cref="HypertextBase.OnClick"/> を使ってクリックイベントを登録してください
        /// </summary>
        protected abstract void AddListeners();

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            base.OnPopulateMesh(vertexHelper);

            spans.Clear();
            AddListeners();

            var vertices = verticesPool.Get();
            vertexHelper.GetUIVertexStream(vertices);

            Modify(ref vertices);

            vertexHelper.Clear();
            vertexHelper.AddUIVertexTriangleStream(vertices);
            verticesPool.Release(vertices);
        }

        void Modify(ref List<UIVertex> vertices)
        {
            var verticesCount = vertices.Count;

            for (int i = 0; i < spans.Count; i++)
            {
                var span = spans[i];
                var endIndex = span.StartIndex + span.Length;

                for (int textIndex = span.StartIndex; textIndex < endIndex; textIndex++)
                {
                    var vertexStartIndex = textIndex * CharVerts;
                    if (vertexStartIndex + CharVerts > verticesCount)
                    {
                        break;
                    }

                    var min = Vector2.one * float.MaxValue;
                    var max = Vector2.one * float.MinValue;

                    for (int vertexIndex = 0; vertexIndex < CharVerts; vertexIndex++)
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

                    span.BoundingBoxes.Add(new Rect { min = min, max = max });
                }

                // 文字ごとのバウンディングボックスを行ごとのバウンディングボックスにまとめる
                span.BoundingBoxes = CalculateLineBoundingBoxes(span.BoundingBoxes);
                spans[i] = span;
            }
        }

        List<Rect> CalculateLineBoundingBoxes(List<Rect> charBoundingBoxes)
        {
            var lineBoundingBoxes = new List<Rect>();
            var lineStartIndex = 0;

            for (int i = 1; i < charBoundingBoxes.Count; i++)
            {
                if (charBoundingBoxes[i].xMin < charBoundingBoxes[i - 1].xMin)
                {
                    lineBoundingBoxes.Add(CalculateAABB(charBoundingBoxes.GetRange(lineStartIndex, i - lineStartIndex)));
                    lineStartIndex = i;
                }
            }

            if (lineStartIndex < charBoundingBoxes.Count)
            {
                lineBoundingBoxes.Add(CalculateAABB(charBoundingBoxes.GetRange(lineStartIndex, charBoundingBoxes.Count - lineStartIndex)));
            }

            return lineBoundingBoxes;
        }

        Rect CalculateAABB(List<Rect> rects)
        {
            var min = Vector2.one * float.MaxValue;
            var max = Vector2.one * float.MinValue;

            for (int i = 0; i < rects.Count; i++)
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

            return new Rect { min = min, max = max };
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

            var localPosition = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, position, pressEventCamera, out localPosition);
            return localPosition;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            var localPosition = CalculateLocalPosition(eventData.position, eventData.pressEventCamera);

            for (int s = 0; s < spans.Count; s++)
            {
                for (int b = 0; b < spans[s].BoundingBoxes.Count; b++)
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
