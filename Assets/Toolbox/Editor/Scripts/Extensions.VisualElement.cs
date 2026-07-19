using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
// ReSharper disable PartialTypeWithSinglePart

namespace Tid.Toolbox.Editor
{
    public static partial class Extensions
    {
        public static TElement? FindElementInParent<TElement>(this VisualElement element)
            where TElement : VisualElement
        {
            while (true)
            {
                element = element.parent;
                switch (element)
                {
                    case null:
                        return null;
                    case TElement typedElement:
                        return typedElement;
                }
            }
        }

        public static IEnumerable<TElement> FindElementsInParent<TElement>(this VisualElement element)
            where TElement : VisualElement
        {
            while (element != null)
            {
                element = element.parent;
                if (element is TElement typedElement)
                {
                    yield return typedElement;
                }
            }
        }

        public static TElement? FindElementInChildren<TElement>(this VisualElement element)
            where TElement : VisualElement
        {
            return element.FindElementsInChildren<TElement>().FirstOrDefault();
        }

        public static IEnumerable<TElement> FindElementsInChildren<TElement>(this VisualElement element)
            where TElement : VisualElement
        {
            return element.Query<TElement>().ToList();
        }
    }
}