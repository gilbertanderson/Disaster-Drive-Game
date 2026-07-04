using System;
using Unity.AppUI.Editor;
using Unity.AppUI.UI;

namespace MyGame.Editor.Storybook
{
    public class IconButtonStoryBookPage : StoryBookPage
    {
        public override string displayName => "Icon Button";

        public override Type componentType => typeof(IconButtonStoryBookComponent);

        public IconButtonStoryBookPage()
        {
            m_Stories.Add(new StoryBookStory("Default", () => new IconButton
            {
                icon = "info"
            }));

            m_Stories.Add(new StoryBookStory("Primary", () => new IconButton
            {
                icon = "info",
                primary = true
            }));

            m_Stories.Add(new StoryBookStory("Quiet", () => new IconButton
            {
                icon = "info",
                quiet = true
            }));
        }
    }

    public class IconButtonStoryBookComponent : StoryBookComponent
    {
        public override Type uiElementType => typeof(IconButton);

        public IconButtonStoryBookComponent()
        {
            m_Properties.Add(new StoryBookStringProperty(
                nameof(IconButton.icon),
                (el) => ((IconButton)el).icon,
                (el, val) => ((IconButton)el).icon = val));

            m_Properties.Add(new StoryBookBooleanProperty(
                nameof(IconButton.primary),
                (el) => ((IconButton)el).primary,
                (el, val) => ((IconButton)el).primary = val));

            m_Properties.Add(new StoryBookBooleanProperty(
                nameof(IconButton.quiet),
                (el) => ((IconButton)el).quiet,
                (el, val) => ((IconButton)el).quiet = val));

            m_Properties.Add(new StoryBookEnumProperty<IconVariant>(
                nameof(IconButton.variant),
                (el) => ((IconButton)el).variant,
                (el, val) => ((IconButton)el).variant = val));

            m_Properties.Add(new StoryBookEnumProperty<Size>(
                nameof(IconButton.size),
                (el) => ((IconButton)el).size,
                (el, val) => ((IconButton)el).size = val));
        }
    }
}
