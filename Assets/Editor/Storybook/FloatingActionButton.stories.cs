using System;
using Unity.AppUI.Editor;
using Unity.AppUI.UI;

namespace MyGame.Editor.Storybook
{
    public class FloatingActionButtonStoryBookPage : StoryBookPage
    {
        public override string displayName => "Floating Action Button";

        public override Type componentType => typeof(FloatingActionButtonStoryBookComponent);

        public FloatingActionButtonStoryBookPage()
        {
            m_Stories.Add(new StoryBookStory("Default", () =>
            {
                var fab = new FloatingActionButton();
                fab.Add(new Icon { iconName = "info" });
                return fab;
            }));

            m_Stories.Add(new StoryBookStory("Accent", () =>
            {
                var fab = new FloatingActionButton { accent = true };
                fab.Add(new Icon { iconName = "info" });
                return fab;
            }));
        }
    }

    public class FloatingActionButtonStoryBookComponent : StoryBookComponent
    {
        public override Type uiElementType => typeof(FloatingActionButton);

        public FloatingActionButtonStoryBookComponent()
        {
            m_Properties.Add(new StoryBookBooleanProperty(
                nameof(FloatingActionButton.accent),
                (el) => ((FloatingActionButton)el).accent,
                (el, val) => ((FloatingActionButton)el).accent = val));

            m_Properties.Add(new StoryBookEnumProperty<Size>(
                nameof(FloatingActionButton.size),
                (el) => ((FloatingActionButton)el).size,
                (el, val) => ((FloatingActionButton)el).size = val));
        }
    }
}
